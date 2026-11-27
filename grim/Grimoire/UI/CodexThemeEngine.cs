using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DarkUI.Controls;

namespace Grimoire.UI
{
    /// <summary>
    /// Lightweight Codex styling engine for WinForms &amp; DarkUI controls.
    /// Zero background timers, cached fonts, native DWM chrome sync, and full custom color support.
    /// </summary>
    public static class CodexThemeEngine
    {
        // Preferred Codex UI font fallback chain
        private static readonly string[] PreferredUiFonts = new[]
        {
            "SF Pro Text",
            "SF Pro Display",
            "SF Pro",
            "Segoe UI Variable Text",
            "Inter",
            "Segoe UI"
        };

        private static readonly string[] PreferredDisplayFonts = new[]
        {
            "SF Pro Display",
            "SF Pro Text",
            "SF Pro",
            "Segoe UI Variable Display",
            "Inter",
            "Segoe UI"
        };

        private static readonly HashSet<string> MonospaceFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Consolas",
            "Courier New",
            "SF Mono",
            "Cascadia Mono",
            "Cascadia Code",
            "Lucida Console"
        };

        private static HashSet<string> _installedFontNames;
        private static string _resolvedUiFontName;
        private static string _resolvedDisplayFontName;
        private static string _customFontPreference = "Auto";

        private static readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _fontLock = new object();
        private static readonly ConditionalWeakTable<Control, CodexControlHook> _attachedHooks = new ConditionalWeakTable<Control, CodexControlHook>();
        private static readonly ConditionalWeakTable<Control, object> _trackedContainers = new ConditionalWeakTable<Control, object>();
        private static readonly ConditionalWeakTable<Form, object> _dpiScaledForms = new ConditionalWeakTable<Form, object>();

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        public static float GetDpiScale(Control ctrl)
        {
            try
            {
                uint dpi = 0;
                if (ctrl != null && !ctrl.IsDisposed && ctrl.IsHandleCreated)
                {
                    dpi = GetDpiForWindow(ctrl.Handle);
                }
                if (dpi < 96)
                {
                    dpi = GetDpiForSystem();
                }
                if (dpi >= 96 && dpi <= 384)
                {
                    return dpi / 96f;
                }
            }
            catch { }

            try
            {
                if (ctrl != null && !ctrl.IsDisposed)
                {
                    int devDpi = ctrl.DeviceDpi;
                    if (devDpi >= 96 && devDpi <= 384)
                        return devDpi / 96f;
                }
            }
            catch { }
            return 1.0f;
        }

        /// <summary>
        /// Scales AutoScaleMode.None forms (such as BotManager or Notepad) cleanly once to native monitor DPI
        /// without triggering WinForms double-anchor drift on TabPages.
        /// </summary>
        public static void EnsureFormDpiScaled(Form form)
        {
            if (form == null || form.IsDisposed) return;
            if (form.AutoScaleMode != AutoScaleMode.None) return;
            if (_dpiScaledForms.TryGetValue(form, out _)) return;

            _dpiScaledForms.Add(form, new object());
            float dpi = GetDpiScale(form);
            if (dpi <= 1.02f) return;

            var suspended = new List<Control>();
            var savedAnchors = new List<KeyValuePair<Control, AnchorStyles>>();
            var savedTabSizes = new List<KeyValuePair<TabPage, Size>>();

            try
            {
                CollectForSafeScale(form, suspended, savedAnchors, savedTabSizes);

                // Temporarily park anchors on Top|Left so Scale() never double-offsets Right/Bottom controls
                foreach (var pair in savedAnchors)
                {
                    pair.Key.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                }

                form.Scale(new SizeF(dpi, dpi));
                ScaleTabControlItemSizes(form, dpi);

                // Ensure every TabPage (including hidden/unselected tabs) matches the exact scaled size
                foreach (var pair in savedTabSizes)
                {
                    TabPage tp = pair.Key;
                    Size orig = pair.Value;
                    tp.Size = new Size(
                        (int)Math.Round(orig.Width * dpi),
                        (int)Math.Round(orig.Height * dpi));
                }
            }
            catch { }
            finally
            {
                // Restore original anchors BEFORE resuming layout so anchors compute margins against scaled parents
                foreach (var pair in savedAnchors)
                {
                    try { pair.Key.Anchor = pair.Value; } catch { }
                }

                for (int i = suspended.Count - 1; i >= 0; i--)
                {
                    try { suspended[i].ResumeLayout(false); } catch { }
                }
            }
        }

        private static void CollectForSafeScale(
            Control ctrl,
            List<Control> suspended,
            List<KeyValuePair<Control, AnchorStyles>> savedAnchors,
            List<KeyValuePair<TabPage, Size>> savedTabSizes)
        {
            if (ctrl == null || ctrl.IsDisposed) return;

            ctrl.SuspendLayout();
            suspended.Add(ctrl);

            if (ctrl is TabPage tp)
            {
                savedTabSizes.Add(new KeyValuePair<TabPage, Size>(tp, tp.Size));
            }

            if (!(ctrl is Form) && !(ctrl is TabPage) && ctrl.Dock == DockStyle.None &&
                ctrl.Anchor != (AnchorStyles.Top | AnchorStyles.Left))
            {
                savedAnchors.Add(new KeyValuePair<Control, AnchorStyles>(ctrl, ctrl.Anchor));
            }

            foreach (Control child in ctrl.Controls)
            {
                CollectForSafeScale(child, suspended, savedAnchors, savedTabSizes);
            }
        }

        private static void ScaleTabControlItemSizes(Control root, float dpi)
        {
            if (root == null) return;
            if (root is TabControl tc && tc.ItemSize.Width > 0 && tc.ItemSize.Height > 0)
            {
                tc.ItemSize = new Size(
                    (int)Math.Round(tc.ItemSize.Width * dpi),
                    (int)Math.Round(Math.Max(22, tc.ItemSize.Height) * dpi));
            }
            foreach (Control child in root.Controls)
            {
                ScaleTabControlItemSizes(child, dpi);
            }
        }

        // Native Win32 / DWM interop
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWCP_ROUND = 2;

        private const int EM_SETMARGINS = 0x00D3;
        private const int EC_LEFTMARGIN = 0x0001;
        private const int EC_RIGHTMARGIN = 0x0002;
        private const int TBM_GETTHUMBRECT = 0x0419;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        public static string UiFontFamily
        {
            get
            {
                EnsureFontsResolved();
                return _resolvedUiFontName;
            }
        }

        public static string DisplayFontFamily
        {
            get
            {
                EnsureFontsResolved();
                return _resolvedDisplayFontName;
            }
        }

        public static string FontPreference
        {
            get => _customFontPreference;
            set
            {
                string next = string.IsNullOrWhiteSpace(value) ? "Auto" : value.Trim();
                if (!string.Equals(_customFontPreference, next, StringComparison.OrdinalIgnoreCase))
                {
                    _customFontPreference = next;
                    _resolvedUiFontName = null;
                    _resolvedDisplayFontName = null;
                }
            }
        }

        public static List<string> GetAvailableFontChoices()
        {
            EnsureInstalledFontSet();
            var list = new List<string> { "Auto (Codex)" };
            string[] candidates = new[]
            {
                "SF Pro Text",
                "SF Pro Display",
                "SF Pro Rounded",
                "Segoe UI Variable Text",
                "Inter",
                "Segoe UI"
            };
            foreach (string name in candidates)
            {
                if (_installedFontNames.Contains(name))
                {
                    list.Add(name);
                }
            }
            return list;
        }

        private static void EnsureInstalledFontSet()
        {
            if (_installedFontNames != null) return;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var installed = new InstalledFontCollection())
                {
                    foreach (FontFamily ff in installed.Families)
                    {
                        set.Add(ff.Name);
                    }
                }
            }
            catch { }
            _installedFontNames = set;
        }

        private static void EnsureFontsResolved()
        {
            if (_resolvedUiFontName != null && _resolvedDisplayFontName != null) return;
            EnsureInstalledFontSet();

            string ui = null;
            string display = null;

            if (!string.IsNullOrWhiteSpace(_customFontPreference) &&
                !_customFontPreference.StartsWith("Auto", StringComparison.OrdinalIgnoreCase))
            {
                if (_installedFontNames.Contains(_customFontPreference))
                {
                    ui = _customFontPreference;
                    display = _customFontPreference;
                }
            }

            if (ui == null)
            {
                foreach (string f in PreferredUiFonts)
                {
                    if (_installedFontNames.Contains(f))
                    {
                        ui = f;
                        break;
                    }
                }
            }

            if (display == null)
            {
                foreach (string f in PreferredDisplayFonts)
                {
                    if (_installedFontNames.Contains(f))
                    {
                        display = f;
                        break;
                    }
                }
            }

            _resolvedUiFontName = ui ?? "Segoe UI";
            _resolvedDisplayFontName = display ?? _resolvedUiFontName;
        }

        public static Font GetCachedFont(float sizePt, FontStyle style = FontStyle.Regular, bool display = false)
        {
            EnsureFontsResolved();
            string family = display ? _resolvedDisplayFontName : _resolvedUiFontName;
            return GetCachedFontByFamily(family, sizePt, style);
        }

        /// <summary>
        /// Returns a crisp, normal-weight font for GDI+ owner-drawn listboxes (like BotManager.lstCommands).
        /// SF Pro OTF maps FontStyle.Regular to an ultra-thin weight inside GDI+ DrawString,
        /// so we map it to Segoe UI (or user's explicit non-SF font) for clean, comfortable readability.
        /// </summary>
        public static Font GetReadableListFont(string explicitFamily, float sizePt)
        {
            if (!string.IsNullOrWhiteSpace(explicitFamily) &&
                !explicitFamily.StartsWith("SF Pro", StringComparison.OrdinalIgnoreCase))
            {
                return GetCachedFontByFamily(explicitFamily, sizePt, FontStyle.Regular);
            }

            EnsureInstalledFontSet();
            string family = "Segoe UI";
            if (!string.IsNullOrWhiteSpace(_customFontPreference) &&
                !_customFontPreference.StartsWith("Auto", StringComparison.OrdinalIgnoreCase) &&
                !_customFontPreference.StartsWith("SF Pro", StringComparison.OrdinalIgnoreCase) &&
                _installedFontNames.Contains(_customFontPreference))
            {
                family = _customFontPreference;
            }
            else if (_installedFontNames.Contains("Segoe UI"))
            {
                family = "Segoe UI";
            }

            return GetCachedFontByFamily(family, Math.Max(8.5f, sizePt), FontStyle.Regular);
        }

        public static Font GetCachedFontByFamily(string family, float sizePt, FontStyle style)
        {
            if (sizePt < 6f) sizePt = 8.25f;
            float roundedSize = (float)Math.Round(sizePt * 4f) / 4f;
            string key = family + "|" + roundedSize.ToString("0.##") + "|" + (int)style;

            lock (_fontLock)
            {
                Font cached;
                if (_fontCache.TryGetValue(key, out cached) && cached != null)
                {
                    return cached;
                }

                try
                {
                    Font f = new Font(family, roundedSize, style, GraphicsUnit.Point, 0);
                    _fontCache[key] = f;
                    return f;
                }
                catch
                {
                    Font fallback = new Font("Segoe UI", roundedSize, style, GraphicsUnit.Point, 0);
                    _fontCache[key] = fallback;
                    return fallback;
                }
            }
        }

        public static bool IsMonospaceFont(Font font)
        {
            if (font == null) return false;
            return MonospaceFamilies.Contains(font.FontFamily.Name);
        }

        public static void ApplyCodexFont(Control ctrl, bool isDenseForm)
        {
            if (ctrl == null || ctrl.IsDisposed) return;

            try
            {
                if (ctrl is Form form)
                {
                    // CRITICAL: prevent WinForms AutoScaleMode.Font from exploding form/control sizes when changing Font
                    if (form.AutoScaleMode == AutoScaleMode.Font)
                    {
                        form.AutoScaleMode = AutoScaleMode.None;
                    }
                }

                Font cur = ctrl.Font;
                if (cur == null) return;
                if (IsMonospaceFont(cur)) return;

                // Keep compact size on ultra-dense BotManager grids so no labels wrap or clip
                float targetSize = cur.Size;
                if (targetSize <= 8.3f)
                {
                    targetSize = isDenseForm ? 8.25f : 8.5f;
                }

                bool useDisplay = targetSize >= 11f;
                // Controls whose item/dropdown rendering uses GDI+ DrawString (DarkComboBox, ListBox, TreeView, ListView)
                // render SF Pro OTF at ultra-thin weight; force crisp normal-weight readable font for uniform appearance.
                bool useReadableGdiPlusFont =
                    ctrl is ComboBox ||
                    ctrl is ListBox ||
                    ctrl is TreeView ||
                    ctrl is DarkTreeView ||
                    ctrl is ListView ||
                    ctrl is RichTextBox;
                Font desired = useReadableGdiPlusFont
                    ? GetReadableListFont(null, targetSize)
                    : GetCachedFont(targetSize, cur.Style, useDisplay);
                if (!ReferenceEquals(cur, desired) &&
                    (!string.Equals(cur.FontFamily.Name, desired.FontFamily.Name, StringComparison.OrdinalIgnoreCase) ||
                     Math.Abs(cur.Size - desired.Size) > 0.05f ||
                     cur.Style != desired.Style))
                {
                    ctrl.Font = desired;
                }
            }
            catch { }
        }

        public static void ApplyCodexFontToMenuItem(ToolStripItem item)
        {
            if (item == null) return;
            try
            {
                Font cur = item.Font;
                float size = cur != null ? Math.Max(8.5f, Math.Min(cur.Size, 9.5f)) : 8.75f;
                FontStyle style = cur != null ? cur.Style : FontStyle.Regular;
                Font desired = item is ToolStripComboBox
                    ? GetReadableListFont(null, size)
                    : GetCachedFont(size, style, false);
                if (!ReferenceEquals(cur, desired))
                {
                    item.Font = desired;
                }
            }
            catch { }
        }

        /// <summary>
        /// Syncs native Windows 10/11 titlebar color, text color, and rounded corners via DWM (zero CPU cost).
        /// </summary>
        public static void ApplyWindowChrome(Form form)
        {
            if (form == null || form.IsDisposed || !form.IsHandleCreated) return;
            try
            {
                IntPtr hwnd = form.Handle;
                int cornerPref = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

                int darkMode = IsDarkColor(UISkinManager.BaseColor) ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                int captionColor = ColorToColorRef(UISkinManager.BaseColor);
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                int textColor = ColorToColorRef(UISkinManager.TextColor);
                DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));

                Color subtleBorder = Blend(UISkinManager.BaseColor, UISkinManager.BorderColor, 0.55f);
                int borderColor = ColorToColorRef(subtleBorder);
                DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            }
            catch { }
        }

        private static int ColorToColorRef(Color c)
        {
            return c.R | (c.G << 8) | (c.B << 16);
        }

        public static bool IsDarkColor(Color c)
        {
            double luma = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            return luma < 0.52;
        }

        public static Color Blend(Color c1, Color c2, float amount)
        {
            if (amount <= 0f) return c1;
            if (amount >= 1f) return c2;
            int r = (int)Math.Round(c1.R + (c2.R - c1.R) * amount);
            int g = (int)Math.Round(c1.G + (c2.G - c1.G) * amount);
            int b = (int)Math.Round(c1.B + (c2.B - c1.B) * amount);
            return Color.FromArgb(255, ClampByte(r), ClampByte(g), ClampByte(b));
        }

        private static int ClampByte(int v)
        {
            return v < 0 ? 0 : (v > 255 ? 255 : v);
        }

        public static Color GetContrastingTextColor(Color bg)
        {
            double luma = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
            return luma > 0.58 ? Color.FromArgb(24, 24, 28) : Color.FromArgb(250, 250, 252);
        }

        /// <summary>
        /// Computes a subtle Codex inset-grouped card surface derived from the active custom BaseColor.
        /// </summary>
        public static Color GetCardSurfaceColor()
        {
            Color b = UISkinManager.BaseColor;
            if (IsDarkColor(b))
            {
                // Slightly elevated dark card surface (Codex elevated dark card surface)
                return Blend(b, Color.White, 0.055f);
            }
            else
            {
                // Soft lifted white card surface on light themes (Codex light card surface)
                return Blend(b, Color.White, 0.55f);
            }
        }

        /// <summary>
        /// Computes a calm, Codex hairline border from the user's custom BorderColor.
        /// </summary>
        public static Color GetSoftBorderColor(float strength = 0.58f)
        {
            return Blend(UISkinManager.BaseColor, UISkinManager.BorderColor, strength);
        }

        /// <summary>
        /// Computes a harmonious accent color for active Codex controls, honoring AccentColor.
        /// </summary>
        public static Color GetEffectiveAccent()
        {
            return UISkinManager.AccentColor;
        }

        public static Color GetParentSurfaceColor(Control ctrl)
        {
            Control p = ctrl != null ? ctrl.Parent : null;
            while (p != null)
            {
                if (p is DarkGroupBox || p is GroupBox)
                {
                    return GetCardSurfaceColor();
                }
                if (p is TabPage || p is Form || p is SplitContainer || p is Panel)
                {
                    if (p.BackColor != Color.Transparent && p.BackColor.A == 255)
                    {
                        if (UISkinManager.IsPermanentDarkBotPanel(p))
                            return UISkinManager.PermanentDarkPanel;
                        return p.BackColor;
                    }
                }
                p = p.Parent;
            }
            return UISkinManager.BaseColor;
        }

        public static GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            float maxR = Math.Min(rect.Width, rect.Height) / 2f;
            float r = Math.Max(0.5f, Math.Min(radius, maxR));
            float d = r * 2f;

            path.AddArc(rect.X, rect.Y, d, d, 180f, 90f);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270f, 90f);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void AttachControlStyler(Control ctrl)
        {
            if (ctrl == null || ctrl.IsDisposed || !UISkinManager.CodexStyleEnabled) return;

            // Skip hidden legacy caption buttons or Alchemist special dark material controls
            if (ctrl.Name == "btnMin" || ctrl.Name == "btnMax" || ctrl.Name == "btnExit" ||
                ctrl.Name == "pnlBase" || ctrl.Name == "pnlNormal" || ctrl.Name == "pnlHover" ||
                ctrl.Name == "pnlPressed" || ctrl.Name == "pnlBorder" || ctrl.Name == "pnlText" ||
                ctrl.Name == "pnlSecondaryText" || ctrl.Name == "pnlAccent")
            {
                return;
            }

            // Track container ControlAdded once so dynamically added controls get styled automatically
            if ((ctrl is Form || ctrl is Panel || ctrl is TabPage || ctrl is GroupBox || ctrl is SplitContainer) &&
                !_trackedContainers.TryGetValue(ctrl, out _))
            {
                _trackedContainers.Add(ctrl, new object());
                ctrl.ControlAdded += OnContainerControlAdded;
            }

            if (ShouldHookControl(ctrl))
            {
                if (!_attachedHooks.TryGetValue(ctrl, out _))
                {
                    var hook = new CodexControlHook(ctrl);
                    _attachedHooks.Add(ctrl, hook);
                }

                // For NumericUpDown, also hook its internal UpDownButtons child for sleek Codex chevrons
                if (ctrl is NumericUpDown nud && nud.Controls.Count > 0)
                {
                    Control spinner = nud.Controls[0];
                    if (spinner != null && !_attachedHooks.TryGetValue(spinner, out _))
                    {
                        var spinHook = new CodexControlHook(spinner, isNumericSpinner: true, numericParent: nud);
                        _attachedHooks.Add(spinner, spinHook);
                    }
                }

                // Apply comfortable inner horizontal text margins for single-line textboxes
                if (ctrl is TextBox tb && !tb.Multiline && tb.IsHandleCreated)
                {
                    ApplyTextBoxMargins(tb);
                }
            }
        }

        private static void OnContainerControlAdded(object sender, ControlEventArgs e)
        {
            if (e != null && e.Control != null)
            {
                UISkinManager.ApplyThemeToControl(e.Control);
            }
        }

        private static bool ShouldHookControl(Control ctrl)
        {
            return ctrl is ButtonBase ||
                   ctrl is GroupBox ||
                   ctrl is ComboBox ||
                   ctrl is TextBox ||
                   ctrl is NumericUpDown ||
                   ctrl is TrackBar ||
                   ctrl is ProgressBar;
        }

        internal static void ApplyTextBoxMargins(TextBox tb)
        {
            if (tb == null || tb.IsDisposed || !tb.IsHandleCreated || tb.Multiline) return;
            try
            {
                int margin = Math.Max(2, (int)Math.Round(2f * GetDpiScale(tb)));
                IntPtr lParam = (IntPtr)((margin << 16) | (margin & 0xFFFF));
                SendMessage(tb.Handle, EM_SETMARGINS, (IntPtr)(EC_LEFTMARGIN | EC_RIGHTMARGIN), lParam);
            }
            catch { }
        }

        private sealed class CodexControlHook : NativeWindow
        {
            private readonly Control _control;
            private readonly bool _isNumericSpinner;
            private readonly NumericUpDown _numericParent;
            private bool _isHovered;
            private bool _isPressed;
            private Point _lastMouse;

            public CodexControlHook(Control control, bool isNumericSpinner = false, NumericUpDown numericParent = null)
            {
                _control = control;
                _isNumericSpinner = isNumericSpinner;
                _numericParent = numericParent;

                _control.HandleCreated += OnHandleCreated;
                _control.HandleDestroyed += OnHandleDestroyed;
                _control.MouseEnter += OnMouseEnter;
                _control.MouseLeave += OnMouseLeave;
                _control.MouseDown += OnMouseDown;
                _control.MouseUp += OnMouseUp;
                _control.MouseMove += OnMouseMove;
                _control.GotFocus += OnFocusChanged;
                _control.LostFocus += OnFocusChanged;

                if (_control.IsHandleCreated)
                {
                    AssignHandle(_control.Handle);
                }
            }

            private void OnHandleCreated(object sender, EventArgs e)
            {
                if (_control != null && !_control.IsDisposed && _control.IsHandleCreated)
                {
                    AssignHandle(_control.Handle);
                    if (_control is TextBox tb && !tb.Multiline)
                    {
                        ApplyTextBoxMargins(tb);
                    }
                }
            }

            private void OnHandleDestroyed(object sender, EventArgs e)
            {
                ReleaseHandle();
            }

            private void OnMouseEnter(object sender, EventArgs e)
            {
                _isHovered = true;
                SafeInvalidate();
            }

            private void OnMouseLeave(object sender, EventArgs e)
            {
                _isHovered = false;
                _isPressed = false;
                SafeInvalidate();
            }

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isPressed = true;
                    _lastMouse = e.Location;
                    SafeInvalidate();
                }
            }

            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                _isPressed = false;
                SafeInvalidate();
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (_isNumericSpinner || _control is TrackBar)
                {
                    _lastMouse = e.Location;
                    SafeInvalidate();
                }
            }

            private void OnFocusChanged(object sender, EventArgs e)
            {
                SafeInvalidate();
            }

            private void SafeInvalidate()
            {
                if (_control != null && !_control.IsDisposed && _control.IsHandleCreated)
                {
                    _control.Invalidate();
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (!UISkinManager.CodexStyleEnabled || _control == null || _control.IsDisposed)
                {
                    base.WndProc(ref m);
                    return;
                }

                const int WM_PAINT = 0x000F;
                const int WM_ERASEBKGND = 0x0014;
                const int WM_NCPAINT = 0x0085;
                const int WM_PRINTCLIENT = 0x0318;

                // TextBox: let Win32 handle native text editing/caret/selection, only refine the border cleanly
                if (_control is TextBox tb)
                {
                    base.WndProc(ref m);
                    if (m.Msg == WM_NCPAINT || m.Msg == WM_PAINT)
                    {
                        PaintTextBoxBorder(tb, m.HWnd);
                    }
                    return;
                }

                // NumericUpDown outer container: let base handle layout, then draw crisp Codex border
                if (_control is NumericUpDown nud && !_isNumericSpinner)
                {
                    base.WndProc(ref m);
                    if (m.Msg == WM_NCPAINT || m.Msg == WM_PAINT)
                    {
                        PaintNumericBorder(nud, m.HWnd);
                    }
                    return;
                }

                if (m.Msg == WM_ERASEBKGND)
                {
                    // Prevent flicker on fully custom-painted controls
                    m.Result = (IntPtr)1;
                    return;
                }

                if (m.Msg == WM_PRINTCLIENT && m.WParam != IntPtr.Zero && !(_control is GroupBox))
                {
                    Rectangle rect = _control.ClientRectangle;
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        try
                        {
                            using (Graphics g = Graphics.FromHdc(m.WParam))
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.PixelOffsetMode = PixelOffsetMode.Default;
                                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                                PaintCustomControl(g, rect);
                            }
                            return;
                        }
                        catch { }
                    }
                }

                if (m.Msg == WM_PAINT)
                {
                    Rectangle rect = _control.ClientRectangle;
                    if (rect.Width <= 0 || rect.Height <= 0)
                    {
                        base.WndProc(ref m);
                        return;
                    }

                    PAINTSTRUCT ps;
                    IntPtr hdc = BeginPaint(m.HWnd, out ps);
                    if (hdc != IntPtr.Zero)
                    {
                        try
                        {
                            BufferedGraphicsContext ctx = BufferedGraphicsManager.Current;
                            using (BufferedGraphics bg = ctx.Allocate(hdc, rect))
                            {
                                Graphics g = bg.Graphics;
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.PixelOffsetMode = PixelOffsetMode.Default;
                                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                                PaintCustomControl(g, rect);
                                bg.Render(hdc);
                            }
                        }
                        catch
                        {
                            // Never crash UI on GDI edge cases
                        }
                        finally
                        {
                            EndPaint(m.HWnd, ref ps);
                        }
                    }
                    return;
                }

                base.WndProc(ref m);
            }

            private void PaintCustomControl(Graphics g, Rectangle rect)
            {
                if (_isNumericSpinner)
                {
                    PaintNumericSpinner(g, rect);
                    return;
                }

                if (_control is CheckBox cb)
                {
                    PaintCodexCheckBox(g, rect, cb);
                }
                else if (_control is RadioButton rb)
                {
                    PaintCodexRadioButton(g, rect, rb);
                }
                else if (_control is Button btn)
                {
                    PaintCodexButton(g, rect, btn);
                }
                else if (_control is GroupBox gb)
                {
                    PaintCodexGroupBox(g, rect, gb);
                }
                else if (_control is ComboBox combo)
                {
                    PaintCodexComboBox(g, rect, combo);
                }
                else if (_control is TrackBar tb)
                {
                    PaintCodexTrackBar(g, rect, tb);
                }
                else if (_control is ProgressBar pb)
                {
                    PaintCodexProgressBar(g, rect, pb);
                }
            }

            private void PaintCodexButton(Graphics g, Rectangle rect, Button btn)
            {
                Color parentBg = GetParentSurfaceColor(btn);
                using (SolidBrush bg = new SolidBrush(parentBg))
                {
                    g.FillRectangle(bg, rect);
                }

                bool isChecked = false;
                bool isHovered = _isHovered;
                bool isPressed = _isPressed;

                if (btn is DarkButton db)
                {
                    isChecked = db.Checked;
                    if (db.ButtonState == DarkControlState.Hover) isHovered = true;
                    if (db.ButtonState == DarkControlState.Pressed) isPressed = true;
                }

                Color fill;
                Color border;
                Color textColor;

                if (!btn.Enabled)
                {
                    fill = Blend(parentBg, UISkinManager.NormalColor, 0.55f);
                    border = GetSoftBorderColor(0.32f);
                    textColor = UISkinManager.SecondaryTextColor;
                }
                else if (isChecked)
                {
                    Color accent = GetEffectiveAccent();
                    fill = isPressed ? Blend(accent, Color.Black, 0.16f)
                         : isHovered ? Blend(accent, Color.White, 0.10f)
                         : accent;
                    border = Blend(accent, Color.Black, 0.12f);
                    textColor = GetContrastingTextColor(fill);
                }
                else if (isPressed)
                {
                    fill = UISkinManager.PressedColor;
                    border = GetSoftBorderColor(0.85f);
                    textColor = UISkinManager.TextColor;
                }
                else if (isHovered)
                {
                    fill = UISkinManager.HoverColor;
                    border = GetSoftBorderColor(0.85f);
                    textColor = GetContrastingTextColor(fill);
                    // Preserve user text color if HoverColor is close in luminance to NormalColor
                    if (Math.Abs(GetLuminance(fill) - GetLuminance(UISkinManager.NormalColor)) < 0.22)
                    {
                        textColor = UISkinManager.TextColor;
                    }
                }
                else
                {
                    fill = UISkinManager.NormalColor;
                    border = GetSoftBorderColor(0.55f);
                    textColor = UISkinManager.TextColor;
                }

                float dpi = GetDpiScale(btn);
                float radius = (rect.Height <= (int)(22 * dpi) ? 5.5f : 7f) * dpi;
                RectangleF cardRect = new RectangleF(0.5f, 0.5f, rect.Width - 1.5f, rect.Height - 1.5f);

                using (GraphicsPath path = CreateRoundedRectPath(cardRect, radius))
                {
                    using (SolidBrush fillBrush = new SolidBrush(fill))
                    {
                        g.FillPath(fillBrush, path);
                    }

                    // Subtle top specular highlight for tactile Codex button depth
                    if (btn.Enabled && !isPressed)
                    {
                        Color highlight = IsDarkColor(fill)
                            ? Color.FromArgb(24, 255, 255, 255)
                            : Color.FromArgb(75, 255, 255, 255);
                        using (Pen hiPen = new Pen(highlight, 1f))
                        {
                            float inset = Math.Min(radius, rect.Width / 3f);
                            g.DrawLine(hiPen, cardRect.Left + inset, cardRect.Top + 1f, cardRect.Right - inset, cardRect.Top + 1f);
                        }
                    }

                    using (Pen borderPen = new Pen(border, 1f))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                // Special crisp vector icon for Root.btnGetCell (refresh/sync cell)
                if (btn.Name == "btnGetCell")
                {
                    DrawRefreshGlyph(g, rect, textColor);
                    return;
                }

                // Optional image
                int textOffsetX = 0;
                if (btn.Image != null)
                {
                    int imgY = (rect.Height - btn.Image.Height) / 2;
                    int imgX = string.IsNullOrEmpty(btn.Text)
                        ? (rect.Width - btn.Image.Width) / 2
                        : 6;
                    g.DrawImage(btn.Image, imgX, imgY, btn.Image.Width, btn.Image.Height);
                    textOffsetX = btn.Image.Width + 4;
                }

                if (!string.IsNullOrEmpty(btn.Text))
                {
                    Rectangle textRect = new Rectangle(rect.X + textOffsetX + 2, rect.Y, rect.Width - textOffsetX - 4, rect.Height);
                    TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
                                            TextFormatFlags.VerticalCenter |
                                            TextFormatFlags.EndEllipsis |
                                            TextFormatFlags.NoPrefix |
                                            TextFormatFlags.NoPadding;
                    TextRenderer.DrawText(g, btn.Text, btn.Font, textRect, textColor, flags);
                }
            }

            private static void DrawRefreshGlyph(Graphics g, Rectangle rect, Color color)
            {
                float cx = rect.Width / 2f;
                float cy = rect.Height / 2f;
                float r = Math.Min(rect.Width, rect.Height) * 0.27f;
                float stroke = Math.Max(1.4f, r * 0.25f);
                RectangleF arcRect = new RectangleF(cx - r, cy - r, r * 2f, r * 2f);
                using (Pen pen = new Pen(color, stroke))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, arcRect, -40f, 290f);
                    // Arrowhead at top-right of arc
                    float ax = cx + r * 0.78f;
                    float ay = cy - r * 0.62f;
                    float tip = Math.Max(2.8f, r * 0.55f);
                    g.DrawLine(pen, ax, ay, ax - tip, ay + tip * 0.12f);
                    g.DrawLine(pen, ax, ay, ax + tip * 0.08f, ay - tip * 0.94f);
                }
            }

            private void PaintCodexCheckBox(Graphics g, Rectangle rect, CheckBox cb)
            {
                Color parentBg = GetParentSurfaceColor(cb);
                using (SolidBrush bg = new SolidBrush(parentBg))
                {
                    g.FillRectangle(bg, rect);
                }

                float dpi = GetDpiScale(cb);
                bool usePillSwitch = IsPillSwitchCheckBox(cb);
                Color accent = GetEffectiveAccent();
                Color textColor = cb.Enabled ? UISkinManager.TextColor : UISkinManager.SecondaryTextColor;

                if (usePillSwitch)
                {
                    int sh = Math.Min(rect.Height - 2, Math.Max(12, (int)Math.Round(14f * dpi)));
                    int sw = Math.Max((int)Math.Round(sh * 1.82f), (int)Math.Round(25f * dpi));
                    float sx = 1f;
                    float sy = (rect.Height - sh) / 2f;
                    RectangleF trackRect = new RectangleF(sx, sy, sw, sh);

                    Color trackColor;
                    if (!cb.Enabled)
                    {
                        trackColor = Blend(parentBg, UISkinManager.BorderColor, 0.35f);
                    }
                    else if (cb.Checked)
                    {
                        trackColor = _isHovered ? Blend(accent, Color.White, 0.12f) : accent;
                    }
                    else
                    {
                        trackColor = _isHovered
                            ? Blend(UISkinManager.NormalColor, UISkinManager.BorderColor, 0.55f)
                            : Blend(UISkinManager.NormalColor, UISkinManager.BorderColor, 0.35f);
                    }

                    using (GraphicsPath trackPath = CreateRoundedRectPath(trackRect, sh / 2f))
                    {
                        using (SolidBrush tb = new SolidBrush(trackColor))
                            g.FillPath(tb, trackPath);
                        using (Pen tp = new Pen(GetSoftBorderColor(cb.Checked ? 0.25f : 0.6f), 1f))
                            g.DrawPath(tp, trackPath);
                    }

                    float inset = Math.Max(2f, 2f * dpi);
                    float knobSize = Math.Max(6f, sh - inset * 2f);
                    float knobX = cb.Checked ? (sx + sw - knobSize - inset) : (sx + inset);
                    float knobY = sy + inset;
                    RectangleF knobRect = new RectangleF(knobX, knobY, knobSize, knobSize);
                    using (SolidBrush kb = new SolidBrush(Color.White))
                        g.FillEllipse(kb, knobRect);
                    using (Pen kp = new Pen(Color.FromArgb(45, 0, 0, 0), 0.75f))
                        g.DrawEllipse(kp, knobRect);

                    if (!string.IsNullOrEmpty(cb.Text))
                    {
                        int gap = (int)Math.Round(6f * dpi);
                        int tx = (int)(sx + sw + gap);
                        Rectangle textRect = new Rectangle(tx, 0, Math.Max(0, rect.Width - tx), rect.Height);
                        TextRenderer.DrawText(g, cb.Text, cb.Font, textRect, textColor,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                    }
                    return;
                }

                // Compact Codex rounded-squircle checkbox scaled cleanly to DPI
                int boxSize = Math.Min((int)Math.Round(14f * dpi), Math.Max(11, rect.Height - 2));
                int bx = 0;
                int by = (rect.Height - boxSize) / 2;
                RectangleF boxRect = new RectangleF(bx + 0.5f, by + 0.5f, boxSize - 1f, boxSize - 1f);

                Color boxFill;
                Color boxBorder;
                if (!cb.Enabled)
                {
                    boxFill = Blend(parentBg, UISkinManager.NormalColor, 0.5f);
                    boxBorder = GetSoftBorderColor(0.35f);
                }
                else if (cb.Checked)
                {
                    boxFill = _isHovered ? Blend(accent, Color.White, 0.12f) : accent;
                    boxBorder = Blend(accent, Color.Black, 0.15f);
                }
                else
                {
                    boxFill = _isHovered ? Blend(UISkinManager.NormalColor, UISkinManager.HoverColor, 0.45f) : UISkinManager.NormalColor;
                    boxBorder = _isHovered ? GetSoftBorderColor(0.95f) : GetSoftBorderColor(0.72f);
                }

                using (GraphicsPath path = CreateRoundedRectPath(boxRect, 3.8f * dpi))
                {
                    using (SolidBrush fillBrush = new SolidBrush(boxFill))
                        g.FillPath(fillBrush, path);
                    using (Pen borderPen = new Pen(boxBorder, Math.Max(1f, 1.1f * dpi)))
                        g.DrawPath(borderPen, path);
                }

                if (cb.Checked)
                {
                    Color checkColor = GetContrastingTextColor(boxFill);
                    using (Pen checkPen = new Pen(checkColor, Math.Max(1.6f, 1.75f * dpi)))
                    {
                        checkPen.StartCap = LineCap.Round;
                        checkPen.EndCap = LineCap.Round;
                        checkPen.LineJoin = LineJoin.Round;

                        PointF p1 = new PointF(boxRect.Left + boxRect.Width * 0.24f, boxRect.Top + boxRect.Height * 0.52f);
                        PointF p2 = new PointF(boxRect.Left + boxRect.Width * 0.44f, boxRect.Top + boxRect.Height * 0.72f);
                        PointF p3 = new PointF(boxRect.Left + boxRect.Width * 0.78f, boxRect.Top + boxRect.Height * 0.30f);
                        g.DrawLines(checkPen, new[] { p1, p2, p3 });
                    }
                }

                if (!string.IsNullOrEmpty(cb.Text))
                {
                    int tx = bx + boxSize + (int)Math.Round(5f * dpi);
                    Rectangle textRect = new Rectangle(tx, 0, Math.Max(0, rect.Width - tx), rect.Height);
                    TextRenderer.DrawText(g, cb.Text, cb.Font, textRect, textColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                }
            }

            private static bool IsPillSwitchCheckBox(CheckBox cb)
            {
                if (cb == null || string.IsNullOrEmpty(cb.Text)) return false;
                if (cb.Tag is string tag && tag == "codex-switch") return true;
                // Primary spacious toggles that look iconic as Codex pill switches
                string n = cb.Name ?? string.Empty;
                return (n == "chkEnableVoice" || n == "chkStartBot" || n == "chkPreviewSwitch") && cb.Width >= 82;
            }

            private void PaintCodexRadioButton(Graphics g, Rectangle rect, RadioButton rb)
            {
                Color parentBg = GetParentSurfaceColor(rb);
                using (SolidBrush bg = new SolidBrush(parentBg))
                {
                    g.FillRectangle(bg, rect);
                }

                float dpi = GetDpiScale(rb);
                int size = Math.Min((int)Math.Round(14f * dpi), Math.Max(11, rect.Height - 2));
                int ry = (rect.Height - size) / 2;
                RectangleF circleRect = new RectangleF(0.5f, ry + 0.5f, size - 1f, size - 1f);
                Color accent = GetEffectiveAccent();
                Color textColor = rb.Enabled ? UISkinManager.TextColor : UISkinManager.SecondaryTextColor;

                if (rb.Checked)
                {
                    Color fill = rb.Enabled ? (_isHovered ? Blend(accent, Color.White, 0.1f) : accent) : GetSoftBorderColor(0.5f);
                    using (SolidBrush fb = new SolidBrush(fill))
                        g.FillEllipse(fb, circleRect);

                    float dot = size * 0.38f;
                    RectangleF dotRect = new RectangleF(
                        circleRect.Left + (circleRect.Width - dot) / 2f,
                        circleRect.Top + (circleRect.Height - dot) / 2f,
                        dot, dot);
                    using (SolidBrush db = new SolidBrush(GetContrastingTextColor(fill)))
                        g.FillEllipse(db, dotRect);
                }
                else
                {
                    Color fill = _isHovered ? Blend(UISkinManager.NormalColor, UISkinManager.HoverColor, 0.4f) : UISkinManager.NormalColor;
                    Color border = _isHovered ? GetSoftBorderColor(0.95f) : GetSoftBorderColor(0.72f);
                    using (SolidBrush fb = new SolidBrush(fill))
                        g.FillEllipse(fb, circleRect);
                    using (Pen bp = new Pen(border, Math.Max(1f, 1.15f * dpi)))
                        g.DrawEllipse(bp, circleRect);
                }

                if (!string.IsNullOrEmpty(rb.Text))
                {
                    int tx = size + (int)Math.Round(5f * dpi);
                    Rectangle textRect = new Rectangle(tx, 0, Math.Max(0, rect.Width - tx), rect.Height);
                    TextRenderer.DrawText(g, rb.Text, rb.Font, textRect, textColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                }
            }

            private void PaintCodexGroupBox(Graphics g, Rectangle rect, GroupBox gb)
            {
                Color outerBg = UISkinManager.BaseColor;
                if (gb.Parent != null && gb.Parent.BackColor != Color.Transparent && gb.Parent.BackColor.A == 255)
                {
                    outerBg = gb.Parent.BackColor;
                }
                Color cardBg = GetCardSurfaceColor();
                Color border = GetSoftBorderColor(0.45f);
                float dpi = GetDpiScale(gb);

                // Never paint over visible child controls inside the GroupBox
                foreach (Control child in gb.Controls)
                {
                    if (child.Visible && child.Width > 0 && child.Height > 0)
                    {
                        g.ExcludeClip(child.Bounds);
                    }
                }

                using (SolidBrush bgBrush = new SolidBrush(outerBg))
                {
                    g.FillRectangle(bgBrush, rect);
                }

                bool hasTitle = !string.IsNullOrWhiteSpace(gb.Text);
                int topY = hasTitle ? (int)Math.Round(7f * dpi) : 1;
                RectangleF cardRect = new RectangleF(0.5f, topY + 0.5f, rect.Width - 1.5f, Math.Max(4, rect.Height - topY - 1.5f));

                using (GraphicsPath path = CreateRoundedRectPath(cardRect, 9f * dpi))
                {
                    using (SolidBrush cardBrush = new SolidBrush(cardBg))
                        g.FillPath(cardBrush, path);
                    using (Pen borderPen = new Pen(border, 1f))
                        g.DrawPath(borderPen, path);
                }

                if (hasTitle)
                {
                    int availWidth = Math.Max(20, rect.Width - (int)Math.Round(10f * dpi));
                    Font titleFont = GetCachedFont(Math.Max(7.75f, gb.Font.Size - 0.25f), FontStyle.Bold, false);
                    Size textSize = TextRenderer.MeasureText(g, gb.Text, titleFont, new Size(int.MaxValue, 40), TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                    if (textSize.Width > availWidth - 4)
                    {
                        titleFont = GetCachedFont(Math.Max(7.5f, gb.Font.Size - 0.5f), FontStyle.Regular, false);
                        textSize = TextRenderer.MeasureText(g, gb.Text, titleFont, new Size(int.MaxValue, 40), TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                    }

                    float bx = 6f * dpi;
                    float pad = 6f * dpi;
                    RectangleF badgeRect = new RectangleF(bx, 0f, Math.Min(rect.Width - bx * 2f, textSize.Width + pad), Math.Max((int)(14 * dpi), textSize.Height));
                    using (GraphicsPath badgePath = CreateRoundedRectPath(badgeRect, 4f * dpi))
                    using (SolidBrush badgeBrush = new SolidBrush(outerBg))
                    {
                        g.FillPath(badgeBrush, badgePath);
                    }

                    int tx = (int)Math.Round(9f * dpi);
                    Rectangle textRect = new Rectangle(tx, 0, Math.Max(0, rect.Width - (int)Math.Round(12f * dpi)), Math.Max((int)(14 * dpi), textSize.Height));
                    Color headerColor = Blend(UISkinManager.TextColor, UISkinManager.SecondaryTextColor, 0.18f);
                    TextRenderer.DrawText(g, gb.Text, titleFont, textRect, headerColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                }
            }

            private void PaintCodexComboBox(Graphics g, Rectangle rect, ComboBox combo)
            {
                Color parentBg = GetParentSurfaceColor(combo);
                using (SolidBrush bg = new SolidBrush(parentBg))
                {
                    g.FillRectangle(bg, rect);
                }

                float dpi = GetDpiScale(combo);
                bool active = combo.Focused || combo.DroppedDown;
                Color fill = !combo.Enabled
                    ? Blend(parentBg, UISkinManager.NormalColor, 0.5f)
                    : _isHovered && !active
                        ? Blend(UISkinManager.NormalColor, UISkinManager.HoverColor, 0.35f)
                        : UISkinManager.NormalColor;

                Color border = !combo.Enabled
                    ? GetSoftBorderColor(0.32f)
                    : active
                        ? GetEffectiveAccent()
                        : _isHovered
                            ? GetSoftBorderColor(0.88f)
                            : GetSoftBorderColor(0.58f);

                Color textColor = combo.Enabled ? UISkinManager.TextColor : UISkinManager.SecondaryTextColor;

                RectangleF boxRect = new RectangleF(0.5f, 0.5f, rect.Width - 1.5f, rect.Height - 1.5f);
                using (GraphicsPath path = CreateRoundedRectPath(boxRect, 6f * dpi))
                {
                    using (SolidBrush fillBrush = new SolidBrush(fill))
                        g.FillPath(fillBrush, path);
                    using (Pen borderPen = new Pen(border, active ? 1.25f : 1f))
                        g.DrawPath(borderPen, path);
                }

                // Draw selected text
                int chevronZone = (int)Math.Round(18f * dpi);
                string displayText = combo.Text;
                if (string.IsNullOrEmpty(displayText) && combo.SelectedItem != null)
                {
                    displayText = combo.GetItemText(combo.SelectedItem);
                }

                if (!string.IsNullOrEmpty(displayText))
                {
                    int padL = (int)Math.Round(6f * dpi);
                    Rectangle textRect = new Rectangle(padL, 1, Math.Max(0, rect.Width - chevronZone - padL), rect.Height - 2);
                    TextRenderer.DrawText(g, displayText, combo.Font, textRect, textColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                }

                // Sleek antialiased Codex chevron
                Color chevronColor = active ? GetEffectiveAccent() : Blend(textColor, UISkinManager.SecondaryTextColor, 0.45f);
                float cx = rect.Width - 10f * dpi;
                float cy = rect.Height / 2f + 0.5f;
                float cw = 3.5f * dpi;
                float ch = 1.8f * dpi;
                using (Pen chevPen = new Pen(chevronColor, Math.Max(1.4f, 1.5f * dpi)))
                {
                    chevPen.StartCap = LineCap.Round;
                    chevPen.EndCap = LineCap.Round;
                    chevPen.LineJoin = LineJoin.Round;
                    g.DrawLines(chevPen, new[]
                    {
                        new PointF(cx - cw, cy - ch),
                        new PointF(cx, cy + ch),
                        new PointF(cx + cw, cy - ch)
                    });
                }
            }

            private void PaintTextBoxBorder(TextBox tb, IntPtr hwnd)
            {
                if (tb.BorderStyle == BorderStyle.None) return;
                IntPtr hdc = GetWindowDC(hwnd);
                if (hdc == IntPtr.Zero) return;
                try
                {
                    using (Graphics g = Graphics.FromHdc(hdc))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        Color border = !tb.Enabled
                            ? GetSoftBorderColor(0.32f)
                            : tb.Focused
                                ? GetEffectiveAccent()
                                : _isHovered
                                    ? GetSoftBorderColor(0.85f)
                                    : GetSoftBorderColor(0.58f);

                        Rectangle r = new Rectangle(0, 0, tb.Width - 1, tb.Height - 1);
                        using (Pen pen = new Pen(border, 1f))
                        {
                            g.DrawRectangle(pen, r);
                        }
                    }
                }
                catch { }
                finally
                {
                    ReleaseDC(hwnd, hdc);
                }
            }

            private void PaintNumericBorder(NumericUpDown nud, IntPtr hwnd)
            {
                IntPtr hdc = GetWindowDC(hwnd);
                if (hdc == IntPtr.Zero) return;
                try
                {
                    using (Graphics g = Graphics.FromHdc(hdc))
                    {
                        bool focused = nud.Focused || nud.ContainsFocus;
                        Color border = !nud.Enabled
                            ? GetSoftBorderColor(0.32f)
                            : focused
                                ? GetEffectiveAccent()
                                : _isHovered
                                    ? GetSoftBorderColor(0.85f)
                                    : GetSoftBorderColor(0.58f);
                        using (Pen pen = new Pen(border, 1f))
                        {
                            g.DrawRectangle(pen, 0, 0, nud.Width - 1, nud.Height - 1);
                        }
                    }
                }
                catch { }
                finally
                {
                    ReleaseDC(hwnd, hdc);
                }
            }

            private void PaintNumericSpinner(Graphics g, Rectangle rect)
            {
                Color fill = UISkinManager.NormalColor;
                Color divider = GetSoftBorderColor(0.45f);
                Color chevron = (_numericParent != null && !_numericParent.Enabled)
                    ? UISkinManager.SecondaryTextColor
                    : Blend(UISkinManager.TextColor, UISkinManager.SecondaryTextColor, 0.25f);

                using (SolidBrush bg = new SolidBrush(fill))
                {
                    g.FillRectangle(bg, rect);
                }

                int midY = rect.Height / 2;

                // Highlight hovered half
                if (_isHovered && (_numericParent == null || _numericParent.Enabled))
                {
                    Rectangle half = _lastMouse.Y < midY
                        ? new Rectangle(1, 0, rect.Width - 1, midY)
                        : new Rectangle(1, midY, rect.Width - 1, rect.Height - midY);
                    Color hi = _isPressed ? UISkinManager.PressedColor : Blend(fill, UISkinManager.HoverColor, 0.55f);
                    using (SolidBrush hb = new SolidBrush(hi))
                    {
                        g.FillRectangle(hb, half);
                    }
                }

                // Subtle left and middle separators
                using (Pen divPen = new Pen(divider, 1f))
                {
                    g.DrawLine(divPen, 0, 1, 0, rect.Height - 2);
                    g.DrawLine(divPen, 2, midY, rect.Width - 2, midY);
                }

                // Crisp Codex up & down chevrons
                float cx = rect.Width / 2f + 0.5f;
                float upY = midY / 2f;
                float dnY = midY + (rect.Height - midY) / 2f;

                using (Pen p = new Pen(chevron, 1.35f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    p.LineJoin = LineJoin.Round;

                    g.DrawLines(p, new[]
                    {
                        new PointF(cx - 2.6f, upY + 1.2f),
                        new PointF(cx, upY - 1.3f),
                        new PointF(cx + 2.6f, upY + 1.2f)
                    });

                    g.DrawLines(p, new[]
                    {
                        new PointF(cx - 2.6f, dnY - 1.2f),
                        new PointF(cx, dnY + 1.3f),
                        new PointF(cx + 2.6f, dnY - 1.2f)
                    });
                }
            }

            private void PaintCodexTrackBar(Graphics g, Rectangle rect, TrackBar tb)
            {
                Color parentBg = GetParentSurfaceColor(tb);
                using (SolidBrush bg = new SolidBrush(parentBg))
                {
                    g.FillRectangle(bg, rect);
                }

                // Query exact native Win32 thumb rect so our Codex thumb aligns 100% with mouse dragging!
                RECT nativeThumb = new RECT();
                SendMessage(tb.Handle, TBM_GETTHUMBRECT, IntPtr.Zero, ref nativeThumb);

                float dpi = GetDpiScale(tb);
                int thumbDiam = Math.Min((int)Math.Round(15f * dpi), Math.Max(11, rect.Height - 4));
                float cy = rect.Height / 2f;
                float trackH = Math.Max(3.5f, 4f * dpi);
                float trackLeft = 8f * dpi;
                float trackRight = Math.Max(trackLeft + 4f, rect.Width - 8f * dpi);
                float trackW = trackRight - trackLeft;

                float thumbCenterX;
                if (nativeThumb.Right > nativeThumb.Left)
                {
                    thumbCenterX = (nativeThumb.Left + nativeThumb.Right) / 2f;
                }
                else
                {
                    float pct = tb.Maximum > tb.Minimum ? (float)(tb.Value - tb.Minimum) / (tb.Maximum - tb.Minimum) : 0f;
                    thumbCenterX = trackLeft + pct * trackW;
                }
                thumbCenterX = Math.Max(trackLeft, Math.Min(trackRight, thumbCenterX));

                // Inactive capsule track
                RectangleF trackRect = new RectangleF(trackLeft, cy - trackH / 2f, trackW, trackH);
                Color inactiveTrack = Blend(UISkinManager.NormalColor, UISkinManager.BorderColor, 0.55f);
                using (GraphicsPath trackPath = CreateRoundedRectPath(trackRect, trackH / 2f))
                using (SolidBrush trBrush = new SolidBrush(inactiveTrack))
                {
                    g.FillPath(trBrush, trackPath);
                }

                // Active filled capsule portion
                float activeW = Math.Max(0f, thumbCenterX - trackLeft);
                if (activeW > 1f)
                {
                    RectangleF activeRect = new RectangleF(trackLeft, cy - trackH / 2f, activeW, trackH);
                    Color accent = tb.Enabled ? GetEffectiveAccent() : UISkinManager.SecondaryTextColor;
                    using (GraphicsPath activePath = CreateRoundedRectPath(activeRect, trackH / 2f))
                    using (SolidBrush acBrush = new SolidBrush(accent))
                    {
                        g.FillPath(acBrush, activePath);
                    }
                }

                // Circular white Codex slider thumb with soft border shadow
                float r = thumbDiam / 2f;
                if (_isPressed) r += 0.6f;
                RectangleF shadowRect = new RectangleF(thumbCenterX - r, cy - r + 1f, r * 2f, r * 2f);
                using (SolidBrush shBrush = new SolidBrush(Color.FromArgb(42, 0, 0, 0)))
                {
                    g.FillEllipse(shBrush, shadowRect);
                }

                RectangleF thumbRect = new RectangleF(thumbCenterX - r, cy - r, r * 2f, r * 2f);
                using (SolidBrush thumbBrush = new SolidBrush(Color.FromArgb(252, 252, 255)))
                {
                    g.FillEllipse(thumbBrush, thumbRect);
                }
                Color thumbRing = _isHovered || _isPressed ? GetEffectiveAccent() : Color.FromArgb(75, 0, 0, 0);
                using (Pen ringPen = new Pen(thumbRing, 1f))
                {
                    g.DrawEllipse(ringPen, thumbRect);
                }
            }

            private void PaintCodexProgressBar(Graphics g, Rectangle rect, ProgressBar pb)
            {
                Color parentBg = GetParentSurfaceColor(pb);
                using (SolidBrush bg = new SolidBrush(parentBg))
                {
                    g.FillRectangle(bg, rect);
                }

                RectangleF trackRect = new RectangleF(0.5f, 0.5f, rect.Width - 1.5f, rect.Height - 1.5f);
                float radius = trackRect.Height / 2f;
                Color trackColor = Blend(UISkinManager.NormalColor, UISkinManager.BorderColor, 0.35f);

                using (GraphicsPath trackPath = CreateRoundedRectPath(trackRect, radius))
                {
                    using (SolidBrush trBrush = new SolidBrush(trackColor))
                        g.FillPath(trBrush, trackPath);
                }

                float pct = pb.Maximum > pb.Minimum
                    ? Math.Max(0f, Math.Min(1f, (float)(pb.Value - pb.Minimum) / (pb.Maximum - pb.Minimum)))
                    : 0f;

                if (pct > 0.005f)
                {
                    float fillW = Math.Max(trackRect.Height, trackRect.Width * pct);
                    fillW = Math.Min(trackRect.Width, fillW);
                    RectangleF fillRect = new RectangleF(trackRect.X, trackRect.Y, fillW, trackRect.Height);

                    Color fillColor = GetEffectiveAccent();
                    if (pb.Name == "pbMicLevel")
                    {
                        fillColor = pct > 0.82f ? Color.FromArgb(255, 69, 58)
                                  : pct > 0.55f ? Color.FromArgb(255, 214, 10)
                                  : Color.FromArgb(48, 209, 88);
                    }

                    using (GraphicsPath fillPath = CreateRoundedRectPath(fillRect, radius))
                    using (SolidBrush fillBrush = new SolidBrush(fillColor))
                    {
                        g.FillPath(fillBrush, fillPath);
                    }
                }
            }

            private static double GetLuminance(Color c)
            {
                return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            }
        }
    }
}

