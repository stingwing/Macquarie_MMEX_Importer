namespace MoneyManagerExMAQ
{
    partial class SettingsForm
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

        #region Windows Form Designer generated code

        // Layout: everything sits in table/flow panels docked to the form, so the path boxes and the
        // accounts grid stretch with the window instead of staying at fixed pixel positions.
        private void InitializeComponent()
        {
            tlpRoot = new TableLayoutPanel();
            tlpPaths = new TableLayoutPanel();
            lblMmbPath = new Label();
            txtMmbPath = new TextBox();
            btnBrowseMmb = new Button();
            lblBackupFolder = new Label();
            txtBackupFolder = new TextBox();
            btnBrowseBackupFolder = new Button();
            lblImportFolder = new Label();
            txtImportFolder = new TextBox();
            btnBrowseImportFolder = new Button();
            lblCategoryRecords = new Label();
            txtCategoryRecords = new TextBox();
            btnBrowseCategoryRecords = new Button();
            lblTransferMarkers = new Label();
            txtTransferMarkers = new TextBox();
            dgvAccounts = new DataGridView();
            colName = new DataGridViewTextBoxColumn();
            colBankLabel = new DataGridViewTextBoxColumn();
            colMmexAccount = new DataGridViewTextBoxColumn();
            colIngFingerprint = new DataGridViewTextBoxColumn();
            tlpButtons = new TableLayoutPanel();
            flpAccountButtons = new FlowLayoutPanel();
            btnAddAccount = new Button();
            btnRemoveAccount = new Button();
            btnLoadMmexAccounts = new Button();
            flpDialogButtons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            tlpRoot.SuspendLayout();
            tlpPaths.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAccounts).BeginInit();
            tlpButtons.SuspendLayout();
            flpAccountButtons.SuspendLayout();
            flpDialogButtons.SuspendLayout();
            SuspendLayout();
            //
            // tlpRoot
            //
            tlpRoot.ColumnCount = 1;
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.Controls.Add(tlpPaths, 0, 0);
            tlpRoot.Controls.Add(dgvAccounts, 0, 1);
            tlpRoot.Controls.Add(tlpButtons, 0, 2);
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.Name = "tlpRoot";
            tlpRoot.Padding = new Padding(12);
            tlpRoot.RowCount = 3;
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpRoot.TabIndex = 0;
            //
            // tlpPaths — label | stretching text box | Browse button
            //
            tlpPaths.AutoSize = true;
            tlpPaths.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpPaths.ColumnCount = 3;
            tlpPaths.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpPaths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPaths.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpPaths.Controls.Add(lblMmbPath, 0, 0);
            tlpPaths.Controls.Add(txtMmbPath, 1, 0);
            tlpPaths.Controls.Add(btnBrowseMmb, 2, 0);
            tlpPaths.Controls.Add(lblBackupFolder, 0, 1);
            tlpPaths.Controls.Add(txtBackupFolder, 1, 1);
            tlpPaths.Controls.Add(btnBrowseBackupFolder, 2, 1);
            tlpPaths.Controls.Add(lblImportFolder, 0, 2);
            tlpPaths.Controls.Add(txtImportFolder, 1, 2);
            tlpPaths.Controls.Add(btnBrowseImportFolder, 2, 2);
            tlpPaths.Controls.Add(lblCategoryRecords, 0, 3);
            tlpPaths.Controls.Add(txtCategoryRecords, 1, 3);
            tlpPaths.Controls.Add(btnBrowseCategoryRecords, 2, 3);
            tlpPaths.Controls.Add(lblTransferMarkers, 0, 4);
            tlpPaths.Controls.Add(txtTransferMarkers, 1, 4);
            tlpPaths.SetColumnSpan(txtTransferMarkers, 2);
            tlpPaths.Dock = DockStyle.Fill;
            tlpPaths.Margin = new Padding(0, 0, 0, 6);
            tlpPaths.Name = "tlpPaths";
            tlpPaths.RowCount = 5;
            tlpPaths.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpPaths.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpPaths.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpPaths.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpPaths.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpPaths.TabIndex = 0;
            //
            // lblMmbPath
            //
            lblMmbPath.Anchor = AnchorStyles.Left;
            lblMmbPath.AutoSize = true;
            lblMmbPath.Margin = new Padding(0, 4, 12, 4);
            lblMmbPath.Name = "lblMmbPath";
            lblMmbPath.TabIndex = 0;
            lblMmbPath.Text = "MMEX Database:";
            //
            // txtMmbPath
            //
            txtMmbPath.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtMmbPath.Margin = new Padding(0, 4, 8, 4);
            txtMmbPath.Name = "txtMmbPath";
            txtMmbPath.ReadOnly = true;
            txtMmbPath.TabIndex = 1;
            //
            // btnBrowseMmb
            //
            btnBrowseMmb.Anchor = AnchorStyles.Left;
            btnBrowseMmb.AutoSize = true;
            btnBrowseMmb.Margin = new Padding(0, 3, 0, 3);
            btnBrowseMmb.MinimumSize = new Size(90, 0);
            btnBrowseMmb.Name = "btnBrowseMmb";
            btnBrowseMmb.TabIndex = 2;
            btnBrowseMmb.Text = "Browse...";
            btnBrowseMmb.UseVisualStyleBackColor = true;
            btnBrowseMmb.Click += btnBrowseMmb_Click;
            //
            // lblBackupFolder
            //
            lblBackupFolder.Anchor = AnchorStyles.Left;
            lblBackupFolder.AutoSize = true;
            lblBackupFolder.Margin = new Padding(0, 4, 12, 4);
            lblBackupFolder.Name = "lblBackupFolder";
            lblBackupFolder.TabIndex = 3;
            lblBackupFolder.Text = "Backup Folder:";
            //
            // txtBackupFolder
            //
            txtBackupFolder.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtBackupFolder.Margin = new Padding(0, 4, 8, 4);
            txtBackupFolder.Name = "txtBackupFolder";
            txtBackupFolder.ReadOnly = true;
            txtBackupFolder.TabIndex = 4;
            //
            // btnBrowseBackupFolder
            //
            btnBrowseBackupFolder.Anchor = AnchorStyles.Left;
            btnBrowseBackupFolder.AutoSize = true;
            btnBrowseBackupFolder.Margin = new Padding(0, 3, 0, 3);
            btnBrowseBackupFolder.MinimumSize = new Size(90, 0);
            btnBrowseBackupFolder.Name = "btnBrowseBackupFolder";
            btnBrowseBackupFolder.TabIndex = 5;
            btnBrowseBackupFolder.Text = "Browse...";
            btnBrowseBackupFolder.UseVisualStyleBackColor = true;
            btnBrowseBackupFolder.Click += btnBrowseBackupFolder_Click;
            //
            // lblImportFolder
            //
            lblImportFolder.Anchor = AnchorStyles.Left;
            lblImportFolder.AutoSize = true;
            lblImportFolder.Margin = new Padding(0, 4, 12, 4);
            lblImportFolder.Name = "lblImportFolder";
            lblImportFolder.TabIndex = 6;
            lblImportFolder.Text = "Import Folder:";
            //
            // txtImportFolder
            //
            txtImportFolder.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtImportFolder.Margin = new Padding(0, 4, 8, 4);
            txtImportFolder.Name = "txtImportFolder";
            txtImportFolder.ReadOnly = true;
            txtImportFolder.TabIndex = 7;
            //
            // btnBrowseImportFolder
            //
            btnBrowseImportFolder.Anchor = AnchorStyles.Left;
            btnBrowseImportFolder.AutoSize = true;
            btnBrowseImportFolder.Margin = new Padding(0, 3, 0, 3);
            btnBrowseImportFolder.MinimumSize = new Size(90, 0);
            btnBrowseImportFolder.Name = "btnBrowseImportFolder";
            btnBrowseImportFolder.TabIndex = 8;
            btnBrowseImportFolder.Text = "Browse...";
            btnBrowseImportFolder.UseVisualStyleBackColor = true;
            btnBrowseImportFolder.Click += btnBrowseImportFolder_Click;
            //
            // lblCategoryRecords
            //
            lblCategoryRecords.Anchor = AnchorStyles.Left;
            lblCategoryRecords.AutoSize = true;
            lblCategoryRecords.Margin = new Padding(0, 4, 12, 4);
            lblCategoryRecords.Name = "lblCategoryRecords";
            lblCategoryRecords.TabIndex = 9;
            lblCategoryRecords.Text = "Alias File (optional):";
            //
            // txtCategoryRecords
            //
            txtCategoryRecords.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtCategoryRecords.Margin = new Padding(0, 4, 8, 4);
            txtCategoryRecords.Name = "txtCategoryRecords";
            txtCategoryRecords.ReadOnly = true;
            txtCategoryRecords.TabIndex = 10;
            //
            // btnBrowseCategoryRecords
            //
            btnBrowseCategoryRecords.Anchor = AnchorStyles.Left;
            btnBrowseCategoryRecords.AutoSize = true;
            btnBrowseCategoryRecords.Margin = new Padding(0, 3, 0, 3);
            btnBrowseCategoryRecords.MinimumSize = new Size(90, 0);
            btnBrowseCategoryRecords.Name = "btnBrowseCategoryRecords";
            btnBrowseCategoryRecords.TabIndex = 11;
            btnBrowseCategoryRecords.Text = "Browse...";
            btnBrowseCategoryRecords.UseVisualStyleBackColor = true;
            btnBrowseCategoryRecords.Click += btnBrowseCategoryRecords_Click;
            //
            // lblTransferMarkers
            //
            lblTransferMarkers.Anchor = AnchorStyles.Left;
            lblTransferMarkers.AutoSize = true;
            lblTransferMarkers.Margin = new Padding(0, 4, 12, 4);
            lblTransferMarkers.Name = "lblTransferMarkers";
            lblTransferMarkers.TabIndex = 12;
            lblTransferMarkers.Text = "Own-transfer text:";
            //
            // txtTransferMarkers
            //
            txtTransferMarkers.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtTransferMarkers.Margin = new Padding(0, 4, 0, 4);
            txtTransferMarkers.Name = "txtTransferMarkers";
            txtTransferMarkers.PlaceholderText = "e.g. timothy mollenha, tim mollenhauer (comma-separated)";
            txtTransferMarkers.TabIndex = 13;
            //
            // dgvAccounts
            //
            dgvAccounts.AllowUserToAddRows = false;
            dgvAccounts.AllowUserToDeleteRows = false;
            dgvAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAccounts.BackgroundColor = SystemColors.Window;
            dgvAccounts.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvAccounts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAccounts.Columns.AddRange(new DataGridViewColumn[] { colName, colBankLabel, colMmexAccount, colIngFingerprint });
            dgvAccounts.Dock = DockStyle.Fill;
            dgvAccounts.Margin = new Padding(0, 6, 0, 6);
            dgvAccounts.Name = "dgvAccounts";
            dgvAccounts.RowHeadersWidth = 30;
            dgvAccounts.TabIndex = 1;
            //
            // colName
            //
            colName.FillWeight = 22F;
            colName.HeaderText = "Account Name";
            colName.Name = "colName";
            //
            // colBankLabel
            //
            colBankLabel.FillWeight = 26F;
            colBankLabel.HeaderText = "Bank CSV Label (Macquarie)";
            colBankLabel.Name = "colBankLabel";
            //
            // colMmexAccount
            //
            colMmexAccount.FillWeight = 24F;
            colMmexAccount.HeaderText = "MMEX Account";
            colMmexAccount.Name = "colMmexAccount";
            //
            // colIngFingerprint
            //
            colIngFingerprint.FillWeight = 28F;
            colIngFingerprint.HeaderText = "Fingerprint (ING/CommBank)";
            colIngFingerprint.Name = "colIngFingerprint";
            colIngFingerprint.ToolTipText = "Comma-separated text found in ING/CommBank descriptions that identifies this account (e.g. a card suffix like 8299).";
            //
            // tlpButtons — account actions on the left, Save/Cancel on the right
            //
            tlpButtons.AutoSize = true;
            tlpButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpButtons.ColumnCount = 2;
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpButtons.Controls.Add(flpAccountButtons, 0, 0);
            tlpButtons.Controls.Add(flpDialogButtons, 1, 0);
            tlpButtons.Dock = DockStyle.Fill;
            tlpButtons.Margin = new Padding(0);
            tlpButtons.Name = "tlpButtons";
            tlpButtons.RowCount = 1;
            tlpButtons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpButtons.TabIndex = 2;
            //
            // flpAccountButtons
            //
            flpAccountButtons.AutoSize = true;
            flpAccountButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpAccountButtons.Controls.Add(btnAddAccount);
            flpAccountButtons.Controls.Add(btnRemoveAccount);
            flpAccountButtons.Controls.Add(btnLoadMmexAccounts);
            flpAccountButtons.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            flpAccountButtons.Margin = new Padding(0);
            flpAccountButtons.Name = "flpAccountButtons";
            flpAccountButtons.TabIndex = 0;
            flpAccountButtons.WrapContents = false;
            //
            // btnAddAccount
            //
            btnAddAccount.AutoSize = true;
            btnAddAccount.Margin = new Padding(0, 0, 6, 0);
            btnAddAccount.MinimumSize = new Size(100, 30);
            btnAddAccount.Name = "btnAddAccount";
            btnAddAccount.Padding = new Padding(6, 0, 6, 0);
            btnAddAccount.TabIndex = 0;
            btnAddAccount.Text = "Add Account";
            btnAddAccount.UseVisualStyleBackColor = true;
            btnAddAccount.Click += btnAddAccount_Click;
            //
            // btnRemoveAccount
            //
            btnRemoveAccount.AutoSize = true;
            btnRemoveAccount.Margin = new Padding(0, 0, 6, 0);
            btnRemoveAccount.MinimumSize = new Size(100, 30);
            btnRemoveAccount.Name = "btnRemoveAccount";
            btnRemoveAccount.Padding = new Padding(6, 0, 6, 0);
            btnRemoveAccount.TabIndex = 1;
            btnRemoveAccount.Text = "Remove Selected";
            btnRemoveAccount.UseVisualStyleBackColor = true;
            btnRemoveAccount.Click += btnRemoveAccount_Click;
            //
            // btnLoadMmexAccounts
            //
            btnLoadMmexAccounts.AutoSize = true;
            btnLoadMmexAccounts.Margin = new Padding(0, 0, 6, 0);
            btnLoadMmexAccounts.MinimumSize = new Size(100, 30);
            btnLoadMmexAccounts.Name = "btnLoadMmexAccounts";
            btnLoadMmexAccounts.Padding = new Padding(6, 0, 6, 0);
            btnLoadMmexAccounts.TabIndex = 2;
            btnLoadMmexAccounts.Text = "Show MMEX Accounts";
            btnLoadMmexAccounts.UseVisualStyleBackColor = true;
            btnLoadMmexAccounts.Click += btnLoadMmexAccounts_Click;
            //
            // flpDialogButtons
            //
            flpDialogButtons.AutoSize = true;
            flpDialogButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpDialogButtons.Controls.Add(btnSave);
            flpDialogButtons.Controls.Add(btnCancel);
            flpDialogButtons.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            flpDialogButtons.Margin = new Padding(12, 0, 0, 0);
            flpDialogButtons.Name = "flpDialogButtons";
            flpDialogButtons.TabIndex = 1;
            flpDialogButtons.WrapContents = false;
            //
            // btnSave
            //
            btnSave.AutoSize = true;
            btnSave.Margin = new Padding(0, 0, 6, 0);
            btnSave.MinimumSize = new Size(80, 30);
            btnSave.Name = "btnSave";
            btnSave.TabIndex = 0;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            //
            // btnCancel
            //
            btnCancel.AutoSize = true;
            btnCancel.Margin = new Padding(0);
            btnCancel.MinimumSize = new Size(80, 30);
            btnCancel.Name = "btnCancel";
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // SettingsForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(900, 520);
            Controls.Add(tlpRoot);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(760, 440);
            Name = "SettingsForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            tlpRoot.ResumeLayout(false);
            tlpRoot.PerformLayout();
            tlpPaths.ResumeLayout(false);
            tlpPaths.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAccounts).EndInit();
            tlpButtons.ResumeLayout(false);
            tlpButtons.PerformLayout();
            flpAccountButtons.ResumeLayout(false);
            flpAccountButtons.PerformLayout();
            flpDialogButtons.ResumeLayout(false);
            flpDialogButtons.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tlpRoot;
        private TableLayoutPanel tlpPaths;
        private Label lblMmbPath;
        private TextBox txtMmbPath;
        private Button btnBrowseMmb;
        private Label lblBackupFolder;
        private TextBox txtBackupFolder;
        private Button btnBrowseBackupFolder;
        private Label lblImportFolder;
        private TextBox txtImportFolder;
        private Button btnBrowseImportFolder;
        private Label lblCategoryRecords;
        private TextBox txtCategoryRecords;
        private Button btnBrowseCategoryRecords;
        private Label lblTransferMarkers;
        private TextBox txtTransferMarkers;
        private DataGridView dgvAccounts;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colBankLabel;
        private DataGridViewTextBoxColumn colMmexAccount;
        private DataGridViewTextBoxColumn colIngFingerprint;
        private TableLayoutPanel tlpButtons;
        private FlowLayoutPanel flpAccountButtons;
        private Button btnAddAccount;
        private Button btnRemoveAccount;
        private Button btnLoadMmexAccounts;
        private FlowLayoutPanel flpDialogButtons;
        private Button btnSave;
        private Button btnCancel;
    }
}
