using Grimoire.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Grimoire.Tools.FlashBridge
{
    public sealed class RuffleFlashBridge : IFlashBridge
    {
        private const int CallTimeoutMs = 5000;
        private const int PipeConnectTimeoutMs = 15000;

        private Panel _embedHost;
        private Control _hostContainer;
        private Process _process;
        private IntPtr _jobHandle = IntPtr.Zero;
        private IntPtr _childHwnd = IntPtr.Zero;

        private NamedPipeServerStream _c2rPipe;
        private NamedPipeServerStream _r2cPipe;
        private StreamWriter _writer;
        private readonly object _writeLock = new object();

        private long _nextCallId;
        private readonly ConcurrentDictionary<long, TaskCompletionSource<JToken>> _pendingCalls =
            new ConcurrentDictionary<long, TaskCompletionSource<JToken>>();
        private readonly BlockingCollection<JObject> _extCallQueue = new BlockingCollection<JObject>();

        private CancellationTokenSource _cts;
        private volatile bool _connected;
        private volatile bool _ready;
        private volatile bool _disposed;
        private string _pendingQuality;

        public string EngineName => "ruffle";
        public bool IsReady => !_disposed && _connected && _ready && _process != null && !_process.HasExited;

        public event Func<string, object[], string> ExternalCallReceived;

        #region Win32 Interop (Job Object + HWND Focus/Resize)

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, int infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int JobObjectExtendedLimitInformation = 9;
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        private void AttachToKillOnCloseJob(Process proc)
        {
            try
            {
                _jobHandle = CreateJobObject(IntPtr.Zero, null);
                if (_jobHandle == IntPtr.Zero)
                    return;

                var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

                int len = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                IntPtr ptr = Marshal.AllocHGlobal(len);
                try
                {
                    Marshal.StructureToPtr(info, ptr, false);
                    if (SetInformationJobObject(_jobHandle, JobObjectExtendedLimitInformation, ptr, (uint)len))
                    {
                        AssignProcessToJobObject(_jobHandle, proc.Handle);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch { }
        }

        #endregion

        public void LoadSwf(string swfPath, Control hostContainer)
        {
            if (hostContainer == null)
                throw new ArgumentNullException(nameof(hostContainer));
            if (string.IsNullOrEmpty(swfPath) || !File.Exists(swfPath))
                throw new FileNotFoundException("SWF file not found.", swfPath);

            string exePath = ResolveRuffleEngineExecutable();
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                throw new FileNotFoundException(
                    "Ruffle engine not found. Place 'runtime\\ruffle-engine.exe' next to GrimCodex Ruffle.exe or set 'rufflePath' in ClientConfig.cfg.");
            }

            _hostContainer = hostContainer;
            _cts = new CancellationTokenSource();

            _embedHost = new Panel
            {
                Name = "ruffleHost",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 24),
                TabStop = true
            };
            _embedHost.SizeChanged += EmbedHost_SizeChanged;
            _embedHost.GotFocus += (s, e) =>
            {
                if (_childHwnd != IntPtr.Zero)
                    SetFocus(_childHwnd);
            };
            _embedHost.MouseDown += (s, e) =>
            {
                if (_childHwnd != IntPtr.Zero)
                    SetFocus(_childHwnd);
            };

            hostContainer.Controls.Clear();
            hostContainer.Controls.Add(_embedHost);

            // Force handle creation so we have a valid parent HWND
            IntPtr parentHwnd = _embedHost.Handle;
            int width = Math.Max(1, _embedHost.ClientSize.Width);
            int height = Math.Max(1, _embedHost.ClientSize.Height);

            string pipeBase = $"grim-ruffle-{Process.GetCurrentProcess().Id}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            _c2rPipe = new NamedPipeServerStream(
                pipeBase + "-c2r",
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                65536,
                65536);

            _r2cPipe = new NamedPipeServerStream(
                pipeBase + "-r2c",
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                65536,
                65536);

            string fullSwfPath = Path.GetFullPath(swfPath);
            string spoofUrl = ClientConfig.GetValue(ClientConfig.C_SPOOF_URL);
            if (string.IsNullOrWhiteSpace(spoofUrl))
                spoofUrl = "https://game.aq.com/game/";

            string workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
            string baseUri = new Uri(workingDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar).AbsoluteUri;

            string args =
                $"--swf \"{fullSwfPath}\" " +
                $"--base \"{baseUri}\" " +
                $"--spoof-url \"{spoofUrl}\" " +
                $"--parent-hwnd {parentHwnd.ToInt64()} " +
                $"--ipc-pipe \"\\\\.\\pipe\\{pipeBase}\" " +
                $"--width {width} " +
                $"--height {height}";

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogDebug($"[Ruffle] {e.Data}");
            };
            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogDebug($"[Ruffle:err] {e.Data}");
            };
            _process.Exited += (s, e) =>
            {
                _connected = false;
                _ready = false;
                CancelAllPendingCalls();
                LogDebug($"[Ruffle] Engine process exited (code={GetExitCodeSafe()}).");
            };

            LogDebug($"[Ruffle] Starting: {exePath}");
            _process.Start();
            AttachToKillOnCloseJob(_process);
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Task.Run(() => ConnectAndPumpAsync(_cts.Token));
            Task.Run(() => ExternalCallWorkerLoop(_cts.Token));
        }

        private string GetExitCodeSafe()
        {
            try { return _process?.ExitCode.ToString() ?? "?"; }
            catch { return "?"; }
        }

        private async Task ConnectAndPumpAsync(CancellationToken ct)
        {
            try
            {
                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    connectCts.CancelAfter(PipeConnectTimeoutMs);
                    await Task.WhenAll(
                        _c2rPipe.WaitForConnectionAsync(connectCts.Token),
                        _r2cPipe.WaitForConnectionAsync(connectCts.Token)
                    ).ConfigureAwait(false);
                }

                lock (_writeLock)
                {
                    _writer = new StreamWriter(_c2rPipe, new UTF8Encoding(false))
                    {
                        AutoFlush = true,
                        NewLine = "\n"
                    };
                }

                _connected = true;
                LogDebug("[Ruffle] IPC Named Pipes connected.");

                using (var reader = new StreamReader(_r2cPipe, Encoding.UTF8))
                {
                    while (!ct.IsCancellationRequested && _r2cPipe.IsConnected)
                    {
                        string line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null)
                            break;
                        if (line.Length == 0)
                            continue;

                        HandleIncomingMessage(line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!_disposed)
                    LogDebug($"[Ruffle] IPC pipe error: {ex.Message}");
            }
            finally
            {
                _connected = false;
                _ready = false;
                CancelAllPendingCalls();
            }
        }

        private void HandleIncomingMessage(string line)
        {
            JObject msg;
            try
            {
                msg = JObject.Parse(line);
            }
            catch (Exception ex)
            {
                LogDebug($"[Ruffle] Invalid IPC JSON: {ex.Message}");
                return;
            }

            string type = msg["type"]?.Value<string>();
            switch (type)
            {
                case "ready":
                    long hwndVal = msg["hwnd"]?.Value<long>() ?? 0;
                    if (hwndVal != 0)
                        _childHwnd = new IntPtr(hwndVal);
                    _ready = true;
                    LogDebug($"[Ruffle] Engine ready (hwnd=0x{hwndVal:X}).");
                    SyncCurrentSizeAndQuality();
                    break;

                case "result":
                    long id = msg["id"]?.Value<long>() ?? 0;
                    if (_pendingCalls.TryRemove(id, out var tcs))
                    {
                        bool ok = msg["ok"]?.Value<bool>() ?? true;
                        if (ok)
                            tcs.TrySetResult(msg["value"]);
                        else
                            tcs.TrySetResult(JValue.CreateNull());
                    }
                    break;

                case "ext_call":
                    if (!_extCallQueue.IsAddingCompleted)
                    {
                        try { _extCallQueue.Add(msg); }
                        catch { }
                    }
                    break;
            }
        }

        private void ExternalCallWorkerLoop(CancellationToken ct)
        {
            try
            {
                foreach (JObject msg in _extCallQueue.GetConsumingEnumerable(ct))
                {
                    long id = msg["id"]?.Value<long>() ?? 0;
                    string name = msg["name"]?.Value<string>();
                    bool needsReturn = msg["needs_return"]?.Value<bool>() ?? false;
                    JArray rawArgs = msg["args"] as JArray;

                    if (string.IsNullOrEmpty(name))
                    {
                        if (needsReturn)
                            SendJson(new JObject { ["id"] = id, ["type"] = "ext_result", ["value"] = JValue.CreateNull() });
                        continue;
                    }

                    object[] args = BuildExternalCallArgs(rawArgs);

                    if (needsReturn)
                    {
                        string result = InvokeExternalCallHandler(name, args, marshalToUi: false);
                        SendJson(new JObject
                        {
                            ["id"] = id,
                            ["type"] = "ext_result",
                            ["value"] = result != null ? (JToken)new JValue(result) : JValue.CreateNull()
                        });
                    }
                    else
                    {
                        InvokeExternalCallHandler(name, args, marshalToUi: true);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private string InvokeExternalCallHandler(string name, object[] args, bool marshalToUi)
        {
            var handler = ExternalCallReceived;
            if (handler == null)
                return null;

            if (marshalToUi && Root.Instance != null && Root.Instance.IsHandleCreated && !Root.Instance.IsDisposed)
            {
                try
                {
                    Root.Instance.BeginInvoke(new Action(() =>
                    {
                        try { handler(name, args); }
                        catch (Exception ex) { LogDebug($"[Ruffle] ExternalCall '{name}' error: {ex.Message}"); }
                    }));
                    return null;
                }
                catch { }
            }

            try
            {
                return handler(name, args);
            }
            catch (Exception ex)
            {
                LogDebug($"[Ruffle] ExternalCall '{name}' error: {ex.Message}");
                return null;
            }
        }

        private void SyncCurrentSizeAndQuality()
        {
            try
            {
                if (_embedHost != null && !_embedHost.IsDisposed)
                {
                    Action sync = () =>
                    {
                        if (_embedHost != null && !_embedHost.IsDisposed)
                            Resize(_embedHost.ClientSize.Width, _embedHost.ClientSize.Height);
                    };
                    if (_embedHost.InvokeRequired && _embedHost.IsHandleCreated)
                        _embedHost.BeginInvoke(sync);
                    else
                        sync();
                }

                if (!string.IsNullOrEmpty(_pendingQuality))
                    SetQuality(_pendingQuality);
            }
            catch { }
        }

        private void EmbedHost_SizeChanged(object sender, EventArgs e)
        {
            if (_embedHost == null || _embedHost.IsDisposed)
                return;
            Resize(_embedHost.ClientSize.Width, _embedHost.ClientSize.Height);
        }

        public string CallFunctionXml(string xmlRequest)
        {
            if (string.IsNullOrEmpty(xmlRequest))
                return string.Empty;

            XElement root = XElement.Parse(xmlRequest);
            string name = root.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            XElement argsEl = root.Element("arguments");
            object[] args = argsEl == null
                ? new object[0]
                : argsEl.Elements().Select(ParseFlashXmlArg).ToArray();

            return CallFunction(name, args);
        }

        public string CallFunction(string functionName, params object[] args)
        {
            if (!IsReady || string.IsNullOrEmpty(functionName))
                return string.Empty;

            long id = Interlocked.Increment(ref _nextCallId);
            var tcs = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingCalls[id] = tcs;

            try
            {
                var argArray = new JArray();
                if (args != null)
                {
                    foreach (object arg in args)
                        argArray.Add(arg == null ? JValue.CreateNull() : JToken.FromObject(arg));
                }

                var req = new JObject
                {
                    ["id"] = id,
                    ["type"] = "call",
                    ["name"] = functionName,
                    ["args"] = argArray
                };

                if (!SendJson(req))
                {
                    _pendingCalls.TryRemove(id, out _);
                    return string.Empty;
                }

                if (tcs.Task.Wait(CallTimeoutMs))
                {
                    return JsonTokenToFlashXml(tcs.Task.Result);
                }

                _pendingCalls.TryRemove(id, out _);
                return string.Empty;
            }
            catch
            {
                _pendingCalls.TryRemove(id, out _);
                return string.Empty;
            }
        }

        public void CallFunctionFireAndForget(string functionName, params object[] args)
        {
            if (!IsReady || string.IsNullOrEmpty(functionName))
                return;

            try
            {
                var argArray = new JArray();
                if (args != null)
                {
                    foreach (object arg in args)
                        argArray.Add(arg == null ? JValue.CreateNull() : JToken.FromObject(arg));
                }

                SendJson(new JObject
                {
                    ["type"] = "call",
                    ["no_reply"] = true,
                    ["name"] = functionName,
                    ["args"] = argArray
                });
            }
            catch { }
        }

        public void SetQuality(string quality)
        {
            if (string.IsNullOrEmpty(quality))
                return;

            _pendingQuality = quality;
            if (!_connected)
                return;

            SendJson(new JObject
            {
                ["type"] = "set_quality",
                ["quality"] = quality.ToLowerInvariant()
            });
        }

        public void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            // Let DPI-aware ruffle-engine settle its render/present threads and resize the child HWND
            // using the parent's physical client rect (avoids HiDPI mismatch and swapchain races).
            if (_connected)
            {
                SendJson(new JObject
                {
                    ["type"] = "resize",
                    ["width"] = width,
                    ["height"] = height
                });
            }
            else if (_childHwnd != IntPtr.Zero)
            {
                MoveWindow(_childHwnd, 0, 0, width, height, true);
            }
        }

        private bool SendJson(JObject obj)
        {
            if (_disposed || !_connected)
                return false;

            try
            {
                string line = obj.ToString(Formatting.None);
                lock (_writeLock)
                {
                    if (_writer == null || _c2rPipe == null || !_c2rPipe.IsConnected)
                        return false;
                    _writer.WriteLine(line);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void CancelAllPendingCalls()
        {
            foreach (var kvp in _pendingCalls)
            {
                if (_pendingCalls.TryRemove(kvp.Key, out var tcs))
                    tcs.TrySetResult(JValue.CreateNull());
            }
        }

        private static object[] BuildExternalCallArgs(JArray rawArgs)
        {
            if (rawArgs == null || rawArgs.Count == 0)
                return new object[0];

            var sb = new StringBuilder();
            sb.Append("<arguments>");
            foreach (JToken tok in rawArgs)
                sb.Append(JsonTokenToFlashXml(tok));
            sb.Append("</arguments>");

            XElement argsEl = XElement.Parse(sb.ToString());
            return new object[] { argsEl.Value };
        }

        private static object ParseFlashXmlArg(XElement el)
        {
            switch (el.Name.LocalName)
            {
                case "null":
                case "undefined":
                    return null;
                case "true":
                    return true;
                case "false":
                    return false;
                case "number":
                    if (long.TryParse(el.Value, out long l)) return l;
                    if (double.TryParse(el.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d)) return d;
                    return 0;
                case "array":
                    return el.Elements("property")
                        .OrderBy(p => int.TryParse(p.Attribute("id")?.Value, out int idx) ? idx : int.MaxValue)
                        .Select(p => ParseFlashXmlArg(p.Elements().FirstOrDefault() ?? new XElement("null")))
                        .ToArray();
                case "object":
                    var dict = new Dictionary<string, object>();
                    foreach (XElement prop in el.Elements("property"))
                    {
                        string key = prop.Attribute("id")?.Value ?? string.Empty;
                        dict[key] = ParseFlashXmlArg(prop.Elements().FirstOrDefault() ?? new XElement("null"));
                    }
                    return dict;
                default:
                    return el.Value;
            }
        }

        private static string JsonTokenToFlashXml(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return "<null/>";

            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return token.Value<bool>() ? "<true/>" : "<false/>";

                case JTokenType.Integer:
                    return $"<number>{token.Value<long>()}</number>";

                case JTokenType.Float:
                    return $"<number>{token.Value<double>().ToString(System.Globalization.CultureInfo.InvariantCulture)}</number>";

                case JTokenType.String:
                    return new XElement("string", token.Value<string>()).ToString(SaveOptions.DisableFormatting);

                case JTokenType.Array:
                    var arr = (JArray)token;
                    var sbArr = new StringBuilder("<array>");
                    for (int i = 0; i < arr.Count; i++)
                    {
                        sbArr.Append($"<property id=\"{i}\">{JsonTokenToFlashXml(arr[i])}</property>");
                    }
                    sbArr.Append("</array>");
                    return sbArr.ToString();

                case JTokenType.Object:
                    var obj = (JObject)token;
                    var sbObj = new StringBuilder("<object>");
                    foreach (var prop in obj.Properties())
                    {
                        string escapedKey = System.Security.SecurityElement.Escape(prop.Name) ?? string.Empty;
                        sbObj.Append($"<property id=\"{escapedKey}\">{JsonTokenToFlashXml(prop.Value)}</property>");
                    }
                    sbObj.Append("</object>");
                    return sbObj.ToString();

                default:
                    return new XElement("string", token.ToString()).ToString(SaveOptions.DisableFormatting);
            }
        }

        private static string ResolveRuffleEngineExecutable()
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;

            string configured = ClientConfig.GetValue(ClientConfig.C_RUFFLE_PATH);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string resolved = Path.IsPathRooted(configured) ? configured : Path.GetFullPath(Path.Combine(exeDir, configured));
                if (File.Exists(resolved))
                    return resolved;
            }

            string[] candidates =
            {
                Path.Combine(exeDir, "runtime", "grimcodex-engine.exe"),
                Path.Combine(exeDir, "grimcodex-engine.exe"),
                Path.Combine(exeDir, "runtime", "ruffle-engine.exe"),
                Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "engine", "target", "release", "ruffle_desktop.exe")),
                Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "engine", "target", "dist", "ruffle_desktop.exe"))
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static void LogDebug(string message)
        {
            try
            {
                Console.WriteLine(message);
                LogForm.Instance.AppendDebug(message + "\r\n");
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try { SendJson(new JObject { ["type"] = "shutdown" }); } catch { }
            try { _cts?.Cancel(); } catch { }
            try { _extCallQueue.CompleteAdding(); } catch { }
            CancelAllPendingCalls();

            lock (_writeLock)
            {
                try { _writer?.Dispose(); } catch { }
                _writer = null;
            }

            try { _c2rPipe?.Dispose(); } catch { }
            try { _r2cPipe?.Dispose(); } catch { }

            try
            {
                if (_process != null)
                {
                    if (!_process.HasExited)
                    {
                        if (!_process.WaitForExit(1000))
                            _process.Kill();
                    }
                    _process.Dispose();
                    _process = null;
                }
            }
            catch { }

            if (_jobHandle != IntPtr.Zero)
            {
                CloseHandle(_jobHandle);
                _jobHandle = IntPtr.Zero;
            }

            try
            {
                if (_embedHost != null)
                {
                    _embedHost.SizeChanged -= EmbedHost_SizeChanged;
                    if (_hostContainer != null && !_hostContainer.IsDisposed && _hostContainer.Controls.Contains(_embedHost))
                        _hostContainer.Controls.Remove(_embedHost);
                    _embedHost.Dispose();
                    _embedHost = null;
                }
            }
            catch { }

            try { _cts?.Dispose(); } catch { }
        }
    }
}

