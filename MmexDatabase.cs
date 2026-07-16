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
        public int CategoriesCreated { get; set; }
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

        // Everything already recorded against one account, consumable entry-by-entry so
        // same-day same-amount duplicates are handled by count rather than collapsed.
        public class ExistingLedger
        {
            internal record Entry(TransactionKey Key, bool IsTransfer)
            {
                public bool Used { get; set; }
            }

            internal List<Entry> Entries { get; } = new();

            // Tries to match a bank row against an unused existing entry. Exact date match
            // first; failing that, internal-transfer-looking rows may match an existing
            // Transfer row with the same amounts within a few days — MMEX stores one date
            // per transfer while the bank posts the two sides on different days (and manual
            // conversions may carry either side's date).
            public bool TryConsume(BankTransaction transaction)
            {
                var key = TransactionKey.From(transaction);

                var exact = Entries.FirstOrDefault(e => !e.Used && e.Key == key);
                if (exact != null)
                {
                    exact.Used = true;
                    return true;
                }

                if (!BankCsv.LooksLikeInternalTransfer(transaction))
                {
                    return false;
                }

                var tolerant = Entries
                    .Where(e => !e.Used && e.IsTransfer &&
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
        public ExistingLedger GetExistingLedger(long accountId)
        {
            var ledger = new ExistingLedger();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT TRANSDATE, TRANSCODE, TRANSAMOUNT, TOTRANSAMOUNT, ACCOUNTID, TOACCOUNTID
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
                    ledger.Entries.Add(new ExistingLedger.Entry(key.Value, transCode == "Transfer"));
                }
            }

            return ledger;
        }

        // Filters the parsed bank transactions down to the ones not already in the database.
        public List<BankTransaction> FindNewTransactions(long accountId, IEnumerable<BankTransaction> parsed)
        {
            var ledger = GetExistingLedger(accountId);
            return parsed.Where(t => !ledger.TryConsume(t)).ToList();
        }

        // Inserts a whole batch — unreconciled (STATUS='') Withdrawal/Deposit singles plus
        // Transfer rows between accounts — creating payees and categories by name as needed.
        // Everything runs in one SQLite transaction: either the whole batch lands or none of it.
        public MmexBatchSummary WriteBatch(
            IReadOnlyList<(long AccountId, BankTransaction Transaction)> singles,
            IReadOnlyList<(long SourceAccountId, long TargetAccountId, BankTransaction Debit, BankTransaction Credit)> transfers)
        {
            var summary = new MmexBatchSummary();
            var payeeCache = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var categoryCache = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            using var dbTransaction = _connection.BeginTransaction();

            foreach (var (accountId, transaction) in singles)
            {
                var payeeId = ResolvePayeeId(transaction.Details, payeeCache, summary);
                var categoryId = ResolveCategoryId(transaction.Category, transaction.Subcategory, categoryCache, summary);
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
                var categoryId = ResolveCategoryId(debit.Category, debit.Subcategory, categoryCache, summary);
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

        private long ResolveCategoryId(string category, string subcategory, Dictionary<string, long> cache, MmexBatchSummary summary)
        {
            if (category.Length == 0)
            {
                return -1;
            }

            var cacheKey = $"{category}{subcategory}";
            if (cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var parentId = ScalarNullableLong(
                "SELECT CATEGID FROM CATEGORY_V1 WHERE CATEGNAME = @n COLLATE NOCASE AND (PARENTID = -1 OR PARENTID IS NULL)",
                ("@n", category));
            if (!parentId.HasValue)
            {
                parentId = NewId();
                Execute("INSERT INTO CATEGORY_V1 (CATEGID, CATEGNAME, ACTIVE, PARENTID) VALUES (@id, @n, 1, -1)",
                    ("@id", parentId.Value), ("@n", category));
                summary.CategoriesCreated++;
            }

            long result = parentId.Value;
            if (subcategory.Length > 0)
            {
                var childId = ScalarNullableLong(
                    "SELECT CATEGID FROM CATEGORY_V1 WHERE CATEGNAME = @n COLLATE NOCASE AND PARENTID = @parent",
                    ("@n", subcategory), ("@parent", parentId.Value));
                if (!childId.HasValue)
                {
                    childId = NewId();
                    Execute("INSERT INTO CATEGORY_V1 (CATEGID, CATEGNAME, ACTIVE, PARENTID) VALUES (@id, @n, 1, @parent)",
                        ("@id", childId.Value), ("@n", subcategory), ("@parent", parentId.Value));
                    summary.CategoriesCreated++;
                }
                result = childId.Value;
            }

            cache[cacheKey] = result;
            return result;
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

        private string? ScalarString(string sql)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
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
