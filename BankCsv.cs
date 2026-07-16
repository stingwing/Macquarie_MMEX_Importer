using System.Globalization;
using System.Text;

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
    }

    public class BankCsvParseResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string AccountLabel { get; set; } = string.Empty;
        public List<BankTransaction> Transactions { get; } = new();
        public List<string> Issues { get; } = new();
    }

    // Parser for Macquarie's transaction export format:
    // Transaction Date,Details,Account,Category,Subcategory,Tags,Notes,Debit,Credit,Balance,Original Description
    public static class BankCsv
    {
        public const string ExpectedHeaderStart = "Transaction Date,Details,Account,Category,Subcategory";

        // The bank posts the two sides of an internal transfer up to a few days apart, and
        // MMEX stores a transfer under a single date — so transfer matching tolerates this gap.
        public const int TransferDateToleranceDays = 3;

        // Wording that marks a row as an internal movement between the user's own accounts.
        private static readonly string[] InternalTransferMarkers =
        {
            "internal transfer",
            "linked account",
            "mbl card service",
            "bpay payment - thank you",
            "online payment",
            "payment to your credit card",
            "credit card payment",
            "to account xx",
            "from account xx"
        };

        public static bool LooksLikeInternalTransfer(BankTransaction transaction)
        {
            var text = $"{transaction.Details} {transaction.OriginalDescription}".ToLowerInvariant();
            return InternalTransferMarkers.Any(text.Contains);
        }

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
        private static string[] ReadAllLinesShared(string filePath)
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

        public static bool LooksLikeBankExport(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var header = reader.ReadLine();
                return header != null && header.StartsWith(ExpectedHeaderStart, StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static BankCsvParseResult Parse(string filePath)
        {
            var result = new BankCsvParseResult { FilePath = filePath };
            var lines = ReadAllLinesShared(filePath);

            if (lines.Length == 0 || !lines[0].StartsWith(ExpectedHeaderStart, StringComparison.OrdinalIgnoreCase))
            {
                result.Issues.Add("File does not start with the expected Macquarie export header.");
                return result;
            }

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

            return result;
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
