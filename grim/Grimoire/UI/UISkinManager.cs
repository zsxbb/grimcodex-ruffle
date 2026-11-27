using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DarkUI.Config;
using DarkUI.Controls;
using Grimoire.Botting;

namespace Grimoire.UI
{
    public static class UISkinManager
    {
        // Default Codex Dark palette (sleek, comfortable, high contrast)
        public static readonly Color DefaultBase = Color.FromArgb(28, 28, 30);
        public static readonly Color DefaultNormal = Color.FromArgb(44, 44, 46);
        public static readonly Color DefaultHover = Color.FromArgb(58, 58, 62);
        public static readonly Color DefaultPressed = Color.FromArgb(36, 36, 39);
        public static readonly Color DefaultBorder = Color.FromArgb(68, 68, 74);
        public static readonly Color DefaultText = Color.FromArgb(245, 245, 247);
        public static readonly Color DefaultSecondaryText = Color.FromArgb(152, 152, 157);
        public static readonly Color DefaultAccent = Color.FromArgb(10, 132, 255); // Codex Blue

        // Permanent default dark colors for inner Bot windows/lists
        public static readonly Color PermanentDarkBackground = Color.FromArgb(22, 22, 26);
        public static readonly Color PermanentDarkText = Color.FromArgb(238, 238, 242);
        public static readonly Color PermanentDarkPanel = Color.FromArgb(24, 24, 28);

        // Active Colors
        public static Color BaseColor { get; set; } = DefaultBase;
        public static Color NormalColor { get; set; } = DefaultNormal;
        public static Color HoverColor { get; set; } = DefaultHover;
        public static Color PressedColor { get; set; } = DefaultPressed;
        public static Color BorderColor { get; set; } = DefaultBorder;
        public static Color TextColor { get; set; } = DefaultText;
        public static Color SecondaryTextColor { get; set; } = DefaultSecondaryText;
        public static Color AccentColor { get; set; } = DefaultAccent;

        // Style & Typography Settings
        public static bool CodexStyleEnabled { get; set; } = true;
        public static string UiFontPreference { get; set; } = "Auto (Codex)";

        private static readonly CustomDarkMenuRenderer SharedMenuRenderer = new CustomDarkMenuRenderer();

        private static readonly HashSet<int> KnownBaseArgb = new HashSet<int>
        {
            Color.FromArgb(28, 28, 30).ToArgb(),
            Color.FromArgb(36, 36, 46).ToArgb(),
            Color.FromArgb(34, 34, 34).ToArgb(),
            Color.FromArgb(30, 30, 30).ToArgb(),
            Color.FromArgb(36, 36, 36).ToArgb(),
            Color.FromArgb(43, 43, 53).ToArgb(),
            Color.FromArgb(46, 46, 56).ToArgb(),
            Color.FromArgb(28, 28, 38).ToArgb(),
            Color.FromArgb(242, 242, 247).ToArgb()
        };

        public static string ConfigPath => Path.Combine(Application.StartupPath, "config.cfg");

        public static void Init()
        {
            try
            {
                Config c = Config.Load(ConfigPath);
                BaseColor = ParseColor(c.Get("BtnBaseColor"), DefaultBase);
                NormalColor = ParseColor(c.Get("BtnNormalColor"), DefaultNormal);
                HoverColor = ParseColor(c.Get("BtnHoverColor"), DefaultHover);
                PressedColor = ParseColor(c.Get("BtnPressedColor"), DefaultPressed);
                BorderColor = ParseColor(c.Get("BtnBorderColor"), DefaultBorder);
                TextColor = ParseColor(c.Get("BtnTextColor"), DefaultText);
                SecondaryTextColor = ParseColor(c.Get("BtnSecondaryTextColor"), DefaultSecondaryText);

                string savedAccent = c.Get("BtnAccentColor");
                if (!string.IsNullOrWhiteSpace(savedAccent))
                {
                    AccentColor = ParseColor(savedAccent, DefaultAccent);
                }
                else
                {
                    AccentColor = DeriveSmartAccent(BaseColor, HoverColor, BorderColor);
                }

                string codexStyleFlag = c.Get("CodexStyleEnabled");
                CodexStyleEnabled = string.IsNullOrWhiteSpace(codexStyleFlag) || !codexStyleFlag.Equals("false", StringComparison.OrdinalIgnoreCase);

                string fontPref = c.Get("UiFontName");
                UiFontPreference = string.IsNullOrWhiteSpace(fontPref) ? "Auto (Codex)" : fontPref;
                CodexThemeEngine.FontPreference = UiFontPreference;

                KnownBaseArgb.Add(BaseColor.ToArgb());
                ApplyColorsToDarkUI();
            }
            catch { }
        }

        public static Color DeriveSmartAccent(Color baseCol, Color hoverCol, Color borderCol)
        {
            // If user customized a vivid theme (e.g. Sakura Pink or Violet), inherit its vibrant hue as accent
            if (GetSaturation(hoverCol) > 0.35f) return hoverCol;
            if (GetSaturation(borderCol) > 0.35f) return borderCol;
            return CodexThemeEngine.IsDarkColor(baseCol)
                ? Color.FromArgb(10, 132, 255)   // Codex Dark Blue
                : Color.FromArgb(0, 122, 255);   // Codex Light Blue
        }

        private static float GetSaturation(Color c)
        {
            return c.GetSaturation();
        }

        public static void Apply(Color baseColor, Color normal, Color hover, Color pressed, Color border, Color text, Color secondaryText, Color accent, bool refreshUi = true)
        {
            BaseColor = baseColor;
            NormalColor = normal;
            HoverColor = hover;
            PressedColor = pressed;
            BorderColor = border;
            TextColor = text;
            SecondaryTextColor = secondaryText;
            AccentColor = accent;

            KnownBaseArgb.Add(baseColor.ToArgb());
            ApplyColorsToDarkUI();

            if (refreshUi)
            {
                RefreshAllForms();
            }
        }

        public static void Apply(Color baseColor, Color normal, Color hover, Color pressed, Color border, Color text, Color secondaryText, bool refreshUi = true)
        {
            Apply(baseColor, normal, hover, pressed, border, text, secondaryText, AccentColor, refreshUi);
        }

        public static void Apply(Color baseColor, Color normal, Color hover, Color pressed, Color border, Color text, bool refreshUi = true)
        {
            Apply(baseColor, normal, hover, pressed, border, text, SecondaryTextColor, AccentColor, refreshUi);
        }

        public static void Save(Color baseColor, Color normal, Color hover, Color pressed, Color border, Color text, Color secondaryText, Color accent, string fontPreference = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(fontPreference))
                {
                    UiFontPreference = fontPreference;
                    CodexThemeEngine.FontPreference = fontPreference;
                }

                Apply(baseColor, normal, hover, pressed, border, text, secondaryText, accent, true);

                Config c = Config.Load(ConfigPath);
                c.Set("BtnBaseColor", baseColor.ToArgb().ToString("X8"));
                c.Set("BtnNormalColor", normal.ToArgb().ToString("X8"));
                c.Set("BtnHoverColor", hover.ToArgb().ToString("X8"));
                c.Set("BtnPressedColor", pressed.ToArgb().ToString("X8"));
                c.Set("BtnBorderColor", border.ToArgb().ToString("X8"));
                c.Set("BtnTextColor", text.ToArgb().ToString("X8"));
                c.Set("BtnSecondaryTextColor", secondaryText.ToArgb().ToString("X8"));
                c.Set("BtnAccentColor", accent.ToArgb().ToString("X8"));
                c.Set("CodexStyleEnabled", CodexStyleEnabled ? "true" : "false");
                c.Set("UiFontName", UiFontPreference);
                c.Save();
            }
            catch { }
        }

        public static void Save(Color baseColor, Color normal, Color hover, Color pressed, Color border, Color text, Color secondaryText)
        {
            Save(baseColor, normal, hover, pressed, border, text, secondaryText, AccentColor, UiFontPreference);
        }

        public static void Save(Color baseColor, Color normal, Color hover, Color pressed, Color border, Color text)
        {
            Save(baseColor, normal, hover, pressed, border, text, SecondaryTextColor, AccentColor, UiFontPreference);
        }

        public static void ResetToDefault()
        {
            CodexStyleEnabled = true;
            UiFontPreference = "Auto (Codex)";
            CodexThemeEngine.FontPreference = UiFontPreference;
            Save(DefaultBase, DefaultNormal, DefaultHover, DefaultPressed, DefaultBorder, DefaultText, DefaultSecondaryText, DefaultAccent, UiFontPreference);
        }

        private static void ApplyColorsToDarkUI()
        {
            SetColorField("GreyBackground", BaseColor);
            SetColorField("HeaderBackground", BaseColor);
            SetColorField("LightBackground", NormalColor);
            SetColorField("MediumBackground", NormalColor);
            SetColorField("LighterBackground", HoverColor);
            SetColorField("DarkBackground", PressedColor);
            SetColorField("GreySelection", BorderColor);
            SetColorField("DarkGreySelection", BorderColor);
            SetColorField("LightBorder", BorderColor);
            SetColorField("DarkBorder", BorderColor);
            SetColorField("DarkBlueBorder", AccentColor);
            SetColorField("LightBlueBorder", AccentColor);
            SetColorField("LightText", TextColor);
            SetColorField("DisabledText", SecondaryTextColor);
            SetColorField("GreyHighlight", HoverColor);
            SetColorField("BlueHighlight", AccentColor);
            SetColorField("BlueSelection", AccentColor);
        }

        private static void SetColorField(string propertyName, Color color)
        {
            try
            {
                var field = typeof(Colors).GetField($"<{propertyName}>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(null, color);
                    return;
                }

                var prop = typeof(Colors).GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                prop?.SetValue(null, color, null);
            }
            catch { }
        }

        public static void RefreshAllForms()
        {
            try
            {
                foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
                {
                    if (form == null || form.IsDisposed) continue;
                    form.SuspendLayout();
                    try
                    {
                        ApplyThemeToControl(form);
                    }
                    finally
                    {
                        form.ResumeLayout(false);
                    }
                    form.Invalidate(true);
                }

                if (Root.Instance != null && !Root.Instance.IsDisposed)
                {
                    Root.Instance.LayoutToolbarControls();
                    Root.Instance.SetUpdateNotification(Root.Instance.IsAppUpdateAvailable);
                }
            }
            catch { }
        }

        public static void ApplyThemeToControl(Control ctrl)
        {
            if (ctrl == null || ctrl.IsDisposed) return;

            Form ownerForm = ctrl as Form ?? ctrl.FindForm();
            bool isDenseForm = ownerForm is BotManager || (ownerForm != null && ownerForm.GetType().Name == "BotManager");

            ApplyThemeInternal(ctrl, isDenseForm);
        }

        private static void ApplyThemeInternal(Control ctrl, bool isDenseForm)
        {
            if (ctrl == null || ctrl.IsDisposed) return;

            if (ctrl.Name == "pnlMaterials" || ctrl.Name == "gbMaterials" ||
                (ctrl.Parent != null && (ctrl.Parent.Name == "pnlMaterials" || ctrl.Parent.Name == "gbMaterials")))
            {
                CodexThemeEngine.ApplyCodexFont(ctrl, isDenseForm);
                if (ctrl is Panel pnl)
                {
                    pnl.BackColor = Color.FromArgb(30, 30, 36);
                    pnl.Invalidate();
                }
                else if (ctrl is Button btn)
                {
                    btn.BackColor = Color.FromArgb(46, 46, 56);
                    btn.ForeColor = Color.White;
                    btn.Invalidate();
                }
                else if (ctrl is Label dl)
                {
                    if (dl.Name == "lblBoxHeader" || dl.Name == "lblCatalystTitle" ||
                        dl.Name == "lblReagent1Title" || dl.Name == "lblReagent2Title")
                    {
                        dl.ForeColor = Color.White;
                    }
                }
                return;
            }

            // Scale AutoScaleMode.None forms cleanly to native monitor DPI before swapping font
            if (ctrl is Form targetForm)
            {
                CodexThemeEngine.EnsureFormDpiScaled(targetForm);
            }

            // Apply Codex typography first (safely locking AutoScaleMode on forms)
            // Keep crisp readable GDI+ font for owner-drawn bot lists (lstCommands, etc.)
            if (!(ctrl is ListBox && IsPermanentDarkBotControl(ctrl)))
            {
                CodexThemeEngine.ApplyCodexFont(ctrl, isDenseForm);
            }

            if (ctrl is Form f)
            {
                f.BackColor = BaseColor;
                f.ForeColor = TextColor;
                CodexThemeEngine.ApplyWindowChrome(f);
                if (f.ContextMenuStrip != null)
                {
                    ApplyThemeToContextMenu(f.ContextMenuStrip);
                }
            }
            else if (ctrl is ThemedTabControl ttc)
            {
                ttc.myBackColor = BaseColor;
                ttc.BackColor = BaseColor;
                ttc.ForeColor = TextColor;
                foreach (TabPage page in ttc.TabPages)
                {
                    page.BackColor = BaseColor;
                    page.ForeColor = TextColor;
                }
                ttc.SyncTabPages();
                ttc.Invalidate();
            }
            else if (ctrl is FlatTabControl.FlatTabControl ftc)
            {
                ftc.myBackColor = BaseColor;
                ftc.BackColor = BaseColor;
                ftc.ForeColor = TextColor;
                foreach (TabPage page in ftc.TabPages)
                {
                    page.BackColor = BaseColor;
                    page.ForeColor = TextColor;
                }
                ftc.Invalidate();
            }
            else if (ctrl is TabPage tp)
            {
                tp.BackColor = BaseColor;
                tp.ForeColor = TextColor;
            }
            else if (ctrl is MenuStrip ms)
            {
                ms.BackColor = BaseColor;
                ms.ForeColor = TextColor;
                ms.Renderer = SharedMenuRenderer;
                foreach (ToolStripItem item in ms.Items)
                {
                    item.Padding = new Padding(3, 1, 3, 1);
                    ApplyThemeToMenuItem(item);
                }
                ms.Invalidate();
            }
            else if (ctrl is ContextMenuStrip cms)
            {
                ApplyThemeToContextMenu(cms);
            }
            else if (ctrl is ToolStrip ts)
            {
                ts.BackColor = BaseColor;
                ts.ForeColor = TextColor;
                ts.Renderer = SharedMenuRenderer;
                foreach (ToolStripItem item in ts.Items)
                {
                    ApplyThemeToMenuItem(item);
                }
                ts.Invalidate();
            }
            else if (ctrl is DarkGroupBox gb)
            {
                gb.BackColor = CodexStyleEnabled ? CodexThemeEngine.GetCardSurfaceColor() : BaseColor;
                gb.BorderColor = BorderColor;
                gb.ForeColor = TextColor;
                gb.Invalidate();
            }
            else if (ctrl is GroupBox sgb)
            {
                sgb.BackColor = CodexStyleEnabled ? CodexThemeEngine.GetCardSurfaceColor() : BaseColor;
                sgb.ForeColor = TextColor;
                sgb.Invalidate();
            }
            else if (ctrl is DarkTextBox dtb)
            {
                dtb.BackColor = NormalColor;
                dtb.ForeColor = TextColor;
                dtb.Invalidate();
            }
            else if (ctrl is TextBox tb)
            {
                tb.BackColor = NormalColor;
                tb.ForeColor = TextColor;
                tb.Invalidate();
            }
            else if (ctrl is DarkComboBox dcb)
            {
                dcb.BackColor = NormalColor;
                dcb.ForeColor = TextColor;
                dcb.BorderColor = BorderColor;
                dcb.ButtonColor = NormalColor;
                dcb.Invalidate();
            }
            else if (ctrl is ComboBox cbStd)
            {
                cbStd.BackColor = NormalColor;
                cbStd.ForeColor = TextColor;
                cbStd.Invalidate();
            }
            else if (ctrl is DarkNumericUpDown nud)
            {
                nud.BackColor = NormalColor;
                nud.ForeColor = TextColor;
                nud.Invalidate();
            }
            else if (ctrl is DarkButton btn)
            {
                btn.ForeColor = TextColor;
                btn.Invalidate();
            }
            else if (ctrl is Button sbtn)
            {
                if (sbtn.Name != "btnMin" && sbtn.Name != "btnMax" && sbtn.Name != "btnExit")
                {
                    sbtn.BackColor = NormalColor;
                    sbtn.ForeColor = TextColor;
                    sbtn.Invalidate();
                }
            }
            else if (ctrl is DarkCheckBox dcb2)
            {
                if (dcb2.BackColor != Color.Transparent)
                    dcb2.BackColor = CodexThemeEngine.GetParentSurfaceColor(dcb2);
                dcb2.ForeColor = TextColor;
                dcb2.Invalidate();
            }
            else if (ctrl is DarkRadioButton drb)
            {
                if (drb.BackColor != Color.Transparent)
                    drb.BackColor = CodexThemeEngine.GetParentSurfaceColor(drb);
                drb.ForeColor = TextColor;
                drb.Invalidate();
            }
            else if (ctrl is DarkLabel dl)
            {
                if (dl.Name == "lblPreviewSecondary" || dl.Name.EndsWith("Hex", StringComparison.Ordinal))
                {
                    dl.ForeColor = SecondaryTextColor;
                }
                else
                {
                    dl.ForeColor = TextColor;
                }
                if (dl.Parent is DarkGroupBox || dl.Parent is GroupBox || (dl.Parent != null && dl.Parent.Name == "pnlColors"))
                {
                    dl.BackColor = Color.Transparent;
                }
            }
            else if (ctrl is CheckBox cb)
            {
                if (IsBaseColor(cb.BackColor) || cb.Parent is GroupBox)
                    cb.BackColor = CodexThemeEngine.GetParentSurfaceColor(cb);
                cb.ForeColor = TextColor;
                cb.Invalidate();
            }
            else if (ctrl is RadioButton rb)
            {
                if (IsBaseColor(rb.BackColor) || rb.Parent is GroupBox)
                    rb.BackColor = CodexThemeEngine.GetParentSurfaceColor(rb);
                rb.ForeColor = TextColor;
                rb.Invalidate();
            }
            else if (ctrl is Label lbl)
            {
                lbl.ForeColor = TextColor;
                if (lbl.Parent is DarkGroupBox || lbl.Parent is GroupBox)
                {
                    lbl.BackColor = Color.Transparent;
                }
            }
            else if (ctrl is TrackBar trackBar)
            {
                trackBar.BackColor = CodexThemeEngine.GetParentSurfaceColor(trackBar);
                trackBar.Invalidate();
            }
            else if (ctrl is ProgressBar progressBar)
            {
                progressBar.BackColor = CodexThemeEngine.GetParentSurfaceColor(progressBar);
                progressBar.Invalidate();
            }
            else if (ctrl is ListBox lb)
            {
                if (IsPermanentDarkBotControl(lb))
                {
                    lb.BackColor = PermanentDarkBackground;
                    lb.ForeColor = PermanentDarkText;
                }
                else
                {
                    if (IsBaseColor(lb.BackColor) || lb.Parent is GroupBox)
                    {
                        lb.BackColor = CodexStyleEnabled && lb.Parent is GroupBox
                            ? CodexThemeEngine.GetCardSurfaceColor()
                            : BaseColor;
                    }
                    lb.ForeColor = TextColor;
                }
                lb.Invalidate();
            }
            else if (ctrl is DarkTreeView dtv)
            {
                if (IsPermanentDarkBotControl(dtv))
                {
                    dtv.BackColor = PermanentDarkBackground;
                    dtv.ForeColor = PermanentDarkText;
                }
                else
                {
                    if (IsBaseColor(dtv.BackColor)) dtv.BackColor = BaseColor;
                    dtv.ForeColor = TextColor;
                }
                try
                {
                    var field = typeof(DarkTreeView).GetField("_borderColor", BindingFlags.Instance | BindingFlags.NonPublic);
                    field?.SetValue(dtv, BorderColor);
                }
                catch { }
                dtv.Invalidate();
            }
            else if (ctrl is TreeView tv)
            {
                if (IsPermanentDarkBotControl(tv))
                {
                    tv.BackColor = PermanentDarkBackground;
                    tv.ForeColor = PermanentDarkText;
                }
                else
                {
                    if (IsBaseColor(tv.BackColor))
                    {
                        tv.BackColor = BaseColor;
                    }
                    tv.ForeColor = TextColor;
                }
                tv.Invalidate();
            }
            else if (ctrl is RichTextBox rtb)
            {
                if (IsPermanentDarkBotControl(rtb))
                {
                    rtb.BackColor = PermanentDarkBackground;
                    rtb.ForeColor = PermanentDarkText;
                }
                else
                {
                    if (IsBaseColor(rtb.BackColor))
                    {
                        rtb.BackColor = BaseColor;
                    }
                    rtb.ForeColor = TextColor;
                }
            }
            else if (ctrl is ListView lv)
            {
                if (IsBaseColor(lv.BackColor)) lv.BackColor = BaseColor;
                lv.ForeColor = TextColor;
                lv.Invalidate();
            }
            else if (ctrl is SplitContainer sc)
            {
                sc.BackColor = BaseColor;
                sc.Panel1.BackColor = BaseColor;
                if (IsBaseColor(sc.Panel2.BackColor)) sc.Panel2.BackColor = BaseColor;
            }
            else if (ctrl is Panel p)
            {
                if (IsPermanentDarkBotPanel(p))
                {
                    p.BackColor = CodexStyleEnabled ? BaseColor : PermanentDarkPanel;
                }
                else if (p.Name == "pnlColors")
                {
                    p.BackColor = CodexStyleEnabled ? CodexThemeEngine.GetCardSurfaceColor() : BaseColor;
                    p.Invalidate();
                }
                // Do not color preview swatch panels in UISkinForm
                else if (p.Name != "pnlBase" && p.Name != "pnlNormal" && p.Name != "pnlHover" &&
                    p.Name != "pnlPressed" && p.Name != "pnlBorder" && p.Name != "pnlText" &&
                    p.Name != "pnlSecondaryText" && p.Name != "pnlAccent" && p.Name != "gameContainer")
                {
                    if (p.Name == "pnlMenuControls" || IsBaseColor(p.BackColor))
                    {
                        p.BackColor = BaseColor;
                    }
                }
            }

            if (ctrl.ContextMenuStrip != null)
            {
                ApplyThemeToContextMenu(ctrl.ContextMenuStrip);
            }

            // Attach lightweight Codex custom painter hook to interactive controls
            CodexThemeEngine.AttachControlStyler(ctrl);

            foreach (Control child in ctrl.Controls)
            {
                ApplyThemeInternal(child, isDenseForm);
            }
        }

        private static void ApplyThemeToContextMenu(ContextMenuStrip cms)
        {
            if (cms == null || cms.IsDisposed) return;
            cms.BackColor = BaseColor;
            cms.ForeColor = TextColor;
            cms.Renderer = SharedMenuRenderer;
            foreach (ToolStripItem item in cms.Items)
            {
                ApplyThemeToMenuItem(item);
            }
            cms.Invalidate();
        }

        public static void ApplyThemeToMenuItem(ToolStripItem item)
        {
            if (item == null) return;
            item.BackColor = BaseColor;
            item.ForeColor = TextColor;
            CodexThemeEngine.ApplyCodexFontToMenuItem(item);

            if (item is ToolStripMenuItem mi)
            {
                if (mi.DropDown != null)
                {
                    mi.DropDown.BackColor = BaseColor;
                    mi.DropDown.ForeColor = TextColor;
                    mi.DropDown.Renderer = SharedMenuRenderer;
                }
                foreach (ToolStripItem sub in mi.DropDownItems)
                {
                    ApplyThemeToMenuItem(sub);
                }
            }
        }

        public static bool IsPermanentDarkBotControl(Control ctrl)
        {
            if (ctrl == null) return false;

            string name = ctrl.Name;
            if (name == "lstCommands" || name == "lstDrops" || name == "lstBoosts" ||
                name == "lstQuests" || name == "lstSkills" || name == "lstItems" ||
                name == "treeBots" || name == "lbLabels" || name == "lstLogText" ||
                name == "lstSoundItems" || name == "rtbInfo" || name == "richTextBox2")
            {
                return true;
            }

            Form form = ctrl.FindForm();
            if (form is BotManager || (form != null && form.GetType().Name == "BotManager"))
            {
                if (ctrl is ListBox || ctrl is TreeView || ctrl is RichTextBox || ctrl is DarkTreeView)
                {
                    return true;
                }
            }

            Control top = ctrl;
            while (top != null)
            {
                if (top is BotManager || top.GetType().Name == "BotManager")
                {
                    if (ctrl is ListBox || ctrl is TreeView || ctrl is RichTextBox || ctrl is DarkTreeView)
                    {
                        return true;
                    }
                    break;
                }
                top = top.Parent;
            }

            return false;
        }

        public static bool IsPermanentDarkBotPanel(Control ctrl)
        {
            if (ctrl == null) return false;
            if (ctrl.Name == "panel1")
            {
                Form form = ctrl.FindForm();
                if (form is BotManager || (form != null && form.GetType().Name == "BotManager"))
                    return true;

                Control top = ctrl;
                while (top != null)
                {
                    if (top is BotManager || top.GetType().Name == "BotManager")
                        return true;
                    top = top.Parent;
                }
            }
            return false;
        }

        public static bool IsBaseColor(Color c)
        {
            int argb = c.ToArgb();
            return argb == BaseColor.ToArgb() || KnownBaseArgb.Contains(argb);
        }

        public static Color ParseColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(hex)) return defaultColor;
            try
            {
                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int argb))
                {
                    return Color.FromArgb(argb);
                }
            }
            catch { }
            return defaultColor;
        }

        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
