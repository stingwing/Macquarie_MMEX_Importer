namespace MoneyManagerExMAQ.Tests
{
    public class ImportServiceTests : IDisposable
    {
        private readonly TestDb _db = new();

        public void Dispose() => _db.Dispose();

        private static AccountInfo Macquarie(string label, string mmex) => new() { Name = label, BankAccountLabel = label, MmexAccountName = mmex };
        private static AccountInfo Fingerprinted(string name, string mmex, string fingerprint) => new() { Name = name, MmexAccountName = mmex, IngFingerprint = fingerprint };

        [Fact]
        public void Analyze_Macquarie_LearnedPayeeCategoryBeatsTheBanksCategory()
        {
            var card = _db.AddAccount("MAC_Credit");
            var leisure = _db.AddCategory("Leisure");
            var magic = _db.AddCategory("Magic", leisure);
            _db.AddCategory("Games", leisure);
            var payee = _db.AddPayee("Mtg Mate Lutwyche");
            _db.AddTransaction(card, "Withdrawal", 119, "2026-06-27", payee, magic);
            var file = _db.WriteCsv("mac.csv", Csv.MacquarieHeader,
                Csv.Macquarie("09 Oct 2026", "Mtg Mate Lutwyche", "Macquarie Platinum Card", "Leisure", "Games", "12", "", "MTG MATE LUTWYCHE AUS"));
            var service = new ImportService(_db.Settings(Macquarie("Macquarie Platinum Card", "MAC_Credit")));

            var detected = service.Analyze(file);

            var row = Assert.Single(detected.NewTransactions);
            Assert.Equal(magic, row.ResolvedCategoryId);
            Assert.Equal("Leisure > Magic", row.Category);
            Assert.Empty(detected.Issues);
        }

        [Fact]
        public void Analyze_Macquarie_UnknownOrHiddenBankCategoryIsNeverRecreated()
        {
            _db.AddAccount("MAC_Credit");
            _db.AddCategory("Other");
            var personal = _db.AddCategory("Personal", active: false);
            _db.AddCategory("Hobbies", personal);
            var file = _db.WriteCsv("mac.csv", Csv.MacquarieHeader,
                Csv.Macquarie("09 Oct 2026", "Pool Shop", "Macquarie Platinum Card", "Other", "Chemicals", "30", "", "POOL SHOP AUS"),
                Csv.Macquarie("09 Oct 2026", "Hobby Warehouse", "Macquarie Platinum Card", "Personal", "Hobbies", "20", "", "HOBBY WAREHOUSE AUS"));
            var service = new ImportService(_db.Settings(Macquarie("Macquarie Platinum Card", "MAC_Credit")));

            var detected = service.Analyze(file);

            Assert.Equal("Other", detected.NewTransactions[0].Category);          // fell back to the parent
            Assert.Null(detected.NewTransactions[1].ResolvedCategoryId);          // whole tree hidden -> uncategorised
            Assert.Contains(detected.Issues, i => i.Contains("\"Other > Chemicals\" is not in MMEX") && i.Contains("filed under \"Other\""));
            Assert.Contains(detected.Issues, i => i.Contains("\"Personal > Hobbies\" is hidden (inactive) in MMEX") && i.Contains("uncategorised"));
        }

        [Fact]
        public void Analyze_IgnoresRowsBeforeTheAccountsOpeningDate()
        {
            _db.AddAccount("ING_Spending", initialDate: "2025-07-01");
            var file = _db.WriteCsv("ing.csv", Csv.IngHeader,
                "02/07/2025,Cafe - Visa Purchase - Card 462263xxxxxx6167,,-5.00,10.00",
                "30/06/2025,Old Cafe - Visa Purchase - Card 462263xxxxxx6167,,-4.00,15.00");
            var service = new ImportService(_db.Settings(Fingerprinted("ING Spending", "ING_Spending", "6167")));

            var detected = service.Analyze(file);

            Assert.Equal(2, detected.TotalRows);
            Assert.Equal(5m, Assert.Single(detected.NewTransactions).Debit);
            Assert.Contains(detected.Issues, i => i.Contains("1 row(s) dated before") && i.Contains("2025-07-01"));
        }

        [Fact]
        public void Analyze_Ing_FingerprintMatchesCardSuffixButNotLongerNumbers()
        {
            _db.AddAccount("ING_Bills");
            _db.AddAccount("ING_Spending");
            var file = _db.WriteCsv("ing.csv", Csv.IngHeader,
                "05/10/2026,Spotify - Visa Purchase - Receipt 198299In Sydney Card 462263xxxxxx6167,,-20.99,15.64",
                "04/10/2026,Cafe - Visa Purchase - Receipt 100001In Sydney Card 462263xxxxxx6167,,-4.00,36.63");
            var service = new ImportService(_db.Settings(
                Fingerprinted("ING Bills", "ING_Bills", "8299"),       // only appears inside receipt "198299"
                Fingerprinted("ING Spending", "ING_Spending", "6167")));

            Assert.Equal("ING_Spending", service.Analyze(file).MmexAccountName);
        }

        [Fact]
        public void Analyze_Ing_TiedFingerprintsAreReportedNotGuessed()
        {
            _db.AddAccount("A");
            _db.AddAccount("B");
            var file = _db.WriteCsv("ing.csv", Csv.IngHeader, "05/10/2026,Shop - Visa Purchase - Card xxxx1111 xxxx2222,,-1.00,1.00");
            var service = new ImportService(_db.Settings(Fingerprinted("A", "A", "1111"), Fingerprinted("B", "B", "2222")));

            var detected = service.Analyze(file);

            Assert.False(detected.IsMapped);
            Assert.Contains(detected.Issues, i => i.Contains("more than one account"));
        }

        [Fact]
        public void Analyze_Ing_ResolvesPayeeAndCategoryFromHistory_AndRemembersManualOverride()
        {
            var spending = _db.AddAccount("ING_Spending");
            var subs = _db.AddCategory("Subscriptions");
            var spotify = _db.AddPayee("Spotify");
            _db.AddPayee("Transfer");
            _db.AddTransaction(spending, "Withdrawal", 20.99m, "2026-09-05", spotify, subs,
                notes: "Spotify P4796D867C - Visa Purchase - Receipt 1In Sydney Card 462263xxxxxx6167");
            var file = _db.WriteCsv("ing.csv", Csv.IngHeader,
                "05/10/2026,Spotify P4796D867C - Visa Purchase - Receipt 165643In Sydney Card 462263xxxxxx6167,,-20.99,15.64");
            var service = new ImportService(_db.Settings());   // no fingerprints: the user picks the account

            var detected = service.Analyze(file, "ING_Spending");

            Assert.Equal("ING_Spending", detected.AccountOverride);
            var row = Assert.Single(detected.NewTransactions);
            Assert.Equal("Spotify", row.Details);
            Assert.Equal(subs, row.ResolvedCategoryId);
        }

        [Fact]
        public void ScanDownloads_ArchivesBankFiles_KeepsNewestMacquariePerAccount_LeavesOthersAlone()
        {
            var downloads = Path.Combine(_db.Folder, "Downloads");
            Directory.CreateDirectory(downloads);
            string Write(string name, DateTime modified, params string[] lines)
            {
                var path = Path.Combine(downloads, name);
                File.WriteAllText(path, string.Join("\n", lines) + "\n");
                File.SetLastWriteTime(path, modified);
                return path;
            }
            var macRow = Csv.Macquarie("01 Oct 2026", "Shop", "Macquarie Platinum Card", "", "", "1", "", "SHOP");
            Write("mac-old.csv", new DateTime(2026, 10, 1), Csv.MacquarieHeader, macRow);
            Write("mac-new.csv", new DateTime(2026, 10, 7), Csv.MacquarieHeader, macRow);
            Write("ing-1.csv", new DateTime(2026, 10, 1), Csv.IngHeader, "01/10/2026,Shop,,-1.00,1.00");
            Write("ing-2.csv", new DateTime(2026, 10, 2), Csv.IngHeader, "02/10/2026,Shop,,-1.00,1.00");
            var unrelated = Write("budget.csv", new DateTime(2026, 10, 3), "01/02/2026,\"12.50\",\"Budget export row\"");
            var service = new ImportService(_db.Settings());

            var toImport = service.ScanDownloads(out var archived, downloads);

            Assert.Equal(new[] { "ing-1.csv", "ing-2.csv", "mac-new.csv" }, toImport.Select(Path.GetFileName).OrderBy(n => n));
            Assert.Equal("mac-old.csv", Path.GetFileName(Assert.Single(archived)));
            Assert.True(File.Exists(unrelated));
            Assert.All(toImport.Concat(archived), p => Assert.StartsWith(_db.Settings().ImportFolder!, p));
        }

        [Fact]
        public void FindTransferPairs_PairsInternalTransfersAcrossAccounts_NotLookalikePurchases()
        {
            var offset = new DetectedFile { MmexAccountName = "MAC_Offset" };
            var card = new DetectedFile { MmexAccountName = "MAC_Credit" };
            offset.NewTransactions.Add(Csv.Row("2026-10-05", 450, 0, "Recurring payment to MBL CARD SERVICE"));
            offset.NewTransactions.Add(Csv.Row("2026-10-05", 30, 0, "BUNNINGS"));
            card.NewTransactions.Add(Csv.Row("2026-10-06", 0, 450, "BPAY PAYMENT - THANK YOU"));
            card.NewTransactions.Add(Csv.Row("2026-10-05", 0, 30, "BUNNINGS REFUND"));
            var service = new ImportService(_db.Settings());

            var pair = Assert.Single(service.FindTransferPairs(new[] { offset, card }));

            Assert.Same(offset, pair.DebitFile);
            Assert.Equal(450m, pair.Credit.Credit);
        }

        [Fact]
        public void WriteBatch_OverlappingFilesForTheSameAccount_InsertTheOverlapOnce()
        {
            if (ImportService.IsMmexRunning())
            {
                return;   // WriteBatch refuses while MoneyManagerEx is open; nothing to verify then.
            }
            _db.AddAccount("ING_Spending");
            var older = _db.WriteCsv("ing-1.csv", Csv.IngHeader,
                "02/10/2026,Cafe - Visa Purchase - Card 6167,,-5.00,10.00",
                "01/10/2026,Cafe - Visa Purchase - Card 6167,,-5.00,15.00");
            var newer = _db.WriteCsv("ing-2.csv", Csv.IngHeader,
                "03/10/2026,Bakery - Visa Purchase - Card 6167,,-7.00,3.00",
                "02/10/2026,Cafe - Visa Purchase - Card 6167,,-5.00,10.00");
            var service = new ImportService(_db.Settings(Fingerprinted("ING Spending", "ING_Spending", "6167")));
            var files = new[] { service.Analyze(older), service.Analyze(newer) };

            var (summary, backup) = service.WriteBatch(files, service.FindTransferPairs(files));

            Assert.Equal(3, summary.TotalInserted);   // 1 Oct, 2 Oct (once), 3 Oct
            Assert.Equal(3, _db.Count("SELECT COUNT(*) FROM CHECKINGACCOUNT_V1"));
            Assert.True(File.Exists(backup));
            Assert.All(files, f => Assert.True(f.Written));
        }
    }
}
