using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Grimoire.UI
{
    public class ThemedTabControl : FlatTabControl.FlatTabControl
    {
        private int _hoveredIndex = -1;

        public ThemedTabControl() : base()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x02000000; // WS_CLIPCHILDREN: clip child TabPage areas so parent background doesn't overwrite children
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SyncTabPages();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            SyncTabPages();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int newHover = -1;
            for (int i = 0; i < this.TabCount; i++)
            {
                if (this.GetTabRect(i).Contains(e.Location))
                {
                    newHover = i;
                    break;
                }
            }
            if (newHover != _hoveredIndex)
            {
                int oldHover = _hoveredIndex;
                _hoveredIndex = newHover;
                if (oldHover >= 0 && oldHover < this.TabCount)
                    this.Invalidate(Rectangle.Inflate(this.GetTabRect(oldHover), 4, 4));
                if (_hoveredIndex >= 0 && _hoveredIndex < this.TabCount)
                    this.Invalidate(Rectangle.Inflate(this.GetTabRect(_hoveredIndex), 4, 4));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                int oldHover = _hoveredIndex;
                _hoveredIndex = -1;
                if (oldHover >= 0 && oldHover < this.TabCount)
                    this.Invalidate(Rectangle.Inflate(this.GetTabRect(oldHover), 4, 4));
            }
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            SyncTabPages();
            Rectangle headerArea = new Rectangle(0, 0, this.Width, this.ItemSize.Height + 10);
            this.Invalidate(headerArea);
        }

        public void SyncTabPages()
        {
            if (this.TabPages.Count == 0) return;
            try
            {
                TabPage selected = this.SelectedTab;
                for (int i = 0; i < this.TabPages.Count; i++)
                {
                    TabPage page = this.TabPages[i];
                    bool isSelected = (page == selected);
                    if (page.Visible != isSelected)
                    {
                        page.Visible = isSelected;
                    }
                }
                if (selected != null)
                {
                    selected.BringToFront();
                    selected.Invalidate(true);
                }
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!this.Visible) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle clientRectangle = this.ClientRectangle;
            Rectangle displayRectangle = this.DisplayRectangle;

            Color baseColor = UISkinManager.BaseColor;
            Color borderColor = UISkinManager.CodexStyleEnabled
                ? CodexThemeEngine.GetSoftBorderColor(0.50f)
                : UISkinManager.BorderColor;
            Color textColor = UISkinManager.TextColor;
            Color secondaryColor = UISkinManager.SecondaryTextColor;
            Color normalColor = UISkinManager.NormalColor;
            Color hoverColor = UISkinManager.HoverColor;
            Color accentColor = CodexThemeEngine.GetEffectiveAccent();

            // Fill entire background
            using (SolidBrush brush = new SolidBrush(baseColor))
            {
                g.FillRectangle(brush, clientRectangle);
            }

            if (UISkinManager.CodexStyleEnabled)
            {
                // Draw sleek rounded content container
                RectangleF contentRect = new RectangleF(
                    Math.Max(0.5f, displayRectangle.Left - 2f),
                    Math.Max(0.5f, displayRectangle.Top - 1.5f),
                    Math.Min(clientRectangle.Width - 1.5f, displayRectangle.Width + 3.5f),
                    Math.Min(clientRectangle.Height - displayRectangle.Top + 0.5f, displayRectangle.Height + 2.5f));

                using (GraphicsPath contentPath = CodexThemeEngine.CreateRoundedRectPath(contentRect, 8f))
                using (Pen borderPen = new Pen(borderColor, 1f))
                {
                    g.DrawPath(borderPen, contentPath);
                }

                // Draw Codex Segmented Control pill track across the tab row
                if (this.TabCount > 0)
                {
                    Rectangle firstTab = this.GetTabRect(0);
                    Rectangle lastTab = this.GetTabRect(this.TabCount - 1);
                    int minLeft = firstTab.Left;
                    int maxRight = lastTab.Right;
                    int minTop = firstTab.Top;
                    int maxBottom = firstTab.Bottom;
                    for (int i = 1; i < this.TabCount; i++)
                    {
                        Rectangle tr = this.GetTabRect(i);
                        if (tr.Left < minLeft) minLeft = tr.Left;
                        if (tr.Right > maxRight) maxRight = tr.Right;
                        if (tr.Top < minTop) minTop = tr.Top;
                        if (tr.Bottom > maxBottom) maxBottom = tr.Bottom;
                    }

                    RectangleF trackRect = new RectangleF(
                        Math.Max(1f, minLeft - 1f),
                        Math.Max(1f, minTop),
                        Math.Min(clientRectangle.Width - 2.5f, (maxRight - minLeft) + 2f),
                        Math.Max(16f, (maxBottom - minTop) - 1f));

                    Color trackColor = CodexThemeEngine.IsDarkColor(baseColor)
                        ? CodexThemeEngine.Blend(baseColor, Color.Black, 0.24f)
                        : CodexThemeEngine.Blend(baseColor, UISkinManager.BorderColor, 0.16f);

                    using (GraphicsPath trackPath = CodexThemeEngine.CreateRoundedRectPath(trackRect, 7f))
                    {
                        using (SolidBrush tb = new SolidBrush(trackColor))
                            g.FillPath(tb, trackPath);
                        using (Pen tp = new Pen(CodexThemeEngine.GetSoftBorderColor(0.32f), 1f))
                            g.DrawPath(tp, trackPath);
                    }
                }

                // Draw each segmented pill tab
                for (int i = 0; i < this.TabCount; i++)
                {
                    DrawCodexSegmentedTab(g, this.TabPages[i], i, baseColor, borderColor, textColor, secondaryColor, normalColor, hoverColor, accentColor);
                }
                return;
            }

            // Classic fallback path
            using (Pen pen = new Pen(borderColor))
            {
                Rectangle r = displayRectangle;
                r.Inflate(SystemInformation.Border3DSize.Width, SystemInformation.Border3DSize.Width);
                g.DrawRectangle(pen, r);
            }

            for (int i = 0; i < this.TabCount; i++)
            {
                DrawClassicTab(g, this.TabPages[i], i, baseColor, borderColor, textColor, normalColor, hoverColor);
            }
        }

        private void DrawCodexSegmentedTab(
            Graphics g, TabPage tabPage, int nIndex,
            Color baseColor, Color borderColor, Color textColor, Color secondaryColor,
            Color normalColor, Color hoverColor, Color accentColor)
        {
            Rectangle tabRect = this.GetTabRect(nIndex);
            if (tabRect.Width <= 0 || tabRect.Height <= 0) return;

            bool isSelected = (this.SelectedIndex == nIndex);
            bool isHovered = (nIndex == _hoveredIndex && !isSelected);

            RectangleF pillRect = new RectangleF(tabRect.X + 1.5f, tabRect.Y + 2f, Math.Max(4f, tabRect.Width - 3f), Math.Max(12f, tabRect.Height - 5f));

            if (isSelected)
            {
                Color pillFill = CodexThemeEngine.IsDarkColor(baseColor)
                    ? CodexThemeEngine.Blend(normalColor, Color.White, 0.08f)
                    : Color.White;
                // Keep custom tint on vivid light themes like Cute Sakura
                if (!CodexThemeEngine.IsDarkColor(baseColor))
                {
                    pillFill = CodexThemeEngine.Blend(baseColor, Color.White, 0.72f);
                }

                using (GraphicsPath pillPath = CodexThemeEngine.CreateRoundedRectPath(pillRect, 5.5f))
                {
                    using (SolidBrush fb = new SolidBrush(pillFill))
                        g.FillPath(fb, pillPath);
                    using (Pen bp = new Pen(CodexThemeEngine.GetSoftBorderColor(0.55f), 1f))
                        g.DrawPath(bp, pillPath);
                }

                // Subtle Codex accent indicator bar at bottom of active pill
                float barW = Math.Min(pillRect.Width - 10f, Math.Max(12f, pillRect.Width * 0.45f));
                if (barW > 6f)
                {
                    RectangleF barRect = new RectangleF(
                        pillRect.X + (pillRect.Width - barW) / 2f,
                        pillRect.Bottom - 2.2f,
                        barW,
                        1.8f);
                    using (GraphicsPath barPath = CodexThemeEngine.CreateRoundedRectPath(barRect, 1f))
                    using (SolidBrush ab = new SolidBrush(accentColor))
                    {
                        g.FillPath(ab, barPath);
                    }
                }
            }
            else if (isHovered)
            {
                Color hoverPill = CodexThemeEngine.Blend(baseColor, hoverColor, 0.45f);
                using (GraphicsPath pillPath = CodexThemeEngine.CreateRoundedRectPath(pillRect, 5.5f))
                using (SolidBrush hb = new SolidBrush(hoverPill))
                {
                    g.FillPath(hb, pillPath);
                }
            }

            int textXOffset = 0;
            if (this.ImageList != null && tabPage.ImageIndex >= 0 && tabPage.ImageIndex < this.ImageList.Images.Count)
            {
                Image img = this.ImageList.Images[tabPage.ImageIndex];
                int imgY = tabRect.Y + (tabRect.Height - img.Height) / 2;
                g.DrawImage(img, tabRect.X + 4, imgY, img.Width, img.Height);
                textXOffset = img.Width + 4;
            }

            Rectangle textRect = new Rectangle(tabRect.X + textXOffset + 1, tabRect.Y + 1, Math.Max(0, tabRect.Width - textXOffset - 2), tabRect.Height - 2);
            Color currentText = isSelected
                ? textColor
                : (isHovered ? textColor : CodexThemeEngine.Blend(textColor, secondaryColor, 0.42f));

            float fontSize = this.Font != null ? Math.Min(this.Font.Size, 8.25f) : 8.25f;
            Font tabFont = CodexThemeEngine.GetCachedFont(fontSize, isSelected ? FontStyle.Bold : FontStyle.Regular, false);

            TextRenderer.DrawText(g, tabPage.Text, tabFont, textRect, currentText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        private void DrawClassicTab(Graphics g, TabPage tabPage, int nIndex, Color baseColor, Color borderColor, Color textColor, Color normalColor, Color hoverColor)
        {
            Rectangle tabRect = this.GetTabRect(nIndex);
            if (tabRect.Width <= 0 || tabRect.Height <= 0) return;

            bool isSelected = (this.SelectedIndex == nIndex);
            bool isHovered = (nIndex == _hoveredIndex && !isSelected);

            Color tabBg = isSelected ? baseColor : (isHovered ? hoverColor : normalColor);

            using (SolidBrush brush = new SolidBrush(tabBg))
                g.FillRectangle(brush, tabRect);
            using (Pen pen = new Pen(borderColor))
                g.DrawRectangle(pen, tabRect);

            Rectangle textRect = new Rectangle(tabRect.X + 2, tabRect.Y, Math.Max(0, tabRect.Width - 4), tabRect.Height);
            Font tabFont = CodexThemeEngine.GetCachedFont(this.Font != null ? this.Font.Size : 8.25f, isSelected ? FontStyle.Bold : FontStyle.Regular, false);
            TextRenderer.DrawText(g, tabPage.Text, tabFont, textRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
    }
}
