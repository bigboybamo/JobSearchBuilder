namespace JobSearchBuilder
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tblMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.lstProfiles = new System.Windows.Forms.ListBox();
            this.flpProfileButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnNewProfile = new System.Windows.Forms.Button();
            this.btnDeleteProfile = new System.Windows.Forms.Button();
            this.lblProfilesHeader = new System.Windows.Forms.Label();
            this.pnlEditor = new System.Windows.Forms.Panel();
            this.pnlScroll = new JobSearchBuilder.NoScrollJumpPanel();
            this.flpEditor = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAtsHeader = new System.Windows.Forms.Label();
            this.clbAtsGroups = new System.Windows.Forms.CheckedListBox();
            this.pnlAtsSpacer = new System.Windows.Forms.Panel();
            this.lblStackHeader = new System.Windows.Forms.Label();
            this.flpStack = new System.Windows.Forms.FlowLayoutPanel();
            this.flpStackAddRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblRolesHeader = new System.Windows.Forms.Label();
            this.flpRoles = new System.Windows.Forms.FlowLayoutPanel();
            this.flpRolesAddRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblLocationsHeader = new System.Windows.Forms.Label();
            this.flpLocations = new System.Windows.Forms.FlowLayoutPanel();
            this.flpLocationsAddRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblVisaHeader = new System.Windows.Forms.Label();
            this.flpVisa = new System.Windows.Forms.FlowLayoutPanel();
            this.flpVisaAddRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblRemoteHeader = new System.Windows.Forms.Label();
            this.flpRemote = new System.Windows.Forms.FlowLayoutPanel();
            this.flpRemoteAddRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTimezoneHeader = new System.Windows.Forms.Label();
            this.flpTimezone = new System.Windows.Forms.FlowLayoutPanel();
            this.flpTimezoneAddRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblExcludeHeader = new System.Windows.Forms.Label();
            this.flpExclude = new System.Windows.Forms.FlowLayoutPanel();
            this.flpExcludeAddRow = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.flpPreviewButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOpenInGoogle = new System.Windows.Forms.Button();
            this.btnCopyQuery = new System.Windows.Forms.Button();
            this.lblPreviewHeader = new System.Windows.Forms.Label();
            this.txtQueryPreview = new System.Windows.Forms.TextBox();
            this.tblTopRow = new System.Windows.Forms.TableLayoutPanel();
            this.lblProfileName = new System.Windows.Forms.Label();
            this.txtProfileName = new System.Windows.Forms.TextBox();
            this.lblSeniority = new System.Windows.Forms.Label();
            this.cboSeniority = new System.Windows.Forms.ComboBox();
            this.btnSaveProfile = new System.Windows.Forms.Button();
            this.txtAddStack = new System.Windows.Forms.TextBox();
            this.txtAddRole = new System.Windows.Forms.TextBox();
            this.txtAddLocation = new System.Windows.Forms.TextBox();
            this.txtAddVisa = new System.Windows.Forms.TextBox();
            this.txtAddRemote = new System.Windows.Forms.TextBox();
            this.txtAddTimezone = new System.Windows.Forms.TextBox();
            this.txtAddExclude = new System.Windows.Forms.TextBox();
            this.tblMain.SuspendLayout();
            this.pnlLeft.SuspendLayout();
            this.flpProfileButtons.SuspendLayout();
            this.pnlEditor.SuspendLayout();
            this.pnlScroll.SuspendLayout();
            this.flpEditor.SuspendLayout();
            this.pnlPreview.SuspendLayout();
            this.flpPreviewButtons.SuspendLayout();
            this.tblTopRow.SuspendLayout();
            this.SuspendLayout();
            // 
            // tblMain
            // 
            this.tblMain.ColumnCount = 2;
            this.tblMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tblMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMain.Controls.Add(this.pnlLeft, 0, 0);
            this.tblMain.Controls.Add(this.pnlEditor, 1, 0);
            this.tblMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMain.Location = new System.Drawing.Point(0, 0);
            this.tblMain.Name = "tblMain";
            this.tblMain.Padding = new System.Windows.Forms.Padding(12);
            this.tblMain.RowCount = 1;
            this.tblMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMain.Size = new System.Drawing.Size(1661, 737);
            this.tblMain.TabIndex = 0;
            // 
            // pnlLeft
            // 
            this.pnlLeft.Controls.Add(this.lstProfiles);
            this.pnlLeft.Controls.Add(this.flpProfileButtons);
            this.pnlLeft.Controls.Add(this.lblProfilesHeader);
            this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLeft.Location = new System.Drawing.Point(15, 15);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.pnlLeft.Size = new System.Drawing.Size(214, 707);
            this.pnlLeft.TabIndex = 0;
            // 
            // lstProfiles
            // 
            this.lstProfiles.BackColor = System.Drawing.Color.White;
            this.lstProfiles.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstProfiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstProfiles.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstProfiles.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lstProfiles.IntegralHeight = false;
            this.lstProfiles.ItemHeight = 28;
            this.lstProfiles.Location = new System.Drawing.Point(0, 28);
            this.lstProfiles.Name = "lstProfiles";
            this.lstProfiles.Size = new System.Drawing.Size(204, 643);
            this.lstProfiles.TabIndex = 0;
            this.lstProfiles.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lstProfiles_DrawItem);
            this.lstProfiles.SelectedIndexChanged += new System.EventHandler(this.lstProfiles_SelectedIndexChanged);
            // 
            // flpProfileButtons
            // 
            this.flpProfileButtons.Controls.Add(this.btnNewProfile);
            this.flpProfileButtons.Controls.Add(this.btnDeleteProfile);
            this.flpProfileButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpProfileButtons.Location = new System.Drawing.Point(0, 671);
            this.flpProfileButtons.Name = "flpProfileButtons";
            this.flpProfileButtons.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.flpProfileButtons.Size = new System.Drawing.Size(204, 36);
            this.flpProfileButtons.TabIndex = 1;
            // 
            // btnNewProfile
            // 
            this.btnNewProfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnNewProfile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNewProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewProfile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnNewProfile.ForeColor = System.Drawing.Color.White;
            this.btnNewProfile.Location = new System.Drawing.Point(0, 6);
            this.btnNewProfile.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnNewProfile.Name = "btnNewProfile";
            this.btnNewProfile.Size = new System.Drawing.Size(80, 28);
            this.btnNewProfile.TabIndex = 0;
            this.btnNewProfile.Text = "+ New";
            this.btnNewProfile.UseVisualStyleBackColor = false;
            this.btnNewProfile.Click += new System.EventHandler(this.btnNewProfile_Click);
            // 
            // btnDeleteProfile
            // 
            this.btnDeleteProfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnDeleteProfile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDeleteProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteProfile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDeleteProfile.ForeColor = System.Drawing.Color.White;
            this.btnDeleteProfile.Location = new System.Drawing.Point(86, 6);
            this.btnDeleteProfile.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnDeleteProfile.Name = "btnDeleteProfile";
            this.btnDeleteProfile.Size = new System.Drawing.Size(70, 28);
            this.btnDeleteProfile.TabIndex = 1;
            this.btnDeleteProfile.Text = "Delete";
            this.btnDeleteProfile.UseVisualStyleBackColor = false;
            this.btnDeleteProfile.Click += new System.EventHandler(this.btnDeleteProfile_Click);
            // 
            // lblProfilesHeader
            // 
            this.lblProfilesHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblProfilesHeader.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblProfilesHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(120)))));
            this.lblProfilesHeader.Location = new System.Drawing.Point(0, 0);
            this.lblProfilesHeader.Name = "lblProfilesHeader";
            this.lblProfilesHeader.Size = new System.Drawing.Size(204, 28);
            this.lblProfilesHeader.TabIndex = 2;
            this.lblProfilesHeader.Text = "SAVED PROFILES";
            this.lblProfilesHeader.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // pnlEditor
            // 
            this.pnlEditor.Controls.Add(this.pnlScroll);
            this.pnlEditor.Controls.Add(this.pnlPreview);
            this.pnlEditor.Controls.Add(this.tblTopRow);
            this.pnlEditor.Controls.Add(this.btnSaveProfile);
            this.pnlEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlEditor.Location = new System.Drawing.Point(235, 15);
            this.pnlEditor.Name = "pnlEditor";
            this.pnlEditor.Size = new System.Drawing.Size(1411, 707);
            this.pnlEditor.TabIndex = 1;
            // 
            // pnlScroll
            // 
            this.pnlScroll.AutoScroll = true;
            this.pnlScroll.Controls.Add(this.flpEditor);
            this.pnlScroll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlScroll.Location = new System.Drawing.Point(0, 48);
            this.pnlScroll.Name = "pnlScroll";
            this.pnlScroll.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.pnlScroll.Size = new System.Drawing.Size(1411, 459);
            this.pnlScroll.TabIndex = 0;
            // 
            // flpEditor
            // 
            this.flpEditor.AutoSize = true;
            this.flpEditor.Controls.Add(this.lblAtsHeader);
            this.flpEditor.Controls.Add(this.clbAtsGroups);
            this.flpEditor.Controls.Add(this.pnlAtsSpacer);
            this.flpEditor.Controls.Add(this.lblStackHeader);
            this.flpEditor.Controls.Add(this.flpStack);
            this.flpEditor.Controls.Add(this.flpStackAddRow);
            this.flpEditor.Controls.Add(this.lblRolesHeader);
            this.flpEditor.Controls.Add(this.flpRoles);
            this.flpEditor.Controls.Add(this.flpRolesAddRow);
            this.flpEditor.Controls.Add(this.lblLocationsHeader);
            this.flpEditor.Controls.Add(this.flpLocations);
            this.flpEditor.Controls.Add(this.flpLocationsAddRow);
            this.flpEditor.Controls.Add(this.lblVisaHeader);
            this.flpEditor.Controls.Add(this.flpVisa);
            this.flpEditor.Controls.Add(this.flpVisaAddRow);
            this.flpEditor.Controls.Add(this.lblRemoteHeader);
            this.flpEditor.Controls.Add(this.flpRemote);
            this.flpEditor.Controls.Add(this.flpRemoteAddRow);
            this.flpEditor.Controls.Add(this.lblTimezoneHeader);
            this.flpEditor.Controls.Add(this.flpTimezone);
            this.flpEditor.Controls.Add(this.flpTimezoneAddRow);
            this.flpEditor.Controls.Add(this.lblExcludeHeader);
            this.flpEditor.Controls.Add(this.flpExclude);
            this.flpEditor.Controls.Add(this.flpExcludeAddRow);
            this.flpEditor.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpEditor.Location = new System.Drawing.Point(4, 0);
            this.flpEditor.Name = "flpEditor";
            this.flpEditor.Size = new System.Drawing.Size(1438, 1006);
            this.flpEditor.TabIndex = 0;
            this.flpEditor.WrapContents = false;
            // 
            // lblAtsHeader
            // 
            this.lblAtsHeader.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblAtsHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(120)))));
            this.lblAtsHeader.Location = new System.Drawing.Point(3, 0);
            this.lblAtsHeader.Name = "lblAtsHeader";
            this.lblAtsHeader.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblAtsHeader.Size = new System.Drawing.Size(880, 22);
            this.lblAtsHeader.TabIndex = 0;
            this.lblAtsHeader.Text = "ATS SOURCE GROUPS";
            this.lblAtsHeader.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // clbAtsGroups
            // 
            this.clbAtsGroups.BackColor = System.Drawing.Color.White;
            this.clbAtsGroups.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.clbAtsGroups.CheckOnClick = true;
            this.clbAtsGroups.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.clbAtsGroups.Location = new System.Drawing.Point(3, 25);
            this.clbAtsGroups.Name = "clbAtsGroups";
            this.clbAtsGroups.Size = new System.Drawing.Size(780, 92);
            this.clbAtsGroups.TabIndex = 1;
            this.clbAtsGroups.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbAtsGroups_ItemCheck);
            // 
            // pnlAtsSpacer
            // 
            this.pnlAtsSpacer.BackColor = System.Drawing.Color.Transparent;
            this.pnlAtsSpacer.Location = new System.Drawing.Point(3, 123);
            this.pnlAtsSpacer.Name = "pnlAtsSpacer";
            this.pnlAtsSpacer.Size = new System.Drawing.Size(880, 8);
            this.pnlAtsSpacer.TabIndex = 2;
            // 
            // lblStackHeader
            // 
            this.lblStackHeader.Location = new System.Drawing.Point(3, 134);
            this.lblStackHeader.Name = "lblStackHeader";
            this.lblStackHeader.Size = new System.Drawing.Size(100, 23);
            this.lblStackHeader.TabIndex = 3;
            // 
            // flpStack
            // 
            this.flpStack.Location = new System.Drawing.Point(3, 160);
            this.flpStack.Name = "flpStack";
            this.flpStack.Size = new System.Drawing.Size(859, 42);
            this.flpStack.TabIndex = 4;
            // 
            // flpStackAddRow
            // 
            this.flpStackAddRow.Location = new System.Drawing.Point(3, 208);
            this.flpStackAddRow.Name = "flpStackAddRow";
            this.flpStackAddRow.Size = new System.Drawing.Size(853, 45);
            this.flpStackAddRow.TabIndex = 5;
            // 
            // lblRolesHeader
            // 
            this.lblRolesHeader.Location = new System.Drawing.Point(3, 256);
            this.lblRolesHeader.Name = "lblRolesHeader";
            this.lblRolesHeader.Size = new System.Drawing.Size(100, 23);
            this.lblRolesHeader.TabIndex = 7;
            // 
            // flpRoles
            // 
            this.flpRoles.Location = new System.Drawing.Point(3, 282);
            this.flpRoles.Name = "flpRoles";
            this.flpRoles.Size = new System.Drawing.Size(853, 45);
            this.flpRoles.TabIndex = 8;
            // 
            // flpRolesAddRow
            // 
            this.flpRolesAddRow.Location = new System.Drawing.Point(3, 333);
            this.flpRolesAddRow.Name = "flpRolesAddRow";
            this.flpRolesAddRow.Size = new System.Drawing.Size(853, 45);
            this.flpRolesAddRow.TabIndex = 9;
            // 
            // lblLocationsHeader
            // 
            this.lblLocationsHeader.Location = new System.Drawing.Point(3, 381);
            this.lblLocationsHeader.Name = "lblLocationsHeader";
            this.lblLocationsHeader.Size = new System.Drawing.Size(100, 23);
            this.lblLocationsHeader.TabIndex = 11;
            // 
            // flpLocations
            // 
            this.flpLocations.Location = new System.Drawing.Point(3, 407);
            this.flpLocations.Name = "flpLocations";
            this.flpLocations.Size = new System.Drawing.Size(853, 45);
            this.flpLocations.TabIndex = 12;
            // 
            // flpLocationsAddRow
            // 
            this.flpLocationsAddRow.Location = new System.Drawing.Point(3, 458);
            this.flpLocationsAddRow.Name = "flpLocationsAddRow";
            this.flpLocationsAddRow.Size = new System.Drawing.Size(853, 45);
            this.flpLocationsAddRow.TabIndex = 13;
            // 
            // lblVisaHeader
            // 
            this.lblVisaHeader.Location = new System.Drawing.Point(3, 506);
            this.lblVisaHeader.Name = "lblVisaHeader";
            this.lblVisaHeader.Size = new System.Drawing.Size(100, 23);
            this.lblVisaHeader.TabIndex = 15;
            // 
            // flpVisa
            // 
            this.flpVisa.Location = new System.Drawing.Point(3, 532);
            this.flpVisa.Name = "flpVisa";
            this.flpVisa.Size = new System.Drawing.Size(853, 45);
            this.flpVisa.TabIndex = 16;
            // 
            // flpVisaAddRow
            // 
            this.flpVisaAddRow.Location = new System.Drawing.Point(3, 583);
            this.flpVisaAddRow.Name = "flpVisaAddRow";
            this.flpVisaAddRow.Size = new System.Drawing.Size(853, 45);
            this.flpVisaAddRow.TabIndex = 17;
            // 
            // lblRemoteHeader
            // 
            this.lblRemoteHeader.Location = new System.Drawing.Point(3, 631);
            this.lblRemoteHeader.Name = "lblRemoteHeader";
            this.lblRemoteHeader.Size = new System.Drawing.Size(100, 23);
            this.lblRemoteHeader.TabIndex = 19;
            // 
            // flpRemote
            // 
            this.flpRemote.Location = new System.Drawing.Point(3, 657);
            this.flpRemote.Name = "flpRemote";
            this.flpRemote.Size = new System.Drawing.Size(853, 45);
            this.flpRemote.TabIndex = 20;
            // 
            // flpRemoteAddRow
            // 
            this.flpRemoteAddRow.Location = new System.Drawing.Point(3, 708);
            this.flpRemoteAddRow.Name = "flpRemoteAddRow";
            this.flpRemoteAddRow.Size = new System.Drawing.Size(853, 45);
            this.flpRemoteAddRow.TabIndex = 21;
            // 
            // lblTimezoneHeader
            // 
            this.lblTimezoneHeader.Location = new System.Drawing.Point(3, 756);
            this.lblTimezoneHeader.Name = "lblTimezoneHeader";
            this.lblTimezoneHeader.Size = new System.Drawing.Size(100, 23);
            this.lblTimezoneHeader.TabIndex = 22;
            // 
            // flpTimezone
            // 
            this.flpTimezone.Location = new System.Drawing.Point(3, 782);
            this.flpTimezone.Name = "flpTimezone";
            this.flpTimezone.Size = new System.Drawing.Size(853, 45);
            this.flpTimezone.TabIndex = 23;
            // 
            // flpTimezoneAddRow
            // 
            this.flpTimezoneAddRow.Location = new System.Drawing.Point(3, 833);
            this.flpTimezoneAddRow.Name = "flpTimezoneAddRow";
            this.flpTimezoneAddRow.Size = new System.Drawing.Size(853, 45);
            this.flpTimezoneAddRow.TabIndex = 24;
            // 
            // lblExcludeHeader
            // 
            this.lblExcludeHeader.Location = new System.Drawing.Point(3, 881);
            this.lblExcludeHeader.Name = "lblExcludeHeader";
            this.lblExcludeHeader.Size = new System.Drawing.Size(100, 23);
            this.lblExcludeHeader.TabIndex = 25;
            // 
            // flpExclude
            // 
            this.flpExclude.Location = new System.Drawing.Point(3, 907);
            this.flpExclude.Name = "flpExclude";
            this.flpExclude.Size = new System.Drawing.Size(853, 45);
            this.flpExclude.TabIndex = 26;
            // 
            // flpExcludeAddRow
            // 
            this.flpExcludeAddRow.Location = new System.Drawing.Point(3, 958);
            this.flpExcludeAddRow.Name = "flpExcludeAddRow";
            this.flpExcludeAddRow.Size = new System.Drawing.Size(853, 45);
            this.flpExcludeAddRow.TabIndex = 27;
            // 
            // pnlPreview
            // 
            this.pnlPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(45)))));
            this.pnlPreview.Controls.Add(this.flpPreviewButtons);
            this.pnlPreview.Controls.Add(this.lblPreviewHeader);
            this.pnlPreview.Controls.Add(this.txtQueryPreview);
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlPreview.Location = new System.Drawing.Point(0, 507);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.pnlPreview.Size = new System.Drawing.Size(1411, 200);
            this.pnlPreview.TabIndex = 1;
            // 
            // flpPreviewButtons
            // 
            this.flpPreviewButtons.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(45)))));
            this.flpPreviewButtons.Controls.Add(this.btnOpenInGoogle);
            this.flpPreviewButtons.Controls.Add(this.btnCopyQuery);
            this.flpPreviewButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpPreviewButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flpPreviewButtons.Location = new System.Drawing.Point(12, 154);
            this.flpPreviewButtons.Name = "flpPreviewButtons";
            this.flpPreviewButtons.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.flpPreviewButtons.Size = new System.Drawing.Size(1387, 38);
            this.flpPreviewButtons.TabIndex = 1;
            // 
            // btnOpenInGoogle
            // 
            this.btnOpenInGoogle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnOpenInGoogle.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOpenInGoogle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenInGoogle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnOpenInGoogle.ForeColor = System.Drawing.Color.White;
            this.btnOpenInGoogle.Location = new System.Drawing.Point(1241, 4);
            this.btnOpenInGoogle.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnOpenInGoogle.Name = "btnOpenInGoogle";
            this.btnOpenInGoogle.Size = new System.Drawing.Size(140, 28);
            this.btnOpenInGoogle.TabIndex = 0;
            this.btnOpenInGoogle.Text = "Open in Google";
            this.btnOpenInGoogle.UseVisualStyleBackColor = false;
            this.btnOpenInGoogle.Click += new System.EventHandler(this.btnOpenInGoogle_Click);
            // 
            // btnCopyQuery
            // 
            this.btnCopyQuery.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(85)))), ((int)(((byte)(100)))));
            this.btnCopyQuery.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCopyQuery.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopyQuery.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCopyQuery.ForeColor = System.Drawing.Color.White;
            this.btnCopyQuery.Location = new System.Drawing.Point(1155, 4);
            this.btnCopyQuery.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnCopyQuery.Name = "btnCopyQuery";
            this.btnCopyQuery.Size = new System.Drawing.Size(80, 28);
            this.btnCopyQuery.TabIndex = 1;
            this.btnCopyQuery.Text = "Copy";
            this.btnCopyQuery.UseVisualStyleBackColor = false;
            this.btnCopyQuery.Click += new System.EventHandler(this.btnCopyQuery_Click);
            // 
            // lblPreviewHeader
            // 
            this.lblPreviewHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(45)))));
            this.lblPreviewHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPreviewHeader.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblPreviewHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(160)))), ((int)(((byte)(220)))));
            this.lblPreviewHeader.Location = new System.Drawing.Point(12, 8);
            this.lblPreviewHeader.Name = "lblPreviewHeader";
            this.lblPreviewHeader.Size = new System.Drawing.Size(1387, 30);
            this.lblPreviewHeader.TabIndex = 2;
            this.lblPreviewHeader.Text = "GENERATED QUERY PREVIEW";
            // 
            // txtQueryPreview
            // 
            this.txtQueryPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(35)))));
            this.txtQueryPreview.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtQueryPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtQueryPreview.Font = new System.Drawing.Font("Courier New", 9F);
            this.txtQueryPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(230)))), ((int)(((byte)(160)))));
            this.txtQueryPreview.Location = new System.Drawing.Point(12, 8);
            this.txtQueryPreview.Multiline = true;
            this.txtQueryPreview.Name = "txtQueryPreview";
            this.txtQueryPreview.ReadOnly = true;
            this.txtQueryPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtQueryPreview.Size = new System.Drawing.Size(1387, 184);
            this.txtQueryPreview.TabIndex = 0;
            // 
            // tblTopRow
            // 
            this.tblTopRow.ColumnCount = 5;
            this.tblTopRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 85F));
            this.tblTopRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tblTopRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.tblTopRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.tblTopRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tblTopRow.Controls.Add(this.lblProfileName, 0, 0);
            this.tblTopRow.Controls.Add(this.txtProfileName, 1, 0);
            this.tblTopRow.Controls.Add(this.lblSeniority, 3, 0);
            this.tblTopRow.Controls.Add(this.cboSeniority, 4, 0);
            this.tblTopRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblTopRow.Location = new System.Drawing.Point(0, 0);
            this.tblTopRow.Name = "tblTopRow";
            this.tblTopRow.Padding = new System.Windows.Forms.Padding(4, 4, 130, 4);
            this.tblTopRow.RowCount = 1;
            this.tblTopRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblTopRow.Size = new System.Drawing.Size(1411, 48);
            this.tblTopRow.TabIndex = 2;
            // 
            // lblProfileName
            // 
            this.lblProfileName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProfileName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblProfileName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(100)))));
            this.lblProfileName.Location = new System.Drawing.Point(7, 4);
            this.lblProfileName.Name = "lblProfileName";
            this.lblProfileName.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblProfileName.Size = new System.Drawing.Size(79, 40);
            this.lblProfileName.TabIndex = 0;
            this.lblProfileName.Text = "Profile name";
            this.lblProfileName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtProfileName
            // 
            this.txtProfileName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtProfileName.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.txtProfileName.Location = new System.Drawing.Point(92, 7);
            this.txtProfileName.Name = "txtProfileName";
            this.txtProfileName.Size = new System.Drawing.Size(654, 37);
            this.txtProfileName.TabIndex = 1;
            this.txtProfileName.TextChanged += new System.EventHandler(this.txtProfileName_TextChanged);
            // 
            // lblSeniority
            // 
            this.lblSeniority.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSeniority.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSeniority.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(100)))));
            this.lblSeniority.Location = new System.Drawing.Point(768, 4);
            this.lblSeniority.Name = "lblSeniority";
            this.lblSeniority.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblSeniority.Size = new System.Drawing.Size(69, 40);
            this.lblSeniority.TabIndex = 2;
            this.lblSeniority.Text = "Seniority";
            this.lblSeniority.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cboSeniority
            // 
            this.cboSeniority.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboSeniority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSeniority.Location = new System.Drawing.Point(843, 7);
            this.cboSeniority.Name = "cboSeniority";
            this.cboSeniority.Size = new System.Drawing.Size(435, 33);
            this.cboSeniority.TabIndex = 3;
            this.cboSeniority.SelectedIndexChanged += new System.EventHandler(this.cboSeniority_SelectedIndexChanged);
            // 
            // btnSaveProfile
            // 
            this.btnSaveProfile.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnSaveProfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnSaveProfile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveProfile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSaveProfile.ForeColor = System.Drawing.Color.White;
            this.btnSaveProfile.Location = new System.Drawing.Point(1000, 95);
            this.btnSaveProfile.Name = "btnSaveProfile";
            this.btnSaveProfile.Size = new System.Drawing.Size(120, 28);
            this.btnSaveProfile.TabIndex = 3;
            this.btnSaveProfile.Text = "Save Profile";
            this.btnSaveProfile.UseVisualStyleBackColor = false;
            this.btnSaveProfile.Click += new System.EventHandler(this.btnSaveProfile_Click);
            // 
            // txtAddStack
            // 
            this.txtAddStack.Location = new System.Drawing.Point(0, 0);
            this.txtAddStack.Name = "txtAddStack";
            this.txtAddStack.Size = new System.Drawing.Size(100, 26);
            this.txtAddStack.TabIndex = 0;
            // 
            // txtAddRole
            // 
            this.txtAddRole.Location = new System.Drawing.Point(0, 0);
            this.txtAddRole.Name = "txtAddRole";
            this.txtAddRole.Size = new System.Drawing.Size(100, 26);
            this.txtAddRole.TabIndex = 0;
            // 
            // txtAddLocation
            // 
            this.txtAddLocation.Location = new System.Drawing.Point(0, 0);
            this.txtAddLocation.Name = "txtAddLocation";
            this.txtAddLocation.Size = new System.Drawing.Size(100, 26);
            this.txtAddLocation.TabIndex = 0;
            // 
            // txtAddVisa
            // 
            this.txtAddVisa.Location = new System.Drawing.Point(0, 0);
            this.txtAddVisa.Name = "txtAddVisa";
            this.txtAddVisa.Size = new System.Drawing.Size(100, 26);
            this.txtAddVisa.TabIndex = 0;
            // 
            // txtAddRemote
            // 
            this.txtAddRemote.Location = new System.Drawing.Point(0, 0);
            this.txtAddRemote.Name = "txtAddRemote";
            this.txtAddRemote.Size = new System.Drawing.Size(100, 26);
            this.txtAddRemote.TabIndex = 0;
            // 
            // txtAddTimezone
            // 
            this.txtAddTimezone.Location = new System.Drawing.Point(0, 0);
            this.txtAddTimezone.Name = "txtAddTimezone";
            this.txtAddTimezone.Size = new System.Drawing.Size(100, 26);
            this.txtAddTimezone.TabIndex = 0;
            // 
            // txtAddExclude
            // 
            this.txtAddExclude.Location = new System.Drawing.Point(0, 0);
            this.txtAddExclude.Name = "txtAddExclude";
            this.txtAddExclude.Size = new System.Drawing.Size(100, 26);
            this.txtAddExclude.TabIndex = 0;
            // 
            // MainForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1661, 737);
            this.Controls.Add(this.tblMain);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1000, 700);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Job Search Query Builder";
            this.tblMain.ResumeLayout(false);
            this.pnlLeft.ResumeLayout(false);
            this.flpProfileButtons.ResumeLayout(false);
            this.pnlEditor.ResumeLayout(false);
            this.pnlScroll.ResumeLayout(false);
            this.pnlScroll.PerformLayout();
            this.flpEditor.ResumeLayout(false);
            this.pnlPreview.ResumeLayout(false);
            this.pnlPreview.PerformLayout();
            this.flpPreviewButtons.ResumeLayout(false);
            this.tblTopRow.ResumeLayout(false);
            this.tblTopRow.PerformLayout();
            this.ResumeLayout(false);

        }

        // ----------------------------------------------------------------
        // Designer helpers — called from InitializeComponent to avoid
        // repetition for the 5 identical keyword sections
        // ----------------------------------------------------------------

        private void ConfigureChipPanel(System.Windows.Forms.FlowLayoutPanel panel)
        {
            panel.Width = 880;
            panel.AutoSize = true;
            panel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            panel.MinimumSize = new System.Drawing.Size(880, 34);
            panel.MinimumSize = new System.Drawing.Size(880, 32);
            panel.BackColor = System.Drawing.Color.White;
            panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panel.Padding = new System.Windows.Forms.Padding(4);
            panel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            panel.WrapContents = true;
        }

        private void ConfigureSectionHeader(System.Windows.Forms.Label lbl, string text)
        {
            lbl.Text = text;
            lbl.Width = 880;
            lbl.Height = 22;
            lbl.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            lbl.ForeColor = System.Drawing.Color.FromArgb(100, 100, 120);
            lbl.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            lbl.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
        }

        private void ConfigureAddRow(System.Windows.Forms.FlowLayoutPanel row,
                                     System.Windows.Forms.TextBox addBox)
        {
            row.Width = 880;
            row.Height = 30;
            row.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;

            addBox.Width = 220;
            addBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            addBox.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);

            row.Controls.Add(addBox);
            // Suggestion buttons are added at runtime in PostInitialize()
            // because they come from appsettings.json
        }

        private void ConfigureSpacer(System.Windows.Forms.Panel spacer)
        {
            spacer.Width = 880;
            spacer.Height = 6;
            spacer.BackColor = System.Drawing.Color.Transparent;
        }

        // ----------------------------------------------------------------
        // Designer-declared controls (fields)
        // ----------------------------------------------------------------

        // Layout
        private System.Windows.Forms.TableLayoutPanel tblMain;

        // Left panel
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Label lblProfilesHeader;
        private System.Windows.Forms.ListBox lstProfiles;
        private System.Windows.Forms.FlowLayoutPanel flpProfileButtons;
        private System.Windows.Forms.Button btnNewProfile;
        private System.Windows.Forms.Button btnDeleteProfile;

        // Editor panel
        private System.Windows.Forms.Panel pnlEditor;
        private System.Windows.Forms.TableLayoutPanel tblTopRow;
        private System.Windows.Forms.Label lblProfileName;
        private System.Windows.Forms.TextBox txtProfileName;
        private System.Windows.Forms.Label lblSeniority;
        private System.Windows.Forms.ComboBox cboSeniority;
        private System.Windows.Forms.Button btnSaveProfile;

        // Scrollable body
        private JobSearchBuilder.NoScrollJumpPanel pnlScroll;
        private System.Windows.Forms.FlowLayoutPanel flpEditor;

        // ATS section
        private System.Windows.Forms.Label lblAtsHeader;
        private System.Windows.Forms.CheckedListBox clbAtsGroups;
        private System.Windows.Forms.Panel pnlAtsSpacer;

        // Stack section
        private System.Windows.Forms.Label lblStackHeader;
        private System.Windows.Forms.FlowLayoutPanel flpStack;
        private System.Windows.Forms.FlowLayoutPanel flpStackAddRow;
        private System.Windows.Forms.TextBox txtAddStack;

        // Roles section
        private System.Windows.Forms.Label lblRolesHeader;
        private System.Windows.Forms.FlowLayoutPanel flpRoles;
        private System.Windows.Forms.FlowLayoutPanel flpRolesAddRow;
        private System.Windows.Forms.TextBox txtAddRole;

        // Locations section
        private System.Windows.Forms.Label lblLocationsHeader;
        private System.Windows.Forms.FlowLayoutPanel flpLocations;
        private System.Windows.Forms.FlowLayoutPanel flpLocationsAddRow;
        private System.Windows.Forms.TextBox txtAddLocation;

        // Visa section
        private System.Windows.Forms.Label lblVisaHeader;
        private System.Windows.Forms.FlowLayoutPanel flpVisa;
        private System.Windows.Forms.FlowLayoutPanel flpVisaAddRow;
        private System.Windows.Forms.TextBox txtAddVisa;

        // Remote section
        private System.Windows.Forms.Label lblRemoteHeader;
        private System.Windows.Forms.FlowLayoutPanel flpRemote;
        private System.Windows.Forms.FlowLayoutPanel flpRemoteAddRow;
        private System.Windows.Forms.TextBox txtAddRemote;

        // Timezone section
        private System.Windows.Forms.Label lblTimezoneHeader;
        private System.Windows.Forms.FlowLayoutPanel flpTimezone;
        private System.Windows.Forms.FlowLayoutPanel flpTimezoneAddRow;
        private System.Windows.Forms.TextBox txtAddTimezone;

        // Exclude section
        private System.Windows.Forms.Label lblExcludeHeader;
        private System.Windows.Forms.FlowLayoutPanel flpExclude;
        private System.Windows.Forms.FlowLayoutPanel flpExcludeAddRow;
        private System.Windows.Forms.TextBox txtAddExclude;

        // Preview panel
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.Label lblPreviewHeader;
        private System.Windows.Forms.TextBox txtQueryPreview;
        private System.Windows.Forms.FlowLayoutPanel flpPreviewButtons;
        private System.Windows.Forms.Button btnOpenInGoogle;
        private System.Windows.Forms.Button btnCopyQuery;
    }

        #endregion
}