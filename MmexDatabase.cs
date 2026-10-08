using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MoneyManagerExMAQ
{
    // A transaction's identity for duplicate detection: same day, same debit amount,
    // same credit amount. Descriptions are excluded on purpose (banks reword them).
    public readonly record struct TransactionKey(DateOnly Date, decimal Debit, decimal Credit)
    {
        public static TransactionKey From(BankTransaction t) =>
            new(DateOnly.FromDateTime(t.Date), Math.Round(t.Debit, 2), Math.Round(t.Credit, 2));
    }

    public class MmexBatchSummary
    {
        // Withdrawal/Deposit rows inserted, keyed by ACCOUNTID.
        public Dictionary<long, int> SinglesByAccount { get; } = new();
        public int TransfersInserted { get; set; }
        public int PayeesCreated { get; set; }
        public int TotalInserted => SinglesByAccount.Values.Sum() + TransfersInserted;
    }

    // Direct access to a MoneyManagerEx .mmb database (plain SQLite, schema as of MMEX 1.9.x).
    public class MmexDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private long _lastGeneratedId;

        private MmexDatabase(SqliteConnection connection)
        {
            _connection = connection;
        }

        public static MmexDatabase Open(string mmbPath, bool readOnly)
        {
            if (!File.Exists(mmbPath))
            {
                throw new FileNotFoundException($"MMEX database not found: {mmbPath}");
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = mmbPath,
                Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
                Pooling = false
            };

            var connection = new SqliteConnection(builder.ConnectionString);
            connection.Open();

            var db = new MmexDatabase(connection);
            try
            {
                db.ValidateSchema();
            }
            catch
            {
                db.Dispose();
                throw;
            }
            return db;
        }

        // Refuse to touch anything that doesn't look like the MMEX 1.9 schema we were built against.
        private void ValidateSchema()
        {
            string[] requiredTables = { "CHECKINGACCOUNT_V1", "ACCOUNTLIST_V1", "PAYEE_V1", "CATEGORY_V1", "INFOTABLE_V1" };
            foreach (var table in requiredTables)
            {
                if (ScalarLong($"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{table}'") != 1)
                {
                    throw new InvalidOperationException($"This file does not look like an MMEX database (missing table {table}).");
                }
            }

            var version = ScalarString("SELECT INFOVALUE FROM INFOTABLE_V1 WHERE INFONAME = 'MMEXVERSION'");
            if (version == null || !version.StartsWith("1.9", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Unsupported MMEX version \"{version ?? "unknown"}\". This tool was verified against MMEX 1.9.x — " +
                    "re-verify the schema before writing to a database from a different version.");
            }
        }

        public List<string> GetAccountNames()
        {
            var names = new List<string>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT ACCOUNTNAME FROM ACCOUNTLIST_V1 ORDER BY ACCOUNTNAME";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                names.Add(reader.GetString(0));
            }
            return names;
        }

        public long? GetAccountId(string accountName)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT ACCOUNTID FROM ACCOUNTLIST_V1 WHERE ACCOUNTNAME = @name COLLATE NOCASE";
            cmd.Parameters.AddWithValue("@name", accountName);
            var result = cmd.ExecuteScalar();
            return result == null || result is DBNull ? null : Convert.ToInt64(result);
        }

        // The account's opening date (INITIALDATE). MMEX folds everything before it into the
        // opening balance (INITIALBAL), so bank rows dated earlier are not part of the ledger.
        public DateTime? GetAccountInitialDate(long accountId)
        {
            var value = ScalarString("SELECT INITIALDATE FROM ACCOUNTLIST_V1 WHERE ACCOUNTID = @id", ("@id", accountId));
            return value != null && value.Length >= 10 &&
                   DateTime.TryParseExact(value[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;
        }

        // Active payee names mapped to the category new transactions for them should get (-1 when
        // none). This is learned from the user's own ledger: the category on the payee's most
        // recent transaction by date, so recategorising the latest transactions in MMEX carries
        // over to the next import. (Not by LASTUPDATEDTIME: bulk history imports and scripted
        // recategorisations restamp thousands of old rows, making "last edited" arbitrary.
        // Backtested on 12 months of the user's ledger, latest-dated predicted the filed
        // category 95% of the time, beating majority-vote and same-amount variants.)
        // Falls back to the payee's default CATEGID when none of
        // its transactions is categorised. Transfers, split parents (CATEGID -1), voided and
        // deleted rows, and rows pointing at a since-deleted or inactive (hidden) category are ignored. On a duplicate
        // name the first row wins; that only affects category inheritance, which the user can correct.
        public Dictionary<string, long> GetPayeeCategories()
        {
            var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                WITH latest AS (
                    SELECT t.PAYEEID, t.CATEGID,
                           ROW_NUMBER() OVER (PARTITION BY t.PAYEEID
                                              ORDER BY t.TRANSDATE DESC, t.LASTUPDATEDTIME DESC, t.TRANSID DESC) AS rn
                    FROM CHECKINGACCOUNT_V1 t
                    JOIN CATEGORY_V1 c ON c.CATEGID = t.CATEGID AND IFNULL(c.ACTIVE, 1) <> 0
                    WHERE t.TRANSCODE <> 'Transfer' AND t.PAYEEID <> -1
                      AND IFNULL(t.DELETEDTIME, '') = '' AND IFNULL(t.STATUS, '') <> 'V'
                )
                SELECT p.PAYEENAME, COALESCE(l.CATEGID, p.CATEGID)
                FROM PAYEE_V1 p
                LEFT JOIN latest l ON l.PAYEEID = p.PAYEEID AND l.rn = 1
                WHERE p.ACTIVE = 1 OR p.ACTIVE IS NULL";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (name.Length == 0 || map.ContainsKey(name))
                {
                    continue;
                }
                map[name] = reader.IsDBNull(1) ? -1 : reader.GetInt64(1);
            }
            return map;
        }

        // The raw bank description and payee of every recorded (non-transfer) transaction, oldest
        // first. The description is the last line of NOTES, which is where imports store the bank's
        // original text. PayeeResolver learns "this description -> this payee" from it.
        public List<(string Description, string Payee)> GetDescriptionPayees()
        {
            var history = new List<(string, string)>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.NOTES, p.PAYEENAME
                FROM CHECKINGACCOUNT_V1 t
                JOIN PAYEE_V1 p ON p.PAYEEID = t.PAYEEID
                WHERE t.TRANSCODE <> 'Transfer' AND IFNULL(t.DELETEDTIME, '') = '' AND IFNULL(t.NOTES, '') <> ''
                ORDER BY t.TRANSDATE, t.TRANSID";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var notes = reader.GetString(0);
                var lastLine = notes[(notes.LastIndexOf('\n') + 1)..].Trim();
                if (lastLine.Length > 0)
                {
                    history.Add((lastLine, reader.GetString(1)));
                }
            }
            return history;
        }

        // Category ids mapped to their full path ("Food & Drink > Groceries"). By default only
        // usable categories: the category and all of its ancestors are active, since deactivating a
        // parent hides its whole subtree in MMEX. includeHidden also returns the rest (used to tell
        // "hidden in MMEX" apart from "not in MMEX" in import messages).
        public Dictionary<long, string> GetCategoryPaths(bool includeHidden = false)
        {
            var name = new Dictionary<long, string>();
            var parent = new Dictionary<long, long>();
            var active = new HashSet<long>();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT CATEGID, CATEGNAME, PARENTID, IFNULL(ACTIVE, 1) FROM CATEGORY_V1";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetInt64(0);
                    name[id] = reader.GetString(1);
                    parent[id] = reader.IsDBNull(2) ? -1 : reader.GetInt64(2);
                    if (reader.GetInt64(3) != 0)
                    {
                        active.Add(id);
                    }
                }
            }

            var paths = new Dictionary<long, string>();
            foreach (var id in name.Keys)
            {
                var parts = new List<string>();
                var usable = true;
                var current = id;
                var guard = 0;
                while (current != -1 && name.ContainsKey(current) && guard++ < 20)
                {
                    parts.Add(name[current]);
                    usable &= active.Contains(current);
                    current = parent.TryGetValue(current, out var p) ? p : -1;
                }
                if (!usable && !includeHidden)
                {
                    continue;
                }
                parts.Reverse();
                paths[id] = string.Join(" > ", parts);
            }
            return paths;
        }

        // Everything already recorded against one account, consumable entry-by-entry so
        // same-day same-amount duplicates are handled by count rather than collapsed.
        public class ExistingLedger
        {
            // Source is null for rows already in the database; for rows queued earlier in the same
            // write batch it is the file they came from (see Record). OwnNameWorded: the transfer's
            // text carries one of the user's own-name markers.
            internal record Entry(TransactionKey Key, bool IsTransfer, bool OwnNameWorded = false, object? Source = null)
            {
                public bool Used { get; set; }
            }

            private readonly TransferDetector _transfers;

            public ExistingLedger(TransferDetector transfers)
            {
                _transfers = transfers;
            }

            internal List<Entry> Entries { get; } = new();

            // Adds a row queued for insertion in the current batch, so the same row arriving from
            // another file for this account (e.g. two overlapping ING downloads) matches it instead
            // of being inserted twice. Rows from the same source file never match each other, so a
            // file's own same-day same-amount repeats are all kept.
            public void Record(BankTransaction transaction, bool isTransfer, object source) =>
                Entries.Add(new Entry(TransactionKey.From(transaction), isTransfer,
                    _transfers.MentionsOwnName($"{transaction.Details} {transaction.OriginalDescription}"), source));

            // Tries to match a bank row against an unused existing entry. Exact date match
            // first; failing that, internal-transfer-looking rows may match an existing
            // Transfer row with the same amounts within a few days — MMEX stores one date
            // per transfer while the bank posts the two sides on different days (and manual
            // conversions may carry either side's date). Own-name wording alone is a weak signal
            // (the user's name also appears on ordinary payments), so such a row may only match a
            // transfer that is itself worded with the user's name — e.g. Macquarie's "From Timothy
            // Mollenha - Transfer Savings" against ING's "... Timothy Mollenhauer ..." transfer —
            // never an unrelated same-amount transfer. source identifies the row's file so
            // entries recorded from that same file are skipped.
            public bool TryConsume(BankTransaction transaction, object? source = null)
            {
                var key = TransactionKey.From(transaction);
                bool Available(Entry e) => !e.Used && (e.Source == null || e.Source != source);

                var exact = Entries.FirstOrDefault(e => Available(e) && e.Key == key);
                if (exact != null)
                {
                    exact.Used = true;
                    return true;
                }

                if (!_transfers.LooksInternal(transaction))
                {
                    return false;
                }
                var bankWorded = _transfers.HasBankWording(transaction);

                var tolerant = Entries
                    .Where(e => Available(e) && e.IsTransfer && (bankWorded || e.OwnNameWorded) &&
                                e.Key.Debit == key.Debit && e.Key.Credit == key.Credit)
                    .Select(e => (Entry: e, Distance: Math.Abs(e.Key.Date.DayNumber - key.Date.DayNumber)))
                    .Where(x => x.Distance <= BankCsv.TransferDateToleranceDays)
                    .OrderBy(x => x.Distance)
                    .Select(x => x.Entry)
                    .FirstOrDefault();
                if (tolerant != null)
                {
                    tolerant.Used = true;
                    return true;
                }

                return false;
            }
        }

        // Builds the ledger for an account. Includes:
        //  - Withdrawal/Deposit rows on the account
        //  - Transfer rows in either direction (an outgoing transfer covers a CSV debit,
        //    an incoming one covers a CSV credit) — otherwise every credit-card payment
        //    already recorded as a Transfer would re-import as a duplicate deposit
        //  - voided and soft-deleted rows (those were removed deliberately; re-importing
        //    them would undo the user's cleanup)
        public ExistingLedger GetExistingLedger(long accountId, TransferDetector transfers)
        {
            var ledger = new ExistingLedger(transfers);
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT TRANSDATE, TRANSCODE, TRANSAMOUNT, TOTRANSAMOUNT, ACCOUNTID, TOACCOUNTID, NOTES
                FROM CHECKINGACCOUNT_V1
                WHERE ACCOUNTID = @id OR TOACCOUNTID = @id";
            cmd.Parameters.AddWithValue("@id", accountId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var transDate = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (transDate == null || transDate.Length < 10 ||
                    !DateOnly.TryParseExact(transDate[..10], "yyyy-MM-dd", out var date))
                {
                    continue;
                }

                var transCode = reader.GetString(1);
                var amount = Math.Round(Convert.ToDecimal(reader.GetDouble(2)), 2);
                var toAmount = reader.IsDBNull(3) ? 0m : Math.Round(Convert.ToDecimal(reader.GetDouble(3)), 2);
                var rowAccountId = reader.GetInt64(4);

                TransactionKey? key = transCode switch
                {
                    "Withdrawal" when rowAccountId == accountId => new TransactionKey(date, amount, 0),
                    "Deposit" when rowAccountId == accountId => new TransactionKey(date, 0, amount),
                    "Transfer" when rowAccountId == accountId => new TransactionKey(date, amount, 0),
                    "Transfer" => new TransactionKey(date, 0, toAmount),
                    _ => null
                };

                if (key.HasValue)
                {
                    var isTransfer = transCode == "Transfer";
                    var notes = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                    ledger.Entries.Add(new ExistingLedger.Entry(key.Value, isTransfer, isTransfer && transfers.MentionsOwnName(notes)));
                }
            }

            return ledger;
        }

        // Filters the parsed bank transactions down to the ones not already in the database.
        public List<BankTransaction> FindNewTransactions(long accountId, IEnumerable<BankTransaction> parsed, TransferDetector transfers)
        {
            var ledger = GetExistingLedger(accountId, transfers);
            return parsed.Where(t => !ledger.TryConsume(t)).ToList();
        }

        // Inserts a whole batch — unreconciled (STATUS='') Withdrawal/Deposit singles plus
        // Transfer rows between accounts — creating payees by name as needed. Categories are never
        // created: each row carries the existing category id chosen in Analyze, re-checked here.
        // Everything runs in one SQLite transaction: either the whole batch lands or none of it.
        public MmexBatchSummary WriteBatch(
            IReadOnlyList<(long AccountId, BankTransaction Transaction)> singles,
            IReadOnlyList<(long SourceAccountId, long TargetAccountId, BankTransaction Debit, BankTransaction Credit)> transfers)
        {
            var summary = new MmexBatchSummary();
            var payeeCache = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            using var dbTransaction = _connection.BeginTransaction();

            // The category was validated in Analyze, but the user may have deleted, merged or
            // hidden it in MMEX since; with no foreign keys an orphaned CATEGID would go unnoticed,
            // so anything no longer an active category is written uncategorised.
            var activeCategories = GetCategoryPaths();
            long CategoryOf(BankTransaction transaction) =>
                transaction.ResolvedCategoryId is long id && activeCategories.ContainsKey(id) ? id : -1;

            foreach (var (accountId, transaction) in singles)
            {
                var payeeId = ResolvePayeeId(transaction.Details, payeeCache, summary);
                var categoryId = CategoryOf(transaction);
                var isDeposit = transaction.Credit > 0;

                InsertRow(
                    accountId: accountId,
                    toAccountId: -1,
                    payeeId: payeeId,
                    transCode: isDeposit ? "Deposit" : "Withdrawal",
                    amount: isDeposit ? transaction.Credit : transaction.Debit,
                    toAmount: 0,
                    notes: BuildNotes(transaction),
                    categoryId: categoryId,
                    date: transaction.Date,
                    now: now);

                summary.SinglesByAccount.TryGetValue(accountId, out var n);
                summary.SinglesByAccount[accountId] = n + 1;
            }

            foreach (var (sourceAccountId, targetAccountId, debit, credit) in transfers)
            {
                // A transfer is one row: source account, TOACCOUNTID = destination, no payee.
                // Date and category come from the debit (source) side, matching how existing
                // transfers in the database are recorded.
                var categoryId = CategoryOf(debit);
                var notes = BuildNotes(debit);
                if (credit.OriginalDescription.Length > 0 &&
                    !string.Equals(credit.OriginalDescription, debit.OriginalDescription, StringComparison.OrdinalIgnoreCase))
                {
                    notes = $"{notes} | {credit.OriginalDescription}";
                }

                InsertRow(
                    accountId: sourceAccountId,
                    toAccountId: targetAccountId,
                    payeeId: -1,
                    transCode: "Transfer",
                    amount: debit.Debit,
                    toAmount: credit.Credit,
                    notes: notes,
                    categoryId: categoryId,
                    date: debit.Date,
                    now: now);

                summary.TransfersInserted++;
            }

            dbTransaction.Commit();
            return summary;
        }

        private static string BuildNotes(BankTransaction transaction) =>
            string.IsNullOrEmpty(transaction.Notes)
                ? transaction.OriginalDescription
                : $"{transaction.Notes}\n{transaction.OriginalDescription}";

        private void InsertRow(long accountId, long toAccountId, long payeeId, string transCode,
            decimal amount, decimal toAmount, string notes, long categoryId, DateTime date, string now)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CHECKINGACCOUNT_V1
                    (TRANSID, ACCOUNTID, TOACCOUNTID, PAYEEID, TRANSCODE, TRANSAMOUNT, STATUS,
                     TRANSACTIONNUMBER, NOTES, CATEGID, TRANSDATE, LASTUPDATEDTIME, DELETEDTIME,
                     FOLLOWUPID, TOTRANSAMOUNT, COLOR)
                VALUES
                    (@transId, @accountId, @toAccountId, @payeeId, @transCode, @amount, '',
                     '', @notes, @categId, @transDate, @lastUpdated, '',
                     -1, @toAmount, -1)";
            cmd.Parameters.AddWithValue("@transId", NewId());
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@toAccountId", toAccountId);
            cmd.Parameters.AddWithValue("@payeeId", payeeId);
            cmd.Parameters.AddWithValue("@transCode", transCode);
            cmd.Parameters.AddWithValue("@amount", (double)amount);
            cmd.Parameters.AddWithValue("@toAmount", (double)toAmount);
            cmd.Parameters.AddWithValue("@notes", notes);
            cmd.Parameters.AddWithValue("@categId", categoryId);
            cmd.Parameters.AddWithValue("@transDate", date.ToString("yyyy-MM-ddT00:00:00"));
            cmd.Parameters.AddWithValue("@lastUpdated", now);
            cmd.ExecuteNonQuery();
        }

        private long ResolvePayeeId(string payeeName, Dictionary<string, long> cache, MmexBatchSummary summary)
        {
            if (payeeName.Length == 0)
            {
                payeeName = "Unknown";
            }

            if (cache.TryGetValue(payeeName, out var cached))
            {
                return cached;
            }

            var existing = ScalarNullableLong("SELECT PAYEEID FROM PAYEE_V1 WHERE PAYEENAME = @p COLLATE NOCASE", ("@p", payeeName));
            if (existing.HasValue)
            {
                cache[payeeName] = existing.Value;
                return existing.Value;
            }

            var id = NewId();
            Execute("INSERT INTO PAYEE_V1 (PAYEEID, PAYEENAME, ACTIVE) VALUES (@id, @p, 1)", ("@id", id), ("@p", payeeName));
            summary.PayeesCreated++;
            cache[payeeName] = id;
            return id;
        }

        // MMEX 1.9 generates IDs as milliseconds-since-epoch * 1000 + random(0..999)
        // (see DB_Table.h newId()). We replicate that, bumping forward on collision so a
        // fast batch never reuses an ID.
        private long NewId()
        {
            var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(0, 1000);
            if (id <= _lastGeneratedId)
            {
                id = _lastGeneratedId + 1;
            }
            _lastGeneratedId = id;
            return id;
        }

        private void Execute(string sql, params (string Name, object Value)[] parameters)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.ExecuteNonQuery();
        }

        private long ScalarLong(string sql)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private string? ScalarString(string sql, params (string Name, object Value)[] parameters)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            var result = cmd.ExecuteScalar();
            return result == null || result is DBNull ? null : Convert.ToString(result);
        }

        private long? ScalarNullableLong(string sql, params (string Name, object Value)[] parameters)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            var result = cmd.ExecuteScalar();
            return result == null || result is DBNull ? null : Convert.ToInt64(result);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
