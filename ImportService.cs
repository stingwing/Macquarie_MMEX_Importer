using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MoneyManagerExMAQ
{
    public class DetectedFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string AccountLabel { get; set; } = string.Empty;
        public string? MmexAccountName { get; set; }
        // The account the user picked by hand in the grid (null = auto-detected); kept so a
        // re-analysis after Settings changes or a write doesn't drop the manual choice.
        public string? AccountOverride { get; set; }
        public BankFileFormat Format { get; set; } = BankFileFormat.Unknown;
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

        // Transfer detection configured from this service's settings (built-in bank wording plus the
        // user's own-name markers), passed explicitly to everything that needs it.
        private readonly TransferDetector _transfers;

        public ImportService(AppSettings settings)
        {
            _settings = settings;
            _transfers = new TransferDetector(settings.TransferMarkers);
        }

        public static string DownloadsFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        // Finds bank-export CSVs (Macquarie/ING/CommBank) in Downloads, moves every one of them into the Import
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
                var format = BankCsv.DetectFileFormat(file);
                if (format == BankFileFormat.Unknown)
                {
                    continue;
                }
                // Macquarie files self-identify by account label; ING/CommBank files don't (empty label),
                // so key those by file path instead — otherwise every such file would collapse into
                // one empty-label group and all but the newest would be dropped. Overlapping ING
                // files for the same account are deduped at write time (see WriteBatch).
                var label = format == BankFileFormat.Macquarie ? BankCsv.Parse(file).AccountLabel : string.Empty;
                if (label.Length == 0)
                {
                    label = file;
                }
                candidates.Add((file, label, File.GetLastWriteTime(file)));
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

        // Parses a bank CSV, resolves which MMEX account it belongs to, and computes which of its
        // transactions are not yet in the database. mmexAccountOverride forces the target account
        // (used when the user assigns a label-less ING file manually in the UI).
        public DetectedFile Analyze(string filePath, string? mmexAccountOverride = null)
        {
            var detected = new DetectedFile
            {
                FilePath = filePath,
                AccountOverride = string.IsNullOrWhiteSpace(mmexAccountOverride) ? null : mmexAccountOverride.Trim()
            };

            var parsed = BankCsv.Parse(filePath);
            detected.AccountLabel = parsed.AccountLabel;
            detected.Format = parsed.Format;
            detected.TotalRows = parsed.Transactions.Count;
            detected.Issues.AddRange(parsed.Issues);

            var mmexAccountName = ResolveMmexAccount(parsed, mmexAccountOverride, detected);
            if (mmexAccountName == null)
            {
                return detected;
            }
            detected.MmexAccountName = mmexAccountName;

            if (string.IsNullOrEmpty(_settings.MmbFilePath))
            {
                detected.Issues.Add("MMEX database path is not configured — set it in Settings.");
                return detected;
            }

            using var db = MmexDatabase.Open(_settings.MmbFilePath, readOnly: true);
            var accountId = db.GetAccountId(mmexAccountName);
            if (!accountId.HasValue)
            {
                detected.Issues.Add($"MMEX account \"{mmexAccountName}\" not found in the database.");
                detected.MmexAccountName = null;
                return detected;
            }

            // Bank exports can reach back years before the account was set up in MMEX (ING files
            // start in 2019); rows before the account's opening date are already in its opening
            // balance, so they are never new.
            var transactions = parsed.Transactions;
            var openingDate = db.GetAccountInitialDate(accountId.Value);
            if (openingDate.HasValue)
            {
                transactions = transactions.Where(t => t.Date >= openingDate.Value).ToList();
                var skipped = parsed.Transactions.Count - transactions.Count;
                if (skipped > 0)
                {
                    detected.Issues.Add($"{skipped} row(s) dated before the account's MMEX opening date ({openingDate.Value:yyyy-MM-dd}) ignored.");
                }
            }

            detected.NewTransactions = db.FindNewTransactions(accountId.Value, transactions, _transfers);

            // Reconcile the new rows' payees/categories against the existing ledger before writing,
            // so a payee the user has recategorised in MMEX is imported under that category. (Done
            // after dedupe: matching ignores descriptions and categories, and this way warnings
            // only mention rows that will be written.) ING/CommBank rows arrive as one raw
            // description with no payee/category, so they are resolved to a clean payee first;
            // Macquarie rows already carry the payee name (Details).
            if (BankCsv.HasAccountColumn(parsed.Format))
            {
                ApplyLearnedCategories(detected.NewTransactions, db, detected.Issues);
            }
            else
            {
                ResolveRawPayees(detected.NewTransactions, db);
            }
            return detected;
        }

        // Determines the target MMEX account: explicit override first, then the Macquarie in-file
        // account label, then (for ING) a configured fingerprint token found in the descriptions.
        // Adds an Issue and returns null when it can't decide.
        private string? ResolveMmexAccount(BankCsvParseResult parsed, string? mmexAccountOverride, DetectedFile detected)
        {
            if (!string.IsNullOrWhiteSpace(mmexAccountOverride))
            {
                return mmexAccountOverride.Trim();
            }

            if (parsed.AccountLabel.Length > 0)
            {
                var account = _settings.Accounts.FirstOrDefault(a =>
                    string.Equals(a.BankAccountLabel, parsed.AccountLabel, StringComparison.OrdinalIgnoreCase));
                if (account == null || string.IsNullOrEmpty(account.MmexAccountName))
                {
                    detected.Issues.Add($"No account mapping for bank label \"{parsed.AccountLabel}\" — add it in Settings.");
                    return null;
                }
                return account.MmexAccountName;
            }

            // No in-file label (ING/CommBank): try each account's fingerprint token(s) against the text.
            var match = MatchByFingerprint(parsed.Transactions, out var ambiguous);
            if (match != null)
            {
                return match;
            }

            detected.Issues.Add(parsed.Format == BankFileFormat.Unknown
                ? "Could not determine the account for this file."
                : ambiguous
                    ? $"{parsed.Format} file — fingerprints for more than one account matched equally. Assign an MMEX account in the grid."
                    : $"{parsed.Format} file — no fingerprint matched. Assign an MMEX account in the grid.");
            return null;
        }

        // Picks the account whose fingerprint token(s) appear in the MOST transaction rows. A file
        // can carry a few stray references to another account (e.g. the other side of a transfer),
        // so the account with the strongest signal wins rather than the first with any match. A tie
        // between different accounts is reported as ambiguous instead of picking one.
        // Tokens must not be embedded in a longer number, so card suffix "8299" matches
        // "Card 462263xxxxxx8299" but not "Receipt 198299".
        private string? MatchByFingerprint(IReadOnlyList<BankTransaction> transactions, out bool ambiguous)
        {
            string? best = null;
            var bestCount = 0;
            ambiguous = false;
            foreach (var account in _settings.Accounts)
            {
                if (string.IsNullOrWhiteSpace(account.IngFingerprint) || string.IsNullOrEmpty(account.MmexAccountName))
                {
                    continue;
                }
                var patterns = account.IngFingerprint
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(token => new Regex($@"(?<!\d){Regex.Escape(token)}(?!\d)",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    .ToList();
                var count = transactions.Count(t => patterns.Any(p => p.IsMatch(t.OriginalDescription)));
                if (count > bestCount)
                {
                    bestCount = count;
                    best = account.MmexAccountName;
                    ambiguous = false;
                }
                else if (count > 0 && count == bestCount &&
                         !string.Equals(best, account.MmexAccountName, StringComparison.OrdinalIgnoreCase))
                {
                    ambiguous = true;
                }
            }
            return bestCount > 0 && !ambiguous ? best : null;
        }

        // The payee resolver and category paths, reused across Analyze calls until the database or
        // alias file changes on disk — so a scan of several ING files (or repeated account picks)
        // doesn't re-read them each time, while category edits made in MMEX are still picked up.
        private record CategoryLookups(
            DateTime DbStamp,
            DateTime AliasStamp,
            Dictionary<string, long> PayeeCategories,
            Dictionary<long, string> CategoryPaths,
            HashSet<string> HiddenPaths)
        {
            // Reverse of CategoryPaths ("Leisure > Magic" -> id); first id wins on a duplicate path.
            public Dictionary<string, long> IdsByPath { get; } = CategoryPaths
                .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

            // Built on first use (ING/CommBank only): it reads every transaction's notes, which
            // Macquarie-only analyses never need.
            public PayeeResolver? Resolver { get; set; }
        }

        private CategoryLookups? _lookups;

        private CategoryLookups GetLookups(MmexDatabase db)
        {
            var dbStamp = File.GetLastWriteTimeUtc(_settings.MmbFilePath!);
            var aliasStamp = string.IsNullOrEmpty(_settings.CategoryRecordsPath)
                ? DateTime.MinValue
                : File.GetLastWriteTimeUtc(_settings.CategoryRecordsPath);
            if (_lookups == null || _lookups.DbStamp != dbStamp || _lookups.AliasStamp != aliasStamp)
            {
                var usable = db.GetCategoryPaths();
                var hidden = db.GetCategoryPaths(includeHidden: true).Values
                    .Except(usable.Values, StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                _lookups = new CategoryLookups(dbStamp, aliasStamp, db.GetPayeeCategories(), usable, hidden);
            }
            return _lookups;
        }

        // ING/CommBank rows: resolve the raw description to a clean payee and inherit its category.
        private void ResolveRawPayees(IReadOnlyList<BankTransaction> transactions, MmexDatabase db)
        {
            var lookups = GetLookups(db);
            lookups.Resolver ??= new PayeeResolver(
                CategoryRecords.Load(_settings.CategoryRecordsPath), db.GetDescriptionPayees(), lookups.PayeeCategories);

            foreach (var transaction in transactions)
            {
                var resolution = lookups.Resolver.Resolve(transaction.OriginalDescription);
                if (!string.IsNullOrEmpty(resolution.PayeeName))
                {
                    transaction.Details = resolution.PayeeName;
                }
                if (!TryApplyCategory(transaction, resolution.CategoryId, lookups))
                {
                    transaction.ResolvedCategoryId = null;
                    transaction.Category = string.Empty;
                    transaction.Subcategory = string.Empty;
                }
            }
        }

        // Macquarie rows: when the payee (Details, which becomes PAYEENAME on write) already exists
        // in MMEX with a learned category, that wins over the bank's own Category/Subcategory —
        // e.g. the bank says "Leisure > Games" for MTG Mate but the user files it under Magic.
        // Payees MMEX has never seen take the bank's category, but only if it exists in MMEX's
        // taxonomy: categories removed in a consolidation are never recreated — the row falls back
        // to the bank's parent category if that exists, else imports uncategorised (with an issue).
        private void ApplyLearnedCategories(IReadOnlyList<BankTransaction> transactions, MmexDatabase db, List<string> issues)
        {
            var lookups = GetLookups(db);
            var unknownBankCategories = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string Why(string bankPath) => lookups.HiddenPaths.Contains(bankPath) ? "is hidden (inactive) in MMEX" : "is not in MMEX";

            foreach (var transaction in transactions)
            {
                if (lookups.PayeeCategories.TryGetValue(transaction.Details, out var learned) &&
                    TryApplyCategory(transaction, learned, lookups))
                {
                    continue;
                }
                if (transaction.Category.Length == 0)
                {
                    continue;
                }

                var bankPath = transaction.Subcategory.Length > 0
                    ? $"{transaction.Category} > {transaction.Subcategory}"
                    : transaction.Category;
                if (lookups.IdsByPath.TryGetValue(bankPath, out var bankId))
                {
                    TryApplyCategory(transaction, bankId, lookups);
                }
                else if (transaction.Subcategory.Length > 0 &&
                         lookups.IdsByPath.TryGetValue(transaction.Category, out var parentId))
                {
                    TryApplyCategory(transaction, parentId, lookups);
                    unknownBankCategories[bankPath] = $"{Why(bankPath)} — filed under \"{transaction.Category}\"";
                }
                else
                {
                    transaction.ResolvedCategoryId = null;
                    transaction.Category = string.Empty;
                    transaction.Subcategory = string.Empty;
                    unknownBankCategories[bankPath] = $"{Why(bankPath)} — imported uncategorised";
                }
            }

            foreach (var (bankPath, outcome) in unknownBankCategories)
            {
                issues.Add($"Bank category \"{bankPath}\" {outcome}.");
            }
        }

        // Sets the row's category to an existing MMEX category id. The DB has no foreign keys, so
        // a payee's CATEGID can point at a category that was since deleted or merged — only ids
        // that still exist are applied. Returns false (row untouched) otherwise.
        private static bool TryApplyCategory(BankTransaction transaction, long? categoryId, CategoryLookups lookups)
        {
            if (!categoryId.HasValue || !lookups.CategoryPaths.TryGetValue(categoryId.Value, out var path))
            {
                return false;
            }
            transaction.ResolvedCategoryId = categoryId;
            // Cosmetic: show the category path in the grid (the write uses the id).
            transaction.Category = path;
            transaction.Subcategory = string.Empty;
            return true;
        }

        public static bool IsMmexRunning() =>
            Process.GetProcesses().Any(p => p.ProcessName.Equals("mmex", StringComparison.OrdinalIgnoreCase));

        // Finds debit/credit pairs across the analyzed files that represent one internal
        // transfer: different MMEX accounts, equal amounts, dates within a few days (Macquarie
        // often posts the two sides a day apart), and internal-transfer wording on both sides.
        // Same-amount recurrences (e.g. weekly $500 card payments) are matched nearest-date-first
        // so pairs never cross over each other. Both sides must look internal (TransferDetector)
        // so a merchant refund coincidentally equal to an unrelated payment is never paired.
        public List<TransferPair> FindTransferPairs(IReadOnlyList<DetectedFile> files)
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

                    foreach (var debit in debitFile.NewTransactions.Where(t => t.Debit > 0 && _transfers.LooksInternal(t)))
                    {
                        foreach (var credit in creditFile.NewTransactions.Where(t => t.Credit > 0 && _transfers.LooksInternal(t)))
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
            // Rows queued earlier in this batch are recorded in the ledger too, so when two files
            // map to the same account (overlapping ING downloads) the overlap is inserted once.
            var ledgers = new Dictionary<long, MmexDatabase.ExistingLedger>();
            var singles = new List<(long AccountId, BankTransaction Transaction)>();
            MmexDatabase.ExistingLedger Ledger(long accountId)
            {
                if (!ledgers.TryGetValue(accountId, out var ledger))
                {
                    ledger = db.GetExistingLedger(accountId, _transfers);
                    ledgers[accountId] = ledger;
                }
                return ledger;
            }
            bool AlreadyInDb(long accountId, BankTransaction transaction, DetectedFile source) =>
                Ledger(accountId).TryConsume(transaction, source);
            void Queue(long accountId, BankTransaction transaction, DetectedFile source)
            {
                singles.Add((accountId, transaction));
                Ledger(accountId).Record(transaction, isTransfer: false, source);
            }

            var batchPairs = transferPairs
                .Where(p => accountIds.ContainsKey(p.DebitFile) && accountIds.ContainsKey(p.CreditFile))
                .ToList();
            var pairedRows = new HashSet<BankTransaction>();
            var transfers = new List<(long SourceAccountId, long TargetAccountId, BankTransaction Debit, BankTransaction Credit)>();

            foreach (var pair in batchPairs)
            {
                pairedRows.Add(pair.Debit);
                pairedRows.Add(pair.Credit);

                var sourceId = accountIds[pair.DebitFile];
                var targetId = accountIds[pair.CreditFile];
                var debitInDb = AlreadyInDb(sourceId, pair.Debit, pair.DebitFile);
                var creditInDb = AlreadyInDb(targetId, pair.Credit, pair.CreditFile);

                if (!debitInDb && !creditInDb)
                {
                    transfers.Add((sourceId, targetId, pair.Debit, pair.Credit));
                    Ledger(sourceId).Record(pair.Debit, isTransfer: true, pair.DebitFile);
                    Ledger(targetId).Record(pair.Credit, isTransfer: true, pair.CreditFile);
                }
                else if (!debitInDb)
                {
                    Queue(sourceId, pair.Debit, pair.DebitFile);
                }
                else if (!creditInDb)
                {
                    Queue(targetId, pair.Credit, pair.CreditFile);
                }
            }

            foreach (var file in files)
            {
                var accountId = accountIds[file];
                foreach (var transaction in file.NewTransactions)
                {
                    if (pairedRows.Contains(transaction) || AlreadyInDb(accountId, transaction, file))
                    {
                        continue;
                    }
                    Queue(accountId, transaction, file);
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
