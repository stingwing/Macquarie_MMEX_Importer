namespace MoneyManagerExMAQ.Tests
{
    public class PayeeResolverTests
    {
        private static readonly Dictionary<string, long> Payees = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Transfer"] = 1,
            ["Rent"] = 2,
            ["Spotify"] = 3,
            ["IGA"] = 4,
            ["ING"] = 5,
            ["Uncategorised Payee"] = -1
        };

        private static PayeeResolver Resolver(
            IEnumerable<CategoryRecords.Alias>? aliases = null,
            IEnumerable<(string, string)>? history = null) =>
            new(aliases ?? Array.Empty<CategoryRecords.Alias>(), history ?? Array.Empty<(string, string)>(), Payees);

        [Theory]
        [InlineData("Spotify P4796D867C - Visa Purchase - Receipt 165643In Sydney Card 462263xxxxxx8299", "spotify")]
        [InlineData("Direct Credit 458106 ACCENT GROUP LTD SEP26/00811257", "direct credit accent group ltd")]
        [InlineData("\"\nMTG MATE PTY LTD BANYO", "mtg mate pty ltd banyo")]
        [InlineData("12345 - Visa Purchase", "")]
        public void DescriptionKey_KeepsMerchantTextAndDropsNumbers(string description, string expected)
        {
            Assert.Equal(expected, PayeeResolver.DescriptionKey(description));
        }

        [Fact]
        public void Resolve_LearnedHistoryBeatsGenericPayeeNameSubstring()
        {
            // "Transfer" appears in the description, but history says this merchant is "Rent".
            var resolver = Resolver(history: new[] { ("Rent - Internal Transfer - Receipt 111", "Rent") });

            var resolution = resolver.Resolve("Rent - Internal Transfer - Receipt 999");

            Assert.Equal("Rent", resolution.PayeeName);
            Assert.Equal(2, resolution.CategoryId);
        }

        [Fact]
        public void Resolve_LatestHistoryEntryWinsForTheSameMerchant()
        {
            var resolver = Resolver(history: new[]
            {
                ("Beem - Osko Payment - Receipt 1", "Transfer"),
                ("Beem - Osko Payment - Receipt 2", "Rent")
            });

            Assert.Equal("Rent", resolver.Resolve("Beem - Osko Payment - Receipt 3").PayeeName);
        }

        [Fact]
        public void Resolve_AliasFileComesFirst()
        {
            var resolver = Resolver(
                aliases: new[] { new CategoryRecords.Alias("HIA ONE PTY LTD", "IGA") },
                history: new[] { ("HIA ONE PTY LTD - Visa Purchase", "Spotify") });

            Assert.Equal("IGA", resolver.Resolve("HIA ONE PTY LTD - Visa Purchase - Receipt 1").PayeeName);
        }

        [Fact]
        public void Resolve_FallsBackToPayeeNameSubstring_IgnoringShortNames()
        {
            var resolver = Resolver();

            Assert.Equal("Spotify", resolver.Resolve("PAYPAL *SPOTIFY 4029357733").PayeeName);
            // "ING" (3 chars) must not match inside "SHOPPING".
            Assert.Null(resolver.Resolve("SHOPPING CENTRE CAR PARK").PayeeName);
        }

        [Fact]
        public void Resolve_PayeeWithoutCategoryYieldsNoCategory()
        {
            var resolver = Resolver(history: new[] { ("Some Shop - Visa Purchase", "Uncategorised Payee") });

            var resolution = resolver.Resolve("Some Shop - Visa Purchase - Receipt 2");

            Assert.Equal("Uncategorised Payee", resolution.PayeeName);
            Assert.Null(resolution.CategoryId);
        }
    }

    public class TransferDetectorTests
    {
        private readonly TransferDetector _detector = new("timothy mollenha, Tim Mollenhauer");

        [Theory]
        [InlineData("To linked account xx1621 - Internal transfer", true)]
        [InlineData("Recurring payment to MBL CARD SERVICE - CRN 4984", true)]
        [InlineData("Savings - Receipt 200597 - From Orange Everyday", true)]
        [InlineData("Fast Transfer From Timothy Mollenhauer Shares Shares", true)]
        [InlineData("BUNNINGS LAWNTON AUS", false)]
        public void HasBankWording_RecognisesBuiltInTransferPhrases(string description, bool expected)
        {
            Assert.Equal(expected, _detector.HasBankWording(Csv.Row("2026-01-01", 1, 0, description)));
        }

        [Fact]
        public void OwnNameMarkers_AreWeakButStillLookInternal()
        {
            var row = Csv.Row("2026-01-01", 0, 1, "From Timothy Mollenha - Transfer Savings");

            Assert.False(_detector.HasBankWording(row));
            Assert.True(_detector.LooksInternal(row));
            Assert.True(_detector.MentionsOwnName("from TIMOTHY MOLLENHAUER - transfer to mac"));
        }

        [Fact]
        public void NoMarkersConfigured_OnlyBankWordingCounts()
        {
            var detector = new TransferDetector(null);
            var row = Csv.Row("2026-01-01", 0, 1, "From Timothy Mollenha - Transfer Savings");

            Assert.False(detector.LooksInternal(row));
            Assert.False(detector.MentionsOwnName("timothy mollenha"));
        }
    }

    public class CategoryRecordsTests
    {
        [Fact]
        public void Load_ReadsPatternPayeePairs_SkippingBlankAndMalformedLines()
        {
            using var file = new TempCsv("HIA ONE PTY LTD,IGA", "", "# a note without a comma", ",no pattern", "pattern only,", " Coles 123 , Coles ");

            var aliases = CategoryRecords.Load(file.Path);

            Assert.Equal(new[] { new CategoryRecords.Alias("HIA ONE PTY LTD", "IGA"), new CategoryRecords.Alias("Coles 123", "Coles") }, aliases);
        }

        [Fact]
        public void Load_MissingOrUnsetFileIsEmpty()
        {
            Assert.Empty(CategoryRecords.Load(null));
            Assert.Empty(CategoryRecords.Load(Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid() + ".csv")));
        }
    }
}
