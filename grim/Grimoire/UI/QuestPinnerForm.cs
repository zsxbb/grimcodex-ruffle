using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DarkUI.Forms;
using Grimoire.Game;
using Grimoire.Tools;
using Grimoire.Networking;
using Newtonsoft.Json;

namespace Grimoire.UI
{
    public partial class QuestPinnerForm : GrimDarkForm
    {
        public static QuestPinnerForm Instance { get; } = new QuestPinnerForm();

        private readonly string filePath = Path.Combine(Application.StartupPath, "PinnedQuests.json");

        private List<PinnedQuest> pinnedQuests = new List<PinnedQuest>();

        public class PinnedQuest
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return string.IsNullOrEmpty(Name) ? Id.ToString() : $"{Id} - {Name}";
            }
        }

        private System.Windows.Forms.Timer _autoTurnInTimer;
        private List<int> _autoTurnInQuestIds = new List<int>();
        private bool _isAutoTurnInActive = false;
        private bool _isProcessingTurnIn = false;

        private QuestPinnerForm()
        {
            InitializeComponent();
            this.TopMost = true;

            _autoTurnInTimer = new System.Windows.Forms.Timer();
            _autoTurnInTimer.Interval = 1500;
            _autoTurnInTimer.Tick += AutoTurnInTimer_Tick;
        }

        private void QuestPinnerForm_Load(object sender, EventArgs e)
        {
            LoadPinnedQuests();
        }

        private void QuestPinnerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private static List<PinnedQuest> CreateDefaultPinnedQuests()
        {
            return new List<PinnedQuest>
            {
                new PinnedQuest { Id = 2330, Name = "Crafted: Scrolls of Enrage" },
                new PinnedQuest { Id = 9305, Name = "Templeshrine Mid Daily" },
                new PinnedQuest { Id = 9303, Name = "Templeshrine Left Daily" },
                new PinnedQuest { Id = 9304, Name = "Templeshrine Right Daily" },
                new PinnedQuest { Id = 8245, Name = "Ultra Tyndarius Daily" },
                new PinnedQuest { Id = 8154, Name = "Ultra Engineer Daily" },
                new PinnedQuest { Id = 8153, Name = "Ultra Warden Daily" },
                new PinnedQuest { Id = 8152, Name = "Ultra Ezrajal Daily" },
                new PinnedQuest { Id = 8216, Name = "Ultra Kala Daily" },
                new PinnedQuest { Id = 8262, Name = "Ultra Iara Daily" },
                new PinnedQuest { Id = 9350, Name = "Kathooldepth: Frenzy Feast Daily" },
                new PinnedQuest { Id = 9803, Name = "Astralshrine: Asterism Toll Daily" },
                new PinnedQuest { Id = 1677, Name = "Exercise 5: God of Depths Daily" },
                new PinnedQuest { Id = 1678, Name = "Exercise 6: Dage Dark Lord Daily" },
                new PinnedQuest { Id = 8653, Name = "Encroaching Daily" },
                new PinnedQuest { Id = 9091, Name = "Wrong Turn at Voidbqq Daily" },
                new PinnedQuest { Id = 9173, Name = "Ultra Speaker Weekly" },
                new PinnedQuest { Id = 8746, Name = "Ultra Darkon Weekly" },
                new PinnedQuest { Id = 8300, Name = "Champion Drakath Weekly" },
                new PinnedQuest { Id = 10301, Name = "Ultra Gramiel Weekly" },
                new PinnedQuest { Id = 8397, Name = "Ultra Drago Weekly" },
                new PinnedQuest { Id = 8547, Name = "Ultra Dage Weekly" },
                new PinnedQuest { Id = 8692, Name = "Ultra Nulgath Weekly" }
            };
        }

        private static bool IsLegacyPlaceholderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            return name.StartsWith("Daily / Ultra Quest ", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Daily Quest ", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Ultra Drakath Weekly", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Ultra Engineer Weekly", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Ultra Warden Weekly", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Ultra Ezrajal Weekly", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Ultra Boss Weekly", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadPinnedQuests()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    pinnedQuests = JsonConvert.DeserializeObject<List<PinnedQuest>>(json) ?? new List<PinnedQuest>();
                    bool modified = false;

                    // Migrate legacy placeholder default file to the new 23-quest template while keeping user additions
                    if (pinnedQuests.Any(q => IsLegacyPlaceholderName(q.Name)))
                    {
                        List<PinnedQuest> defaults = CreateDefaultPinnedQuests();
                        Dictionary<int, PinnedQuest> existingById = pinnedQuests
                            .GroupBy(q => q.Id)
                            .ToDictionary(g => g.Key, g => g.First());

                        List<PinnedQuest> merged = new List<PinnedQuest>(defaults);
                        foreach (var custom in pinnedQuests)
                        {
                            if (!merged.Any(d => d.Id == custom.Id))
                            {
                                merged.Add(custom);
                            }
                        }
                        pinnedQuests = merged;
                        modified = true;
                    }
                    else if (!pinnedQuests.Any(q => q.Id == 2330))
                    {
                        pinnedQuests.Insert(0, new PinnedQuest { Id = 2330, Name = "Crafted: Scrolls of Enrage" });
                        modified = true;
                    }

                    if (modified)
                    {
                        SavePinnedQuests();
                    }
                }
                else
                {
                    pinnedQuests = CreateDefaultPinnedQuests();
                    SavePinnedQuests();
                }
            }
            catch
            {
                pinnedQuests = CreateDefaultPinnedQuests();
            }

            UpdateList();
        }

        private void SavePinnedQuests()
        {
            try
            {
                string json = JsonConvert.SerializeObject(pinnedQuests, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save pinned quests: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateList()
        {
            lstQuests.Items.Clear();
            foreach (var quest in pinnedQuests)
            {
                lstQuests.Items.Add(quest);
            }
        }

        private void btnAddQuest_Click(object sender, EventArgs e)
        {
            int questId = (int)numQuestId.Value;
            if (questId <= 0)
            {
                MessageBox.Show("Please enter a valid Quest ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (pinnedQuests.Any(q => q.Id == questId))
            {
                MessageBox.Show("This Quest ID is already pinned.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = tbQuestName.Text.Trim();
            pinnedQuests.Add(new PinnedQuest { Id = questId, Name = name });
            UpdateList();

            // Clear inputs
            numQuestId.Value = 0;
            tbQuestName.Text = "";
        }

        private void btnRemoveQuest_Click(object sender, EventArgs e)
        {
            var selectedQuests = lstQuests.SelectedItems.Cast<PinnedQuest>().ToList();
            if (selectedQuests.Count > 0)
            {
                foreach (var selected in selectedQuests)
                {
                    pinnedQuests.Remove(selected);
                }
                UpdateList();
            }
            else
            {
                MessageBox.Show("Please select a quest to remove.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnLoadSelected_Click(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn)
            {
                MessageBox.Show("Please log in first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedQuests = lstQuests.SelectedItems.Cast<PinnedQuest>().ToList();
            if (selectedQuests.Count > 0)
            {
                List<int> ids = selectedQuests.Select(q => q.Id).ToList();
                Player.Quests.Load(ids);
            }
            else
            {
                MessageBox.Show("Please select a quest to load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnAcceptSelected_Click(object sender, EventArgs e)
        {
            if (!Player.IsLoggedIn)
            {
                MessageBox.Show("Please log in first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedQuests = lstQuests.SelectedItems.Cast<PinnedQuest>().ToList();
            if (selectedQuests.Count == 0)
            {
                MessageBox.Show("Please select at least one quest to accept.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var quest in selectedQuests)
            {
                if (!Player.Quests.HasQuest(quest.Id))
                {
                    Player.Quests.Load(quest.Id);
                    int delayCount = 0;
                    while (!Player.Quests.HasQuest(quest.Id) && delayCount < 10)
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                        delayCount++;
                    }
                }
                Player.Quests.Accept(quest.Id);
                await System.Threading.Tasks.Task.Delay(700);
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            var selectedIndices = lstQuests.SelectedIndices.Cast<int>().OrderBy(i => i).ToList();
            if (selectedIndices.Count == 0 || selectedIndices[0] == 0)
                return;

            foreach (int index in selectedIndices)
            {
                var item = pinnedQuests[index];
                pinnedQuests.RemoveAt(index);
                pinnedQuests.Insert(index - 1, item);
            }

            UpdateList();
            foreach (int index in selectedIndices)
            {
                lstQuests.SetSelected(index - 1, true);
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            var selectedIndices = lstQuests.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
            if (selectedIndices.Count == 0 || selectedIndices[0] == pinnedQuests.Count - 1)
                return;

            foreach (int index in selectedIndices)
            {
                var item = pinnedQuests[index];
                pinnedQuests.RemoveAt(index);
                pinnedQuests.Insert(index + 1, item);
            }

            UpdateList();
            foreach (int index in selectedIndices)
            {
                lstQuests.SetSelected(index + 1, true);
            }
        }

        private void btnAutoTurnIn_Click(object sender, EventArgs e)
        {
            if (!_isAutoTurnInActive)
            {
                if (!Player.IsLoggedIn)
                {
                    MessageBox.Show("Please log in first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedQuests = lstQuests.SelectedItems.Cast<PinnedQuest>().ToList();
                if (selectedQuests.Count == 0)
                {
                    MessageBox.Show("Please select at least one quest for auto turn in.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _autoTurnInQuestIds = selectedQuests.Select(q => q.Id).ToList();

                List<int> toLoad = _autoTurnInQuestIds.Where(id => !Player.Quests.HasQuest(id)).ToList();
                if (toLoad.Count > 0)
                {
                    Player.Quests.Load(toLoad);
                }

                _isAutoTurnInActive = true;
                btnAutoTurnIn.Text = "Off Turn In Selected";
                btnAutoTurnIn.Checked = true;
                _autoTurnInTimer.Start();
            }
            else
            {
                StopAutoTurnIn();
            }
        }

        private void StopAutoTurnIn()
        {
            _isAutoTurnInActive = false;
            _autoTurnInTimer?.Stop();
            _autoTurnInQuestIds.Clear();
            if (btnAutoTurnIn != null)
            {
                btnAutoTurnIn.Text = "Auto Turn In Selected";
                btnAutoTurnIn.Checked = false;
            }
        }

        private async void AutoTurnInTimer_Tick(object sender, EventArgs e)
        {
            if (_isProcessingTurnIn) return;
            if (!Player.IsLoggedIn) return;
            if (_autoTurnInQuestIds == null || _autoTurnInQuestIds.Count == 0) return;

            _isProcessingTurnIn = true;
            try
            {
                foreach (int questId in _autoTurnInQuestIds.ToList())
                {
                    if (!_isAutoTurnInActive) break;

                    if (Player.Quests.CanComplete(questId))
                    {
                        Player.Quests.Complete(questId);
                        LogForm.Instance?.AppendDebug($"[Quest Pinner] Auto turned in quest {questId}");
                        await System.Threading.Tasks.Task.Delay(500);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _isProcessingTurnIn = false;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SavePinnedQuests();
            MessageBox.Show("Pinned quests list saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
