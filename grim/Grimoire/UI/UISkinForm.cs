using DarkUI.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Grimoire.UI
{
    public partial class UISkinForm : GrimDarkForm
    {
        public static UISkinForm Instance = new UISkinForm();

        private Color _curBase;
        private Color _curNormal;
        private Color _curHover;
        private Color _curPressed;
        private Color _curBorder;
        private Color _curText;
        private Color _curSecondaryText;
        private Color _curAccent;
        private bool _suppressEvents = false;

        private class ThemePreset
        {
            public string Name { get; set; }
            public Color Base { get; set; }
            public Color Normal { get; set; }
            public Color Hover { get; set; }
            public Color Pressed { get; set; }
            public Color Border { get; set; }
            public Color Text { get; set; }
            public Color SecondaryText { get; set; }
            public Color Accent { get; set; }

            public override string ToString() => Name;
        }

        private readonly List<ThemePreset> _presets = new List<ThemePreset>
        {
            new ThemePreset
            {
                Name = "(Custom)",
                Base = Color.Empty,
                Normal = Color.Empty,
                Hover = Color.Empty,
                Pressed = Color.Empty,
                Border = Color.Empty,
                Text = Color.Empty,
                SecondaryText = Color.Empty,
                Accent = Color.Empty
            },
            new ThemePreset
            {
                Name = "Codex Dark (Default)",
                Base = Color.FromArgb(28, 28, 30),
                Normal = Color.FromArgb(44, 44, 46),
                Hover = Color.FromArgb(58, 58, 62),
                Pressed = Color.FromArgb(36, 36, 39),
                Border = Color.FromArgb(68, 68, 74),
                Text = Color.FromArgb(245, 245, 247),
                SecondaryText = Color.FromArgb(152, 152, 157),
                Accent = Color.FromArgb(10, 132, 255)
            },
            new ThemePreset
            {
                Name = "Classic Dark (Grimlite Li)",
                Base = Color.FromArgb(36, 36, 46),
                Normal = Color.FromArgb(46, 46, 56),
                Hover = Color.FromArgb(90, 90, 100),
                Pressed = Color.FromArgb(43, 43, 53),
                Border = Color.FromArgb(66, 66, 76),
                Text = Color.FromArgb(220, 220, 220),
                SecondaryText = Color.FromArgb(160, 160, 170),
                Accent = Color.FromArgb(10, 132, 255)
            },
            new ThemePreset
            {
                Name = "CODEX Violet",
                Base = Color.FromArgb(24, 19, 36),
                Normal = Color.FromArgb(56, 39, 82),
                Hover = Color.FromArgb(86, 60, 125),
                Pressed = Color.FromArgb(37, 26, 54),
                Border = Color.FromArgb(138, 87, 216),
                Text = Color.FromArgb(241, 233, 255),
                SecondaryText = Color.FromArgb(180, 165, 205),
                Accent = Color.FromArgb(168, 118, 255)
            },
            new ThemePreset
            {
                Name = "Cute Sakura (Classic)",
                Base = Color.FromArgb(243, 222, 240),
                Normal = Color.FromArgb(247, 168, 247),
                Hover = Color.FromArgb(245, 37, 125),
                Pressed = Color.FromArgb(239, 23, 244),
                Border = Color.FromArgb(135, 22, 135),
                Text = Color.FromArgb(64, 0, 2),
                SecondaryText = Color.FromArgb(130, 40, 70),
                Accent = Color.FromArgb(245, 37, 125)
            },
            new ThemePreset
            {
                Name = "Emerald Matrix",
                Base = Color.FromArgb(15, 30, 21),
                Normal = Color.FromArgb(29, 59, 43),
                Hover = Color.FromArgb(44, 92, 67),
                Pressed = Color.FromArgb(19, 38, 28),
                Border = Color.FromArgb(46, 139, 87),
                Text = Color.FromArgb(230, 255, 240),
                SecondaryText = Color.FromArgb(140, 185, 160),
                Accent = Color.FromArgb(48, 209, 88)
            },
            new ThemePreset
            {
                Name = "Crimson Shadow",
                Base = Color.FromArgb(34, 18, 21),
                Normal = Color.FromArgb(64, 30, 36),
                Hover = Color.FromArgb(97, 45, 54),
                Pressed = Color.FromArgb(41, 19, 23),
                Border = Color.FromArgb(197, 48, 48),
                Text = Color.FromArgb(255, 235, 235),
                SecondaryText = Color.FromArgb(195, 150, 155),
                Accent = Color.FromArgb(255, 69, 58)
            },
            new ThemePreset
            {
                Name = "Pitch Black",
                Base = Color.FromArgb(14, 14, 16),
                Normal = Color.FromArgb(27, 27, 32),
                Hover = Color.FromArgb(44, 44, 52),
                Pressed = Color.FromArgb(20, 20, 24),
                Border = Color.FromArgb(68, 68, 80),
                Text = Color.FromArgb(245, 245, 245),
                SecondaryText = Color.FromArgb(150, 150, 155),
                Accent = Color.FromArgb(10, 132, 255)
            }
        };

        public UISkinForm()
        {
            InitializeComponent();
            PopulatePresets();
            PopulateFonts();

            this.pnlColors.BorderStyle = BorderStyle.None;
            this.pnlColors.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(0.5f, 0.5f, pnlColors.Width - 1.5f, pnlColors.Height - 1.5f);
                Color border = UISkinManager.CodexStyleEnabled
                    ? CodexThemeEngine.GetSoftBorderColor(0.45f)
                    : _curBorder;
                using (GraphicsPath path = CodexThemeEngine.CreateRoundedRectPath(r, UISkinManager.CodexStyleEnabled ? 10f : 1f))
                using (Pen pen = new Pen(border, 1f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            StyleSwatchPanel(pnlBase);
            StyleSwatchPanel(pnlNormal);
            StyleSwatchPanel(pnlHover);
            StyleSwatchPanel(pnlPressed);
            StyleSwatchPanel(pnlBorder);
            StyleSwatchPanel(pnlText);
            StyleSwatchPanel(pnlSecondaryText);
            StyleSwatchPanel(pnlAccent);
        }

        private void StyleSwatchPanel(Panel swatch)
        {
            swatch.BorderStyle = BorderStyle.None;
            swatch.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color bg = CodexThemeEngine.GetParentSurfaceColor(swatch);
                using (SolidBrush outer = new SolidBrush(bg))
                    e.Graphics.FillRectangle(outer, swatch.ClientRectangle);

                RectangleF r = new RectangleF(0.5f, 0.5f, swatch.Width - 1.5f, swatch.Height - 1.5f);
                using (GraphicsPath path = CodexThemeEngine.CreateRoundedRectPath(r, 6f))
                {
                    using (SolidBrush fill = new SolidBrush(swatch.BackColor))
                        e.Graphics.FillPath(fill, path);
                    using (Pen pen = new Pen(CodexThemeEngine.GetSoftBorderColor(0.65f), 1f))
                        e.Graphics.DrawPath(pen, path);
                }
            };
        }

        private void PopulatePresets()
        {
            if (cbPresets.Items.Count > 0) return;
            _suppressEvents = true;
            cbPresets.Items.Clear();
            foreach (var preset in _presets)
            {
                cbPresets.Items.Add(preset);
            }
            _suppressEvents = false;
        }

        private void PopulateFonts()
        {
            if (cbFont.Items.Count > 0) return;
            _suppressEvents = true;
            cbFont.Items.Clear();
            foreach (string f in CodexThemeEngine.GetAvailableFontChoices())
            {
                cbFont.Items.Add(f);
            }
            _suppressEvents = false;
        }

        private void UISkinForm_Load(object sender, EventArgs e)
        {
            PopulatePresets();
            PopulateFonts();
            SyncFromActiveColors();
        }

        public void SyncFromActiveColors()
        {
            PopulatePresets();
            PopulateFonts();
            _curBase = UISkinManager.BaseColor;
            _curNormal = UISkinManager.NormalColor;
            _curHover = UISkinManager.HoverColor;
            _curPressed = UISkinManager.PressedColor;
            _curBorder = UISkinManager.BorderColor;
            _curText = UISkinManager.TextColor;
            _curSecondaryText = UISkinManager.SecondaryTextColor;
            _curAccent = UISkinManager.AccentColor;

            _suppressEvents = true;
            chkCodexStyle.Checked = UISkinManager.CodexStyleEnabled;
            int fontIdx = cbFont.FindStringExact(UISkinManager.UiFontPreference);
            cbFont.SelectedIndex = fontIdx >= 0 ? fontIdx : 0;
            _suppressEvents = false;

            DetectPreset();
            UpdateUiElements();
        }

        private void DetectPreset()
        {
            PopulatePresets();
            _suppressEvents = true;
            int matchedIndex = 0; // (Custom)
            for (int i = 1; i < _presets.Count; i++)
            {
                var p = _presets[i];
                if (ColorsEqual(p.Base, _curBase) &&
                    ColorsEqual(p.Normal, _curNormal) &&
                    ColorsEqual(p.Hover, _curHover) &&
                    ColorsEqual(p.Pressed, _curPressed) &&
                    ColorsEqual(p.Border, _curBorder) &&
                    ColorsEqual(p.Text, _curText) &&
                    ColorsEqual(p.SecondaryText, _curSecondaryText) &&
                    ColorsEqual(p.Accent, _curAccent))
                {
                    matchedIndex = i;
                    break;
                }
            }
            if (cbPresets.Items.Count > matchedIndex && matchedIndex >= 0)
            {
                cbPresets.SelectedIndex = matchedIndex;
            }
            _suppressEvents = false;
        }

        private bool ColorsEqual(Color c1, Color c2)
        {
            return c1.ToArgb() == c2.ToArgb();
        }

        private void UpdateUiElements()
        {
            pnlBase.BackColor = _curBase;
            lblBaseHex.Text = UISkinManager.ColorToHex(_curBase);
            pnlBase.Invalidate();

            pnlNormal.BackColor = _curNormal;
            lblNormalHex.Text = UISkinManager.ColorToHex(_curNormal);
            pnlNormal.Invalidate();

            pnlHover.BackColor = _curHover;
            lblHoverHex.Text = UISkinManager.ColorToHex(_curHover);
            pnlHover.Invalidate();

            pnlPressed.BackColor = _curPressed;
            lblPressedHex.Text = UISkinManager.ColorToHex(_curPressed);
            pnlPressed.Invalidate();

            pnlBorder.BackColor = _curBorder;
            lblBorderHex.Text = UISkinManager.ColorToHex(_curBorder);
            pnlBorder.Invalidate();

            pnlText.BackColor = _curText;
            lblTextHex.Text = UISkinManager.ColorToHex(_curText);
            pnlText.Invalidate();

            pnlSecondaryText.BackColor = _curSecondaryText;
            lblSecondaryTextHex.Text = UISkinManager.ColorToHex(_curSecondaryText);
            lblPreviewSecondary.ForeColor = _curSecondaryText;
            pnlSecondaryText.Invalidate();

            pnlAccent.BackColor = _curAccent;
            lblAccentHex.Text = UISkinManager.ColorToHex(_curAccent);
            pnlAccent.Invalidate();

            cbPresets.BorderColor = _curBorder;
            cbPresets.ButtonColor = _curNormal;
            cbPresets.Invalidate();
            cbFont.BorderColor = _curBorder;
            cbFont.ButtonColor = _curNormal;
            cbFont.Invalidate();
            grpPreview.BorderColor = _curBorder;
            grpPreview.Invalidate();
            pnlColors.Invalidate();
        }

        private void ApplyLive()
        {
            UpdateUiElements();
            UISkinManager.Apply(_curBase, _curNormal, _curHover, _curPressed, _curBorder, _curText, _curSecondaryText, _curAccent, true);
        }

        private void cbPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;

            if (cbPresets.SelectedItem is ThemePreset preset && preset.Name != "(Custom)")
            {
                _curBase = preset.Base;
                _curNormal = preset.Normal;
                _curHover = preset.Hover;
                _curPressed = preset.Pressed;
                _curBorder = preset.Border;
                _curText = preset.Text;
                _curSecondaryText = preset.SecondaryText;
                _curAccent = preset.Accent;

                ApplyLive();
            }
        }

        private void cbFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            string selected = cbFont.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selected))
            {
                UISkinManager.UiFontPreference = selected;
                CodexThemeEngine.FontPreference = selected;
                UISkinManager.RefreshAllForms();
            }
        }

        private void chkCodexStyle_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            UISkinManager.CodexStyleEnabled = chkCodexStyle.Checked;
            UISkinManager.RefreshAllForms();
        }

        private void PickColor(ref Color target)
        {
            colorDialog1.Color = target;
            if (colorDialog1.ShowDialog(this) == DialogResult.OK)
            {
                target = colorDialog1.Color;
                _suppressEvents = true;
                cbPresets.SelectedIndex = 0; // (Custom)
                _suppressEvents = false;
                ApplyLive();
            }
        }

        private void btnPickBase_Click(object sender, EventArgs e) => PickColor(ref _curBase);
        private void btnPickNormal_Click(object sender, EventArgs e) => PickColor(ref _curNormal);
        private void btnPickHover_Click(object sender, EventArgs e) => PickColor(ref _curHover);
        private void btnPickPressed_Click(object sender, EventArgs e) => PickColor(ref _curPressed);
        private void btnPickBorder_Click(object sender, EventArgs e) => PickColor(ref _curBorder);
        private void btnPickText_Click(object sender, EventArgs e) => PickColor(ref _curText);
        private void btnPickSecondaryText_Click(object sender, EventArgs e) => PickColor(ref _curSecondaryText);
        private void btnPickAccent_Click(object sender, EventArgs e) => PickColor(ref _curAccent);

        private void btnApply_Click(object sender, EventArgs e)
        {
            ApplyLive();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string fontPref = cbFont.SelectedItem as string ?? UISkinManager.UiFontPreference;
            UISkinManager.CodexStyleEnabled = chkCodexStyle.Checked;
            UISkinManager.Save(_curBase, _curNormal, _curHover, _curPressed, _curBorder, _curText, _curSecondaryText, _curAccent, fontPref);
            DarkMessageBox.Show(this, "Skin settings successfully saved to config!", "UI Skins", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            var result = DarkMessageBox.Show(
                this,
                "Reset UI style, font, and colors to the default Codex Dark theme?",
                "Reset UI Skin",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                UISkinManager.ResetToDefault();
                SyncFromActiveColors();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
