namespace MoneyManagerExMAQ.Tests
{
    public class MmexDatabaseTests : IDisposable
    {
        private readonly TestDb _db = new();
        private readonly TransferDetector _transfers = new("timothy mollenha");

        public void Dispose() => _db.Dispose();

        private MmexDatabase Open(bool readOnly = true) => MmexDatabase.Open(_db.Path, readOnly);

        [Fact]
        public void Open_RefusesNonMmexOrWrongVersionDatabases()
        {
            _db.Exec("UPDATE INFOTABLE_V1 SET INFOVALUE = '1.8.0' WHERE INFONAME = 'MMEXVERSION'");

            var ex = Assert.Throws<InvalidOperationException>(() => Open());
            Assert.Contains("1.8.0", ex.Message);
        }

        [Fact]
        public void GetPayeeCategories_LearnsFromLatestDatedTransaction_NotLastEdited()
        {
            var account = _db.AddAccount("Card");
            var leisure = _db.AddCategory("Leisure");
            var games = _db.AddCategory("Games", leisure);
            var magic = _db.AddCategory("Magic", leisure);
            var payee = _db.AddPayee("MTG Mate", defaultCategoryId: games);
            // The newest transaction is filed under Magic; an older one was re-stamped by a bulk edit.
            _db.AddTransaction(account, "Withdrawal", 15, "2026-09-01", payee, magic, lastUpdated: "2026-09-02T00:00:00");
            _db.AddTransaction(account, "Withdrawal", 12, "2025-03-01", payee, games, lastUpdated: "2026-10-08T00:00:00");

            using var db = Open();

            Assert.Equal(magic, db.GetPayeeCategories()["mtg mate"]);
        }

        [Fact]
        public void GetPayeeCategories_FallsBackToDefault_AndSkipsInactiveCategoriesAndPayees()
        {
            var account = _db.AddAccount("Card");
            var food = _db.AddCategory("Food");
            var hidden = _db.AddCategory("Old Stuff", active: false);
            var cafe = _db.AddPayee("Cafe", defaultCategoryId: food);
            _db.AddTransaction(account, "Withdrawal", 5, "2026-09-01", cafe, hidden);   // only history is a hidden category
            _db.AddPayee("Gone", food, active: false);

            using var db = Open();
            var map = db.GetPayeeCategories();

            Assert.Equal(food, map["Cafe"]);
            Assert.False(map.ContainsKey("Gone"));
        }

        [Fact]
        public void GetCategoryPaths_HidesWholeSubtreeOfAnInactiveParent()
        {
            var motorcycle = _db.AddCategory("Motorcycle", active: false);
            var insurance = _db.AddCategory("Insurance", motorcycle);
            var home = _db.AddCategory("Home");
            var furnishings = _db.AddCategory("Furnishings", home);

            using var db = Open();
            var usable = db.GetCategoryPaths();
            var all = db.GetCategoryPaths(includeHidden: true);

            Assert.Equal("Home > Furnishings", usable[furnishings]);
            Assert.False(usable.ContainsKey(insurance));
            Assert.False(usable.ContainsKey(motorcycle));
            Assert.Equal("Motorcycle > Insurance", all[insurance]);
        }

        [Fact]
        public void FindNewTransactions_MatchesByDateAndAmount_CountingSameDayRepeats()
        {
            var account = _db.AddAccount("Card");
            _db.AddTransaction(account, "Withdrawal", 3, "2026-04-25");
            _db.AddTransaction(account, "Withdrawal", 3, "2026-04-25");
            var rows = new[]
            {
                Csv.Row("2026-04-25", 3, 0, "MTG MATE PTY LTD BANYO"),
                Csv.Row("2026-04-25", 3, 0, "MTG MATE PTY LTD BANYO"),
                Csv.Row("2026-04-25", 3, 0, "MTG MATE PTY LTD BANYO"),   // a third $3 that day is genuinely new
                Csv.Row("2026-04-26", 3, 0, "MTG MATE PTY LTD BANYO")
            };

            using var db = Open();
            var fresh = db.FindNewTransactions(account, rows, _transfers);

            Assert.Equal(new[] { rows[2], rows[3] }, fresh);
        }

        [Fact]
        public void FindNewTransactions_BankWordedTransferMatchesAnExistingTransferDaysApart()
        {
            var savings = _db.AddAccount("Savings");
            var everyday = _db.AddAccount("Everyday");
            _db.AddTransaction(savings, "Transfer", 1000, "2026-09-30", notes: "unrelated wording", toAccountId: everyday);

            using var db = Open();
            var fresh = db.FindNewTransactions(everyday, new[]
            {
                Csv.Row("2026-10-02", 0, 1000, "From linked account xx8193 - Internal transfer")
            }, _transfers);

            Assert.Empty(fresh);
        }

        [Fact]
        public void FindNewTransactions_OwnNameOnlyRow_MatchesOnlyATransferWordedWithTheName()
        {
            var ing = _db.AddAccount("ING");
            var mac = _db.AddAccount("MAC");
            var other = _db.AddAccount("Other");
            // A same-amount transfer with no own-name wording, then one that carries the name.
            _db.AddTransaction(other, "Transfer", 50, "2026-10-01", notes: "linked account xx1", toAccountId: mac);
            _db.AddTransaction(ing, "Transfer", 202922.50m, "2025-11-01", notes: "from timothy mollenhauer - transfer to mac", toAccountId: mac);

            using var db = Open();
            var fresh = db.FindNewTransactions(mac, new[]
            {
                Csv.Row("2026-10-02", 0, 50, "Tim friend payment - Timothy Mollenha"),          // must NOT swallow the unrelated $50
                Csv.Row("2025-11-03", 0, 202922.50m, "From Timothy Mollenha - Transfer Savings") // matches the named transfer
            }, _transfers);

            var row = Assert.Single(fresh);
            Assert.Equal(50m, row.Credit);
        }

        [Fact]
        public void GetDescriptionPayees_UsesTheLastLineOfNotes()
        {
            var account = _db.AddAccount("Card");
            var spotify = _db.AddPayee("Spotify");
            _db.AddTransaction(account, "Withdrawal", 20.99m, "2026-09-01", spotify, notes: "\"\nSpotify P4796D867C - Visa Purchase");

            using var db = Open();

            Assert.Equal(("Spotify P4796D867C - Visa Purchase", "Spotify"), Assert.Single(db.GetDescriptionPayees()));
        }

        [Fact]
        public void WriteBatch_NeverCreatesCategories_AndWritesUnusableIdsAsUncategorised()
        {
            var account = _db.AddAccount("Card");
            var hidden = _db.AddCategory("Hidden", active: false);
            var food = _db.AddCategory("Food");
            var categoriesBefore = _db.Count("SELECT COUNT(*) FROM CATEGORY_V1");
            var rows = new[]
            {
                new BankTransaction { Date = new DateTime(2026, 10, 1), Details = "New Shop", Debit = 10, Category = "Brand New", Subcategory = "Thing" },
                new BankTransaction { Date = new DateTime(2026, 10, 2), Details = "Old Shop", Debit = 11, ResolvedCategoryId = hidden },
                new BankTransaction { Date = new DateTime(2026, 10, 3), Details = "Cafe", Debit = 12, ResolvedCategoryId = food }
            };

            using (var db = Open(readOnly: false))
            {
                var summary = db.WriteBatch(rows.Select(r => (account, r)).ToList(), Array.Empty<(long, long, BankTransaction, BankTransaction)>());
                Assert.Equal(3, summary.TotalInserted);
                Assert.Equal(3, summary.PayeesCreated);
            }

            Assert.Equal(categoriesBefore, _db.Count("SELECT COUNT(*) FROM CATEGORY_V1"));
            Assert.Equal(-1L, _db.Scalar("SELECT CATEGID FROM CHECKINGACCOUNT_V1 WHERE TRANSAMOUNT = 10"));
            Assert.Equal(-1L, _db.Scalar("SELECT CATEGID FROM CHECKINGACCOUNT_V1 WHERE TRANSAMOUNT = 11"));
            Assert.Equal(food, _db.Scalar("SELECT CATEGID FROM CHECKINGACCOUNT_V1 WHERE TRANSAMOUNT = 12"));
        }

        [Fact]
        public void WriteBatch_TransferIsOneRowFromDebitToCreditAccount()
        {
            var from = _db.AddAccount("Offset");
            var to = _db.AddAccount("Card");
            var debit = Csv.Row("2026-10-05", 450, 0, "Recurring payment to MBL CARD SERVICE");
            var credit = Csv.Row("2026-10-06", 0, 450, "BPAY PAYMENT - THANK YOU");

            using (var db = Open(readOnly: false))
            {
                db.WriteBatch(Array.Empty<(long, BankTransaction)>(), new[] { (from, to, debit, credit) });
            }

            Assert.Equal(1, _db.Count("SELECT COUNT(*) FROM CHECKINGACCOUNT_V1"));
            Assert.Equal(1, _db.Count("SELECT COUNT(*) FROM CHECKINGACCOUNT_V1 WHERE TRANSCODE = 'Transfer' AND ACCOUNTID = @f AND TOACCOUNTID = @t AND TRANSDATE LIKE '2026-10-05%'",
                ("@f", from), ("@t", to)));
        }
    }
}
