using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarkUI.Forms;
using Grimoire.Tools;

namespace Grimoire.UI
{
    public partial class AppUpdateDialog : GrimDarkForm
    {
        private readonly CodexManifest _manifest;
        private CancellationTokenSource _cts;
        private bool _isUpdating = false;

        public AppUpdateDialog(CodexManifest manifest)
        {
            InitializeComponent();
            _manifest = manifest;
            this.TopMost = true;
        }

        private async void AppUpdateDialog_Load(object sender, EventArgs e)
        {
            if (_manifest == null || string.IsNullOrEmpty(_manifest.AppDownloadUrl))
            {
                MessageBox.Show("No download URL found for this release.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            lblTitle.Text = $"Downloading GrimCodex Ruffle v{_manifest.AppVersion}...";
            _cts = new CancellationTokenSource();
            _isUpdating = true;

            await StartDownloadAsync(_cts.Token);
        }

        private async Task DownloadFileWithProgressAsync(HttpClient client, string url, string destinationPath, string label, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;

                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalRead += bytesRead;

                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            int progressPct = (int)((double)totalRead / totalBytes.Value * 100);
                            progressBar.Value = Math.Max(0, Math.Min(100, progressPct));

                            double readMb = totalRead / (1024.0 * 1024.0);
                            double totalMb = totalBytes.Value / (1024.0 * 1024.0);
                            lblStatus.Text = $"{label}: {readMb:F1} MB / {totalMb:F1} MB ({progressPct}%)...";
                        }
                        else
                        {
                            double readMb = totalRead / (1024.0 * 1024.0);
                            lblStatus.Text = $"{label}: {readMb:F1} MB...";
                        }
                    }
                }
            }
        }

        private async Task StartDownloadAsync(CancellationToken cancellationToken)
        {
            string targetExePath = Path.Combine(Application.StartupPath, Path.GetFileName(Application.ExecutablePath) + ".new");
            string runtimeDir = Path.Combine(Application.StartupPath, "runtime");
            string engineZipTemp = Path.Combine(runtimeDir, "grimcodex-engine.zip.tmp");
            string targetEnginePath = Path.Combine(runtimeDir, "grimcodex-engine.exe.new");
            bool updatedEngine = false;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    client.DefaultRequestHeaders.Add("User-Agent", "GrimCodex-Ruffle-Client");

                    // 1. Download main GrimCodex Ruffle executable
                    await DownloadFileWithProgressAsync(client, _manifest.AppDownloadUrl, targetExePath, "Downloading app", cancellationToken);

                    // 2. Download & extract updated Ruffle engine only when missing or SHA-256 changed
                    if (CodexBankManager.Instance.NeedsEngineUpdate(_manifest))
                    {
                        Directory.CreateDirectory(runtimeDir);
                        progressBar.Value = 0;
                        await DownloadFileWithProgressAsync(client, _manifest.EngineDownloadUrl, engineZipTemp, "Downloading engine", cancellationToken);

                        lblStatus.Text = "Extracting Ruffle engine...";
                        await Task.Run(() =>
                        {
                            using (FileStream zipStream = new FileStream(engineZipTemp, FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                            {
                                ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e =>
                                    e.Name.Equals("grimcodex-engine.exe", StringComparison.OrdinalIgnoreCase));
                                if (entry == null)
                                    throw new InvalidDataException("grimcodex-engine.exe not found inside engine archive.");

                                using (Stream entryStream = entry.Open())
                                using (FileStream outStream = new FileStream(targetEnginePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    entryStream.CopyTo(outStream);
                                }
                            }
                        }, cancellationToken);

                        TryDeleteFile(engineZipTemp);

                        if (!string.IsNullOrWhiteSpace(_manifest.EngineSha256) &&
                            !CodexBankManager.Instance.VerifyHash(targetEnginePath, _manifest.EngineSha256.Trim()))
                        {
                            throw new InvalidDataException("Engine SHA-256 verification failed.");
                        }

                        updatedEngine = true;
                    }
                }

                // Download completed successfully
                progressBar.Value = 100;
                lblStatus.Text = "Download completed! Restarting application...";
                btnCancel.Enabled = false;

                await Task.Delay(1500);

                // Apply update and restart
                CodexBankManager.ApplyUpdateAndRestart(targetExePath, updatedEngine ? targetEnginePath : null);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Update cancelled.";
                TryDeleteFile(targetExePath);
                TryDeleteFile(engineZipTemp);
                TryDeleteFile(targetEnginePath);
                Close();
            }
            catch (Exception ex)
            {
                TryDeleteFile(targetExePath);
                TryDeleteFile(engineZipTemp);
                TryDeleteFile(targetEnginePath);
                _isUpdating = false;
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_isUpdating && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            else
            {
                Close();
            }
        }

        private void AppUpdateDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isUpdating && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        private void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
    }
}
