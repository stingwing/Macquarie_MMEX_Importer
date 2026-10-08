namespace MoneyManagerExMAQ
{
    // Decides whether a bank row looks like a move between the user's own accounts. Built-in bank
    // wording is a strong signal. The user's own markers from Settings (AppSettings.TransferMarkers —
    // mainly their name as other banks print it on cross-bank transfers) are weaker: the same name
    // also appears on ordinary payments, so callers can tell the two apart (see ExistingLedger).
    public class TransferDetector
    {
        private static readonly string[] BankMarkers =
        {
            "internal transfer",
            "linked account",
            "mbl card service",
            "bpay payment - thank you",
            "online payment",
            "payment to your credit card",
            "credit card payment",
            "to account xx",
            "from account xx",
            // ING / CommBank wording for moves between the user's own accounts.
            "orange everyday",
            "orange one",
            "savings accelerator",
            "fast transfer from",
            "transfer to term deposit"
        };

        private readonly string[] _ownMarkers;

        public TransferDetector(string? ownMarkersCommaSeparated)
        {
            _ownMarkers = (ownMarkersCommaSeparated ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(m => m.ToLowerInvariant())
                .ToArray();
        }

        public bool LooksInternal(BankTransaction transaction)
        {
            var text = Text(transaction);
            return BankMarkers.Any(text.Contains) || _ownMarkers.Any(text.Contains);
        }

        public bool HasBankWording(BankTransaction transaction) => BankMarkers.Any(Text(transaction).Contains);

        public bool MentionsOwnName(string text)
        {
            var lower = text.ToLowerInvariant();
            return _ownMarkers.Any(lower.Contains);
        }

        private static string Text(BankTransaction transaction) =>
            $"{transaction.Details} {transaction.OriginalDescription}".ToLowerInvariant();
    }
}
