using System;
using System.Windows.Forms;
using DarkUI.Forms;

namespace Grimoire.UI
{
    public class GrimDarkForm : DarkForm
    {
        public GrimDarkForm()
        {
            // Reset baseline font before child InitializeComponent() runs so AutoScaleDimensions(6F, 13F)
            // scales cleanly at native DPI (~1.35x) instead of inflating ~1.67x from DarkForm's 9pt font.
            this.Font = Control.DefaultFont;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth flicker-free double buffering for all child controls
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            CodexThemeEngine.ApplyWindowChrome(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UISkinManager.ApplyThemeToControl(this);
        }
    }
}
