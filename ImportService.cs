using System.Diagnostics;

namespace MoneyManagerExMAQ
{
    public class DetectedFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string AccountLabel { get; set; } = string.Empty;
        public string? MmexAccountName { get; set; }
        public int TotalRows { get; set; }
        public List<BankTransaction> NewTransactions { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public bool Written { get; set; }

        public string FileName => Path.GetFileName(FilePath);
        public bool IsMapped => !string.IsNullOrEmpty(MmexAccountName);
    }

    // One detected internal transfer: a debit in one account's file and the matching
    // credit in another account's file. Written to MMEX as a single Transfer row.
    public class TransferPair
    {
        public required DetectedFile DebitFile { get; init; }
        public required BankTransaction Debit { get; init; }
        public required DetectedFile CreditFile { get; init; }
        public required BankTransaction Credit { get; init; }
    }

    public class ImportService
    {
        private readonly AppSettings _settings;

        public ImportService(AppSettings settings)
        {
            _settings = settings;
        }

        public static string DownloadsFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        // Finds Macquarie-format CSVs in Downloads, moves every one of them into the Import
        // folder (archive), and returns only the newest file per bank account label — older
        // re-downloads of the same account are archived but not offered for import.
        // sourceFolder overrides Downloads (used by tests).
        public List<string> ScanDownloads(out List<string> archivedDuplicates, string? sourceFolder = null)
        {
            if (string.IsNullOrEmpty(_settings.ImportFolder))
            {
                throw new InvalidOperationException("Import folder is not configured. Set it in Settings first.");
            }
            Directory.CreateDirectory(_settings.ImportFolder);

            var candidates = new List<(string Path, string Label, DateTime Modified)>();
            foreach (var file in Directory.EnumerateFiles(sourceFolder ?? DownloadsFolder, "*.csv"))
            {
                if (!BankCsv.LooksLikeBankExport(file))
                {
                    continue;
                }
                var parsed = BankCsv.Parse(file);
                if (parsed.AccountLabel.Length == 0)
                {
                    continue;
                }
                candidates.Add((file, parsed.AccountLabel, File.GetLastWriteTime(file)));
            }

            var newestPerLabel = candidates
                .GroupBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(c => c.Modified).First().Path)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toImport = new List<string>();
            archivedDuplicates = new List<string>();

            foreach (var (path, _, _) in candidates)
            {
                var movedPath = MoveToImportFolder(path);
                if (newestPerLabel.Contains(path))
                {
                    toImport.Add(movedPath);
                }
                else
                {
                    archivedDuplicates.Add(movedPath);
                }
            }

            return toImport;
        }

        private string MoveToImportFolder(string sourcePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourcePath);
            var extension = Path.GetExtension(sourcePath);
            var destination = Path.Combine(_settings.ImportFolder!, fileName + extension);

            for (int suffix = 1; File.Exists(destination); suffix++)
            {
                destination = Path.Combine(_settings.ImportFolder!, $"{fileName}_{suffix}{extension}");
            }

            File.Move(sourcePath, destination);
            return destination;
        }

        // Parses a bank CSV, resolves which MMEX account it belongs to, and computes which
        // of its transactions are not yet in the database.
        public DetectedFile Analyze(string filePath)
        {
            var detected = new DetectedFile { FilePath = filePath };

            var parsed = BankCsv.Parse(filePath);
            detected.AccountLabel = parsed.AccountLabel;
            detected.TotalRows = parsed.Transactions.Count;
            detected.Issues.AddRange(parsed.Issues);

            var account = _settings.Accounts.FirstOrDefault(a =>
                string.Equals(a.BankAccountLabel, parsed.AccountLabel, StringComparison.OrdinalIgnoreCase));
            if (account == null || string.IsNullOrEmpty(account.MmexAccountName))
            {
                detected.Issues.Add($"No account mapping for bank label \"{parsed.AccountLabel}\" — add it in Settings.");
                return detected;
            }
            detected.MmexAccountName = account.MmexAccountName;

            if (string.IsNullOrEmpty(_settings.MmbFilePath))
            {
                detected.Issues.Add("MMEX database path is not configured — set it in Settings.");
                return detected;
            }

            using var db = MmexDatabase.Open(_settings.MmbFilePath, readOnly: true);
            var accountId = db.GetAccountId(account.MmexAccountName);
            if (!accountId.HasValue)
            {
                detected.Issues.Add($"MMEX account \"{account.MmexAccountName}\" not found in the database.");
                detected.MmexAccountName = null;
                return detected;
            }

            detected.NewTransactions = db.FindNewTransactions(accountId.Value, parsed.Transactions);
            return detected;
        }

        public static bool IsMmexRunning() =>
            Process.GetProcesses().Any(p => p.ProcessName.Equals("mmex", StringComparison.OrdinalIgnoreCase));

        // Both sides of a candidate pair must look internal (see BankCsv.LooksLikeInternalTransfer)
        // before they are matched, so a merchant refund coincidentally equal to an unrelated
        // payment is never paired.
        public static bool LooksLikeInternalTransfer(BankTransaction transaction) =>
            BankCsv.LooksLikeInternalTransfer(transaction);

        // Finds debit/credit pairs across the analyzed files that represent one internal
        // transfer: different MMEX accounts, equal amounts, dates within a few days (Macquarie
        // often posts the two sides a day apart), and internal-transfer wording on both sides.
        // Same-amount recurrences (e.g. weekly $500 card payments) are matched nearest-date-first
        // so pairs never cross over each other.
        public static List<TransferPair> FindTransferPairs(IReadOnlyList<DetectedFile> files)
        {
            var eligible = files.Where(f => f.IsMapped && !f.Written).ToList();

            var candidates = new List<TransferPair>();
            foreach (var debitFile in eligible)
            {
                foreach (var creditFile in eligible)
                {
                    if (string.Equals(debitFile.MmexAccountName, creditFile.MmexAccountName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (var debit in debitFile.NewTransactions.Where(t => t.Debit > 0 && LooksLikeInternalTransfer(t)))
                    {
                        foreach (var credit in creditFile.NewTransactions.Where(t => t.Credit > 0 && LooksLikeInternalTransfer(t)))
                        {
                            if (debit.Debit == credit.Credit &&
                                Math.Abs((debit.Date - credit.Date).TotalDays) <= BankCsv.TransferDateToleranceDays)
                            {
                                candidates.Add(new TransferPair
                                {
                                    DebitFile = debitFile,
                                    Debit = debit,
                                    CreditFile = creditFile,
                                    Credit = credit
                                });
                            }
                        }
                    }
                }
            }

            var pairs = new List<TransferPair>();
            var used = new HashSet<BankTransaction>();
            foreach (var candidate in candidates.OrderBy(c => Math.Abs((c.Debit.Date - c.Credit.Date).TotalDays)))
            {
                if (used.Contains(candidate.Debit) || used.Contains(candidate.Credit))
                {
                    continue;
                }
                used.Add(candidate.Debit);
                used.Add(candidate.Credit);
                pairs.Add(candidate);
            }

            return pairs;
        }

        // Writes a batch of files into MMEX in one database transaction with one backup.
        // Pairs whose two sides are both in this batch become single Transfer rows; everything
        // else is written as Withdrawal/Deposit singles (V1 behavior). Refuses if MMEX is running.
        public (MmexBatchSummary Summary, string BackupPath) WriteBatch(
            IReadOnlyList<DetectedFile> files, IReadOnlyList<TransferPair> transferPairs)
        {
            files = files.Where(f => f.IsMapped && !f.Written && f.NewTransactions.Count > 0).ToList();
            if (files.Count == 0)
            {
                throw new InvalidOperationException("There are no new transactions to write.");
            }
            if (IsMmexRunning())
            {
                throw new InvalidOperationException("MoneyManagerEx is currently running. Close it before writing.");
            }

            var backupPath = BackupDatabase();

            using var db = MmexDatabase.Open(_settings.MmbFilePath!, readOnly: false);

            var accountIds = new Dictionary<DetectedFile, long>();
            foreach (var file in files)
            {
                accountIds[file] = db.GetAccountId(file.MmexAccountName!) ??
                    throw new InvalidOperationException($"MMEX account \"{file.MmexAccountName}\" not found.");
            }

            // Re-check every row against the database at write time (it may have changed since
            // Analyze), consuming ledger entries so same-day same-amount repeats stay correct.
            var ledgers = new Dictionary<long, MmexDatabase.ExistingLedger>();
            bool AlreadyInDb(long accountId, BankTransaction transaction)
            {
                if (!ledgers.TryGetValue(accountId, out var ledger))
                {
                    ledger = db.GetExistingLedger(accountId);
                    ledgers[accountId] = ledger;
                }
                return ledger.TryConsume(transaction);
            }

            var batchPairs = transferPairs
                .Where(p => accountIds.ContainsKey(p.DebitFile) && accountIds.ContainsKey(p.CreditFile))
                .ToList();
            var pairedRows = new HashSet<BankTransaction>();
            var singles = new List<(long AccountId, BankTransaction Transaction)>();
            var transfers = new List<(long SourceAccountId, long TargetAccountId, BankTransaction Debit, BankTransaction Credit)>();

            foreach (var pair in batchPairs)
            {
                pairedRows.Add(pair.Debit);
                pairedRows.Add(pair.Credit);

                var sourceId = accountIds[pair.DebitFile];
                var targetId = accountIds[pair.CreditFile];
                var debitInDb = AlreadyInDb(sourceId, pair.Debit);
                var creditInDb = AlreadyInDb(targetId, pair.Credit);

                if (!debitInDb && !creditInDb)
                {
                    transfers.Add((sourceId, targetId, pair.Debit, pair.Credit));
                }
                else if (!debitInDb)
                {
                    singles.Add((sourceId, pair.Debit));
                }
                else if (!creditInDb)
                {
                    singles.Add((targetId, pair.Credit));
                }
            }

            foreach (var file in files)
            {
                var accountId = accountIds[file];
                foreach (var transaction in file.NewTransactions)
                {
                    if (pairedRows.Contains(transaction) || AlreadyInDb(accountId, transaction))
                    {
                        continue;
                    }
                    singles.Add((accountId, transaction));
                }
            }

            var summary = db.WriteBatch(singles, transfers);

            foreach (var file in files)
            {
                file.Written = true;
                file.NewTransactions = new List<BankTransaction>();
            }

            return (summary, backupPath);
        }

        private string BackupDatabase()
        {
            var mmbPath = _settings.MmbFilePath!;
            var backupFolder = string.IsNullOrEmpty(_settings.BackupFolder)
                ? Path.Combine(Path.GetDirectoryName(mmbPath)!, "Backups")
                : _settings.BackupFolder;
            Directory.CreateDirectory(backupFolder);

            var baseName = $"{Path.GetFileNameWithoutExtension(mmbPath)}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var extension = Path.GetExtension(mmbPath);
            var backupPath = Path.Combine(backupFolder, baseName + extension);
            for (int suffix = 1; File.Exists(backupPath); suffix++)
            {
                backupPath = Path.Combine(backupFolder, $"{baseName}_{suffix}{extension}");
            }

            File.Copy(mmbPath, backupPath, overwrite: false);
            return backupPath;
        }
    }
}
