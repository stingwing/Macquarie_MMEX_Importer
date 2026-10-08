using Microsoft.Data.Sqlite;

namespace MoneyManagerExMAQ.Tests
{
    // A throwaway MMEX 1.9 database in a temp folder: just the tables and columns the importer
    // touches. Each test gets its own, so tests never see each other's data or the real .mmb.
    public sealed class TestDb : IDisposable
    {
        public string Folder { get; }
        public string Path { get; }
        private long _nextId = 1000;

        public TestDb()
        {
            Folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MmexMaqTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            Path = System.IO.Path.Combine(Folder, "test.mmb");
            Exec(@"
                CREATE TABLE INFOTABLE_V1 (INFOID INTEGER PRIMARY KEY, INFONAME TEXT, INFOVALUE TEXT);
                INSERT INTO INFOTABLE_V1 (INFONAME, INFOVALUE) VALUES ('MMEXVERSION', '1.9.0');
                CREATE TABLE ACCOUNTLIST_V1 (ACCOUNTID INTEGER PRIMARY KEY, ACCOUNTNAME TEXT, ACCOUNTTYPE TEXT,
                    STATUS TEXT, INITIALBAL NUMERIC, INITIALDATE TEXT);
                CREATE TABLE PAYEE_V1 (PAYEEID INTEGER PRIMARY KEY, PAYEENAME TEXT, CATEGID INTEGER, ACTIVE INTEGER);
                CREATE TABLE CATEGORY_V1 (CATEGID INTEGER PRIMARY KEY, CATEGNAME TEXT, ACTIVE INTEGER, PARENTID INTEGER);
                CREATE TABLE CHECKINGACCOUNT_V1 (TRANSID INTEGER PRIMARY KEY, ACCOUNTID INTEGER, TOACCOUNTID INTEGER,
                    PAYEEID INTEGER, TRANSCODE TEXT, TRANSAMOUNT NUMERIC, STATUS TEXT, TRANSACTIONNUMBER TEXT,
                    NOTES TEXT, CATEGID INTEGER, TRANSDATE TEXT, LASTUPDATEDTIME TEXT, DELETEDTIME TEXT,
                    FOLLOWUPID INTEGER, TOTRANSAMOUNT NUMERIC, COLOR INTEGER);");
        }

        public long AddAccount(string name, string initialDate = "2000-01-01")
        {
            var id = _nextId++;
            Exec("INSERT INTO ACCOUNTLIST_V1 VALUES (@id, @n, 'Checking', 'Open', 0, @d)", ("@id", id), ("@n", name), ("@d", initialDate));
            return id;
        }

        public long AddCategory(string name, long parentId = -1, bool active = true)
        {
            var id = _nextId++;
            Exec("INSERT INTO CATEGORY_V1 VALUES (@id, @n, @a, @p)", ("@id", id), ("@n", name), ("@a", active ? 1 : 0), ("@p", parentId));
            return id;
        }

        public long AddPayee(string name, long defaultCategoryId = -1, bool active = true)
        {
            var id = _nextId++;
            Exec("INSERT INTO PAYEE_V1 VALUES (@id, @n, @c, @a)", ("@id", id), ("@n", name), ("@c", defaultCategoryId), ("@a", active ? 1 : 0));
            return id;
        }

        // date as yyyy-MM-dd. For a Transfer, accountId is the source and toAccountId the destination.
        public long AddTransaction(long accountId, string transCode, decimal amount, string date,
            long payeeId = -1, long categoryId = -1, string notes = "", long toAccountId = -1,
            string lastUpdated = "2026-01-01T00:00:00")
        {
            var id = _nextId++;
            Exec(@"INSERT INTO CHECKINGACCOUNT_V1 VALUES (@id, @acc, @to, @payee, @code, @amt, '', '', @notes, @cat,
                       @date, @upd, '', -1, @toamt, -1)",
                ("@id", id), ("@acc", accountId), ("@to", toAccountId), ("@payee", payeeId), ("@code", transCode),
                ("@amt", (double)amount), ("@notes", notes), ("@cat", categoryId), ("@date", date + "T00:00:00"),
                ("@upd", lastUpdated), ("@toamt", transCode == "Transfer" ? (double)amount : 0.0));
            return id;
        }

        public void Exec(string sql, params (string Name, object Value)[] parameters)
        {
            using var connection = Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.ExecuteNonQuery();
        }

        public object? Scalar(string sql, params (string Name, object Value)[] parameters)
        {
            using var connection = Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            return cmd.ExecuteScalar();
        }

        public long Count(string sql, params (string Name, object Value)[] parameters) =>
            Convert.ToInt64(Scalar(sql, parameters));

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, Pooling = false }.ConnectionString);
            connection.Open();
            return connection;
        }

        // Writes a CSV into this database's temp folder and returns its path.
        public string WriteCsv(string fileName, params string[] lines)
        {
            var path = System.IO.Path.Combine(Folder, fileName);
            File.WriteAllText(path, string.Join("\n", lines) + "\n");
            return path;
        }

        public AppSettings Settings(params AccountInfo[] accounts)
        {
            var settings = new AppSettings
            {
                MmbFilePath = Path,
                BackupFolder = System.IO.Path.Combine(Folder, "Backups"),
                ImportFolder = System.IO.Path.Combine(Folder, "Import")
            };
            settings.Accounts.AddRange(accounts);
            return settings;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Folder, recursive: true);
            }
            catch (IOException)
            {
                // A lingering handle shouldn't fail the test; the OS temp cleaner will get it.
            }
        }
    }

    public static class Csv
    {
        public const string MacquarieHeader =
            "Transaction Date,Details,Account,Category,Subcategory,Tags,Notes,Debit,Credit,Balance,Original Description";
        public const string IngHeader = "Date,Description,Credit,Debit,Balance";

        public static string Macquarie(string date, string details, string account, string category, string subcategory,
            string debit, string credit, string original) =>
            $"\"{date}\",\"{details}\",\"{account}\",\"{category}\",\"{subcategory}\",\"\",\"\",\"{debit}\",\"{credit}\",\"\",\"{original}\"";

        public static BankTransaction Row(string date, decimal debit, decimal credit, string description, string details = "") =>
            new()
            {
                Date = DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture),
                Debit = debit,
                Credit = credit,
                OriginalDescription = description,
                Details = details
            };
    }
}
