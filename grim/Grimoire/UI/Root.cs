using Grimoire.Game;
using Grimoire.Networking;
using Grimoire.Tools;
using Grimoire.Tools.FlashBridge;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grimoire.Botting;
using Grimoire.Game.Data;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using DarkUI.Controls;
using System.Reflection;
using System.Linq;
using Grimoire.FlashTools;
using Grimoire.Utils;
using DarkUI.Renderers;
using DarkUI.Config;

namespace Grimoire.UI
{
    public class Root : Form
    {
        private IContainer components;

        private NotifyIcon nTray;
        private ToolStripMenuItem startToolStripMenuItem;
        private ToolStripMenuItem stopToolStripMenuItem;
        private Button btnMax;
        private Button btnExit;
        private Button btnMin;
        private ToolStripMenuItem managerToolStripMenuItem;
        private ToolStripMenuItem loadBotToolStripMenuItem;
        private Panel panel1;
        private Panel pnlMenuControls;
        private List<Form> _visibleFormsBeforeHide = new List<Form>();
        public MenuStrip MenuMain;
        private DarkComboBox cbAutoAttackMode;
        private DarkComboBox cbPads;
        private DarkComboBox cbCells;
        private DarkButton btnBank;
        internal DarkCheckBox chkAutoAttack;
        public DarkCheckBox chkStartBot;
        private DarkButton btnGetCell;
        public SplitContainer splitContainer1;
        public DarkMenuStrip darkMenuStrip1;
        private ToolStripMenuItem botToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem fastTravelsToolStripMenuItem;
        private ToolStripMenuItem loadersgrabbersToolStripMenuItem;
        private ToolStripMenuItem hotkeysToolStripMenuItem;
        private ToolStripMenuItem pluginManagerToolStripMenuItem;
        private ToolStripMenuItem cosmeticsToolStripMenuItem;
        private ToolStripMenuItem bankToolStripMenuItem;
        private ToolStripMenuItem setsToolStripMenuItem;
        private ToolStripMenuItem eyeDropperToolStripMenuItem;
        private ToolStripMenuItem logsToolStripMenuItem1;
        private ToolStripMenuItem notepadToolStripMenuItem1;
        private ToolStripMenuItem changeServerMenuItem;
        public ToolStripComboBox changeServerList;
        private ToolStripMenuItem DPSMeterToolStripMenuItem;
        private ToolStripMenuItem packetsToolStripMenuItem;
        private ToolStripMenuItem snifferToolStripMenuItem;
        private ToolStripMenuItem spammerToolStripMenuItem;
        private ToolStripMenuItem tampererToolStripMenuItem;
        public ToolStripMenuItem optionsToolStripMenuItem;
        public ToolStripMenuItem infRangeToolStripMenuItem;
        public ToolStripMenuItem provokeToolStripMenuItem1;
        public ToolStripMenuItem enemyMagnetToolStripMenuItem;
        public ToolStripMenuItem lagKillerToolStripMenuItem;
        public ToolStripMenuItem hidePlayersToolStripMenuItem;
        public ToolStripMenuItem skipCutscenesToolStripMenuItem;
        public ToolStripMenuItem disableAnimationsToolStripMenuItem;
        public ToolStripMenuItem pluginsStrip;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem pvptoolStripMenuItem1;
        private Panel gameContainer;

        public static Root Instance
        {
            get;
            private set;
        }

        public IFlashBridge FlashBridge { get; private set; }
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem devTestToolStripMenuItem;
        public ToolStripMenuItem walkspeedToolStripMenuItem;
        private ToolStripTextBox toolStripTextBox1;
        public ToolStripMenuItem enableOptionsToolStripMenuItem;
        private ToolStripMenuItem menuCharSelect;
        public ToolStripMenuItem maidStrip;
        public ToolStripMenuItem codexStrip;
        private ToolStripMenuItem dropUiToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem voiceChatMenuItem;
        private System.Windows.Forms.ToolStripMenuItem skillMomentMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loopEnrageMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bypassMapMenuItem;
        private System.Windows.Forms.ToolStripMenuItem questPinnerMenuItem;
        private System.Windows.Forms.ToolStripMenuItem alchemistMenuItem;
        private System.Windows.Forms.ToolStripMenuItem appDetailMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateAppMenuItem;
        private System.Windows.Forms.ToolStripMenuItem syncScriptMenuItem;
        private System.Windows.Forms.ToolStripMenuItem uiSkinsMenuItem;

        //public AxShockwaveFlash Client => flashPlayer;

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        private static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        private string title = $"GrimCodex Ruffle v{CodexBankManager.CURRENT_APP_VERSION}";

        public Root()
        {
            UISkinManager.Init();
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
            InitializeComponent();
            Instance = this;
            this.Text = title;
            UISkinManager.ApplyThemeToControl(this);
            LayoutToolbarControls();
            ApplyInitialWindowSize();
            this.WindowState = FormWindowState.Normal;
            this.CenterToScreen();
        }

        private void ApplyInitialWindowSize()
        {
            try
            {
                Rectangle work = Screen.FromHandle(this.Handle).WorkingArea;
                float dpi = CodexThemeEngine.GetDpiScale(this);
                int barH = splitContainer1 != null ? splitContainer1.Height : (int)Math.Round(30f * dpi);

                // Measure top-level menu items using Available (Visible is false before Show())
                int leftMenuW = (int)Math.Round(10f * dpi);
                if (darkMenuStrip1 != null)
                {
                    foreach (ToolStripItem item in darkMenuStrip1.Items)
                    {
                        if (item.Available)
                            leftMenuW += item.GetPreferredSize(Size.Empty).Width;
                    }
                }
                int rightCtrlW = pnlMenuControls != null ? pnlMenuControls.Width : (int)Math.Round(380f * dpi);
                int minToolbarW = leftMenuW + rightCtrlW + (int)Math.Round(14f * dpi);

                // Compact windowed default: 960px at 100% DPI, gently scaled at high DPI without filling the screen
                int desiredW = (int)Math.Round(960f * Math.Min(dpi, 1.12f));
                int maxW = Math.Max(640, (int)Math.Round(work.Width * 0.72));
                int clientW = Math.Min(maxW, Math.Max(desiredW, minToolbarW));

                // Preserve AQW's native 960:550 stage ratio + top bar height, clamped to comfortable screen height
                int gameH = (int)Math.Round(clientW * (550.0 / 960.0));
                int maxH = Math.Max(420, (int)Math.Round(work.Height * 0.74));
                int clientH = barH + gameH;
                if (clientH > maxH)
                {
                    clientH = maxH;
                    int adjustedGameH = Math.Max(240, clientH - barH);
                    clientW = Math.Min(maxW, Math.Max(minToolbarW, (int)Math.Round(adjustedGameH * (960.0 / 550.0))));
                }

                this.ClientSize = new Size(clientW, clientH);
            }
            catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            CodexThemeEngine.ApplyWindowChrome(this);
            LayoutToolbarControls();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            LayoutToolbarControls();
        }

        public void LayoutToolbarControls()
        {
            if (splitContainer1 == null || pnlMenuControls == null || chkStartBot == null)
                return;

            try
            {
                float dpi = CodexThemeEngine.GetDpiScale(this);

                // Match buttons to the real Win32 ComboBox height so every control aligns pixel-for-pixel vertically
                int comboH = cbAutoAttackMode.Height;
                if (comboH < 18) comboH = (int)Math.Round(21f * dpi);

                int barH = Math.Max(comboH + (int)Math.Round(8f * dpi), (int)Math.Round(30f * dpi));
                int ctrlY = (barH - comboH) / 2;
                int gap = Math.Max(2, (int)Math.Round(3f * dpi));
                int tightGap = Math.Max(2, (int)Math.Round(2f * dpi));

                splitContainer1.Height = barH;
                pnlMenuControls.Height = barH;

                // Measure "Start Bot" dynamically so there is no dead whitespace to the right of the label
                int startBotH = Math.Min(comboH, Math.Max(16, (int)Math.Round(18f * dpi)));
                int swH = Math.Min(startBotH - 2, Math.Max(12, (int)Math.Round(14f * dpi)));
                int swW = Math.Max((int)Math.Round(swH * 1.82f), (int)Math.Round(25f * dpi));
                int textW = TextRenderer.MeasureText(chkStartBot.Text, chkStartBot.Font, new Size(int.MaxValue, startBotH),
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                int startBotW = swW + (int)Math.Round(6f * dpi) + textW + (int)Math.Round(2f * dpi);

                int x = gap;
                chkStartBot.SetBounds(x, (barH - startBotH) / 2, startBotW, startBotH);
                x = chkStartBot.Right + gap;

                int chkAA = Math.Min(comboH - 2, Math.Max(14, (int)Math.Round(15f * dpi)));
                chkAutoAttack.SetBounds(x, (barH - chkAA) / 2, chkAA, chkAA);
                x = chkAutoAttack.Right + tightGap;

                int aaTextW = TextRenderer.MeasureText("Auto and Pot", cbAutoAttackMode.Font, new Size(int.MaxValue, comboH),
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                int aaW = Math.Max((int)Math.Round(78f * dpi), aaTextW + (int)Math.Round(18f * dpi));
                cbAutoAttackMode.SetBounds(x, ctrlY, aaW, comboH);
                x = cbAutoAttackMode.Right + tightGap;

                int padW = (int)Math.Round(50f * dpi);
                cbPads.SetBounds(x, ctrlY, padW, comboH);
                x = cbPads.Right + tightGap;

                int cellW = (int)Math.Round(50f * dpi);
                cbCells.SetBounds(x, ctrlY, cellW, comboH);
                x = cbCells.Right + tightGap;

                // Exact same height and top Y as the ComboBoxes for razor-clean horizontal alignment
                btnGetCell.SetBounds(x, ctrlY, comboH, comboH);
                x = btnGetCell.Right + tightGap;

                int bankTextW = TextRenderer.MeasureText(btnBank.Text, btnBank.Font, new Size(int.MaxValue, comboH),
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                int bankW = Math.Max((int)Math.Round(36f * dpi), bankTextW + (int)Math.Round(10f * dpi));
                btnBank.SetBounds(x, ctrlY, bankW, comboH);
                x = btnBank.Right + gap;

                pnlMenuControls.Width = x;
                if (darkMenuStrip1 != null)
                {
                    int vPad = Math.Max(2, (barH - comboH) / 2);
                    darkMenuStrip1.Padding = new Padding((int)Math.Round(3f * dpi), vPad, 2, vPad);
                    int itemHPad = Math.Max(2, (int)Math.Round(2f * dpi));
                    foreach (ToolStripItem item in darkMenuStrip1.Items)
                    {
                        item.Padding = new Padding(itemHPad, 0, itemHPad, 0);
                    }
                }
            }
            catch { }
        }

        private void Root_Load(object sender, EventArgs e)
        {
            UISkinManager.ApplyThemeToControl(this);
            LayoutToolbarControls();
            ApplyInitialWindowSize();
            this.WindowState = FormWindowState.Normal;
            this.CenterToScreen();
            //Task.Factory.StartNew(Proxy.Instance.Start, TaskCreationOptions.LongRunning);
            Flash.FlashError += (flashCtrl, ex, func, args) => {
                try
                {
                    LogForm.Instance.AppendDebug($"[FlashError] Function: {func}, Error: {ex.Message}", true, true);
                }
                catch {}
            };
            InitFlashMovie(ClientConfig.GetValue(ClientConfig.C_FLASH));
            Hotkeys.Instance.LoadHotkeys();
            LoadPlugins();
            LoadCharSelect();
            UISkinManager.ApplyThemeToControl(this);
            LayoutToolbarControls();
            //auraClearer();

            // Periodic memory optimizer to clear Flash ActiveX leaks
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(3));
                    try
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
                        }
                    }
                    catch { }
                }
            });

            // Highlight About & Update App in yellow if an update is available
            CodexBankManager.Instance.UpdateAvailabilityChanged += isAvail =>
            {
                SetUpdateNotification(isAvail);
            };

            // Silent background update check loop (starts 4s after launch, checks every 30m)
            Task.Run(async () =>
            {
                await Task.Delay(4000);
                while (!this.IsDisposed)
                {
                    try
                    {
                        await CodexBankManager.Instance.FetchManifestAsync();
                    }
                    catch { }
                    await Task.Delay(TimeSpan.FromMinutes(30));
                }
            });
        }

        public bool IsAppUpdateAvailable { get; private set; } = false;

        public void SetUpdateNotification(bool available)
        {
            IsAppUpdateAvailable = available;
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                try
                {
                    this.BeginInvoke(new Action(() => SetUpdateNotification(available)));
                }
                catch { }
                return;
            }

            if (available)
            {
                Color highlight = Color.Gold;
                aboutToolStripMenuItem.ForeColor = highlight;
                updateAppMenuItem.ForeColor = highlight;
            }
            else
            {
                Color defaultColor = UISkinManager.TextColor;
                aboutToolStripMenuItem.ForeColor = defaultColor;
                updateAppMenuItem.ForeColor = defaultColor;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        private void LoadPlugins()
        {
            Program.PluginsManager.LoadRange(Directory.GetFiles(Program.PluginsPath));
        }

        class CharInfo
        {
            public string Username;
            public string Password;
        }

        private List<CharInfo> chars = new List<CharInfo>();
        public void LoadCharSelect()
        {
            if (this.InvokeRequired && this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(LoadCharSelect));
                return;
            }
            menuCharSelect.Visible = true;
            if (chars.Count > 0) return;
            Config config = Config.Load(Application.StartupPath + "\\CharSelect.cfg");
            int i = 0;
            while (i >= 0)
            {
                try
                {
                    string data = config.Get(i.ToString());
                    if (data == null)
                    {
                        i = -1;
                        return;
                    }
                    CharInfo charInfo = new CharInfo
                    {
                        Username = data.Split(',')[0],
                        Password = data.Split(',')[1]
                    };
                    chars.Add(charInfo);
                    menuCharSelect.DropDownItems.Add(charInfo.Username);
                    menuCharSelect.DropDownItems[i].Click -= new EventHandler(menuCharSelectItem_Click);
                    menuCharSelect.DropDownItems[i].Click += new EventHandler(menuCharSelectItem_Click);
                }
                catch
                {
                    i = -1;
                }
                i++;
            }
        }

        public void HideCharSelect()
        {
            if (this.InvokeRequired && this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(HideCharSelect));
                return;
            }
            menuCharSelect.Visible = false;
        }

        private void menuCharSelectItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripItem;
            var menuText = menuItem.Text;
            foreach (var item in chars)
            {
                if (item.Username == menuText)
                    AutoRelogin.Login(item.Username, item.Password);
            }
        }

        private void Root_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (VoiceChatManager.Instance.isRunning)
                {
                    VoiceChatManager.Instance.StopVoice();
                }
            }
            catch {}

            try
            {
                FlashBridge?.Dispose();
                FlashBridge = null;
                Flash.Bridge = null;
            }
            catch { }

            Hotkeys.InstalledHotkeys.ForEach(delegate (Hotkey h)
            {
                h.Uninstall();
            });
            SaveRecentMapItemMerged("MapItemLog.txt");
            KeyboardHook.Instance.Dispose();
            Proxy.Instance.Stop(appClosing: true);
            CommandColorForm.Instance.Dispose();
            nTray.Visible = false;
            nTray.Icon.Dispose();
            nTray.Dispose();
        }

        public void InitFlashMovie(string gameSwf)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string swfPath = null;
            if (!string.IsNullOrEmpty(gameSwf))
            {
                try
                {
                    string rel = gameSwf.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    swfPath = Path.Combine(exeDir, rel);
                }
                catch { swfPath = null; }
            }

            if (string.IsNullOrEmpty(gameSwf) || string.IsNullOrEmpty(swfPath) || !File.Exists(swfPath))
            {
                try
                {
                    LogForm.Instance.AppendDebug("InitFlashMovie: flash path is not configured (null or empty). Showing placeholder.\r\n");
                }
                catch
                {
                }

                // Clear any existing controls and show a friendly message in the game container
                gameContainer.Controls.Clear();
                Label msg = new Label();
                msg.AutoSize = false;
                msg.Dock = DockStyle.Fill;
                msg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                msg.ForeColor = System.Drawing.Color.LightGray;
                msg.BackColor = System.Drawing.Color.FromArgb(30, 30, 36);
                msg.Text = "Game SWF not configured.\n\nPlace your game SWF at 'Loader\\grimoire.swf' relative to the exe,\nor add 'flash=Loader\\grimoire.swf' to ClientConfig.cfg.";
                msg.Font = new System.Drawing.Font(msg.Font.FontFamily, 12f);
                msg.Padding = new Padding(20);

                // Add a button allowing the user to pick a SWF now
                FlowLayoutPanel fl = new FlowLayoutPanel();
                fl.Dock = DockStyle.Fill;
                fl.FlowDirection = FlowDirection.TopDown;
                fl.WrapContents = false;
                fl.AutoScroll = true;
                fl.Padding = new Padding(20);
                fl.BackColor = gameContainer.BackColor;

                Button chooseBtn = new Button();
                chooseBtn.Text = "Choose SWF...";
                chooseBtn.AutoSize = true;
                chooseBtn.Padding = new Padding(8);
                chooseBtn.Anchor = AnchorStyles.None;

                chooseBtn.Click += (s, ev) =>
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "Flash files (*.swf)|*.swf|All files (*.*)|*.*";
                        ofd.Title = "Select Game SWF";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                string exeDirLocal = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                                string loaderDir = Path.Combine(exeDirLocal, "Loader");
                                Directory.CreateDirectory(loaderDir);
                                string dest = Path.Combine(loaderDir, "grimoire.swf");
                                File.Copy(ofd.FileName, dest, true);
                                ClientConfig.SetValue(ClientConfig.C_FLASH, "Loader\\grimoire.swf");
                                // Reload using the newly set path
                                InitFlashMovie("Loader\\grimoire.swf");
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    LogForm.Instance.AppendDebug("InitFlashMovie: failed to set SWF: " + ex.Message + "\r\n");
                                }
                                catch { }
                                MessageBox.Show("Failed to set SWF: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };

                fl.Controls.Add(msg);
                fl.SetFlowBreak(msg, true);
                fl.Controls.Add(chooseBtn);

                gameContainer.Controls.Add(fl);

                return;
            }
            try
            {
                FlashBridge?.Dispose();
                FlashBridge = null;
                Flash.Bridge = null;
            }
            catch { }

            Flash.SwfLoadProgress -= OnLoadProgress;
            Flash.SwfLoadProgress += OnLoadProgress;

            IFlashBridge bridge = new RuffleFlashBridge();
            bridge.ExternalCallReceived += Flash.HandleExternalCall;
            Flash.Bridge = bridge;
            FlashBridge = bridge;

            try
            {
                bridge.LoadSwf(swfPath, gameContainer);
            }
            catch (Exception ex)
            {
                try
                {
                    LogForm.Instance.AppendDebug($"[Ruffle] Failed to start engine: {ex.Message}\r\n");
                }
                catch { }
                MessageBox.Show(
                    $"Failed to start GrimCodex Ruffle engine:\n\n{ex.Message}",
                    "GrimCodex Ruffle",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                string quality = ClientConfig.GetValue(ClientConfig.C_QUALITY);
                if (string.IsNullOrEmpty(quality))
                {
                    quality = "High";
                    ClientConfig.SetValue(ClientConfig.C_QUALITY, quality);
                }
                bridge.SetQuality(quality);
            }
            catch (Exception ex)
            {
                try
                {
                    LogForm.Instance.AppendDebug($"[Quality] Failed to set rendering quality ({ClientConfig.GetValue(ClientConfig.C_QUALITY)}): {ex.Message}\r\n");
                }
                catch { }
            }
        }

        private void OnLoadProgress(int progress)
        {
            if (progress < 100) return;
            Flash.SwfLoadProgress -= OnLoadProgress;
        }

        public BotManager botManager = BotManager.Instance;

        private void botToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShowForm(BotManager.Instance);
        }

        private void fastTravelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Travel.Instance);
        }

        private void loadersgrabbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Loaders.Instance);
        }

        private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Hotkeys.Instance);
        }

        private void pluginManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PluginManager.Instance);
        }

        private void snifferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PacketLogger.Instance);
        }

        private void spammerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PacketSpammer.Instance);
        }

        private void tampererToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PacketTamperer.Instance);
        }

        public void ShowForm(Form form)
        {
            if (form == this)
            {
                if (this.Visible)
                {
                    _visibleFormsBeforeHide.Clear();
                    List<Form> toHide = new List<Form>();
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f != this && f.Visible)
                        {
                            toHide.Add(f);
                        }
                    }
                    foreach (Form f in toHide)
                    {
                        f.Hide();
                        _visibleFormsBeforeHide.Add(f);
                    }
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    this.BringToFront();
                    this.Focus();
                    foreach (Form f in _visibleFormsBeforeHide)
                    {
                        if (f != null && !f.IsDisposed)
                        {
                            f.Show();
                            f.BringToFront();
                        }
                    }
                    _visibleFormsBeforeHide.Clear();
                }
                return;
            }

            if (form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
                UISkinManager.ApplyThemeToControl(form);
                form.Show();
                form.BringToFront();
                form.Focus();
                return;
            }
            else if (form.Visible)
            {
                form.Hide();
                return;
            }
            UISkinManager.ApplyThemeToControl(form);
            form.Show();
            form.BringToFront();
            form.Focus();
        }

        private void cbCells_Click(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn) return;
            cbCells.Items.Clear();
            ComboBox.ObjectCollection items = cbCells.Items;
            object[] cells = World.Cells;
            object[] items2 = cells;
            items.AddRange(items2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Root));
            this.nTray = new System.Windows.Forms.NotifyIcon(this.components);
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.managerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadBotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnMax = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnMin = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pnlMenuControls = new System.Windows.Forms.Panel();
            this.MenuMain = new System.Windows.Forms.MenuStrip();
            this.gameContainer = new System.Windows.Forms.Panel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.btnGetCell = new DarkUI.Controls.DarkButton();
            this.chkStartBot = new DarkUI.Controls.DarkCheckBox();
            this.chkAutoAttack = new DarkUI.Controls.DarkCheckBox();
            this.cbAutoAttackMode = new DarkUI.Controls.DarkComboBox();
            this.btnBank = new DarkUI.Controls.DarkButton();
            this.cbCells = new DarkUI.Controls.DarkComboBox();
            this.cbPads = new DarkUI.Controls.DarkComboBox();
            this.darkMenuStrip1 = new DarkUI.Controls.DarkMenuStrip();
            this.botToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fastTravelsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadersgrabbersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hotkeysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cosmeticsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bankToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.eyeDropperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.notepadToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.changeServerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeServerList = new System.Windows.Forms.ToolStripComboBox();
            this.DPSMeterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pvptoolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.devTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dropUiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packetsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.snifferToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spammerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tampererToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.infRangeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.provokeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.enemyMagnetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lagKillerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hidePlayersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.skipCutscenesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disableAnimationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.walkspeedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.enableOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.uiSkinsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.maidStrip = new System.Windows.Forms.ToolStripMenuItem();
            this.codexStrip = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginsStrip = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCharSelect = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.darkMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // nTray
            // 
            this.nTray.Icon = ((System.Drawing.Icon)(resources.GetObject("nTray.Icon")));
            this.nTray.Text = "GrimLite";
            this.nTray.Visible = true;
            this.nTray.MouseClick += new System.Windows.Forms.MouseEventHandler(this.nTray_MouseClick);
            // 
            // startToolStripMenuItem
            // 
            this.startToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.startToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.startToolStripMenuItem.Name = "startToolStripMenuItem";
            this.startToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.startToolStripMenuItem.Text = "Start";
            this.startToolStripMenuItem.Click += new System.EventHandler(this.startToolStripMenuItem_Click);
            // 
            // stopToolStripMenuItem
            // 
            this.stopToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.stopToolStripMenuItem.Enabled = false;
            this.stopToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            this.stopToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.stopToolStripMenuItem.Text = "Stop";
            this.stopToolStripMenuItem.Click += new System.EventHandler(this.stopToolStripMenuItem_Click);
            // 
            // managerToolStripMenuItem
            // 
            this.managerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.managerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.managerToolStripMenuItem.Name = "managerToolStripMenuItem";
            this.managerToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.managerToolStripMenuItem.Text = "Manager";
            this.managerToolStripMenuItem.Click += new System.EventHandler(this.managerToolStripMenuItem_Click);
            // 
            // loadBotToolStripMenuItem
            // 
            this.loadBotToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.loadBotToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.loadBotToolStripMenuItem.Name = "loadBotToolStripMenuItem";
            this.loadBotToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.loadBotToolStripMenuItem.Text = "Load bot";
            this.loadBotToolStripMenuItem.Click += new System.EventHandler(this.loadBotToolStripMenuItem_Click);
            // 
            // btnMax
            // 
            this.btnMax.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.btnMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMax.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMax.ForeColor = System.Drawing.Color.Black;
            this.btnMax.Location = new System.Drawing.Point(49, 0);
            this.btnMax.Name = "btnMax";
            this.btnMax.Size = new System.Drawing.Size(37, 27);
            this.btnMax.TabIndex = 32;
            this.btnMax.Text = "🗖";
            this.btnMax.UseVisualStyleBackColor = true;
            this.btnMax.Click += new System.EventHandler(this.btnMax_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.btnExit.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.ForeColor = System.Drawing.Color.Black;
            this.btnExit.Location = new System.Drawing.Point(86, 0);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(49, 27);
            this.btnExit.TabIndex = 33;
            this.btnExit.Text = "×";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnMin
            // 
            this.btnMin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.btnMin.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnMin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMin.ForeColor = System.Drawing.Color.Black;
            this.btnMin.Location = new System.Drawing.Point(0, 0);
            this.btnMin.Name = "btnMin";
            this.btnMin.Size = new System.Drawing.Size(49, 27);
            this.btnMin.TabIndex = 34;
            this.btnMin.Text = "🗕";
            this.btnMin.UseVisualStyleBackColor = true;
            this.btnMin.Click += new System.EventHandler(this.btnMin_Click);
            // 
            // panel1
            // 
            this.panel1.ImeMode = System.Windows.Forms.ImeMode.On;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 100);
            this.panel1.TabIndex = 0;
            // 
            // MenuMain
            // 
            this.MenuMain.Dock = System.Windows.Forms.DockStyle.None;
            this.MenuMain.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.MenuMain.Location = new System.Drawing.Point(0, 0);
            this.MenuMain.Name = "MenuMain";
            this.MenuMain.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.MenuMain.Size = new System.Drawing.Size(202, 24);
            this.MenuMain.TabIndex = 37;
            this.MenuMain.Text = "pluginHolder";
            this.MenuMain.Visible = false;
            this.MenuMain.ItemAdded += new System.Windows.Forms.ToolStripItemEventHandler(this.pluginAdded);
            this.MenuMain.ItemRemoved += new System.Windows.Forms.ToolStripItemEventHandler(this.pluginRemoved);
            // 
            // gameContainer
            // 
            this.gameContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(24)))), ((int)(((byte)(28)))));
            this.gameContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gameContainer.Location = new System.Drawing.Point(0, 32);
            this.gameContainer.Name = "gameContainer";
            this.gameContainer.Size = new System.Drawing.Size(962, 547);
            this.gameContainer.TabIndex = 40;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.pnlMenuControls.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlMenuControls.Size = new System.Drawing.Size(422, 32);
            this.pnlMenuControls.BackColor = System.Drawing.Color.FromArgb(28, 28, 30);
            this.pnlMenuControls.Controls.Add(this.btnBank);
            this.pnlMenuControls.Controls.Add(this.btnGetCell);
            this.pnlMenuControls.Controls.Add(this.cbCells);
            this.pnlMenuControls.Controls.Add(this.cbPads);
            this.pnlMenuControls.Controls.Add(this.cbAutoAttackMode);
            this.pnlMenuControls.Controls.Add(this.chkAutoAttack);
            this.pnlMenuControls.Controls.Add(this.chkStartBot);
            this.splitContainer1.Panel1.Controls.Add(this.pnlMenuControls);
            this.splitContainer1.Panel1.Controls.Add(this.darkMenuStrip1);
            this.splitContainer1.Panel1.ImeMode = System.Windows.Forms.ImeMode.On;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.splitContainer1.Panel2Collapsed = true;
            this.splitContainer1.Size = new System.Drawing.Size(962, 32);
            this.splitContainer1.SplitterDistance = 824;
            this.splitContainer1.SplitterWidth = 1;
            this.splitContainer1.TabIndex = 38;
            // 
            // btnGetCell
            // 
            this.btnGetCell.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnGetCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(44)))), ((int)(((byte)(46)))));
            this.btnGetCell.Checked = false;
            this.btnGetCell.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
            this.btnGetCell.Location = new System.Drawing.Point(354, 5);
            this.btnGetCell.Name = "btnGetCell";
            this.btnGetCell.Size = new System.Drawing.Size(22, 22);
            this.btnGetCell.TabIndex = 39;
            this.btnGetCell.Text = "x";
            this.btnGetCell.Click += new System.EventHandler(this.btnGetCell_Click);
            // 
            // chkStartBot
            // 
            this.chkStartBot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.chkStartBot.AutoSize = false;
            this.chkStartBot.BackColor = System.Drawing.Color.Transparent;
            this.chkStartBot.Location = new System.Drawing.Point(2, 7);
            this.chkStartBot.Name = "chkStartBot";
            this.chkStartBot.Size = new System.Drawing.Size(84, 18);
            this.chkStartBot.TabIndex = 38;
            this.chkStartBot.Text = "Start Bot";
            this.chkStartBot.CheckedChanged += new System.EventHandler(this.chkStartBot_CheckedChanged);
            // 
            // chkAutoAttack
            // 
            this.chkAutoAttack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.chkAutoAttack.AutoSize = false;
            this.chkAutoAttack.BackColor = System.Drawing.Color.Transparent;
            this.chkAutoAttack.Location = new System.Drawing.Point(90, 8);
            this.chkAutoAttack.Name = "chkAutoAttack";
            this.chkAutoAttack.Size = new System.Drawing.Size(15, 16);
            this.chkAutoAttack.TabIndex = 37;
            this.chkAutoAttack.Text = "";
            this.chkAutoAttack.CheckedChanged += new System.EventHandler(this.chkAutoAttack_CheckedChanged);
            // 
            // cbAutoAttackMode
            // 
            this.cbAutoAttackMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.cbAutoAttackMode.FormattingEnabled = true;
            this.cbAutoAttackMode.Items.AddRange(new object[] {
            "Auto Attack",
            "Auto and Pot"});
            this.cbAutoAttackMode.Location = new System.Drawing.Point(108, 5);
            this.cbAutoAttackMode.Name = "cbAutoAttackMode";
            this.cbAutoAttackMode.Size = new System.Drawing.Size(94, 22);
            this.cbAutoAttackMode.TabIndex = 40;
            this.cbAutoAttackMode.SelectedIndex = 0;
            // 
            // btnBank
            // 
            this.btnBank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnBank.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(44)))), ((int)(((byte)(46)))));
            this.btnBank.Checked = false;
            this.btnBank.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
            this.btnBank.Location = new System.Drawing.Point(380, 5);
            this.btnBank.Name = "btnBank";
            this.btnBank.Size = new System.Drawing.Size(40, 22);
            this.btnBank.TabIndex = 36;
            this.btnBank.Text = "Bank";
            this.btnBank.Click += new System.EventHandler(this.btnBank_Click_1);
            // 
            // cbCells
            // 
            this.cbCells.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.cbCells.FormattingEnabled = true;
            this.cbCells.Location = new System.Drawing.Point(280, 5);
            this.cbCells.MaxDropDownItems = 50;
            this.cbCells.Name = "cbCells";
            this.cbCells.Size = new System.Drawing.Size(70, 22);
            this.cbCells.TabIndex = 18;
            this.cbCells.TabStop = false;
            this.cbCells.SelectedIndexChanged += new System.EventHandler(this.cbCells_SelectedIndexChanged);
            this.cbCells.Click += new System.EventHandler(this.cbCells_Click);
            this.cbCells.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ToolbarCombo_BlockKeyboard);
            this.cbCells.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ToolbarCombo_BlockKeyPress);
            this.cbCells.DropDownClosed += new System.EventHandler(this.ToolbarCombo_DropDownClosed);
            // 
            // cbPads
            // 
            this.cbPads.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.cbPads.FormattingEnabled = true;
            this.cbPads.Items.AddRange(new object[] {
            "Center",
            "Spawn",
            "Left",
            "Right",
            "Top",
            "Bottom",
            "Up",
            "Down"});
            this.cbPads.Location = new System.Drawing.Point(206, 5);
            this.cbPads.MaxDropDownItems = 50;
            this.cbPads.Name = "cbPads";
            this.cbPads.Size = new System.Drawing.Size(70, 22);
            this.cbPads.TabIndex = 17;
            this.cbPads.TabStop = false;
            this.cbPads.SelectedIndexChanged += new System.EventHandler(this.cbPads_SelectedIndexChanged);
            this.cbPads.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ToolbarCombo_BlockKeyboard);
            this.cbPads.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ToolbarCombo_BlockKeyPress);
            this.cbPads.DropDownClosed += new System.EventHandler(this.ToolbarCombo_DropDownClosed);
            // 
            // darkMenuStrip1
            // 
            this.darkMenuStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.darkMenuStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.darkMenuStrip1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkMenuStrip1.GripMargin = new System.Windows.Forms.Padding(0);
            this.darkMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.darkMenuStrip1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            //AWAS LU KLO KERUBAH SENDIRI NJING
            this.darkMenuStrip1.Renderer = new CustomDarkMenuRenderer();
            this.darkMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.botToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.packetsToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.maidStrip,
            this.codexStrip,
            this.pluginsStrip,
            this.aboutToolStripMenuItem,
            this.menuCharSelect});
            this.darkMenuStrip1.Location = new System.Drawing.Point(0, 0);
            this.darkMenuStrip1.Name = "darkMenuStrip1";
            this.darkMenuStrip1.Padding = new System.Windows.Forms.Padding(6, 4, 2, 4);
            this.darkMenuStrip1.Size = new System.Drawing.Size(962, 32);
            this.darkMenuStrip1.Stretch = false;
            this.darkMenuStrip1.TabIndex = 35;
            this.darkMenuStrip1.Text = "darkMenuStrip1";
            this.darkMenuStrip1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MenuMain_MouseDown);
            // 
            // botToolStripMenuItem
            // 
            this.botToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.botToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.botToolStripMenuItem.Name = "botToolStripMenuItem";
            this.botToolStripMenuItem.Size = new System.Drawing.Size(37, 23);
            this.botToolStripMenuItem.Text = "Bot";
            this.botToolStripMenuItem.Click += new System.EventHandler(this.botToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fastTravelsToolStripMenuItem,
            this.loadersgrabbersToolStripMenuItem,
            this.hotkeysToolStripMenuItem,
            this.pluginManagerToolStripMenuItem,
            this.cosmeticsToolStripMenuItem,
            this.bankToolStripMenuItem,
            this.setsToolStripMenuItem,
            this.eyeDropperToolStripMenuItem,
            this.logsToolStripMenuItem1,
            this.notepadToolStripMenuItem1,
            this.changeServerMenuItem,
            this.DPSMeterToolStripMenuItem,
            this.pvptoolStripMenuItem1,
            this.devTestToolStripMenuItem,
            this.dropUiToolStripMenuItem});
            this.toolsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 23);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // fastTravelsToolStripMenuItem
            // 
            this.fastTravelsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.fastTravelsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.fastTravelsToolStripMenuItem.Name = "fastTravelsToolStripMenuItem";
            this.fastTravelsToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.fastTravelsToolStripMenuItem.Text = "Fast travels";
            this.fastTravelsToolStripMenuItem.Click += new System.EventHandler(this.fastTravelsToolStripMenuItem_Click);
            // 
            // loadersgrabbersToolStripMenuItem
            // 
            this.loadersgrabbersToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.loadersgrabbersToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.loadersgrabbersToolStripMenuItem.Name = "loadersgrabbersToolStripMenuItem";
            this.loadersgrabbersToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.loadersgrabbersToolStripMenuItem.Text = "Loaders/grabbers";
            this.loadersgrabbersToolStripMenuItem.Click += new System.EventHandler(this.loadersgrabbersToolStripMenuItem_Click);
            // 
            // hotkeysToolStripMenuItem
            // 
            this.hotkeysToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.hotkeysToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.hotkeysToolStripMenuItem.Name = "hotkeysToolStripMenuItem";
            this.hotkeysToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.hotkeysToolStripMenuItem.Text = "Hotkeys";
            this.hotkeysToolStripMenuItem.Click += new System.EventHandler(this.hotkeysToolStripMenuItem_Click);
            // 
            // pluginManagerToolStripMenuItem
            // 
            this.pluginManagerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.pluginManagerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.pluginManagerToolStripMenuItem.Name = "pluginManagerToolStripMenuItem";
            this.pluginManagerToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.pluginManagerToolStripMenuItem.Text = "Plugin manager";
            this.pluginManagerToolStripMenuItem.Click += new System.EventHandler(this.pluginManagerToolStripMenuItem_Click);
            // 
            // 
            // cosmeticsToolStripMenuItem
            // 
            this.cosmeticsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.cosmeticsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.cosmeticsToolStripMenuItem.Name = "cosmeticsToolStripMenuItem";
            this.cosmeticsToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.cosmeticsToolStripMenuItem.Text = "Cosmetics";
            this.cosmeticsToolStripMenuItem.Click += new System.EventHandler(this.cosmeticsToolStripMenuItem_Click);
            // 
            // bankToolStripMenuItem
            // 
            this.bankToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.bankToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.bankToolStripMenuItem.Name = "bankToolStripMenuItem";
            this.bankToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.bankToolStripMenuItem.Text = "Bank";
            this.bankToolStripMenuItem.Click += new System.EventHandler(this.bankToolStripMenuItem_Click);
            // 
            // setsToolStripMenuItem
            // 
            this.setsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.setsToolStripMenuItem.Enabled = false;
            this.setsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.setsToolStripMenuItem.Name = "setsToolStripMenuItem";
            this.setsToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.setsToolStripMenuItem.Text = "Sets";
            this.setsToolStripMenuItem.Visible = false;
            this.setsToolStripMenuItem.Click += new System.EventHandler(this.setsToolStripMenuItem_Click);
            // 
            // eyeDropperToolStripMenuItem
            // 
            this.eyeDropperToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.eyeDropperToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.eyeDropperToolStripMenuItem.Name = "eyeDropperToolStripMenuItem";
            this.eyeDropperToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.eyeDropperToolStripMenuItem.Text = "Eye Dropper";
            this.eyeDropperToolStripMenuItem.Click += new System.EventHandler(this.eyeDropperToolStripMenuItem_Click_1);
            // 
            // logsToolStripMenuItem1
            // 
            this.logsToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.logsToolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.logsToolStripMenuItem1.Name = "logsToolStripMenuItem1";
            this.logsToolStripMenuItem1.Size = new System.Drawing.Size(187, 22);
            this.logsToolStripMenuItem1.Text = "Logs";
            this.logsToolStripMenuItem1.Click += new System.EventHandler(this.logsToolStripMenuItem1_Click);
            // 
            // notepadToolStripMenuItem1
            // 
            this.notepadToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.notepadToolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.notepadToolStripMenuItem1.Name = "notepadToolStripMenuItem1";
            this.notepadToolStripMenuItem1.Size = new System.Drawing.Size(187, 22);
            this.notepadToolStripMenuItem1.Text = "Notepad";
            this.notepadToolStripMenuItem1.Click += new System.EventHandler(this.notepadToolStripMenuItem1_Click);
            // 
            // changeServerMenuItem
            // 
            this.changeServerMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.changeServerMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeServerList});
            this.changeServerMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.changeServerMenuItem.Name = "changeServerMenuItem";
            this.changeServerMenuItem.Size = new System.Drawing.Size(187, 22);
            this.changeServerMenuItem.Text = "Change Server";
            this.changeServerMenuItem.Visible = false;
            // 
            // changeServerList
            // 
            this.changeServerList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.changeServerList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.changeServerList.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.changeServerList.Name = "changeServerList";
            this.changeServerList.Size = new System.Drawing.Size(180, 23);
            this.changeServerList.SelectedIndexChanged += new System.EventHandler(this.changeServerList_SelectedIndexChanged);
            // 
            // DPSMeterToolStripMenuItem
            // 
            this.DPSMeterToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.DPSMeterToolStripMenuItem.Enabled = false;
            this.DPSMeterToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.DPSMeterToolStripMenuItem.Name = "DPSMeterToolStripMenuItem";
            this.DPSMeterToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.DPSMeterToolStripMenuItem.Text = "DPS Meter";
            this.DPSMeterToolStripMenuItem.Visible = false;
            this.DPSMeterToolStripMenuItem.Click += new System.EventHandler(this.dPSMeterToolStripMenuItem_Click);
            // 
            // pvptoolStripMenuItem1
            // 
            this.pvptoolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.pvptoolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.pvptoolStripMenuItem1.Name = "pvptoolStripMenuItem1";
            this.pvptoolStripMenuItem1.Size = new System.Drawing.Size(187, 22);
            this.pvptoolStripMenuItem1.Text = "PvP";
            this.pvptoolStripMenuItem1.Visible = false;
            this.pvptoolStripMenuItem1.Click += new System.EventHandler(this.pvptoolStripMenuItem1_Click);
            // 
            // devTestToolStripMenuItem
            // 
            this.devTestToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.devTestToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.devTestToolStripMenuItem.Name = "devTestToolStripMenuItem";
            this.devTestToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.devTestToolStripMenuItem.Text = "Swf Dev Test";
            this.devTestToolStripMenuItem.Click += new System.EventHandler(this.devTestToolStripMenuItem_Click);
            // 
            // dropUiToolStripMenuItem
            // 
            this.dropUiToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.dropUiToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.dropUiToolStripMenuItem.Name = "dropUiToolStripMenuItem";
            this.dropUiToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.dropUiToolStripMenuItem.Text = "Drop Ui";
            this.dropUiToolStripMenuItem.Click += new System.EventHandler(this.dropUiToolStripMenuItem_Click);
            // 
            // packetsToolStripMenuItem
            // 
            this.packetsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.packetsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.snifferToolStripMenuItem,
            this.spammerToolStripMenuItem,
            this.tampererToolStripMenuItem,
            this.toolStripMenuItem1});
            this.packetsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.packetsToolStripMenuItem.Name = "packetsToolStripMenuItem";
            this.packetsToolStripMenuItem.Size = new System.Drawing.Size(59, 23);
            this.packetsToolStripMenuItem.Text = "Packets";
            // 
            // snifferToolStripMenuItem
            // 
            this.snifferToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.snifferToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.snifferToolStripMenuItem.Name = "snifferToolStripMenuItem";
            this.snifferToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.snifferToolStripMenuItem.Text = "Sniffer";
            this.snifferToolStripMenuItem.Click += new System.EventHandler(this.snifferToolStripMenuItem_Click);
            // 
            // spammerToolStripMenuItem
            // 
            this.spammerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.spammerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.spammerToolStripMenuItem.Name = "spammerToolStripMenuItem";
            this.spammerToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.spammerToolStripMenuItem.Text = "Spammer";
            this.spammerToolStripMenuItem.Click += new System.EventHandler(this.spammerToolStripMenuItem_Click);
            // 
            // tampererToolStripMenuItem
            // 
            this.tampererToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.tampererToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tampererToolStripMenuItem.Name = "tampererToolStripMenuItem";
            this.tampererToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.tampererToolStripMenuItem.Text = "Tamperer";
            this.tampererToolStripMenuItem.Click += new System.EventHandler(this.tampererToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.toolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(132, 22);
            this.toolStripMenuItem1.Text = "Interceptor";
            this.toolStripMenuItem1.Visible = false;
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.infRangeToolStripMenuItem,
            this.provokeToolStripMenuItem1,
            this.enemyMagnetToolStripMenuItem,
            this.lagKillerToolStripMenuItem,
            this.hidePlayersToolStripMenuItem,
            this.skipCutscenesToolStripMenuItem,
            this.disableAnimationsToolStripMenuItem,
            this.walkspeedToolStripMenuItem,
            this.enableOptionsToolStripMenuItem,
            new System.Windows.Forms.ToolStripSeparator(),
            this.uiSkinsMenuItem});
            this.optionsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 23);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // infRangeToolStripMenuItem
            // 
            this.infRangeToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.infRangeToolStripMenuItem.CheckOnClick = true;
            this.infRangeToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.infRangeToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.infRangeToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.infRangeToolStripMenuItem.Name = "infRangeToolStripMenuItem";
            this.infRangeToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.infRangeToolStripMenuItem.Text = "Infinite Range";
            this.infRangeToolStripMenuItem.Click += new System.EventHandler(this.infRangeToolStripMenuItem_Click);
            // 
            // provokeToolStripMenuItem1
            // 
            this.provokeToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.provokeToolStripMenuItem1.CheckOnClick = true;
            this.provokeToolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.provokeToolStripMenuItem1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.provokeToolStripMenuItem1.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.provokeToolStripMenuItem1.Name = "provokeToolStripMenuItem1";
            this.provokeToolStripMenuItem1.Size = new System.Drawing.Size(176, 22);
            this.provokeToolStripMenuItem1.Text = "Provoke";
            this.provokeToolStripMenuItem1.Click += new System.EventHandler(this.provokeToolStripMenuItem1_Click);
            // 
            // enemyMagnetToolStripMenuItem
            // 
            this.enemyMagnetToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.enemyMagnetToolStripMenuItem.CheckOnClick = true;
            this.enemyMagnetToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.enemyMagnetToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.enemyMagnetToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.enemyMagnetToolStripMenuItem.Name = "enemyMagnetToolStripMenuItem";
            this.enemyMagnetToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.enemyMagnetToolStripMenuItem.Text = "Enemy Magnet";
            this.enemyMagnetToolStripMenuItem.Click += new System.EventHandler(this.enemyMagnetToolStripMenuItem_Click);
            // 
            // lagKillerToolStripMenuItem
            // 
            this.lagKillerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.lagKillerToolStripMenuItem.CheckOnClick = true;
            this.lagKillerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lagKillerToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.lagKillerToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.lagKillerToolStripMenuItem.Name = "lagKillerToolStripMenuItem";
            this.lagKillerToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.lagKillerToolStripMenuItem.Text = "Lag Killer";
            this.lagKillerToolStripMenuItem.Click += new System.EventHandler(this.lagKillerToolStripMenuItem_Click);
            // 
            // hidePlayersToolStripMenuItem
            // 
            this.hidePlayersToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.hidePlayersToolStripMenuItem.CheckOnClick = true;
            this.hidePlayersToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.hidePlayersToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.hidePlayersToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.hidePlayersToolStripMenuItem.Name = "hidePlayersToolStripMenuItem";
            this.hidePlayersToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.hidePlayersToolStripMenuItem.Text = "Hide Players";
            this.hidePlayersToolStripMenuItem.Click += new System.EventHandler(this.hidePlayersToolStripMenuItem_Click);
            // 
            // skipCutscenesToolStripMenuItem
            // 
            this.skipCutscenesToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.skipCutscenesToolStripMenuItem.CheckOnClick = true;
            this.skipCutscenesToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.skipCutscenesToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.skipCutscenesToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.skipCutscenesToolStripMenuItem.Name = "skipCutscenesToolStripMenuItem";
            this.skipCutscenesToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.skipCutscenesToolStripMenuItem.Text = "Skip Cutscenes";
            this.skipCutscenesToolStripMenuItem.Click += new System.EventHandler(this.skipCutscenesToolStripMenuItem_Click);
            // 
            // disableAnimationsToolStripMenuItem
            // 
            this.disableAnimationsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.disableAnimationsToolStripMenuItem.CheckOnClick = true;
            this.disableAnimationsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.disableAnimationsToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.disableAnimationsToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.disableAnimationsToolStripMenuItem.Name = "disableAnimationsToolStripMenuItem";
            this.disableAnimationsToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.disableAnimationsToolStripMenuItem.Text = "Disable Animations";
            this.disableAnimationsToolStripMenuItem.Visible = false;
            this.disableAnimationsToolStripMenuItem.Click += new System.EventHandler(this.disableAnimationsToolStripMenuItem_Click);
            // 
            // walkspeedToolStripMenuItem
            // 
            this.walkspeedToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.walkspeedToolStripMenuItem.CheckOnClick = true;
            this.walkspeedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBox1});
            this.walkspeedToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.walkspeedToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.walkspeedToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.walkspeedToolStripMenuItem.Name = "walkspeedToolStripMenuItem";
            this.walkspeedToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.walkspeedToolStripMenuItem.Text = "Walkspeed";
            this.walkspeedToolStripMenuItem.Click += new System.EventHandler(this.walkspeedToolStripMenuItem_Click);
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.toolStripTextBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.toolStripTextBox1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 23);
            this.toolStripTextBox1.Text = "20";
            // 
            // enableOptionsToolStripMenuItem
            // 
            this.enableOptionsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.enableOptionsToolStripMenuItem.CheckOnClick = true;
            this.enableOptionsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.enableOptionsToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.enableOptionsToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.enableOptionsToolStripMenuItem.Name = "enableOptionsToolStripMenuItem";
            this.enableOptionsToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.enableOptionsToolStripMenuItem.Text = "Enable Options";
            this.enableOptionsToolStripMenuItem.Visible = false;
            this.enableOptionsToolStripMenuItem.Click += new System.EventHandler(this.enableOptionsToolStripMenuItem_Click);
            // 
            // uiSkinsMenuItem
            // 
            this.uiSkinsMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.uiSkinsMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.uiSkinsMenuItem.Name = "uiSkinsMenuItem";
            this.uiSkinsMenuItem.Size = new System.Drawing.Size(176, 22);
            this.uiSkinsMenuItem.Text = "UI Skins";
            this.uiSkinsMenuItem.Click += new System.EventHandler(this.uiSkinsMenuItem_Click);
            // 
            // maidStrip
            // 
            this.maidStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.maidStrip.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.maidStrip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.maidStrip.Name = "maidStrip";
            this.maidStrip.Size = new System.Drawing.Size(46, 23);
            this.maidStrip.Text = "Maid";
            this.maidStrip.Click += new System.EventHandler(this.maidStrip_Click);
            // 
            // codexStrip
            // 
            this.codexStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.codexStrip.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.codexStrip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.codexStrip.Name = "codexStrip";
            this.codexStrip.Size = new System.Drawing.Size(58, 23);
            this.codexStrip.Text = "CODEX";
            
            // 
            // voiceChatMenuItem
            // 
            this.voiceChatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.voiceChatMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.voiceChatMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.voiceChatMenuItem.Name = "voiceChatMenuItem";
            this.voiceChatMenuItem.Size = new System.Drawing.Size(180, 22);
            this.voiceChatMenuItem.Text = "Voice Chat";
            this.voiceChatMenuItem.Click += new System.EventHandler(this.voiceChatMenuItem_Click);
            
            // 
            // skillMomentMenuItem
            // 
            this.skillMomentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.skillMomentMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.skillMomentMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.skillMomentMenuItem.Name = "skillMomentMenuItem";
            this.skillMomentMenuItem.Size = new System.Drawing.Size(180, 22);
            this.skillMomentMenuItem.Text = "Skill Moment";
            this.skillMomentMenuItem.Click += new System.EventHandler(this.skillMomentMenuItem_Click);

            // 
            // loopEnrageMenuItem
            // 
            this.loopEnrageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loopEnrageMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.loopEnrageMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.loopEnrageMenuItem.Name = "loopEnrageMenuItem";
            this.loopEnrageMenuItem.Size = new System.Drawing.Size(180, 22);
            this.loopEnrageMenuItem.Text = "Loop Taunt";
            this.loopEnrageMenuItem.Click += new System.EventHandler(this.loopEnrageMenuItem_Click);

            // 
            // bypassMapMenuItem
            // 
            this.bypassMapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bypassMapMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.bypassMapMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.bypassMapMenuItem.Name = "bypassMapMenuItem";
            this.bypassMapMenuItem.Size = new System.Drawing.Size(180, 22);
            this.bypassMapMenuItem.Text = "Bypass Map";
            this.bypassMapMenuItem.Click += new System.EventHandler(this.bypassMapMenuItem_Click);

            // 
            // questPinnerMenuItem
            // 
            this.questPinnerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.questPinnerMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.questPinnerMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.questPinnerMenuItem.Name = "questPinnerMenuItem";
            this.questPinnerMenuItem.Size = new System.Drawing.Size(180, 22);
            this.questPinnerMenuItem.Text = "Quest Pinner";
            this.questPinnerMenuItem.Click += new System.EventHandler(this.questPinnerMenuItem_Click);
            
            // 
            // 
            // alchemistMenuItem
            // 
            this.alchemistMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alchemistMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.alchemistMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.alchemistMenuItem.Name = "alchemistMenuItem";
            this.alchemistMenuItem.Size = new System.Drawing.Size(180, 22);
            this.alchemistMenuItem.Text = "Alchemist";
            this.alchemistMenuItem.Click += new System.EventHandler(this.alchemistMenuItem_Click);
            
            this.codexStrip.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.voiceChatMenuItem,
                this.skillMomentMenuItem,
                this.loopEnrageMenuItem,
                this.bypassMapMenuItem,
                this.questPinnerMenuItem,
                this.alchemistMenuItem
            });
            // 
            // pluginsStrip
            // 
            this.pluginsStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.pluginsStrip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.pluginsStrip.Name = "pluginsStrip";
            this.pluginsStrip.Size = new System.Drawing.Size(58, 23);
            this.pluginsStrip.Text = "Plugins";
            // 
            // appDetailMenuItem
            // 
            this.appDetailMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.appDetailMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.appDetailMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.appDetailMenuItem.Name = "appDetailMenuItem";
            this.appDetailMenuItem.Size = new System.Drawing.Size(180, 22);
            this.appDetailMenuItem.Text = "App Detail";
            this.appDetailMenuItem.Click += new System.EventHandler(this.appDetailMenuItem_Click);
            // 
            // updateAppMenuItem
            // 
            this.updateAppMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateAppMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.updateAppMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.updateAppMenuItem.Name = "updateAppMenuItem";
            this.updateAppMenuItem.Size = new System.Drawing.Size(180, 22);
            this.updateAppMenuItem.Text = "Update App";
            this.updateAppMenuItem.Click += new System.EventHandler(this.updateAppMenuItem_Click);
            // 
            // syncScriptMenuItem
            // 
            this.syncScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.syncScriptMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.syncScriptMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.syncScriptMenuItem.Name = "syncScriptMenuItem";
            this.syncScriptMenuItem.Size = new System.Drawing.Size(180, 22);
            this.syncScriptMenuItem.Text = "Sync Update Script";
            this.syncScriptMenuItem.Click += new System.EventHandler(this.syncScriptMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.appDetailMenuItem,
                this.updateAppMenuItem,
                this.syncScriptMenuItem
            });
            this.aboutToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 23);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // menuCharSelect
            // 
            this.menuCharSelect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.menuCharSelect.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.menuCharSelect.Name = "menuCharSelect";
            this.menuCharSelect.Size = new System.Drawing.Size(78, 23);
            this.menuCharSelect.Text = "Char Select";
            // 
            // Root
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.ClientSize = new System.Drawing.Size(962, 579);
            this.Controls.Add(this.gameContainer);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.MenuMain);
            this.ForeColor = System.Drawing.SystemColors.Window;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "Root";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GrimCodex Ruffle";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Root_FormClosing);
            this.Load += new System.EventHandler(this.Root_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Root_KeyPress);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.darkMenuStrip1.ResumeLayout(false);
            this.darkMenuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void Instance_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void logsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(LogForm.Instance);
        }

        private void btnJump_Click(object sender, EventArgs e)
        {
            string Cell = (string)this.cbCells.SelectedItem;
            string Pad = (string)this.cbPads.SelectedItem;
            Player.MoveToCell(Cell ?? Player.Cell, Pad ?? Player.Pad);
        }

        private void cosmeticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(CosmeticForm.Instance);
        }

        private void bankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(BankForm.Instance);
        }

        private void setsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Set.Instance);
        }

        private void grimoireSuggestionsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        public void BotStateChanged(bool IsRunning)
        {
            if (IsRunning)
            {
                startToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = true;
            }
            else
            {
                startToolStripMenuItem.Enabled = true;
                stopToolStripMenuItem.Enabled = false;
            }
        }

        private async void changeServerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn) return;
            string server = changeServerList.Items[changeServerList.SelectedIndex].ToString();
            changeServerList.Visible = false;
            Player.Logout();
            await AutoRelogin.Login(new Server() { Name = server }, 3000, cts: new System.Threading.CancellationTokenSource(), ensureSuccess: false);
            changeServerList.Visible = true;
        }

        private void nTray_MouseClick(object sender, MouseEventArgs e)
        {
            ShowForm(this);
        }

        private void eyeDropperToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ShowForm(EyeDropper.Instance);
        }

        private void logsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowForm(LogForm.Instance);
        }

        private void notepadToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowForm(Notepad.Instance);
        }

        private void infRangeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool check = infRangeToolStripMenuItem.Checked;
            OptionsManager.InfiniteRange = check;
            botManager.chkInfiniteRange.Checked = check;
        }

        private void provokeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            bool check = provokeToolStripMenuItem1.Checked;
            OptionsManager.ProvokeMonsters = check;
            botManager.chkProvoke.Checked = check;
        }

        private void enemyMagnetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool check = enemyMagnetToolStripMenuItem.Checked;
            OptionsManager.EnemyMagnet = check;
            botManager.chkMagnet.Checked = check;
        }

        private void lagKillerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool check = lagKillerToolStripMenuItem.Checked;
            OptionsManager.LagKiller = check;
            OptionsManager.SetLagKiller(check);
            botManager.chkLag.Checked = check;
        }

        private void hidePlayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool check = hidePlayersToolStripMenuItem.Checked;
            OptionsManager.HidePlayers = check;
            botManager.chkHidePlayers.Checked = check;
        }

        private void skipCutscenesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool check = skipCutscenesToolStripMenuItem.Checked;
            if (check) OptionsManager.SetSkipCutscenes();
            OptionsManager.SkipCutscenes = check;
            botManager.chkSkipCutscenes.Checked = check;
        }

        private void disableAnimationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool check = disableAnimationsToolStripMenuItem.Checked;
            OptionsManager.DisableAnimations = check;
            botManager.chkDisableAnims.Checked = check;
        }

        private void enableOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            botManager.chkEnableSettings.Checked = enableOptionsToolStripMenuItem.Checked;
        }

        private void walkspeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (walkspeedToolStripMenuItem.Checked)
            {
                BotManager.Instance.numWalkSpeed.Value = int.Parse(walkspeedToolStripMenuItem.DropDownItems[0].Text);
                BotManager.Instance.chkWalkSpeed.Checked = true;
            }
            else
            {
                BotManager.Instance.chkWalkSpeed.Checked = false;
            }
        }

        private void uiSkinsMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(UISkinForm.Instance);
            UISkinForm.Instance.SyncFromActiveColors();
        }

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private void MenuMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                //Message.Create(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            const UInt32 WM_NCHITTEST = 0x0084;
            const UInt32 WM_MOUSEMOVE = 0x0200;
            const int WM_GETMINMAXINFO = 0x0024;

            if (m.Msg == WM_GETMINMAXINFO)
            {
                try
                {
                    Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                    this.MaximizedBounds = new Rectangle(0, 0, workingArea.Width, workingArea.Height);
                }
                catch { }
            }

            const UInt32 HTLEFT = 10;
            const UInt32 HTRIGHT = 11;
            const UInt32 HTBOTTOMRIGHT = 17;
            const UInt32 HTBOTTOM = 15;
            const UInt32 HTBOTTOMLEFT = 16;
            const UInt32 HTTOP = 12;
            const UInt32 HTTOPLEFT = 13;
            const UInt32 HTTOPRIGHT = 14;

            const int RESIZE_HANDLE_SIZE = 10;
            bool handled = false;
            if (m.Msg == WM_NCHITTEST || m.Msg == WM_MOUSEMOVE)
            {
                Size formSize = this.Size;
                Point screenPoint = new Point(m.LParam.ToInt32());
                Point clientPoint = this.PointToClient(screenPoint);

                Dictionary<UInt32, Rectangle> boxes = new Dictionary<UInt32, Rectangle>() {
            {HTBOTTOMLEFT, new Rectangle(0, formSize.Height - RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE)},
            {HTBOTTOM, new Rectangle(RESIZE_HANDLE_SIZE, formSize.Height - RESIZE_HANDLE_SIZE, formSize.Width - 2*RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE)},
            {HTBOTTOMRIGHT, new Rectangle(formSize.Width - RESIZE_HANDLE_SIZE, formSize.Height - RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE)},
            {HTRIGHT, new Rectangle(formSize.Width - RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE, formSize.Height - 2*RESIZE_HANDLE_SIZE)},
            {HTTOPRIGHT, new Rectangle(formSize.Width - RESIZE_HANDLE_SIZE, 0, RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE) },
            {HTTOP, new Rectangle(RESIZE_HANDLE_SIZE, 0, formSize.Width - 2*RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE) },
            {HTTOPLEFT, new Rectangle(0, 0, RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE) },
            {HTLEFT, new Rectangle(0, RESIZE_HANDLE_SIZE, RESIZE_HANDLE_SIZE, formSize.Height - 2*RESIZE_HANDLE_SIZE) }
        };

                foreach (KeyValuePair<UInt32, Rectangle> hitBox in boxes)
                {
                    if (hitBox.Value.Contains(clientPoint))
                    {
                        m.Result = (IntPtr)hitBox.Key;
                        handled = true;
                        break;
                    }
                }
            }

            if (!handled)
                base.WndProc(ref m);
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Player.IsAlive && Player.IsLoggedIn && BotManager.Instance.lstCommands.Items.Count > 0)
            {
                BotManager.Instance.MultiMode();
                BotManager.Instance.ActiveBotEngine.IsRunningChanged += BotManager.Instance.OnIsRunningChanged;
                BotManager.Instance.ActiveBotEngine.IndexChanged += BotManager.Instance.OnIndexChanged;
                BotManager.Instance.ActiveBotEngine.ConfigurationChanged += BotManager.Instance.OnConfigurationChanged;
                BotManager.Instance.ActiveBotEngine.Start(BotManager.Instance.GenerateConfiguration());
                BotManager.Instance.BotStateChanged(IsRunning: true);
                this.BotStateChanged(IsRunning: true);
            }
        }

        private async void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Configuration.Instance.Items != null && Configuration.Instance.BankOnStop)
            {
                foreach (InventoryItem item in Player.Inventory.Items)
                {
                    if (!item.IsEquipped && item.IsAcItem && item.Category != "Class" && item.Name.ToLower() != "treasure potion" && Configuration.Instance.Items.Contains(item.Name))
                    {
                        Player.Bank.TransferToBank(item.Name);
                        await Task.Delay(70);
                        LogForm.Instance.AppendDebug("Transferred to Bank: " + item.Name + "\r\n");
                    }
                }
                LogForm.Instance.AppendDebug("Banked all AC Items in Items list \r\n");
            }
            BotManager.Instance.ActiveBotEngine.Stop();
            this.stopToolStripMenuItem.Enabled = false;
            BotManager.Instance.MultiMode();
            await Task.Delay(2000);
            BotManager.Instance.BotStateChanged(IsRunning: false);
            this.BotStateChanged(IsRunning: false);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                {
                    cp.Style |= 0x20000 | 0x80000 | 0x40000; //WS_MINIMIZEBOX | WS_SYSMENU | WS_SIZEBOX;
                }
                return cp;
            }
        }

        private void btnMax_Click(object sender, EventArgs e)
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        private void btnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pluginAdded(object sender, ToolStripItemEventArgs e)
        {
            if (e.Item != null)
            {
                UISkinManager.ApplyThemeToMenuItem(e.Item);
            }
            pluginsStrip.DropDownItems.Add(e.Item);
        }

        private void pluginRemoved(object sender, ToolStripItemEventArgs e)
        {
            pluginsStrip.DropDownItems.Remove(e.Item);
        }

        private void managerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(botManager);
        }

        private void Root_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void dPSMeterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(DPSForm.Instance);
        }

        private void loadBotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            botManager.btnLoad_Click(sender, e);
        }

        private void btnBank_Click_1(object sender, EventArgs e)
        {
            Player.Bank.Show();
        }

        private void ToolbarCombo_BlockKeyboard(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            ReturnFocusToGame();
        }

        private void ToolbarCombo_BlockKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            ReturnFocusToGame();
        }

        private void ToolbarCombo_DropDownClosed(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker)ReturnFocusToGame);
        }

        private void ReturnFocusToGame()
        {
            try
            {
                this.ActiveControl = null;
                if (gameContainer != null && gameContainer.Controls.Count > 0)
                    gameContainer.Controls[0].Focus();
                else
                    gameContainer?.Focus();
            }
            catch { }
        }

        private void cbCells_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn) return;
            string cell = (string)this.cbCells.SelectedItem;
            if (cell != null)
            {
                string pad = (string)this.cbPads.SelectedItem;
                Player.MoveToCell(cell, pad ?? "Spawn");
            }
            ReturnFocusToGame();
        }
        private void cbPads_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn) return;
            string pad = (string)cbPads.SelectedItem;
            if (pad != null)
            {
                string cell = (string)this.cbCells.SelectedItem;
                Player.MoveToCell(cell ?? Player.Cell, pad);
            }
            ReturnFocusToGame();
        }

        private List<Skill> getSkillPreset()
        {
            List<Skill> listSkill = new List<Skill>();
            try
            {
                bool wait = false;
                string playerClass = Player.EquippedClass;
                string skillPreset = ClientConfig.GetValue($"{ClientConfig.C_SKILL_PRESET_PREFIX}{playerClass.ToUpper()}");
                if (skillPreset != null)
                {
                    if (skillPreset.Contains("wait"))
                    {
                        skillPreset = skillPreset.Replace("wait", "");
                        wait = true;
                    }
                    string[] skills = skillPreset.Split(';');
                    foreach (string skill in skills)
                    {
                        if (skill.Contains(':'))
                        {
                            string skillIndex = skill.Split(':')[0];
                            string safer = skill.Split(':')[1];
                            string sType = safer.Remove(0, 2)[0].ToString();
                            int sValue = int.Parse(safer.Remove(0, 3));
                            switch (safer.Substring(0, 2))
                            {
                                case "SH":
                                    listSkill.Add(new Skill
                                    {
                                        Type = Skill.SkillType.Safe,
                                        SType = sType == ">" ? Skill.SafeType.GreaterThan : Skill.SafeType.LowerThan,
                                        SafeValue = sValue,
                                        IsSafeMp = false,
                                        Index = skillIndex,
                                    });
                                    break;
                                case "SM":
                                    listSkill.Add(
                                        new Skill
                                        {
                                            Type = Skill.SkillType.Safe,
                                            SType = sType == ">" ? Skill.SafeType.GreaterThan : Skill.SafeType.LowerThan,
                                            SafeValue = sValue,
                                            IsSafeMp = true,
                                            Index = skillIndex
                                        });
                                    break;
                            }
                        }
                        else
                        {
                            listSkill.Add(
                                new Skill { Type = Skill.SkillType.Normal, Index = skill, waitCd = wait}
                            );
                        }
                    }
                }
            }
            catch
            {
                LogForm.Instance.AppendDebug("Error while loading skill preset for " + Player.EquippedClass + "\r\n");
            }
            return listSkill;
        }

        private async void chkAutoAttack_CheckedChanged(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn || chkStartBot.Checked)
            {
                chkAutoAttack.Checked = false;
                return;
            }

            bool usePotion = cbAutoAttackMode != null && cbAutoAttackMode.SelectedIndex == 1;
            if (cbAutoAttackMode != null)
                cbAutoAttackMode.Enabled = !chkAutoAttack.Checked;

            List<Skill> listSkill = getSkillPreset();
            if (listSkill.Count <= 0)
            {
                listSkill = new List<Skill> {
                    new Skill {Type = Skill.SkillType.Normal, Index = "1"},
                    new Skill {Type = Skill.SkillType.Normal, Index = "2"},
                    new Skill {Type = Skill.SkillType.Normal, Index = "3"},
                    new Skill {Type = Skill.SkillType.Normal, Index = "4"}
                };
                if (usePotion)
                {
                    listSkill.Add(new Skill { Type = Skill.SkillType.Normal, Index = "5" });
                }
            }
            else if (usePotion && !listSkill.Any(s => s.Index == "5"))
            {
                listSkill.Add(new Skill { Type = Skill.SkillType.Normal, Index = "5" });
            }
            int i = 0;
			BotData.BotState = BotData.State.Combat;

            string lastCell = "";
            string lastPad = "Spawn";
            bool wasDead = false;

            while (chkAutoAttack.Checked && Player.IsLoggedIn)
            {
                if (!Player.IsAlive)
                {
                    wasDead = true;
                    await Task.Delay(1000);
                    continue;
                }

                if (wasDead)
                {
                    if (!string.IsNullOrEmpty(lastCell) && lastCell != "null")
                    {
                        LogForm.Instance.AppendDebug($"[AutoAttack] Respawning back to cell: {lastCell}, pad: {lastPad}\r\n");
                        Player.MoveToCell(lastCell, lastPad);
                        await Task.Delay(1000);
                    }
                    wasDead = false;
                }

                if (Player.IsAlive)
                {
                    string currentCell = Player.Cell;
                    string currentPad = Player.Pad;
                    if (!string.IsNullOrEmpty(currentCell) && currentCell != "null")
                    {
                        lastCell = currentCell;
                        lastPad = !string.IsNullOrEmpty(currentPad) && currentPad != "null" ? currentPad : "Spawn";
                    }
                }

                if (BotData.BotState == BotData.State.Combat)
                {
                    await Task.Delay(150);
                    if (!Player.HasTarget) 
						Player.AttackMonster("*");
                    if (!Player.HasTarget) 
						continue;
                    bool comboUsed = await SpecialClassCombo();
                    if (!comboUsed && listSkill.Count > 0)
                    {
                        if (listSkill[i].waitCd)
                            await Task.Delay(Player.SkillAvailable(listSkill[i].Index));
                        if (listSkill[i].Type != Skill.SkillType.Label && Player.HasTarget)
                            listSkill[i].ExecuteSkill();
                        i++;
                        if (i >= listSkill.Count) i = 0;
                    }
                }
                else
                {
                    await Task.Delay(5000);
                    BotData.BotState = BotData.State.Combat;
                }
            }
            chkAutoAttack.Checked = false;
            if (cbAutoAttackMode != null)
                cbAutoAttackMode.Enabled = true;
        }
        static string playerClass;
        private async Task<bool> SpecialClassCombo()
        {
            if (!Player.IsLoggedIn)
                return false;

            playerClass = Player.EquippedClass?.ToLower() ?? "";
            if (playerClass.Contains("chrono shadow"))
            {
                if (Player.GetAuras(true, "Rounds Empty") == 1 || Player.Mana < 15)
                {
                    await useSkill("4");
                    await useSkill("1");
                    await Task.Delay(200);
                    return true;
                }
            }
            else if (playerClass.Equals("arcana invoker"))
            {
                if (Player.GetAuras(true, "XX - Judgement") == 1 ||
                    Player.GetAuras(true, "End of the world") >= 15 ||
                    Player.GetAuras(true, "XXI - The World") == 0 && 
                    Player.GetAuras(true, "0 - The Fool") == 0 )
                {
                    await useSkill("1");
                    return true;
                }
            }
            else if (playerClass.Equals("archmage"))
            {
                if (Player.GetAuras(true, "Arcane Flux") == 1 &&
                    Player.GetAuras(true, "Corporeal Ascension") == 0 ||
                    Player.GetAuras(true, "Arcane Sigil") == 0)
                {
                    await useSkill("4");
                    await Task.Delay(200);
                    return true;
                }
            }
            return false;
        }

        private async Task useSkill(string skillIndex)
        {
            //await Task.Delay(150);
            await Task.Delay(Player.SkillAvailable(skillIndex));
            if (playerClass.Contains("CHRONO SHADOW"))
                Player.ForceUseSkill(skillIndex);
            else Player.UseSkill(skillIndex);
        }

        private void chkStartBot_CheckedChanged(object sender, EventArgs e)
        {
            if (chkStartBot.Checked && (BotManager.Instance.lstCommands.Items.Count <= 0 || !Player.IsLoggedIn))
            {
                chkStartBot.Checked = false;
                return;
            }

            if (chkStartBot.Checked)
            {
                if (!BotManager.Instance.chkEnable.Checked)
                    BotManager.Instance.chkEnable.Checked = true;
            }
            else
            {
                if (BotManager.Instance.chkEnable.Checked)
                    BotManager.Instance.chkEnable.Checked = false;
            }
        }

        private void appDetailMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(AboutForm.Instance);
        }

        private async void updateAppMenuItem_Click(object sender, EventArgs e)
        {
            await CodexBankManager.Instance.CheckAppUpdatePromptAsync();
        }

        private void syncScriptMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(CodexBankForm.Instance);
        }

        public void ResetBattle()
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired && this.IsHandleCreated)
            {
                try { this.BeginInvoke(new Action(ResetBattle)); } catch { }
                return;
            }
            btnGetCell_Click(btnGetCell, EventArgs.Empty);
        }

        private void btnGetCell_Click(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn || World.IsMapLoading) return;
            cbPads.SelectedIndexChanged -= cbPads_SelectedIndexChanged;
            cbCells.SelectedIndexChanged -= cbCells_SelectedIndexChanged;

            cbCells.Items.Clear();
            ComboBox.ObjectCollection items = cbCells.Items;
            object[] cells = World.Cells;
            object[] items2 = cells;
            items.AddRange(items2);

            cbPads.SelectedItem = Player.Pad;
            cbCells.SelectedItem = Player.Cell;

            Player.MoveToCell(Player.Cell, Player.Pad);

            cbPads.SelectedIndexChanged += cbPads_SelectedIndexChanged;
            cbCells.SelectedIndexChanged += cbCells_SelectedIndexChanged;
        }

        private void btnReloadBank_Click(object sender, EventArgs e)
        {
            _ = Proxy.Instance.SendToServer($"%xt%zm%loadBank%{World.RoomId}%All%");
        }

        private void pvptoolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowForm(PvP.Instance);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowForm(PacketInterceptor.Instance);
        }

        private void devTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(DevTest.Instance);
        }

        public void ToggleLauncherSkin()
        {
            if (this.Text == title)
            {
                this.Text = "Artix Game Launcher";
                this.Icon = global::Properties.Resources.Artix;
                this.nTray.Icon = global::Properties.Resources.Artix;
                this.splitContainer1.Visible = false;
                this.Height -= this.splitContainer1.Height;
            }
            else
            {
                this.Text = title;
                this.Icon = global::Properties.Resources.GrimoireIcon;
                this.nTray.Icon = global::Properties.Resources.GrimoireIcon;
                this.splitContainer1.Visible = true;
                this.Height += this.splitContainer1.Height;
            }
        }

        private void maidStrip_Click(object sender, EventArgs e)
        {
            ShowForm(Maid.MaidRemake.Instance);
        }

        private void voiceChatMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(CodexForm.Instance);
        }

        private void skillMomentMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(CodexSkillForm.Instance);
        }

        private void loopEnrageMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(CodexEnrageForm.Instance);
        }

        private void bypassMapMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(BypassMapForm.Instance);
        }

        private void questPinnerMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(QuestPinnerForm.Instance);
        }

        private void alchemistMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(AlchemistForm.Instance);
        }


        private void dropUiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(DropUi.instance);
        }
		
		private const string Separator = " = ";
        public static void SaveRecentMapItemMerged(string filePath)
        {
			if (Player.recentMapItem.Count == 0)
				return;
			
            HashSet<string> merged = new HashSet<string>();
            filePath = Path.Combine(Application.StartupPath, filePath);

            // Load file lama
            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        merged.Add(line.Trim());
                }
            }
        
            // Tambah data baru
            foreach (var kv in Player.recentMapItem)
            {
                merged.Add($"{kv.Key}{Separator}{kv.Value}");
            }
        
            // Sort by key lalu value
            var sorted = merged
                .Select(x =>
                {
                    var p = x.Split(new[] { Separator }, StringSplitOptions.None);
                    return new
                    {
                        Key = int.TryParse(p[0], out int k) ? k : int.MaxValue,
                        Value = p.Length > 1 ? p[1] : ""
                    };
                })
                .OrderBy(x => x.Key)
                .ThenBy(x => x.Value)
                .Select(x => $"{x.Key}{Separator}{x.Value}")
                .ToList();
        
            File.WriteAllLines(filePath, sorted);
        }
    }
}
public class CustomDarkMenuRenderer : DarkMenuRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        Color bg = e.ToolStrip.IsDropDown
            ? (Grimoire.UI.UISkinManager.CodexStyleEnabled
                ? Grimoire.UI.CodexThemeEngine.GetCardSurfaceColor()
                : Grimoire.UI.UISkinManager.BaseColor)
            : Grimoire.UI.UISkinManager.BaseColor;

        using (SolidBrush brush = new SolidBrush(bg))
        {
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        // Subtle Codex hairline divider under the top toolbar
        if (!e.ToolStrip.IsDropDown && Grimoire.UI.UISkinManager.CodexStyleEnabled)
        {
            Color divider = Grimoire.UI.CodexThemeEngine.GetSoftBorderColor(0.38f);
            using (Pen pen = new Pen(divider, 1f))
            {
                e.Graphics.DrawLine(pen, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }
        }
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip.IsDropDown)
        {
            Color border = Grimoire.UI.UISkinManager.CodexStyleEnabled
                ? Grimoire.UI.CodexThemeEngine.GetSoftBorderColor(0.60f)
                : Grimoire.UI.UISkinManager.BorderColor;
            using (Pen pen = new Pen(border, 1f))
            {
                Rectangle r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                e.Graphics.DrawRectangle(pen, r);
            }
        }
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Graphics g = e.Graphics;
        bool isActive = e.Item.Selected || (e.Item is ToolStripMenuItem mi && mi.DropDown.Visible);

        if (!isActive)
        {
            return;
        }

        Color fill = Grimoire.UI.UISkinManager.CodexStyleEnabled
            ? (e.ToolStrip.IsDropDown
                ? Grimoire.UI.CodexThemeEngine.GetEffectiveAccent()
                : Grimoire.UI.CodexThemeEngine.Blend(Grimoire.UI.UISkinManager.BaseColor, Grimoire.UI.UISkinManager.HoverColor, 0.78f))
            : Grimoire.UI.UISkinManager.HoverColor;

        if (Grimoire.UI.UISkinManager.CodexStyleEnabled)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            RectangleF pillRect = e.ToolStrip.IsDropDown
                ? new RectangleF(4f, 1.5f, Math.Max(4f, e.Item.Width - 8f), Math.Max(4f, e.Item.Height - 3f))
                : new RectangleF(1.5f, 1.5f, Math.Max(4f, e.Item.Width - 3f), Math.Max(4f, e.Item.Height - 3f));

            using (System.Drawing.Drawing2D.GraphicsPath path = Grimoire.UI.CodexThemeEngine.CreateRoundedRectPath(pillRect, 6f))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }
        }
        else
        {
            Rectangle rect = new Rectangle(2, 0, e.Item.Width - 2, e.Item.Height);
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillRectangle(brush, rect);
            }
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        bool isActive = e.Item.Selected || (e.Item is ToolStripMenuItem mi && mi.DropDown.Visible);
        if (!e.Item.Enabled)
        {
            e.TextColor = Grimoire.UI.UISkinManager.SecondaryTextColor;
        }
        else if (Grimoire.UI.UISkinManager.CodexStyleEnabled && isActive && e.ToolStrip.IsDropDown)
        {
            e.TextColor = Grimoire.UI.CodexThemeEngine.GetContrastingTextColor(Grimoire.UI.CodexThemeEngine.GetEffectiveAccent());
        }
        else if (e.Item.ForeColor == Color.Gold)
        {
            e.TextColor = Color.Gold;
        }
        else
        {
            e.TextColor = Grimoire.UI.UISkinManager.TextColor;
        }

        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        if (!Grimoire.UI.UISkinManager.CodexStyleEnabled)
        {
            base.OnRenderItemCheck(e);
            return;
        }

        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Rectangle r = e.ImageRectangle;
        bool isSelected = e.Item != null && e.Item.Selected;
        Color checkColor = isSelected
            ? Grimoire.UI.CodexThemeEngine.GetContrastingTextColor(Grimoire.UI.CodexThemeEngine.GetEffectiveAccent())
            : Grimoire.UI.CodexThemeEngine.GetEffectiveAccent();

        using (Pen pen = new Pen(checkColor, 1.8f))
        {
            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            float x = r.X + 2f;
            float y = r.Y + 2f;
            float w = Math.Max(10f, r.Width - 4f);
            float h = Math.Max(10f, r.Height - 4f);
            g.DrawLines(pen, new[]
            {
                new PointF(x + w * 0.16f, y + h * 0.52f),
                new PointF(x + w * 0.42f, y + h * 0.78f),
                new PointF(x + w * 0.85f, y + h * 0.24f)
            });
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        Color line = Grimoire.UI.CodexThemeEngine.GetSoftBorderColor(0.38f);
        int y = e.Item.Height / 2;
        using (Pen pen = new Pen(line, 1f))
        {
            e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
        }
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        if (!Grimoire.UI.UISkinManager.CodexStyleEnabled)
        {
            base.OnRenderArrow(e);
            return;
        }

        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        bool isSelected = e.Item != null && e.Item.Selected;
        Color arrowColor = isSelected
            ? Grimoire.UI.CodexThemeEngine.GetContrastingTextColor(Grimoire.UI.CodexThemeEngine.GetEffectiveAccent())
            : Grimoire.UI.UISkinManager.SecondaryTextColor;

        Rectangle r = e.ArrowRectangle;
        float cx = r.Left + r.Width / 2f;
        float cy = r.Top + r.Height / 2f;
        using (Pen pen = new Pen(arrowColor, 1.5f))
        {
            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            g.DrawLines(pen, new[]
            {
                new PointF(cx - 2f, cy - 3.5f),
                new PointF(cx + 2f, cy),
                new PointF(cx - 2f, cy + 3.5f)
            });
        }
    }
}