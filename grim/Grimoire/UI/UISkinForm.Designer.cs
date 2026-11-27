using DarkUI.Controls;
using DarkUI.Forms;
using System.Drawing;
using System.Windows.Forms;

namespace Grimoire.UI
{
    partial class UISkinForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.lblHeader = new DarkUI.Controls.DarkLabel();
            this.lblPreset = new DarkUI.Controls.DarkLabel();
            this.cbPresets = new DarkUI.Controls.DarkComboBox();
            this.lblFont = new DarkUI.Controls.DarkLabel();
            this.cbFont = new DarkUI.Controls.DarkComboBox();
            this.chkCodexStyle = new DarkUI.Controls.DarkCheckBox();
            this.pnlColors = new System.Windows.Forms.Panel();
            this.lblBase = new DarkUI.Controls.DarkLabel();
            this.pnlBase = new System.Windows.Forms.Panel();
            this.lblBaseHex = new DarkUI.Controls.DarkLabel();
            this.btnPickBase = new DarkUI.Controls.DarkButton();
            this.lblNormal = new DarkUI.Controls.DarkLabel();
            this.pnlNormal = new System.Windows.Forms.Panel();
            this.lblNormalHex = new DarkUI.Controls.DarkLabel();
            this.btnPickNormal = new DarkUI.Controls.DarkButton();
            this.lblHover = new DarkUI.Controls.DarkLabel();
            this.pnlHover = new System.Windows.Forms.Panel();
            this.lblHoverHex = new DarkUI.Controls.DarkLabel();
            this.btnPickHover = new DarkUI.Controls.DarkButton();
            this.lblPressed = new DarkUI.Controls.DarkLabel();
            this.pnlPressed = new System.Windows.Forms.Panel();
            this.lblPressedHex = new DarkUI.Controls.DarkLabel();
            this.btnPickPressed = new DarkUI.Controls.DarkButton();
            this.lblBorder = new DarkUI.Controls.DarkLabel();
            this.pnlBorder = new System.Windows.Forms.Panel();
            this.lblBorderHex = new DarkUI.Controls.DarkLabel();
            this.btnPickBorder = new DarkUI.Controls.DarkButton();
            this.lblText = new DarkUI.Controls.DarkLabel();
            this.pnlText = new System.Windows.Forms.Panel();
            this.lblTextHex = new DarkUI.Controls.DarkLabel();
            this.btnPickText = new DarkUI.Controls.DarkButton();
            this.lblSecondaryText = new DarkUI.Controls.DarkLabel();
            this.pnlSecondaryText = new System.Windows.Forms.Panel();
            this.lblSecondaryTextHex = new DarkUI.Controls.DarkLabel();
            this.btnPickSecondaryText = new DarkUI.Controls.DarkButton();
            this.lblAccent = new DarkUI.Controls.DarkLabel();
            this.pnlAccent = new System.Windows.Forms.Panel();
            this.lblAccentHex = new DarkUI.Controls.DarkLabel();
            this.btnPickAccent = new DarkUI.Controls.DarkButton();
            this.grpPreview = new DarkUI.Controls.DarkGroupBox();
            this.btnPreview1 = new DarkUI.Controls.DarkButton();
            this.btnPreview2 = new DarkUI.Controls.DarkButton();
            this.chkPreviewSwitch = new DarkUI.Controls.DarkCheckBox();
            this.tbPreviewSlider = new System.Windows.Forms.TrackBar();
            this.lblPreviewSecondary = new DarkUI.Controls.DarkLabel();
            this.btnApply = new DarkUI.Controls.DarkButton();
            this.btnSave = new DarkUI.Controls.DarkButton();
            this.btnReset = new DarkUI.Controls.DarkButton();
            this.btnClose = new DarkUI.Controls.DarkButton();
            ((System.ComponentModel.ISupportInitialize)(this.tbPreviewSlider)).BeginInit();
            this.pnlColors.SuspendLayout();
            this.grpPreview.SuspendLayout();
            this.SuspendLayout();
            // 
            // colorDialog1
            // 
            this.colorDialog1.AnyColor = true;
            this.colorDialog1.FullOpen = true;
            // 
            // lblHeader
            // 
            this.lblHeader.AutoSize = true;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
            this.lblHeader.Location = new System.Drawing.Point(12, 8);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(180, 17);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "Appearance & Custom Colors";
            this.lblHeader.UseMnemonic = false;
            // 
            // lblPreset
            // 
            this.lblPreset.AutoSize = true;
            this.lblPreset.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
            this.lblPreset.Location = new System.Drawing.Point(12, 33);
            this.lblPreset.Name = "lblPreset";
            this.lblPreset.Size = new System.Drawing.Size(42, 13);
            this.lblPreset.TabIndex = 1;
            this.lblPreset.Text = "Preset:";
            // 
            // cbPresets
            // 
            this.cbPresets.FormattingEnabled = true;
            this.cbPresets.Location = new System.Drawing.Point(70, 29);
            this.cbPresets.Name = "cbPresets";
            this.cbPresets.Size = new System.Drawing.Size(264, 21);
            this.cbPresets.TabIndex = 2;
            this.cbPresets.SelectedIndexChanged += new System.EventHandler(this.cbPresets_SelectedIndexChanged);
            // 
            // lblFont
            // 
            this.lblFont.AutoSize = true;
            this.lblFont.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
            this.lblFont.Location = new System.Drawing.Point(12, 58);
            this.lblFont.Name = "lblFont";
            this.lblFont.Size = new System.Drawing.Size(46, 13);
            this.lblFont.TabIndex = 3;
            this.lblFont.Text = "UI Font:";
            // 
            // cbFont
            // 
            this.cbFont.FormattingEnabled = true;
            this.cbFont.Location = new System.Drawing.Point(70, 54);
            this.cbFont.Name = "cbFont";
            this.cbFont.Size = new System.Drawing.Size(144, 21);
            this.cbFont.TabIndex = 4;
            this.cbFont.SelectedIndexChanged += new System.EventHandler(this.cbFont_SelectedIndexChanged);
            // 
            // chkCodexStyle
            // 
            this.chkCodexStyle.AutoSize = false;
            this.chkCodexStyle.Checked = true;
            this.chkCodexStyle.Location = new System.Drawing.Point(222, 56);
            this.chkCodexStyle.Name = "chkCodexStyle";
            this.chkCodexStyle.Size = new System.Drawing.Size(112, 18);
            this.chkCodexStyle.TabIndex = 5;
            this.chkCodexStyle.Tag = "codex-switch";
            this.chkCodexStyle.Text = "Codex Style";
            this.chkCodexStyle.CheckedChanged += new System.EventHandler(this.chkCodexStyle_CheckedChanged);
            // 
            // pnlColors
            // 
            this.pnlColors.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pnlColors.Controls.Add(this.lblBase);
            this.pnlColors.Controls.Add(this.pnlBase);
            this.pnlColors.Controls.Add(this.lblBaseHex);
            this.pnlColors.Controls.Add(this.btnPickBase);
            this.pnlColors.Controls.Add(this.lblNormal);
            this.pnlColors.Controls.Add(this.pnlNormal);
            this.pnlColors.Controls.Add(this.lblNormalHex);
            this.pnlColors.Controls.Add(this.btnPickNormal);
            this.pnlColors.Controls.Add(this.lblHover);
            this.pnlColors.Controls.Add(this.pnlHover);
            this.pnlColors.Controls.Add(this.lblHoverHex);
            this.pnlColors.Controls.Add(this.btnPickHover);
            this.pnlColors.Controls.Add(this.lblPressed);
            this.pnlColors.Controls.Add(this.pnlPressed);
            this.pnlColors.Controls.Add(this.lblPressedHex);
            this.pnlColors.Controls.Add(this.btnPickPressed);
            this.pnlColors.Controls.Add(this.lblBorder);
            this.pnlColors.Controls.Add(this.pnlBorder);
            this.pnlColors.Controls.Add(this.lblBorderHex);
            this.pnlColors.Controls.Add(this.btnPickBorder);
            this.pnlColors.Controls.Add(this.lblText);
            this.pnlColors.Controls.Add(this.pnlText);
            this.pnlColors.Controls.Add(this.lblTextHex);
            this.pnlColors.Controls.Add(this.btnPickText);
            this.pnlColors.Controls.Add(this.lblSecondaryText);
            this.pnlColors.Controls.Add(this.pnlSecondaryText);
            this.pnlColors.Controls.Add(this.lblSecondaryTextHex);
            this.pnlColors.Controls.Add(this.btnPickSecondaryText);
            this.pnlColors.Controls.Add(this.lblAccent);
            this.pnlColors.Controls.Add(this.pnlAccent);
            this.pnlColors.Controls.Add(this.lblAccentHex);
            this.pnlColors.Controls.Add(this.btnPickAccent);
            this.pnlColors.Location = new System.Drawing.Point(12, 82);
            this.pnlColors.Name = "pnlColors";
            this.pnlColors.Size = new System.Drawing.Size(322, 202);
            this.pnlColors.TabIndex = 6;
            // 
            // lblBase
            // 
            this.lblBase.AutoSize = true;
            this.lblBase.Location = new System.Drawing.Point(10, 10);
            this.lblBase.Name = "lblBase";
            this.lblBase.Size = new System.Drawing.Size(95, 13);
            this.lblBase.TabIndex = 0;
            this.lblBase.Text = "Base Background";
            // 
            // pnlBase
            // 
            this.pnlBase.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlBase.Location = new System.Drawing.Point(134, 7);
            this.pnlBase.Name = "pnlBase";
            this.pnlBase.Size = new System.Drawing.Size(34, 18);
            this.pnlBase.TabIndex = 1;
            this.pnlBase.Click += new System.EventHandler(this.btnPickBase_Click);
            // 
            // lblBaseHex
            // 
            this.lblBaseHex.AutoSize = true;
            this.lblBaseHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBaseHex.Location = new System.Drawing.Point(176, 10);
            this.lblBaseHex.Name = "lblBaseHex";
            this.lblBaseHex.Size = new System.Drawing.Size(52, 13);
            this.lblBaseHex.TabIndex = 2;
            this.lblBaseHex.Text = "#1C1C1E";
            // 
            // btnPickBase
            // 
            this.btnPickBase.Checked = false;
            this.btnPickBase.Location = new System.Drawing.Point(250, 7);
            this.btnPickBase.Name = "btnPickBase";
            this.btnPickBase.Size = new System.Drawing.Size(62, 18);
            this.btnPickBase.TabIndex = 3;
            this.btnPickBase.Text = "Pick...";
            this.btnPickBase.Click += new System.EventHandler(this.btnPickBase_Click);
            // 
            // lblNormal
            // 
            this.lblNormal.AutoSize = true;
            this.lblNormal.Location = new System.Drawing.Point(10, 34);
            this.lblNormal.Name = "lblNormal";
            this.lblNormal.Size = new System.Drawing.Size(88, 13);
            this.lblNormal.TabIndex = 4;
            this.lblNormal.Text = "Control Surface";
            // 
            // pnlNormal
            // 
            this.pnlNormal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlNormal.Location = new System.Drawing.Point(134, 31);
            this.pnlNormal.Name = "pnlNormal";
            this.pnlNormal.Size = new System.Drawing.Size(34, 18);
            this.pnlNormal.TabIndex = 5;
            this.pnlNormal.Click += new System.EventHandler(this.btnPickNormal_Click);
            // 
            // lblNormalHex
            // 
            this.lblNormalHex.AutoSize = true;
            this.lblNormalHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNormalHex.Location = new System.Drawing.Point(176, 34);
            this.lblNormalHex.Name = "lblNormalHex";
            this.lblNormalHex.Size = new System.Drawing.Size(52, 13);
            this.lblNormalHex.TabIndex = 6;
            this.lblNormalHex.Text = "#2C2C2E";
            // 
            // btnPickNormal
            // 
            this.btnPickNormal.Checked = false;
            this.btnPickNormal.Location = new System.Drawing.Point(250, 31);
            this.btnPickNormal.Name = "btnPickNormal";
            this.btnPickNormal.Size = new System.Drawing.Size(62, 18);
            this.btnPickNormal.TabIndex = 7;
            this.btnPickNormal.Text = "Pick...";
            this.btnPickNormal.Click += new System.EventHandler(this.btnPickNormal_Click);
            // 
            // lblHover
            // 
            this.lblHover.AutoSize = true;
            this.lblHover.Location = new System.Drawing.Point(10, 58);
            this.lblHover.Name = "lblHover";
            this.lblHover.Size = new System.Drawing.Size(101, 13);
            this.lblHover.TabIndex = 8;
            this.lblHover.Text = "Hover Background";
            // 
            // pnlHover
            // 
            this.pnlHover.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlHover.Location = new System.Drawing.Point(134, 55);
            this.pnlHover.Name = "pnlHover";
            this.pnlHover.Size = new System.Drawing.Size(34, 18);
            this.pnlHover.TabIndex = 9;
            this.pnlHover.Click += new System.EventHandler(this.btnPickHover_Click);
            // 
            // lblHoverHex
            // 
            this.lblHoverHex.AutoSize = true;
            this.lblHoverHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHoverHex.Location = new System.Drawing.Point(176, 58);
            this.lblHoverHex.Name = "lblHoverHex";
            this.lblHoverHex.Size = new System.Drawing.Size(52, 13);
            this.lblHoverHex.TabIndex = 10;
            this.lblHoverHex.Text = "#3A3A3E";
            // 
            // btnPickHover
            // 
            this.btnPickHover.Checked = false;
            this.btnPickHover.Location = new System.Drawing.Point(250, 55);
            this.btnPickHover.Name = "btnPickHover";
            this.btnPickHover.Size = new System.Drawing.Size(62, 18);
            this.btnPickHover.TabIndex = 11;
            this.btnPickHover.Text = "Pick...";
            this.btnPickHover.Click += new System.EventHandler(this.btnPickHover_Click);
            // 
            // lblPressed
            // 
            this.lblPressed.AutoSize = true;
            this.lblPressed.Location = new System.Drawing.Point(10, 82);
            this.lblPressed.Name = "lblPressed";
            this.lblPressed.Size = new System.Drawing.Size(109, 13);
            this.lblPressed.TabIndex = 12;
            this.lblPressed.Text = "Pressed Background";
            // 
            // pnlPressed
            // 
            this.pnlPressed.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlPressed.Location = new System.Drawing.Point(134, 79);
            this.pnlPressed.Name = "pnlPressed";
            this.pnlPressed.Size = new System.Drawing.Size(34, 18);
            this.pnlPressed.TabIndex = 13;
            this.pnlPressed.Click += new System.EventHandler(this.btnPickPressed_Click);
            // 
            // lblPressedHex
            // 
            this.lblPressedHex.AutoSize = true;
            this.lblPressedHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPressedHex.Location = new System.Drawing.Point(176, 82);
            this.lblPressedHex.Name = "lblPressedHex";
            this.lblPressedHex.Size = new System.Drawing.Size(52, 13);
            this.lblPressedHex.TabIndex = 14;
            this.lblPressedHex.Text = "#242427";
            // 
            // btnPickPressed
            // 
            this.btnPickPressed.Checked = false;
            this.btnPickPressed.Location = new System.Drawing.Point(250, 79);
            this.btnPickPressed.Name = "btnPickPressed";
            this.btnPickPressed.Size = new System.Drawing.Size(62, 18);
            this.btnPickPressed.TabIndex = 15;
            this.btnPickPressed.Text = "Pick...";
            this.btnPickPressed.Click += new System.EventHandler(this.btnPickPressed_Click);
            // 
            // lblBorder
            // 
            this.lblBorder.AutoSize = true;
            this.lblBorder.Location = new System.Drawing.Point(10, 106);
            this.lblBorder.Name = "lblBorder";
            this.lblBorder.Size = new System.Drawing.Size(75, 13);
            this.lblBorder.TabIndex = 16;
            this.lblBorder.Text = "Border Color";
            // 
            // pnlBorder
            // 
            this.pnlBorder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlBorder.Location = new System.Drawing.Point(134, 103);
            this.pnlBorder.Name = "pnlBorder";
            this.pnlBorder.Size = new System.Drawing.Size(34, 18);
            this.pnlBorder.TabIndex = 17;
            this.pnlBorder.Click += new System.EventHandler(this.btnPickBorder_Click);
            // 
            // lblBorderHex
            // 
            this.lblBorderHex.AutoSize = true;
            this.lblBorderHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBorderHex.Location = new System.Drawing.Point(176, 106);
            this.lblBorderHex.Name = "lblBorderHex";
            this.lblBorderHex.Size = new System.Drawing.Size(52, 13);
            this.lblBorderHex.TabIndex = 18;
            this.lblBorderHex.Text = "#44444A";
            // 
            // btnPickBorder
            // 
            this.btnPickBorder.Checked = false;
            this.btnPickBorder.Location = new System.Drawing.Point(250, 103);
            this.btnPickBorder.Name = "btnPickBorder";
            this.btnPickBorder.Size = new System.Drawing.Size(62, 18);
            this.btnPickBorder.TabIndex = 19;
            this.btnPickBorder.Text = "Pick...";
            this.btnPickBorder.Click += new System.EventHandler(this.btnPickBorder_Click);
            // 
            // lblText
            // 
            this.lblText.AutoSize = true;
            this.lblText.Location = new System.Drawing.Point(10, 130);
            this.lblText.Name = "lblText";
            this.lblText.Size = new System.Drawing.Size(63, 13);
            this.lblText.TabIndex = 20;
            this.lblText.Text = "Text Color";
            // 
            // pnlText
            // 
            this.pnlText.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlText.Location = new System.Drawing.Point(134, 127);
            this.pnlText.Name = "pnlText";
            this.pnlText.Size = new System.Drawing.Size(34, 18);
            this.pnlText.TabIndex = 21;
            this.pnlText.Click += new System.EventHandler(this.btnPickText_Click);
            // 
            // lblTextHex
            // 
            this.lblTextHex.AutoSize = true;
            this.lblTextHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTextHex.Location = new System.Drawing.Point(176, 130);
            this.lblTextHex.Name = "lblTextHex";
            this.lblTextHex.Size = new System.Drawing.Size(52, 13);
            this.lblTextHex.TabIndex = 22;
            this.lblTextHex.Text = "#F5F5F7";
            // 
            // btnPickText
            // 
            this.btnPickText.Checked = false;
            this.btnPickText.Location = new System.Drawing.Point(250, 127);
            this.btnPickText.Name = "btnPickText";
            this.btnPickText.Size = new System.Drawing.Size(62, 18);
            this.btnPickText.TabIndex = 23;
            this.btnPickText.Text = "Pick...";
            this.btnPickText.Click += new System.EventHandler(this.btnPickText_Click);
            // 
            // lblSecondaryText
            // 
            this.lblSecondaryText.AutoSize = true;
            this.lblSecondaryText.Location = new System.Drawing.Point(10, 154);
            this.lblSecondaryText.Name = "lblSecondaryText";
            this.lblSecondaryText.Size = new System.Drawing.Size(86, 13);
            this.lblSecondaryText.TabIndex = 24;
            this.lblSecondaryText.Text = "Secondary Text";
            // 
            // pnlSecondaryText
            // 
            this.pnlSecondaryText.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSecondaryText.Location = new System.Drawing.Point(134, 151);
            this.pnlSecondaryText.Name = "pnlSecondaryText";
            this.pnlSecondaryText.Size = new System.Drawing.Size(34, 18);
            this.pnlSecondaryText.TabIndex = 25;
            this.pnlSecondaryText.Click += new System.EventHandler(this.btnPickSecondaryText_Click);
            // 
            // lblSecondaryTextHex
            // 
            this.lblSecondaryTextHex.AutoSize = true;
            this.lblSecondaryTextHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSecondaryTextHex.Location = new System.Drawing.Point(176, 154);
            this.lblSecondaryTextHex.Name = "lblSecondaryTextHex";
            this.lblSecondaryTextHex.Size = new System.Drawing.Size(52, 13);
            this.lblSecondaryTextHex.TabIndex = 26;
            this.lblSecondaryTextHex.Text = "#98989D";
            // 
            // btnPickSecondaryText
            // 
            this.btnPickSecondaryText.Checked = false;
            this.btnPickSecondaryText.Location = new System.Drawing.Point(250, 151);
            this.btnPickSecondaryText.Name = "btnPickSecondaryText";
            this.btnPickSecondaryText.Size = new System.Drawing.Size(62, 18);
            this.btnPickSecondaryText.TabIndex = 27;
            this.btnPickSecondaryText.Text = "Pick...";
            this.btnPickSecondaryText.Click += new System.EventHandler(this.btnPickSecondaryText_Click);
            // 
            // lblAccent
            // 
            this.lblAccent.AutoSize = true;
            this.lblAccent.Location = new System.Drawing.Point(10, 178);
            this.lblAccent.Name = "lblAccent";
            this.lblAccent.Size = new System.Drawing.Size(80, 13);
            this.lblAccent.TabIndex = 28;
            this.lblAccent.Text = "Accent Color";
            // 
            // pnlAccent
            // 
            this.pnlAccent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlAccent.Location = new System.Drawing.Point(134, 175);
            this.pnlAccent.Name = "pnlAccent";
            this.pnlAccent.Size = new System.Drawing.Size(34, 18);
            this.pnlAccent.TabIndex = 29;
            this.pnlAccent.Click += new System.EventHandler(this.btnPickAccent_Click);
            // 
            // lblAccentHex
            // 
            this.lblAccentHex.AutoSize = true;
            this.lblAccentHex.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAccentHex.Location = new System.Drawing.Point(176, 178);
            this.lblAccentHex.Name = "lblAccentHex";
            this.lblAccentHex.Size = new System.Drawing.Size(52, 13);
            this.lblAccentHex.TabIndex = 30;
            this.lblAccentHex.Text = "#0A84FF";
            // 
            // btnPickAccent
            // 
            this.btnPickAccent.Checked = false;
            this.btnPickAccent.Location = new System.Drawing.Point(250, 175);
            this.btnPickAccent.Name = "btnPickAccent";
            this.btnPickAccent.Size = new System.Drawing.Size(62, 18);
            this.btnPickAccent.TabIndex = 31;
            this.btnPickAccent.Text = "Pick...";
            this.btnPickAccent.Click += new System.EventHandler(this.btnPickAccent_Click);
            // 
            // grpPreview
            // 
            this.grpPreview.Controls.Add(this.btnPreview1);
            this.grpPreview.Controls.Add(this.btnPreview2);
            this.grpPreview.Controls.Add(this.chkPreviewSwitch);
            this.grpPreview.Controls.Add(this.tbPreviewSlider);
            this.grpPreview.Controls.Add(this.lblPreviewSecondary);
            this.grpPreview.Location = new System.Drawing.Point(12, 290);
            this.grpPreview.Name = "grpPreview";
            this.grpPreview.Size = new System.Drawing.Size(322, 82);
            this.grpPreview.TabIndex = 7;
            this.grpPreview.TabStop = false;
            this.grpPreview.Text = "Live Codex Preview";
            // 
            // btnPreview1
            // 
            this.btnPreview1.Checked = false;
            this.btnPreview1.Location = new System.Drawing.Point(10, 18);
            this.btnPreview1.Name = "btnPreview1";
            this.btnPreview1.Size = new System.Drawing.Size(147, 22);
            this.btnPreview1.TabIndex = 0;
            this.btnPreview1.Text = "Normal Button";
            // 
            // btnPreview2
            // 
            this.btnPreview2.Checked = true;
            this.btnPreview2.Location = new System.Drawing.Point(165, 18);
            this.btnPreview2.Name = "btnPreview2";
            this.btnPreview2.Size = new System.Drawing.Size(147, 22);
            this.btnPreview2.TabIndex = 1;
            this.btnPreview2.Text = "Active Accent";
            // 
            // chkPreviewSwitch
            // 
            this.chkPreviewSwitch.AutoSize = false;
            this.chkPreviewSwitch.Checked = true;
            this.chkPreviewSwitch.Location = new System.Drawing.Point(10, 45);
            this.chkPreviewSwitch.Name = "chkPreviewSwitch";
            this.chkPreviewSwitch.Size = new System.Drawing.Size(88, 16);
            this.chkPreviewSwitch.TabIndex = 2;
            this.chkPreviewSwitch.Tag = "codex-switch";
            this.chkPreviewSwitch.Text = "Toggle";
            // 
            // tbPreviewSlider
            // 
            this.tbPreviewSlider.AutoSize = false;
            this.tbPreviewSlider.Location = new System.Drawing.Point(102, 43);
            this.tbPreviewSlider.Maximum = 100;
            this.tbPreviewSlider.Minimum = 0;
            this.tbPreviewSlider.Name = "tbPreviewSlider";
            this.tbPreviewSlider.Size = new System.Drawing.Size(210, 20);
            this.tbPreviewSlider.TabIndex = 3;
            this.tbPreviewSlider.TickStyle = System.Windows.Forms.TickStyle.None;
            this.tbPreviewSlider.Value = 65;
            // 
            // lblPreviewSecondary
            // 
            this.lblPreviewSecondary.AutoSize = true;
            this.lblPreviewSecondary.Location = new System.Drawing.Point(10, 64);
            this.lblPreviewSecondary.Name = "lblPreviewSecondary";
            this.lblPreviewSecondary.Size = new System.Drawing.Size(190, 13);
            this.lblPreviewSecondary.TabIndex = 4;
            this.lblPreviewSecondary.Text = "Sample secondary & placeholder text";
            this.lblPreviewSecondary.UseMnemonic = false;
            // 
            // btnApply
            // 
            this.btnApply.Checked = false;
            this.btnApply.Location = new System.Drawing.Point(12, 378);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(76, 24);
            this.btnApply.TabIndex = 8;
            this.btnApply.Text = "Apply";
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnSave
            // 
            this.btnSave.Checked = true;
            this.btnSave.Location = new System.Drawing.Point(94, 378);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(76, 24);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "Save";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnReset
            // 
            this.btnReset.Checked = false;
            this.btnReset.Location = new System.Drawing.Point(176, 378);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(76, 24);
            this.btnReset.TabIndex = 10;
            this.btnReset.Text = "Reset";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnClose
            // 
            this.btnClose.Checked = false;
            this.btnClose.Location = new System.Drawing.Point(258, 378);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(76, 24);
            this.btnClose.TabIndex = 11;
            this.btnClose.Text = "Close";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // UISkinForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(346, 410);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.lblPreset);
            this.Controls.Add(this.cbPresets);
            this.Controls.Add(this.lblFont);
            this.Controls.Add(this.cbFont);
            this.Controls.Add(this.chkCodexStyle);
            this.Controls.Add(this.pnlColors);
            this.Controls.Add(this.grpPreview);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UISkinForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "UI Skins";
            this.Load += new System.EventHandler(this.UISkinForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tbPreviewSlider)).EndInit();
            this.pnlColors.ResumeLayout(false);
            this.pnlColors.PerformLayout();
            this.grpPreview.ResumeLayout(false);
            this.grpPreview.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ColorDialog colorDialog1;
        private DarkUI.Controls.DarkLabel lblHeader;
        private DarkUI.Controls.DarkLabel lblPreset;
        private DarkUI.Controls.DarkComboBox cbPresets;
        private DarkUI.Controls.DarkLabel lblFont;
        private DarkUI.Controls.DarkComboBox cbFont;
        private DarkUI.Controls.DarkCheckBox chkCodexStyle;
        private System.Windows.Forms.Panel pnlColors;
        private DarkUI.Controls.DarkLabel lblBase;
        private System.Windows.Forms.Panel pnlBase;
        private DarkUI.Controls.DarkLabel lblBaseHex;
        private DarkUI.Controls.DarkButton btnPickBase;
        private DarkUI.Controls.DarkLabel lblNormal;
        private System.Windows.Forms.Panel pnlNormal;
        private DarkUI.Controls.DarkLabel lblNormalHex;
        private DarkUI.Controls.DarkButton btnPickNormal;
        private DarkUI.Controls.DarkLabel lblHover;
        private System.Windows.Forms.Panel pnlHover;
        private DarkUI.Controls.DarkLabel lblHoverHex;
        private DarkUI.Controls.DarkButton btnPickHover;
        private DarkUI.Controls.DarkLabel lblPressed;
        private System.Windows.Forms.Panel pnlPressed;
        private DarkUI.Controls.DarkLabel lblPressedHex;
        private DarkUI.Controls.DarkButton btnPickPressed;
        private DarkUI.Controls.DarkLabel lblBorder;
        private System.Windows.Forms.Panel pnlBorder;
        private DarkUI.Controls.DarkLabel lblBorderHex;
        private DarkUI.Controls.DarkButton btnPickBorder;
        private DarkUI.Controls.DarkLabel lblText;
        private System.Windows.Forms.Panel pnlText;
        private DarkUI.Controls.DarkLabel lblTextHex;
        private DarkUI.Controls.DarkButton btnPickText;
        private DarkUI.Controls.DarkLabel lblSecondaryText;
        private System.Windows.Forms.Panel pnlSecondaryText;
        private DarkUI.Controls.DarkLabel lblSecondaryTextHex;
        private DarkUI.Controls.DarkButton btnPickSecondaryText;
        private DarkUI.Controls.DarkLabel lblAccent;
        private System.Windows.Forms.Panel pnlAccent;
        private DarkUI.Controls.DarkLabel lblAccentHex;
        private DarkUI.Controls.DarkButton btnPickAccent;
        private DarkUI.Controls.DarkGroupBox grpPreview;
        private DarkUI.Controls.DarkButton btnPreview1;
        private DarkUI.Controls.DarkButton btnPreview2;
        private DarkUI.Controls.DarkCheckBox chkPreviewSwitch;
        private System.Windows.Forms.TrackBar tbPreviewSlider;
        private DarkUI.Controls.DarkLabel lblPreviewSecondary;
        private DarkUI.Controls.DarkButton btnApply;
        private DarkUI.Controls.DarkButton btnSave;
        private DarkUI.Controls.DarkButton btnReset;
        private DarkUI.Controls.DarkButton btnClose;
    }
}
