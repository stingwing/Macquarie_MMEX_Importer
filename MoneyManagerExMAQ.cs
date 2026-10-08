namespace MoneyManagerExMAQ
{
    public partial class MoneyManagerExMAQ : Form
    {
        private AppSettings _settings;
        private ImportService _importService;
        private readonly List<DetectedFile> _detectedFiles = new();
        private List<TransferPair> _transferPairs = new();
        // Guards the files grid's change events while it is being repopulated programmatically.
        private bool _suppressGridEvents;

        public MoneyManagerExMAQ()
        {
            InitializeComponent();
            using (var iconStream = typeof(MoneyManagerExMAQ).Assembly.GetManifestResourceStream("MoneyManagerExMAQ.app.ico"))
            {
                if (iconStream != null)
                {
                    Icon = new Icon(iconStream);
                }
            }
            // Files-grid rows are mapped to _detectedFiles by index, so the user must not re-sort them.
            foreach (DataGridViewColumn column in dgvFiles.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            _settings = AppSettings.Load();
            _importService = new ImportService(_settings);
            LoadMmexAccountNames();
            UpdateDbLabel();
        }

        // Fills the MMEX Account dropdown column from the configured database so label-less ING
        // files can be assigned to an account by hand. Falls back to just a blank entry if the
        // database can't be read.
        private void LoadMmexAccountNames()
        {
            var names = new List<string>();
            if (!string.IsNullOrEmpty(_settings.MmbFilePath) && File.Exists(_settings.MmbFilePath))
            {
                try
                {
                    using var db = MmexDatabase.Open(_settings.MmbFilePath, readOnly: true);
                    names = db.GetAccountNames();
                }
                catch
                {
                    // Leave the list empty; the dropdown will still offer the blank entry.
                }
            }

            colMmexAccount.Items.Clear();
            colMmexAccount.Items.Add(string.Empty);
            foreach (var name in names)
            {
                colMmexAccount.Items.Add(name);
            }
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
                LoadMmexAccountNames();
                UpdateDbLabel();
                // Loaded files were analyzed under the old settings (fingerprints, transfer text,
                // database) — redo them so the grid and any later write use the new ones.
                ReanalyzeUnwritten();
            }
        }

        // Re-runs Analyze for every loaded file not yet written, keeping manual account picks, then
        // refreshes transfer pairs and the grid. A file that can no longer be read (moved/deleted)
        // keeps its previous analysis and is reported.
        private void ReanalyzeUnwritten()
        {
            var failures = new List<string>();
            for (int i = 0; i < _detectedFiles.Count; i++)
            {
                var file = _detectedFiles[i];
                if (file.Written)
                {
                    continue;
                }
                try
                {
                    _detectedFiles[i] = _importService.Analyze(file.FilePath, file.AccountOverride);
                }
                catch (Exception ex)
                {
                    failures.Add($"{file.FileName}: {ex.Message}");
                }
            }

            _transferPairs = _importService.FindTransferPairs(_detectedFiles);
            RefreshFilesGrid();

            if (failures.Count > 0)
            {
                MessageBox.Show($"Could not re-check:\n\n{string.Join("\n", failures)}", "Re-analysis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                _transferPairs = _importService.FindTransferPairs(_detectedFiles);
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

            _suppressGridEvents = true;
            try
            {
                dgvFiles.Rows.Clear();
                foreach (var file in _detectedFiles)
                {
                    var pairedCount = _transferPairs.Count(p => p.DebitFile == file || p.CreditFile == file);
                    var status = file.Written
                        ? "Written to MMEX"
                        : file.Format == BankFileFormat.Unknown
                            ? "Unrecognised file format"
                        : !file.IsMapped
                            ? BankCsv.HasAccountColumn(file.Format) ? "No account mapping" : "Pick an account →"
                            : file.NewTransactions.Count == 0
                                ? "Up to date"
                                : pairedCount > 0
                                    ? $"{file.NewTransactions.Count} new ({pairedCount} in transfers)"
                                    : $"{file.NewTransactions.Count} new";

                    // The combo cell value must be one of the column's items or it raises DataError.
                    var account = file.MmexAccountName ?? string.Empty;
                    if (account.Length > 0 && !colMmexAccount.Items.Contains(account))
                    {
                        colMmexAccount.Items.Add(account);
                    }

                    var rowIndex = dgvFiles.Rows.Add(file.FileName, file.AccountLabel, account, file.TotalRows,
                        file.NewTransactions.Count, file.Issues.Count, status);
                    // Assigning an account is only meaningful before the file is written.
                    dgvFiles.Rows[rowIndex].Cells[colMmexAccount.Index].ReadOnly = file.Written;
                }
            }
            finally
            {
                _suppressGridEvents = false;
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

        // Commit a dropdown change as soon as the user picks a value (rather than on cell leave)
        // so re-analysis happens immediately.
        private void dgvFiles_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvFiles.IsCurrentCellDirty && dgvFiles.CurrentCell is DataGridViewComboBoxCell)
            {
                dgvFiles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvFiles_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressGridEvents || e.ColumnIndex != colMmexAccount.Index ||
                e.RowIndex < 0 || e.RowIndex >= _detectedFiles.Count)
            {
                return;
            }

            var file = _detectedFiles[e.RowIndex];
            var chosen = Convert.ToString(dgvFiles.Rows[e.RowIndex].Cells[colMmexAccount.Index].Value)?.Trim() ?? string.Empty;
            if (string.Equals(chosen, file.MmexAccountName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Deferred: this event fires mid-commit while the combo is still editing, and
            // AssignAccount rebuilds the grid (Rows.Clear), which WinForms rejects as reentrant.
            BeginInvoke(() => AssignAccount(file, chosen));
        }

        // Swallow the transient "value is not valid" errors a combo column raises while its cell
        // value and item list are being reconciled; RefreshFilesGrid keeps them consistent.
        // Anything else is a real problem, so show it rather than dropping it silently.
        private void dgvFiles_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            var transient = e.ColumnIndex == colMmexAccount.Index &&
                            (e.Context & (DataGridViewDataErrorContexts.Formatting | DataGridViewDataErrorContexts.Display)) != 0;
            if (!transient)
            {
                MessageBox.Show($"Files grid error ({e.Context}): {e.Exception?.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Re-analyzes a file against a manually chosen MMEX account (empty reverts to auto-detect).
        private void AssignAccount(DetectedFile file, string mmexAccountName)
        {
            if (file.Written)
            {
                return;
            }

            try
            {
                var reanalyzed = _importService.Analyze(file.FilePath,
                    string.IsNullOrEmpty(mmexAccountName) ? null : mmexAccountName);
                var index = _detectedFiles.IndexOf(file);
                if (index >= 0)
                {
                    _detectedFiles[index] = reanalyzed;
                }
                _transferPairs = _importService.FindTransferPairs(_detectedFiles);
                RefreshFilesGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not assign account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                results.Add($"Backup: {backupPath}");

                // Rows just written may also appear in other loaded files (overlapping downloads,
                // the other side of a transfer), so re-check those against the updated database.
                ReanalyzeUnwritten();
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
