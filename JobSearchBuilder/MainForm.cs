using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JobSearchBuilder
{
    public partial class MainForm : Form
    {
        // -------------------------------------------------------------------
        // Services & state
        // -------------------------------------------------------------------
        private readonly AppSettings _config;
        private readonly IProfileStore _store;
        private readonly QueryBuilder _queryBuilder;

        private SearchProfile _workingProfile;
        private bool _isDirty;
        private bool _isLoading;
        private QueryResult _lastQueryResult;

        // -------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------

        public MainForm()
        {
            _config = AppSettingsLoader.Load();
            _store = new SqlProfileStore(new SqlConnectionFactory(_config.ConnectionString));
            _queryBuilder = new QueryBuilder(_config.AtsSourceGroups);
            _workingProfile = new SearchProfile();

            InitializeComponent();   // designer-generated wiring
            PostInitialize();        // runtime-only: populate combos, add suggestion buttons
            _isDirty = false;        // PostInitialize triggers change events; reset before first profile load

            PopulateProfileList();
            if (lstProfiles.Items.Count > 0)
                lstProfiles.SelectedIndex = 0;
        }

        // -------------------------------------------------------------------
        // PostInitialize — things that can't go in the designer because they
        // depend on data from appsettings.json
        // -------------------------------------------------------------------

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

            ConfigureChipPanel(flpStack);
            ConfigureChipPanel(flpRoles);
            ConfigureChipPanel(flpLocations);
            ConfigureChipPanel(flpVisa);
            ConfigureChipPanel(flpRemote);

            // Suggestion buttons for each keyword section
            List<string> stackSugg = new List<string> { "C#", ".NET", "ASP.NET Core", "Azure", "React", "TypeScript", "Python", "Java" };
            List<string> roleSugg = new List<string>(_config.CommonRoles);
            List<string> locationSugg = new List<string>(_config.CommonLocations);
            List<string> visaSugg = new List<string>(_config.CommonVisaTerms);
            List<string> remoteSugg = new List<string>(_config.CommonRemoteTerms);

            AddSuggestionButtons(flpStackAddRow, flpStack, txtAddStack, stackSugg);
            AddSuggestionButtons(flpRolesAddRow, flpRoles, txtAddRole, roleSugg);
            AddSuggestionButtons(flpLocationsAddRow, flpLocations, txtAddLocation, locationSugg);
            AddSuggestionButtons(flpVisaAddRow, flpVisa, txtAddVisa, visaSugg);
            AddSuggestionButtons(flpRemoteAddRow, flpRemote, txtAddRemote, remoteSugg);

            // Wire up Enter-key handler for each add-box
            WireAddBox(txtAddStack, flpStack);
            WireAddBox(txtAddRole, flpRoles);
            WireAddBox(txtAddLocation, flpLocations);
            WireAddBox(txtAddVisa, flpVisa);
            WireAddBox(txtAddRemote, flpRemote);
        }

        private void AddSuggestionButtons(FlowLayoutPanel addRow, FlowLayoutPanel chipPanel,
                                          TextBox addBox, List<string> suggestions)
        {
            foreach (string suggestion in suggestions.Take(8))
            {
                string sug = suggestion;
                Button btn = new Button
                {
                    Text = sug,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(240, 243, 255),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    Font = new Font("Segoe UI", 8.5f),
                    Cursor = Cursors.Hand,
                    Height = 26,
                    Padding = new Padding(6, 2, 6, 2),
                    Margin = new Padding(2, 0, 2, 0)
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += (s, e) =>
                {
                    AddChip(chipPanel, sug);
                    MarkDirtyAndRebuild();
                };
                addRow.Controls.Add(btn);
            }
        }

        private void WireAddBox(TextBox addBox, FlowLayoutPanel chipPanel)
        {
            addBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(addBox.Text))
                {
                    AddChip(chipPanel, addBox.Text.Trim());
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
            foreach (string k in profile.StackKeywords) AddChip(flpStack, k);

            ClearChips(flpRoles);
            foreach (string k in profile.RoleKeywords) AddChip(flpRoles, k);

            ClearChips(flpLocations);
            foreach (string k in profile.LocationFilters) AddChip(flpLocations, k);

            ClearChips(flpVisa);
            foreach (string k in profile.VisaFilters) AddChip(flpVisa, k);

            ClearChips(flpRemote);
            foreach (string k in profile.RemoteFilters) AddChip(flpRemote, k);
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

        private void AddChip(FlowLayoutPanel panel, string term)
        {
            foreach (Control c in panel.Controls)
                if (c.Tag is string t && string.Equals(t, term, StringComparison.OrdinalIgnoreCase))
                    return;

            int chipWidth = TextRenderer.MeasureText(term, new Font("Segoe UI", 9f)).Width + 38;

            Panel chip = new Panel
            {
                Height = 26,
                Width = chipWidth,
                BackColor = Color.FromArgb(235, 240, 255),
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
                ForeColor = Color.FromArgb(30, 60, 120),
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

        private void flpEditor_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tblMain_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pnlLeft_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flpProfileButtons_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblProfilesHeader_Click(object sender, EventArgs e)
        {

        }

        private void pnlEditor_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pnlScroll_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblAtsHeader_Click(object sender, EventArgs e)
        {

        }

        private void clbAtsGroups_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pnlAtsSpacer_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblStackHeader_Click(object sender, EventArgs e)
        {

        }

        private void flpStack_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flpStackAddRow_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblRolesHeader_Click(object sender, EventArgs e)
        {

        }

        private void flpRoles_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flpRolesAddRow_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblLocationsHeader_Click(object sender, EventArgs e)
        {

        }

        private void flpLocations_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flpLocationsAddRow_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblVisaHeader_Click(object sender, EventArgs e)
        {

        }

        private void flpVisa_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flpVisaAddRow_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblRemoteHeader_Click(object sender, EventArgs e)
        {

        }

        private void flpRemote_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flpRemoteAddRow_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pnlPreview_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtQueryPreview_TextChanged(object sender, EventArgs e)
        {

        }

        private void flpPreviewButtons_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblPreviewHeader_Click(object sender, EventArgs e)
        {

        }

        private void tblTopRow_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblProfileName_Click(object sender, EventArgs e)
        {

        }

        private void lblSeniority_Click(object sender, EventArgs e)
        {

        }

        private void txtAddStack_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtAddRole_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtAddLocation_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtAddVisa_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtAddRemote_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
