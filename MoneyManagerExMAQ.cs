namespace MoneyManagerExMAQ
{
    public partial class MoneyManagerExMAQ : Form
    {
        private AppSettings _settings;
        private ImportService _importService;
        private readonly List<DetectedFile> _detectedFiles = new();
        private List<TransferPair> _transferPairs = new();

        public MoneyManagerExMAQ()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            _importService = new ImportService(_settings);
            UpdateDbLabel();
        }

        private void UpdateDbLabel()
        {
            lblDb.Text = string.IsNullOrEmpty(_settings.MmbFilePath)
                ? "MMEX Database: (not configured — open Settings)"
                : $"MMEX Database: {_settings.MmbFilePath}";
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm(_settings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                _importService = new ImportService(_settings);
                UpdateDbLabel();
            }
        }

        private void btnScanDownloads_Click(object sender, EventArgs e)
        {
            try
            {
                var toImport = _importService.ScanDownloads(out var archivedDuplicates);

                foreach (var path in toImport)
                {
                    AnalyzeAndAdd(path);
                }

                var message = toImport.Count == 0
                    ? "No new bank export files found in Downloads."
                    : $"Found {toImport.Count} file(s) to import.";
                if (archivedDuplicates.Count > 0)
                {
                    message += $"\n{archivedDuplicates.Count} older duplicate download(s) were archived without importing.";
                }
                MessageBox.Show(message, "Scan Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Scan failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddFile_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Bank Export CSV",
                InitialDirectory = Directory.Exists(_settings.ImportFolder) ? _settings.ImportFolder : ImportService.DownloadsFolder
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                AnalyzeAndAdd(ofd.FileName);
            }
        }

        private void AnalyzeAndAdd(string filePath)
        {
            try
            {
                var existing = _detectedFiles.FindIndex(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                var detected = _importService.Analyze(filePath);

                if (existing >= 0)
                {
                    _detectedFiles[existing] = detected;
                }
                else
                {
                    _detectedFiles.Add(detected);
                }
                _transferPairs = ImportService.FindTransferPairs(_detectedFiles);
                RefreshFilesGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not analyze {Path.GetFileName(filePath)}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshFilesGrid()
        {
            var selectedPath = SelectedFile()?.FilePath;

            dgvFiles.Rows.Clear();
            foreach (var file in _detectedFiles)
            {
                var pairedCount = _transferPairs.Count(p => p.DebitFile == file || p.CreditFile == file);
                var status = file.Written
                    ? "Written to MMEX"
                    : !file.IsMapped
                        ? "No account mapping"
                        : file.NewTransactions.Count == 0
                            ? "Up to date"
                            : pairedCount > 0
                                ? $"{file.NewTransactions.Count} new ({pairedCount} in transfers)"
                                : $"{file.NewTransactions.Count} new";
                dgvFiles.Rows.Add(file.FileName, file.AccountLabel, file.MmexAccountName ?? "—", file.TotalRows,
                    file.NewTransactions.Count, file.Issues.Count, status);
            }

            if (selectedPath != null)
            {
                var index = _detectedFiles.FindIndex(f => f.FilePath == selectedPath);
                if (index >= 0)
                {
                    dgvFiles.Rows[index].Selected = true;
                }
            }

            RefreshTransactionsGrid();
        }

        private DetectedFile? SelectedFile()
        {
            if (dgvFiles.SelectedRows.Count == 0)
            {
                return null;
            }
            var index = dgvFiles.SelectedRows[0].Index;
            return index >= 0 && index < _detectedFiles.Count ? _detectedFiles[index] : null;
        }

        private void dgvFiles_SelectionChanged(object sender, EventArgs e)
        {
            RefreshTransactionsGrid();
        }

        private void RefreshTransactionsGrid()
        {
            dgvTransactions.Rows.Clear();
            var file = SelectedFile();
            if (file == null)
            {
                return;
            }

            foreach (var t in file.NewTransactions)
            {
                var debitPair = _transferPairs.FirstOrDefault(p => p.Debit == t);
                var creditPair = _transferPairs.FirstOrDefault(p => p.Credit == t);
                var type = debitPair != null
                    ? $"Transfer → {debitPair.CreditFile.MmexAccountName}"
                    : creditPair != null
                        ? $"Transfer ← {creditPair.DebitFile.MmexAccountName}"
                        : t.Credit > 0 ? "Deposit" : "Withdrawal";

                dgvTransactions.Rows.Add(t.Date.ToString("yyyy-MM-dd"), t.Details, t.Category, t.Subcategory,
                    t.Debit == 0 ? string.Empty : t.Debit.ToString("0.00"),
                    t.Credit == 0 ? string.Empty : t.Credit.ToString("0.00"),
                    type,
                    t.OriginalDescription);
            }

            lblTransactions.Text = file.Issues.Count > 0
                ? $"New transactions in {file.FileName} — {file.Issues.Count} issue(s): {string.Join(" | ", file.Issues.Take(3))}"
                : $"New transactions in {file.FileName}:";
        }

        private void btnWriteSelected_Click(object sender, EventArgs e)
        {
            var file = SelectedFile();
            if (file == null)
            {
                MessageBox.Show("Select a file first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            WriteFiles(new List<DetectedFile> { file });
        }

        private void btnWriteAll_Click(object sender, EventArgs e)
        {
            var ready = _detectedFiles.Where(f => f.IsMapped && !f.Written && f.NewTransactions.Count > 0).ToList();
            if (ready.Count == 0)
            {
                MessageBox.Show("No files with new transactions are ready to write.", "Nothing to Write", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            WriteFiles(ready);
        }

        private void WriteFiles(List<DetectedFile> files)
        {
            files = files.Where(f => f.IsMapped && !f.Written && f.NewTransactions.Count > 0).ToList();
            if (files.Count == 0)
            {
                MessageBox.Show("Nothing new to write for this selection.", "Nothing to Write", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (ImportService.IsMmexRunning())
            {
                MessageBox.Show("MoneyManagerEx is currently running. Close it first, then try again.",
                    "MMEX Is Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Pairs only merge into a Transfer row when both sides' files are in this write.
            var batchPairs = _transferPairs
                .Where(p => files.Contains(p.DebitFile) && files.Contains(p.CreditFile))
                .ToList();
            var unpairable = _transferPairs.Count(p =>
                files.Contains(p.DebitFile) != files.Contains(p.CreditFile));

            var lines = files.Select(f => $"  • {f.NewTransactions.Count} transaction(s) from {f.MmexAccountName}").ToList();
            if (batchPairs.Count > 0)
            {
                lines.Add($"  • {batchPairs.Count} pair(s) will be combined into Transfer rows");
            }
            var warning = unpairable > 0
                ? $"\n\nWarning: {unpairable} detected transfer pair(s) span a file NOT in this write — " +
                  "those rows will be written as plain withdrawals/deposits. Use \"Write All\" to combine them."
                : string.Empty;

            var confirm = MessageBox.Show(
                $"About to write to the MMEX database:\n\n{string.Join("\n", lines)}{warning}\n\n" +
                "A backup of the database will be taken first. Transactions are written as unreconciled. Continue?",
                "Confirm Write to MMEX", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var (summary, backupPath) = _importService.WriteBatch(files, batchPairs);

                var results = new List<string>
                {
                    $"Accounts written: {string.Join(", ", files.Select(f => f.MmexAccountName))}",
                    $"Singles written: {summary.SinglesByAccount.Values.Sum()}"
                };
                if (summary.TransfersInserted > 0)
                {
                    results.Add($"Transfers written: {summary.TransfersInserted}");
                }
                if (summary.PayeesCreated > 0)
                {
                    results.Add($"New payees created: {summary.PayeesCreated}");
                }
                if (summary.CategoriesCreated > 0)
                {
                    results.Add($"New categories created: {summary.CategoriesCreated}");
                }
                results.Add($"Backup: {backupPath}");

                _transferPairs = ImportService.FindTransferPairs(_detectedFiles);
                RefreshFilesGrid();
                MessageBox.Show(string.Join("\n", results), "Write Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                RefreshFilesGrid();
                MessageBox.Show($"Write failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
