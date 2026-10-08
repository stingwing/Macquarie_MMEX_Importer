namespace MoneyManagerExMAQ
{
    public partial class SettingsForm : Form
    {
        private readonly AppSettings _settings;

        public SettingsForm(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadData();
        }

        private void LoadData()
        {
            txtMmbPath.Text = _settings.MmbFilePath ?? string.Empty;
            txtBackupFolder.Text = _settings.BackupFolder ?? string.Empty;
            txtImportFolder.Text = _settings.ImportFolder ?? string.Empty;
            txtCategoryRecords.Text = _settings.CategoryRecordsPath ?? string.Empty;
            txtTransferMarkers.Text = _settings.TransferMarkers ?? string.Empty;

            foreach (var account in _settings.Accounts)
            {
                dgvAccounts.Rows.Add(account.Name, account.BankAccountLabel ?? string.Empty,
                    account.MmexAccountName ?? string.Empty, account.IngFingerprint ?? string.Empty);
            }
        }

        private void btnBrowseMmb_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MMEX Database (*.mmb)|*.mmb|All Files (*.*)|*.*",
                Title = "Select MoneyManagerEx Database"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtMmbPath.Text = ofd.FileName;
            }
        }

        private void btnBrowseBackupFolder_Click(object sender, EventArgs e)
        {
            BrowseFolder(txtBackupFolder, "Choose a folder for .mmb backups");
        }

        private void btnBrowseImportFolder_Click(object sender, EventArgs e)
        {
            BrowseFolder(txtImportFolder, "Choose the folder scanned bank files are archived to");
        }

        private void btnBrowseCategoryRecords_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select the ING alias file (raw description → clean payee)",
                InitialDirectory = File.Exists(txtCategoryRecords.Text)
                    ? Path.GetDirectoryName(txtCategoryRecords.Text)
                    : string.Empty,
                CheckFileExists = false
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtCategoryRecords.Text = ofd.FileName;
            }
        }

        private static void BrowseFolder(TextBox target, string description)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = description,
                SelectedPath = Directory.Exists(target.Text) ? target.Text : string.Empty
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                target.Text = fbd.SelectedPath;
            }
        }

        // Fills the MMEX Account column's autocomplete by reading account names from the
        // configured database, so mappings don't have to be typed from memory.
        private void btnLoadMmexAccounts_Click(object sender, EventArgs e)
        {
            var mmbPath = txtMmbPath.Text.Trim();
            if (string.IsNullOrEmpty(mmbPath) || !File.Exists(mmbPath))
            {
                MessageBox.Show("Set a valid MMEX database path first.", "No Database", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = MmexDatabase.Open(mmbPath, readOnly: true);
                var names = db.GetAccountNames();
                MessageBox.Show(
                    "Accounts in this MMEX database:\n\n" + string.Join("\n", names),
                    "MMEX Accounts", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not read the database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddAccount_Click(object sender, EventArgs e)
        {
            dgvAccounts.Rows.Add(string.Empty, string.Empty, string.Empty);
        }

        private void btnRemoveAccount_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvAccounts.SelectedRows.Cast<DataGridViewRow>().ToList())
            {
                dgvAccounts.Rows.Remove(row);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var accounts = new List<AccountInfo>();

            foreach (DataGridViewRow row in dgvAccounts.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var name = Convert.ToString(row.Cells["colName"].Value)?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (!names.Add(name))
                {
                    MessageBox.Show($"Duplicate account name: {name}", "Invalid Accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var bankLabel = Convert.ToString(row.Cells["colBankLabel"].Value)?.Trim();
                var mmexName = Convert.ToString(row.Cells["colMmexAccount"].Value)?.Trim();
                var fingerprint = Convert.ToString(row.Cells["colIngFingerprint"].Value)?.Trim();
                accounts.Add(new AccountInfo
                {
                    Name = name,
                    BankAccountLabel = string.IsNullOrEmpty(bankLabel) ? null : bankLabel,
                    MmexAccountName = string.IsNullOrEmpty(mmexName) ? null : mmexName,
                    IngFingerprint = string.IsNullOrEmpty(fingerprint) ? null : fingerprint
                });
            }

            _settings.Accounts = accounts;
            _settings.MmbFilePath = NullIfEmpty(txtMmbPath.Text);
            _settings.BackupFolder = NullIfEmpty(txtBackupFolder.Text);
            _settings.ImportFolder = NullIfEmpty(txtImportFolder.Text);
            _settings.CategoryRecordsPath = NullIfEmpty(txtCategoryRecords.Text);
            _settings.TransferMarkers = NullIfEmpty(txtTransferMarkers.Text);
            _settings.Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string? NullIfEmpty(string text)
        {
            var trimmed = text.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
