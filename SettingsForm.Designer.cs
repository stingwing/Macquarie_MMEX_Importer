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

        private void InitializeComponent()
        {
            lblMmbPath = new Label();
            txtMmbPath = new TextBox();
            btnBrowseMmb = new Button();
            lblBackupFolder = new Label();
            txtBackupFolder = new TextBox();
            btnBrowseBackupFolder = new Button();
            lblImportFolder = new Label();
            txtImportFolder = new TextBox();
            btnBrowseImportFolder = new Button();
            dgvAccounts = new DataGridView();
            colName = new DataGridViewTextBoxColumn();
            colBankLabel = new DataGridViewTextBoxColumn();
            colMmexAccount = new DataGridViewTextBoxColumn();
            btnAddAccount = new Button();
            btnRemoveAccount = new Button();
            btnLoadMmexAccounts = new Button();
            btnSave = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvAccounts).BeginInit();
            SuspendLayout();
            //
            // lblMmbPath
            //
            lblMmbPath.AutoSize = true;
            lblMmbPath.Location = new Point(20, 23);
            lblMmbPath.Name = "lblMmbPath";
            lblMmbPath.Size = new Size(120, 20);
            lblMmbPath.TabIndex = 0;
            lblMmbPath.Text = "MMEX Database:";
            //
            // txtMmbPath
            //
            txtMmbPath.Location = new Point(160, 20);
            txtMmbPath.Name = "txtMmbPath";
            txtMmbPath.ReadOnly = true;
            txtMmbPath.Size = new Size(440, 27);
            txtMmbPath.TabIndex = 1;
            //
            // btnBrowseMmb
            //
            btnBrowseMmb.Location = new Point(610, 20);
            btnBrowseMmb.Name = "btnBrowseMmb";
            btnBrowseMmb.Size = new Size(90, 27);
            btnBrowseMmb.TabIndex = 2;
            btnBrowseMmb.Text = "Browse...";
            btnBrowseMmb.UseVisualStyleBackColor = true;
            btnBrowseMmb.Click += btnBrowseMmb_Click;
            //
            // lblBackupFolder
            //
            lblBackupFolder.AutoSize = true;
            lblBackupFolder.Location = new Point(20, 58);
            lblBackupFolder.Name = "lblBackupFolder";
            lblBackupFolder.Size = new Size(110, 20);
            lblBackupFolder.TabIndex = 3;
            lblBackupFolder.Text = "Backup Folder:";
            //
            // txtBackupFolder
            //
            txtBackupFolder.Location = new Point(160, 55);
            txtBackupFolder.Name = "txtBackupFolder";
            txtBackupFolder.ReadOnly = true;
            txtBackupFolder.Size = new Size(440, 27);
            txtBackupFolder.TabIndex = 4;
            //
            // btnBrowseBackupFolder
            //
            btnBrowseBackupFolder.Location = new Point(610, 55);
            btnBrowseBackupFolder.Name = "btnBrowseBackupFolder";
            btnBrowseBackupFolder.Size = new Size(90, 27);
            btnBrowseBackupFolder.TabIndex = 5;
            btnBrowseBackupFolder.Text = "Browse...";
            btnBrowseBackupFolder.UseVisualStyleBackColor = true;
            btnBrowseBackupFolder.Click += btnBrowseBackupFolder_Click;
            //
            // lblImportFolder
            //
            lblImportFolder.AutoSize = true;
            lblImportFolder.Location = new Point(20, 93);
            lblImportFolder.Name = "lblImportFolder";
            lblImportFolder.Size = new Size(105, 20);
            lblImportFolder.TabIndex = 6;
            lblImportFolder.Text = "Import Folder:";
            //
            // txtImportFolder
            //
            txtImportFolder.Location = new Point(160, 90);
            txtImportFolder.Name = "txtImportFolder";
            txtImportFolder.ReadOnly = true;
            txtImportFolder.Size = new Size(440, 27);
            txtImportFolder.TabIndex = 7;
            //
            // btnBrowseImportFolder
            //
            btnBrowseImportFolder.Location = new Point(610, 90);
            btnBrowseImportFolder.Name = "btnBrowseImportFolder";
            btnBrowseImportFolder.Size = new Size(90, 27);
            btnBrowseImportFolder.TabIndex = 8;
            btnBrowseImportFolder.Text = "Browse...";
            btnBrowseImportFolder.UseVisualStyleBackColor = true;
            btnBrowseImportFolder.Click += btnBrowseImportFolder_Click;
            //
            // dgvAccounts
            //
            dgvAccounts.AllowUserToAddRows = false;
            dgvAccounts.AllowUserToDeleteRows = false;
            dgvAccounts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAccounts.Columns.AddRange(new DataGridViewColumn[] { colName, colBankLabel, colMmexAccount });
            dgvAccounts.Location = new Point(20, 130);
            dgvAccounts.Name = "dgvAccounts";
            dgvAccounts.RowHeadersWidth = 30;
            dgvAccounts.Size = new Size(680, 250);
            dgvAccounts.TabIndex = 9;
            //
            // colName
            //
            colName.HeaderText = "Account Name";
            colName.Name = "colName";
            colName.Width = 160;
            //
            // colBankLabel
            //
            colBankLabel.HeaderText = "Bank CSV Label";
            colBankLabel.Name = "colBankLabel";
            colBankLabel.Width = 240;
            //
            // colMmexAccount
            //
            colMmexAccount.HeaderText = "MMEX Account";
            colMmexAccount.Name = "colMmexAccount";
            colMmexAccount.Width = 200;
            //
            // btnAddAccount
            //
            btnAddAccount.Location = new Point(20, 390);
            btnAddAccount.Name = "btnAddAccount";
            btnAddAccount.Size = new Size(120, 30);
            btnAddAccount.TabIndex = 10;
            btnAddAccount.Text = "Add Account";
            btnAddAccount.UseVisualStyleBackColor = true;
            btnAddAccount.Click += btnAddAccount_Click;
            //
            // btnRemoveAccount
            //
            btnRemoveAccount.Location = new Point(150, 390);
            btnRemoveAccount.Name = "btnRemoveAccount";
            btnRemoveAccount.Size = new Size(130, 30);
            btnRemoveAccount.TabIndex = 11;
            btnRemoveAccount.Text = "Remove Selected";
            btnRemoveAccount.UseVisualStyleBackColor = true;
            btnRemoveAccount.Click += btnRemoveAccount_Click;
            //
            // btnLoadMmexAccounts
            //
            btnLoadMmexAccounts.Location = new Point(290, 390);
            btnLoadMmexAccounts.Name = "btnLoadMmexAccounts";
            btnLoadMmexAccounts.Size = new Size(170, 30);
            btnLoadMmexAccounts.TabIndex = 12;
            btnLoadMmexAccounts.Text = "Show MMEX Accounts";
            btnLoadMmexAccounts.UseVisualStyleBackColor = true;
            btnLoadMmexAccounts.Click += btnLoadMmexAccounts_Click;
            //
            // btnSave
            //
            btnSave.Location = new Point(490, 390);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 30);
            btnSave.TabIndex = 13;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            //
            // btnCancel
            //
            btnCancel.Location = new Point(600, 390);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 30);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // SettingsForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 440);
            Controls.Add(lblMmbPath);
            Controls.Add(txtMmbPath);
            Controls.Add(btnBrowseMmb);
            Controls.Add(lblBackupFolder);
            Controls.Add(txtBackupFolder);
            Controls.Add(btnBrowseBackupFolder);
            Controls.Add(lblImportFolder);
            Controls.Add(txtImportFolder);
            Controls.Add(btnBrowseImportFolder);
            Controls.Add(dgvAccounts);
            Controls.Add(btnAddAccount);
            Controls.Add(btnRemoveAccount);
            Controls.Add(btnLoadMmexAccounts);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)dgvAccounts).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblMmbPath;
        private TextBox txtMmbPath;
        private Button btnBrowseMmb;
        private Label lblBackupFolder;
        private TextBox txtBackupFolder;
        private Button btnBrowseBackupFolder;
        private Label lblImportFolder;
        private TextBox txtImportFolder;
        private Button btnBrowseImportFolder;
        private DataGridView dgvAccounts;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colBankLabel;
        private DataGridViewTextBoxColumn colMmexAccount;
        private Button btnAddAccount;
        private Button btnRemoveAccount;
        private Button btnLoadMmexAccounts;
        private Button btnSave;
        private Button btnCancel;
    }
}
