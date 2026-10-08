using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MoneyManagerExMAQ
{
    public class BankTransaction
    {
        public DateTime Date { get; set; }
        public string Details { get; set; } = string.Empty;
        public string AccountLabel { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string OriginalDescription { get; set; } = string.Empty;

        // When set, the category is taken directly from this MMEX category id — inherited from the
        // payee's category already in MMEX (see MmexDatabase.GetPayeeCategories) — instead of by
        // Category/Subcategory name. Null means no category was inherited (unknown payee, or one
        // with no valid category), so the write falls back to the name columns (the bank's
        // category for Macquarie; uncategorised for ING).
        public long? ResolvedCategoryId { get; set; }
    }

    public enum BankFileFormat
    {
        Unknown = 0,
        // Macquarie's enriched export (11 columns, with Account/Category/Original Description).
        Macquarie,
        // ING's raw export: Date,Description,Credit,Debit,Balance (no account or category).
        Ing,
        // CommBank's NetBank export: header-less Date,"±Amount","Description","±Balance"
        // (no account or category).
        CommBank
    }

    public class BankCsvParseResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string AccountLabel { get; set; } = string.Empty;
        public BankFileFormat Format { get; set; } = BankFileFormat.Unknown;
        public List<BankTransaction> Transactions { get; } = new();
        public List<string> Issues { get; } = new();
    }

    // Parser for Macquarie's transaction export format:
    // Transaction Date,Details,Account,Category,Subcategory,Tags,Notes,Debit,Credit,Balance,Original Description
    public static class BankCsv
    {
        public const string ExpectedHeaderStart = "Transaction Date,Details,Account,Category,Subcategory";

        // ING's raw export header. Column order is Credit,Debit (opposite of Macquarie), and
        // there is no Account or Category column, so ING files can't self-identify their account.
        public const string IngHeaderStart = "Date,Description,Credit,Debit";

        public static BankFileFormat DetectFormat(string? header)
        {
            if (header == null)
            {
                return BankFileFormat.Unknown;
            }
            if (header.StartsWith(ExpectedHeaderStart, StringComparison.OrdinalIgnoreCase))
            {
                return BankFileFormat.Macquarie;
            }
            if (header.StartsWith(IngHeaderStart, StringComparison.OrdinalIgnoreCase))
            {
                return BankFileFormat.Ing;
            }
            if (CommBankRow.IsMatch(header))
            {
                return BankFileFormat.CommBank;
            }
            return BankFileFormat.Unknown;
        }

        // CommBank exports have no header, so the format is recognised from the first data row,
        // which must have exactly CommBank's shape — four columns: date, signed quoted amount with
        // cents, quoted description, quoted balance with cents (ScanDownloads archives whatever is
        // detected, so a loose match would sweep up unrelated CSVs):
        // 16/09/2026,"-21.05","Transfer To ...","0.00"
        private static readonly Regex CommBankRow = new(
            @"^\d{2}/\d{2}/\d{4},""[+-][\d,]*\d\.\d{2}"",""(?:[^""]|"""")*"",""[+-]?[\d,]*\d\.\d{2}""$",
            RegexOptions.CultureInvariant);

        // Only Macquarie files name their account in-file; the others are matched to an MMEX
        // account by fingerprint or assigned by hand, and their raw description is resolved to a payee.
        public static bool HasAccountColumn(BankFileFormat format) => format == BankFileFormat.Macquarie;

        // The bank posts the two sides of an internal transfer up to a few days apart, and
        // MMEX stores a transfer under a single date — so transfer matching tolerates this gap.
        public const int TransferDateToleranceDays = 3;

        private const int ColDate = 0;
        private const int ColDetails = 1;
        private const int ColAccount = 2;
        private const int ColCategory = 3;
        private const int ColSubcategory = 4;
        private const int ColNotes = 6;
        private const int ColDebit = 7;
        private const int ColCredit = 8;
        private const int ColOriginalDescription = 10;

        private static readonly string[] DateFormats = { "dd MMM yyyy", "dd-MMM-yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };

        // Reads even when the file is open in another program (e.g. Excel or a text editor).
        internal static string[] ReadAllLinesShared(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
            return lines.ToArray();
        }

        public static bool LooksLikeBankExport(string filePath) =>
            DetectFileFormat(filePath) != BankFileFormat.Unknown;

        // Detects the format from the header line only, without parsing the rows.
        public static BankFileFormat DetectFileFormat(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                return DetectFormat(reader.ReadLine());
            }
            catch (IOException)
            {
                return BankFileFormat.Unknown;
            }
        }

        public static BankCsvParseResult Parse(string filePath)
        {
            var result = new BankCsvParseResult { FilePath = filePath };
            var lines = ReadAllLinesShared(filePath);

            result.Format = lines.Length == 0 ? BankFileFormat.Unknown : DetectFormat(lines[0]);
            switch (result.Format)
            {
                case BankFileFormat.Macquarie:
                    ParseMacquarie(lines, result);
                    break;
                case BankFileFormat.Ing:
                    ParseIng(lines, result);
                    break;
                case BankFileFormat.CommBank:
                    ParseCommBank(lines, result);
                    break;
                default:
                    result.Issues.Add("File is not a recognised Macquarie, ING or CommBank export.");
                    break;
            }
            return result;
        }

        private static void ParseMacquarie(string[] lines, BankCsvParseResult result)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var parts = SplitCsvLine(rawLine);
                if (parts.Length <= ColCredit)
                {
                    result.Issues.Add($"Line {i + 1}: not enough columns; skipped.");
                    continue;
                }

                if (!DateTime.TryParseExact(parts[ColDate].Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    result.Issues.Add($"Line {i + 1}: could not parse date \"{parts[ColDate]}\"; skipped.");
                    continue;
                }

                var debitOk = TryParseAmount(parts[ColDebit], out decimal debit);
                var creditOk = TryParseAmount(parts[ColCredit], out decimal credit);
                if (!debitOk && !creditOk)
                {
                    result.Issues.Add($"Line {i + 1}: could not parse debit or credit; skipped.");
                    continue;
                }

                var transaction = new BankTransaction
                {
                    Date = date,
                    Details = parts[ColDetails].Trim(),
                    AccountLabel = parts[ColAccount].Trim(),
                    Category = parts[ColCategory].Trim(),
                    Subcategory = parts[ColSubcategory].Trim(),
                    Notes = parts.Length > ColNotes ? parts[ColNotes].Trim() : string.Empty,
                    Debit = debit,
                    Credit = credit,
                    OriginalDescription = parts.Length > ColOriginalDescription ? parts[ColOriginalDescription].Trim() : string.Empty
                };

                result.Transactions.Add(transaction);

                if (result.AccountLabel.Length == 0 && transaction.AccountLabel.Length > 0)
                {
                    result.AccountLabel = transaction.AccountLabel;
                }
            }
        }

        // ING columns: Date(0), Description(1), Credit(2), Debit(3), Balance(4).
        // Debits are stored negative (e.g. "-100.00"); we keep BankTransaction.Debit a positive
        // magnitude to match the rest of the pipeline. ING has no account/category columns, so
        // AccountLabel stays empty (the importer identifies the account by fingerprint or manual
        // assignment) and the raw text goes to OriginalDescription for payee resolution later.
        private const int IngColDate = 0;
        private const int IngColDescription = 1;
        private const int IngColCredit = 2;
        private const int IngColDebit = 3;

        private static void ParseIng(string[] lines, BankCsvParseResult result)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var parts = SplitCsvLine(rawLine);
                if (parts.Length <= IngColDebit)
                {
                    result.Issues.Add($"Line {i + 1}: not enough columns; skipped.");
                    continue;
                }

                if (!DateTime.TryParseExact(parts[IngColDate].Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    result.Issues.Add($"Line {i + 1}: could not parse date \"{parts[IngColDate]}\"; skipped.");
                    continue;
                }

                var creditOk = TryParseAmount(parts[IngColCredit], out decimal credit);
                var debitOk = TryParseAmount(parts[IngColDebit], out decimal debit);
                if (!debitOk && !creditOk)
                {
                    // ING emits zero-value lines with both amounts blank (e.g. some internal
                    // transfer notices) — nothing to import, so skip them without an issue.
                    if (parts[IngColCredit].Trim().Length > 0 || parts[IngColDebit].Trim().Length > 0)
                    {
                        result.Issues.Add($"Line {i + 1}: could not parse debit or credit; skipped.");
                    }
                    continue;
                }
                if (credit == 0 && debit == 0)
                {
                    continue;
                }

                var description = parts[IngColDescription].Trim();
                var transaction = new BankTransaction
                {
                    Date = date,
                    Details = CleanIngDescription(description),
                    AccountLabel = string.Empty,
                    Category = string.Empty,
                    Subcategory = string.Empty,
                    Notes = string.Empty,
                    Debit = Math.Abs(debit),
                    Credit = Math.Abs(credit),
                    OriginalDescription = description
                };

                result.Transactions.Add(transaction);
            }
        }

        // Best-effort merchant name from an ING description, used only as a fallback when the
        // payee resolver finds no alias/existing-payee match. ING descriptions look like
        // "SUSHI N DON - Visa Purchase - Receipt 19..." — the merchant is the text before the
        // first " - " separator. Collapses runs of whitespace.
        public static string CleanIngDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }
            var cut = description.IndexOf(" - ", StringComparison.Ordinal);
            var head = cut > 0 ? description[..cut] : description;
            return string.Join(' ', head.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        // CommBank columns: Date(0), Amount(1), Description(2), Balance(3) — no header row. The
        // amount is signed ("-21.05" / "+21.05"); split into positive Debit/Credit magnitudes.
        private const int CbaColDate = 0;
        private const int CbaColAmount = 1;
        private const int CbaColDescription = 2;

        private static void ParseCommBank(string[] lines, BankCsvParseResult result)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var parts = SplitCsvLine(rawLine);
                if (parts.Length <= CbaColDescription)
                {
                    result.Issues.Add($"Line {i + 1}: not enough columns; skipped.");
                    continue;
                }

                if (!DateTime.TryParseExact(parts[CbaColDate].Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    result.Issues.Add($"Line {i + 1}: could not parse date \"{parts[CbaColDate]}\"; skipped.");
                    continue;
                }

                if (!TryParseAmount(parts[CbaColAmount], out decimal amount))
                {
                    result.Issues.Add($"Line {i + 1}: could not parse amount \"{parts[CbaColAmount]}\"; skipped.");
                    continue;
                }
                if (amount == 0)
                {
                    continue;   // zero-value line: nothing to import
                }

                var description = parts[CbaColDescription].Trim();
                result.Transactions.Add(new BankTransaction
                {
                    Date = date,
                    Details = CleanCommBankDescription(description),
                    AccountLabel = string.Empty,
                    Category = string.Empty,
                    Subcategory = string.Empty,
                    Notes = string.Empty,
                    Debit = amount < 0 ? -amount : 0,
                    Credit = amount > 0 ? amount : 0,
                    OriginalDescription = description
                });
            }
        }

        // Best-effort payee from a CommBank description, used only as a fallback when the payee
        // resolver finds no alias/existing-payee match. Drops the "Direct Credit 458106 " prefix
        // (the number is the payer's bank ID) and trailing reference tokens containing digits:
        // "Direct Credit 458106 ACCENT GROUP LTD SEP26/00811257" -> "ACCENT GROUP LTD".
        public static string CleanCommBankDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }
            var text = Regex.Replace(description.Trim(), @"^Direct (Credit|Debit) \d+\s+", string.Empty, RegexOptions.IgnoreCase);
            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
            while (words.Count > 1 && words[^1].Any(char.IsDigit))
            {
                words.RemoveAt(words.Count - 1);
            }
            return string.Join(' ', words);
        }

        private static bool TryParseAmount(string text, out decimal value)
        {
            text = text.Trim();
            if (text.Length == 0)
            {
                value = 0;
                return false;
            }
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
