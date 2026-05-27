using JobSearchBuilder.Models;
using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Services;
using JobSearchBuilder.Services.Providers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JobSearchBuilder
{
    public partial class MainForm : Form
    {
        private readonly AppSettings _config;
        private readonly IProfileStore _store;
        private readonly QueryBuilder _queryBuilder;
        private ILlmProvider _provider;
        private Label _lblModelId;

        private SearchProfile _workingProfile;
        private bool _isDirty;
        private bool _isLoading;
        private QueryResult _lastQueryResult;
        private ComboBox _cboLocationPicker;

        public MainForm()
        {
            _config = AppSettingsLoader.Load();
            _store = new SqlProfileStore(new SqlConnectionFactory(_config.ConnectionString));
            _queryBuilder = new QueryBuilder(_config.AtsSourceGroups);
            try
            {
                _provider = LlmProviderFactory.Create(_config);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AI provider init failed: " + ex.Message);
                _provider = null;
            }
            _workingProfile = new SearchProfile();

            InitializeComponent();   
            PostInitialize();      
            _isDirty = false;        

            PopulateProfileList();
            if (lstProfiles.Items.Count > 0)
                lstProfiles.SelectedIndex = 0;

            LoadCountriesAsync();
        }

        //Post-initialization UI setup: populating dropdowns, styling, wiring up events that require controls to be created first.
        private void PostInitialize()
        {
            btnSaveProfile.BringToFront();
            txtQueryPreview.BringToFront(); 
            // Seniority combo items
            foreach (string level in _config.SeniorityLevels)
                cboSeniority.Items.Add(level);
            if (cboSeniority.Items.Count > 0)
                cboSeniority.SelectedIndex = 0;

            // ATS groups checklist — height adjusted to fit all items
            foreach (AtsSourceGroup g in _config.AtsSourceGroups)
                clbAtsGroups.Items.Add(g, false);
            clbAtsGroups.Height = (_config.AtsSourceGroups.Count * 22) + 6;

            // Section headers and chip panel styling
            ConfigureSectionHeader(lblStackHeader, "TECH STACK");
            ConfigureSectionHeader(lblRolesHeader, "ROLES");
            ConfigureSectionHeader(lblLocationsHeader, "LOCATIONS");
            ConfigureSectionHeader(lblVisaHeader, "VISA FILTERS");
            ConfigureSectionHeader(lblRemoteHeader, "REMOTE / HYBRID");
            ConfigureSectionHeader(lblTimezoneHeader, "TIMEZONE");
            ConfigureSectionHeader(lblExcludeHeader, "EXCLUDE TERMS  (added as -\"term\" to block unwanted results)");
            lblExcludeHeader.ForeColor = Color.FromArgb(180, 50, 50);

            ConfigureChipPanel(flpStack);
            ConfigureChipPanel(flpRoles);
            ConfigureChipPanel(flpLocations);
            ConfigureChipPanel(flpVisa);
            ConfigureChipPanel(flpRemote);
            ConfigureChipPanel(flpTimezone);
            ConfigureChipPanel(flpExclude);
            flpExclude.BackColor = Color.FromArgb(255, 248, 248);

            // Suggestion buttons for each keyword section
            List<string> stackSugg = new List<string> { "C#", ".NET", "ASP.NET Core", "Azure", "React", "TypeScript", "Python", "Java" };
            List<string> roleSugg = new List<string>(_config.CommonRoles);
            List<string> visaSugg = new List<string>(_config.CommonVisaTerms);
            List<string> remoteSugg = new List<string>(_config.CommonRemoteTerms);
            List<string> excludeSugg = new List<string>(_config.CommonExcludeTerms);
            List<string> timezoneSugg = new List<string>(_config.CommonTimezoneTerms);

            AddSuggestionButtons(flpStackAddRow, flpStack, txtAddStack, stackSugg);
            AddSuggestionButtons(flpRolesAddRow, flpRoles, txtAddRole, roleSugg);
            AddSuggestionButtons(flpVisaAddRow, flpVisa, txtAddVisa, visaSugg);
            AddSuggestionButtons(flpRemoteAddRow, flpRemote, txtAddRemote, remoteSugg);
            AddSuggestionButtons(flpTimezoneAddRow, flpTimezone, txtAddTimezone, timezoneSugg);
            AddSuggestionButtons(flpExcludeAddRow, flpExclude, txtAddExclude, excludeSugg, isExclude: true);

            // Locations: searchable dropdown populated async from CountryService
            _cboLocationPicker = new ComboBox
            {
                Width = 260,
                Font = new Font("Segoe UI", 9f),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                Margin = new Padding(0, 0, 8, 0)
            };
            _cboLocationPicker.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(_cboLocationPicker.Text))
                {
                    AddChip(flpLocations, _cboLocationPicker.Text.Trim());
                    _cboLocationPicker.Text = string.Empty;
                    e.SuppressKeyPress = true;
                    MarkDirtyAndRebuild();
                }
            };
            _cboLocationPicker.SelectionChangeCommitted += (s, e) =>
            {
                if (_cboLocationPicker.SelectedItem != null)
                {
                    AddChip(flpLocations, _cboLocationPicker.SelectedItem.ToString());
                    _cboLocationPicker.SelectedIndex = -1;
                    _cboLocationPicker.Text = string.Empty;
                    MarkDirtyAndRebuild();
                }
            };
            flpLocationsAddRow.Controls.Add(_cboLocationPicker);

            // Wire up Enter-key handler for each add-box
            WireAddBox(txtAddStack, flpStack);
            WireAddBox(txtAddRole, flpRoles);
            WireAddBox(txtAddVisa, flpVisa);
            WireAddBox(txtAddRemote, flpRemote);
            WireAddBox(txtAddTimezone, flpTimezone);
            WireAddBox(txtAddExclude, flpExclude, isExclude: true);

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(240, 242, 248),
                Padding = new Padding(8, 0, 12, 0)
            };

            Label lblProviderCaption = new Label
            {
                Text = "Provider:",
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 100, 120),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblProviderCaption.Top = (pnlFooter.Height - lblProviderCaption.PreferredHeight) / 2;
            lblProviderCaption.Left = 8;

            ComboBox cboProvider = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8f),
                Width = 110
            };
            cboProvider.Items.AddRange(new object[] { "Anthropic", "OpenAI", "Gemini" });
            int providerIdx = cboProvider.Items.IndexOf(_provider != null ? _provider.ProviderName : "Anthropic");
            cboProvider.SelectedIndex = providerIdx >= 0 ? providerIdx : 0;
            cboProvider.Top = (pnlFooter.Height - cboProvider.Height) / 2;
            cboProvider.Left = lblProviderCaption.Left + lblProviderCaption.PreferredWidth + 4;

            _lblModelId = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 120),
                Text = _provider != null ? _provider.ModelId : string.Empty
            };
            _lblModelId.Top = (pnlFooter.Height - _lblModelId.PreferredHeight) / 2;
            _lblModelId.Left = cboProvider.Left + cboProvider.Width + 10;

            cboProvider.SelectedIndexChanged += (s, e) =>
            {
                string selected = cboProvider.SelectedItem.ToString();
                try
                {
                    _provider = LlmProviderFactory.Create(_config, selected);
                    _lblModelId.Text = _provider.ModelId;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Provider switch failed: " + ex.Message);
                    _provider = null;
                    _lblModelId.Text = "unavailable";
                }
            };

            pnlFooter.Controls.Add(lblProviderCaption);
            pnlFooter.Controls.Add(cboProvider);
            pnlFooter.Controls.Add(_lblModelId);
            this.Controls.Add(pnlFooter);
        }

        private async void LoadCountriesAsync()
        {
            try
            {
                List<string> countries = await Task.Run(() => new CountryService().GetCountries());

                AutoCompleteStringCollection source = new AutoCompleteStringCollection();
                source.AddRange(countries.ToArray());
                _cboLocationPicker.AutoCompleteCustomSource = source;

                _cboLocationPicker.Items.AddRange(countries.ToArray());
            }
            catch
            {
                // Fall back silently — user can still type a location manually
            }
        }

        private void AddSuggestionButtons(FlowLayoutPanel addRow, FlowLayoutPanel chipPanel,
                                          TextBox addBox, List<string> suggestions, bool isExclude = false)
        {
            Color btnBack = isExclude ? Color.FromArgb(255, 240, 240) : Color.FromArgb(240, 243, 255);
            Color btnFore = isExclude ? Color.FromArgb(180, 50, 50)   : Color.FromArgb(37, 99, 235);

            foreach (string suggestion in suggestions.Take(8))
            {
                string sug = suggestion;
                Button btn = new Button
                {
                    Text = sug,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = btnBack,
                    ForeColor = btnFore,
                    Font = new Font("Segoe UI", 8.5f),
                    Cursor = Cursors.Hand,
                    Height = 26,
                    Padding = new Padding(6, 2, 6, 2),
                    Margin = new Padding(2, 0, 2, 0)
                };
                btn.FlatAppearance.BorderColor = btnFore;
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += (s, e) =>
                {
                    AddChip(chipPanel, sug, isExclude);
                    MarkDirtyAndRebuild();
                };
                addRow.Controls.Add(btn);
            }
        }

        private void WireAddBox(TextBox addBox, FlowLayoutPanel chipPanel, bool isExclude = false)
        {
            // Embed the textbox inline inside the chip panel, styled to be invisible
            addBox.BorderStyle = BorderStyle.None;
            addBox.BackColor = isExclude ? Color.FromArgb(255, 248, 248) : Color.White;
            addBox.ForeColor = isExclude ? Color.FromArgb(160, 30, 30) : Color.FromArgb(30, 60, 120);
            addBox.Font = new Font("Segoe UI", 9f);
            addBox.Width = 140;
            addBox.Height = 20;
            addBox.Margin = new Padding(4, 3, 4, 3);
            chipPanel.Controls.Add(addBox);

            // Clicking anywhere on the panel focuses the inline input
            chipPanel.Click += (s, e) => addBox.Focus();

            addBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(addBox.Text))
                {
                    AddChip(chipPanel, addBox.Text.Trim(), isExclude);
                    addBox.Clear();
                    e.SuppressKeyPress = true;
                    MarkDirtyAndRebuild();
                }
            };
        }

        // -------------------------------------------------------------------
        // Designer event handlers
        // -------------------------------------------------------------------

        private void lstProfiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            SearchProfile profile = (SearchProfile)lstProfiles.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            e.Graphics.FillRectangle(
                new SolidBrush(selected ? Color.FromArgb(37, 99, 235) : Color.White),
                e.Bounds);

            e.Graphics.DrawString(
                profile.Name,
                new Font("Segoe UI", 9.5f),
                new SolidBrush(selected ? Color.White : Color.FromArgb(30, 30, 50)),
                e.Bounds.X + 10,
                e.Bounds.Y + 6);
        }

        private void lstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            SearchProfile profile = lstProfiles.SelectedItem as SearchProfile;
            if (profile == null) return;

            if (_isDirty)
            {
                DialogResult r = MessageBox.Show(
                    "You have unsaved changes. Discard them?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (r == DialogResult.No)
                {
                    lstProfiles.SelectedIndexChanged -= lstProfiles_SelectedIndexChanged;
                    lstProfiles.SelectedItem = _workingProfile;
                    lstProfiles.SelectedIndexChanged += lstProfiles_SelectedIndexChanged;
                    return;
                }
            }

            LoadProfileIntoUi(profile);
        }

        private void txtProfileName_TextChanged(object sender, EventArgs e)
        {
            MarkDirtyAndRebuild();
        }

        private void cboSeniority_SelectedIndexChanged(object sender, EventArgs e)
        {
            MarkDirtyAndRebuild();
        }

        private void clbAtsGroups_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (IsHandleCreated)
                BeginInvoke((MethodInvoker)(() => MarkDirtyAndRebuild()));
        }

        private void btnNewProfile_Click(object sender, EventArgs e)
        {
            SearchProfile fresh = new SearchProfile { Name = "New Profile" };
            LoadProfileIntoUi(fresh);
            lstProfiles.ClearSelected();
            txtProfileName.Focus();
            txtProfileName.SelectAll();
        }

        private void btnSaveProfile_Click(object sender, EventArgs e)
        {
            SearchProfile profile = ReadProfileFromUi();

            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                MessageBox.Show("Please give the profile a name.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtProfileName.Focus();
                return;
            }

            _store.Save(profile);
            _workingProfile = profile;
            _isDirty = false;

            PopulateProfileList();

            for (int i = 0; i < lstProfiles.Items.Count; i++)
            {
                if (((SearchProfile)lstProfiles.Items[i]).Id == profile.Id)
                {
                    lstProfiles.SelectedIndex = i;
                    break;
                }
            }
        }

        private void btnDeleteProfile_Click(object sender, EventArgs e)
        {
            SearchProfile profile = lstProfiles.SelectedItem as SearchProfile;
            if (profile == null) return;

            DialogResult r = MessageBox.Show(
                "Delete \"" + profile.Name + "\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (r != DialogResult.Yes) return;

            _store.Delete(profile.Id);
            PopulateProfileList();
            if (lstProfiles.Items.Count > 0)
                lstProfiles.SelectedIndex = 0;
        }

        private void btnOpenInGoogle_Click(object sender, EventArgs e)
        {
            if (_lastQueryResult == null || string.IsNullOrWhiteSpace(_lastQueryResult.GoogleSearchUrl))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = _lastQueryResult.GoogleSearchUrl,
                UseShellExecute = true
            });
        }

        private void btnCopyQuery_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtQueryPreview.Text)) return;

            Clipboard.SetText(txtQueryPreview.Text);
            btnCopyQuery.Text = "Copied!";

            Timer timer = new Timer { Interval = 1500 };
            timer.Tick += (s2, e2) =>
            {
                btnCopyQuery.Text = "Copy";
                timer.Stop();
            };
            timer.Start();
        }

        // -------------------------------------------------------------------
        // Profile list
        // -------------------------------------------------------------------

        private void PopulateProfileList()
        {
            lstProfiles.Items.Clear();
            foreach (SearchProfile p in _store.GetAll())
                lstProfiles.Items.Add(p);
        }

        // -------------------------------------------------------------------
        // Load profile into UI
        // -------------------------------------------------------------------

        private void LoadProfileIntoUi(SearchProfile profile)
        {
            _workingProfile = profile;  
            _isLoading = true;

            txtProfileName.Text = profile.Name;

            int idx = cboSeniority.Items.IndexOf(profile.Seniority);
            cboSeniority.SelectedIndex = idx >= 0 ? idx : 0;

            for (int i = 0; i < clbAtsGroups.Items.Count; i++)
            {
                AtsSourceGroup group = (AtsSourceGroup)clbAtsGroups.Items[i];
                clbAtsGroups.SetItemChecked(i, profile.SourceGroupIds.Contains(group.Id));
            }

            ClearChips(flpStack);
            foreach (string k in profile.StackKeywords ?? Enumerable.Empty<string>()) AddChip(flpStack, k);

            ClearChips(flpRoles);
            foreach (string k in profile.RoleKeywords ?? Enumerable.Empty<string>()) AddChip(flpRoles, k);

            ClearChips(flpLocations);
            foreach (string k in profile.LocationFilters ?? Enumerable.Empty<string>()) AddChip(flpLocations, k);

            ClearChips(flpVisa);
            foreach (string k in profile.VisaFilters ?? Enumerable.Empty<string>()) AddChip(flpVisa, k);

            ClearChips(flpRemote);
            foreach (string k in profile.RemoteFilters ?? Enumerable.Empty<string>()) AddChip(flpRemote, k);

            ClearChips(flpTimezone);
            foreach (string k in profile.TimezoneFilters ?? Enumerable.Empty<string>()) AddChip(flpTimezone, k);

            ClearChips(flpExclude);
            foreach (string k in profile.ExcludeKeywords ?? Enumerable.Empty<string>()) AddChip(flpExclude, k, isExclude: true);
            _isLoading = false;
            _isDirty = false;
            RebuildQuery();
        }

        // -------------------------------------------------------------------
        // Read UI -> SearchProfile
        // -------------------------------------------------------------------

        private SearchProfile ReadProfileFromUi()
        {
            return new SearchProfile
            {
                Id = _workingProfile.Id,
                CreatedAt = _workingProfile.CreatedAt,
                Name = txtProfileName.Text.Trim(),
                Seniority = cboSeniority.SelectedItem != null
                                    ? cboSeniority.SelectedItem.ToString()
                                    : "Any",
                StackKeywords = GetChips(flpStack),
                RoleKeywords = GetChips(flpRoles),
                LocationFilters = GetChips(flpLocations),
                VisaFilters = GetChips(flpVisa),
                RemoteFilters = GetChips(flpRemote),
                TimezoneFilters = GetChips(flpTimezone),
                ExcludeKeywords = GetChips(flpExclude),
                SourceGroupIds = clbAtsGroups.CheckedItems
                                     .OfType<AtsSourceGroup>()
                                     .Select(g => g.Id)
                                     .ToList()
            };
        }

        // -------------------------------------------------------------------
        // Query rebuild
        // -------------------------------------------------------------------

        private void RebuildQuery()
        {
            try
            {
                QueryResult result = _queryBuilder.Build(ReadProfileFromUi());
                txtQueryPreview.Text = result.RawQuery;
                _lastQueryResult = result;
                btnOpenInGoogle.Enabled = !string.IsNullOrWhiteSpace(result.GoogleSearchUrl);
            }
            catch (Exception ex)
            {
                txtQueryPreview.Text = "[Error building query: " + ex.Message + "]";
                btnOpenInGoogle.Enabled = false;
            }
        }

        private void MarkDirtyAndRebuild()
        {
            if (_isLoading) return;
            _isDirty = true;
            RebuildQuery();
        }

        // -------------------------------------------------------------------
        // Chip helpers
        // -------------------------------------------------------------------

        private void AddChip(FlowLayoutPanel panel, string term, bool isExclude = false)
        {
            foreach (Control c in panel.Controls)
                if (c.Tag is string t && string.Equals(t, term, StringComparison.OrdinalIgnoreCase))
                    return;

            int chipWidth = TextRenderer.MeasureText(term, new Font("Segoe UI", 9f)).Width + 38;

            Color chipBack = isExclude ? Color.FromArgb(255, 228, 228) : Color.FromArgb(235, 240, 255);
            Color chipFore = isExclude ? Color.FromArgb(160, 30, 30)   : Color.FromArgb(30, 60, 120);

            Panel chip = new Panel
            {
                Height = 26,
                Width = chipWidth,
                BackColor = chipBack,
                Margin = new Padding(2),
                Tag = term
            };

            Label lbl = new Label
            {
                Text = term,
                AutoSize = false,
                Width = chipWidth - 22,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f),
                ForeColor = chipFore,
                Location = new Point(6, 0)
            };

            Label close = new Label
            {
                Text = "x",
                AutoSize = false,
                Width = 18,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 150, 170),
                Cursor = Cursors.Hand,
                Location = new Point(chipWidth - 20, 0)
            };

            Panel chipRef = chip;
            close.Click += (s, e) => { panel.Controls.Remove(chipRef); MarkDirtyAndRebuild(); };
            close.MouseEnter += (s, e) => close.ForeColor = Color.FromArgb(220, 50, 50);
            close.MouseLeave += (s, e) => close.ForeColor = Color.FromArgb(150, 150, 170);

            chip.Controls.Add(lbl);
            chip.Controls.Add(close);
            panel.Controls.Add(chip);

            // Keep inline TextBox last so it always appears after all chips
            TextBox inlineBox = panel.Controls.OfType<TextBox>().FirstOrDefault();
            if (inlineBox != null)
                panel.Controls.SetChildIndex(inlineBox, panel.Controls.Count - 1);
        }

        private static List<string> GetChips(FlowLayoutPanel panel)
        {
            List<string> result = new List<string>();
            foreach (Control c in panel.Controls)
                if (c is Panel && c.Tag is string t)
                    result.Add(t);
            return result;
        }

        private void ClearChips(FlowLayoutPanel panel)
        {
            List<Control> toRemove = panel.Controls.OfType<Panel>().Cast<Control>().ToList();
            foreach (Control c in toRemove)
                panel.Controls.Remove(c);
        }
    }
}
