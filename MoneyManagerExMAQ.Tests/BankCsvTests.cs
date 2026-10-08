namespace MoneyManagerExMAQ.Tests
{
    public class BankCsvTests
    {
        [Theory]
        [InlineData(Csv.MacquarieHeader, BankFileFormat.Macquarie)]
        [InlineData(Csv.IngHeader, BankFileFormat.Ing)]
        [InlineData("16/09/2026,\"-21.05\",\"Transfer To Timothy Ian Mollenhauer CommBank App Dividends\",\"0.00\"", BankFileFormat.CommBank)]
        [InlineData("14/09/2026,\"+21.05\",\"Direct Credit 458106 ACCENT GROUP LTD SEP26/00811257\",\"+21.05\"", BankFileFormat.CommBank)]
        [InlineData("01/02/2026,\"12.50\",\"Budget export row\"", BankFileFormat.Unknown)]                 // 3 columns
        [InlineData("01/02/2026,\"12.50\",\"Unsigned amount\",\"0.00\"", BankFileFormat.Unknown)]          // amount not signed
        [InlineData("01/02/2026,\"+12.50\",\"Extra column\",\"0.00\",\"x\"", BankFileFormat.Unknown)]      // 5 columns
        [InlineData("Some,Other,Header", BankFileFormat.Unknown)]
        [InlineData(null, BankFileFormat.Unknown)]
        public void DetectFormat_RecognisesEachBankAndRejectsLookalikes(string? firstLine, BankFileFormat expected)
        {
            Assert.Equal(expected, BankCsv.DetectFormat(firstLine));
        }

        [Fact]
        public void ParseIng_StoresDebitsAsPositiveMagnitudesAndCleansDetails()
        {
            using var file = new TempCsv(
                Csv.IngHeader,
                "05/10/2026,Spotify P4796D867C - Visa Purchase - Receipt 165643In Sydney Date 03 Oct 2026 Card 462263xxxxxx8299,,-20.99,15.64",
                "17/09/2026,Internal Transfer - Internal Transfer - Receipt 79857 Orange Everyday 0030578101,10.00,,65.99");

            var result = BankCsv.Parse(file.Path);

            Assert.Equal(BankFileFormat.Ing, result.Format);
            Assert.Empty(result.Issues);
            Assert.Equal(string.Empty, result.AccountLabel);
            var spotify = result.Transactions[0];
            Assert.Equal(new DateTime(2026, 10, 5), spotify.Date);
            Assert.Equal(20.99m, spotify.Debit);
            Assert.Equal(0m, spotify.Credit);
            Assert.Equal("Spotify P4796D867C", spotify.Details);
            Assert.StartsWith("Spotify P4796D867C - Visa Purchase", spotify.OriginalDescription);
            Assert.Equal(10.00m, result.Transactions[1].Credit);
        }

        [Fact]
        public void ParseIng_SkipsBlankAndZeroAmountRowsSilently_ButReportsGarbage()
        {
            using var file = new TempCsv(
                Csv.IngHeader,
                "21/07/2025,\"From my account 0820602887 - Internal Transfer - Receipt 27697 \",,,588.51",
                "22/07/2025,Zero row,0.00,0.00,588.51",
                "23/07/2025,Bad amount,abc,,588.51",
                "24/07/2025,Real row,,-5.00,583.51");

            var result = BankCsv.Parse(file.Path);

            Assert.Single(result.Transactions);
            Assert.Equal(5m, result.Transactions[0].Debit);
            var issue = Assert.Single(result.Issues);
            Assert.Contains("Line 4", issue);
        }

        [Fact]
        public void ParseCommBank_SplitsSignedAmountsAndSkipsZeroRows()
        {
            using var file = new TempCsv(
                "16/09/2026,\"-21.05\",\"Transfer To Timothy Ian Mollenhauer CommBank App Dividends\",\"0.00\"",
                "14/09/2026,\"+21.05\",\"Direct Credit 458106 ACCENT GROUP LTD SEP26/00811257\",\"+21.05\"",
                "13/09/2026,\"+0.00\",\"Credit Interest\",\"+0.00\"");

            var result = BankCsv.Parse(file.Path);

            Assert.Equal(BankFileFormat.CommBank, result.Format);
            Assert.Empty(result.Issues);
            Assert.Equal(2, result.Transactions.Count);
            Assert.Equal(21.05m, result.Transactions[0].Debit);
            Assert.Equal(0m, result.Transactions[0].Credit);
            Assert.Equal(21.05m, result.Transactions[1].Credit);
            Assert.Equal("ACCENT GROUP LTD", result.Transactions[1].Details);
        }

        [Fact]
        public void ParseMacquarie_ReadsAccountLabelAndBankCategory()
        {
            using var file = new TempCsv(
                Csv.MacquarieHeader,
                Csv.Macquarie("07 Oct 2026", "Bunnings", "Macquarie Platinum Card", "Home", "Other Home Expenses", "40.96", "", "BUNNINGS LAWNTON 8212 LAWNTON AUS"));

            var result = BankCsv.Parse(file.Path);

            Assert.Equal(BankFileFormat.Macquarie, result.Format);
            Assert.Equal("Macquarie Platinum Card", result.AccountLabel);
            var row = Assert.Single(result.Transactions);
            Assert.Equal(40.96m, row.Debit);
            Assert.Equal("Home", row.Category);
            Assert.Equal("Other Home Expenses", row.Subcategory);
            Assert.Equal("BUNNINGS LAWNTON 8212 LAWNTON AUS", row.OriginalDescription);
        }

        [Fact]
        public void Parse_UnrecognisedFile_ReportsAnIssueAndNoRows()
        {
            using var file = new TempCsv("Name,Amount", "Fred,12");

            var result = BankCsv.Parse(file.Path);

            Assert.Equal(BankFileFormat.Unknown, result.Format);
            Assert.Empty(result.Transactions);
            Assert.Single(result.Issues);
        }

        [Theory]
        [InlineData("SUSHI N DON - Visa Purchase - Receipt 19", "SUSHI N DON")]
        [InlineData("No   separator   here", "No separator here")]
        [InlineData("", "")]
        public void CleanIngDescription_TakesTextBeforeFirstSeparator(string description, string expected)
        {
            Assert.Equal(expected, BankCsv.CleanIngDescription(description));
        }

        [Theory]
        [InlineData("Direct Credit 458106 ACCENT GROUP LTD SEP26/00811257", "ACCENT GROUP LTD")]
        [InlineData("Direct Credit 386258 BOQ SPC DIV 001360297693", "BOQ SPC DIV")]
        [InlineData("Direct Debit 062934 COMMSEC SECURITI COMMSEC", "COMMSEC SECURITI COMMSEC")]
        [InlineData("Credit Interest", "Credit Interest")]
        public void CleanCommBankDescription_DropsBankIdAndReferenceTokens(string description, string expected)
        {
            Assert.Equal(expected, BankCsv.CleanCommBankDescription(description));
        }
    }

    // A CSV file in the temp folder, deleted when disposed.
    public sealed class TempCsv : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mmexmaq-{Guid.NewGuid():N}.csv");

        public TempCsv(params string[] lines) => File.WriteAllText(Path, string.Join("\n", lines) + "\n");

        public void Dispose() => File.Delete(Path);
    }
}
