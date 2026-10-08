namespace MoneyManagerExMAQ
{
    // Turns an ING/CommBank transaction's raw description into a clean payee name and (when known)
    // the category to file it under. Three matching passes:
    //   1. the alias file (raw fragment -> clean payee; longest match wins) — explicit curation;
    //   2. the user's own history: the payee most recently recorded for a transaction whose
    //      description has the same merchant key (see DescriptionKey);
    //   3. existing MMEX payee names appearing as a substring of the description (longest wins) —
    //      only a fallback, because generic payees ("Transfer", "Refund", "Payment") swallow
    //      unrelated descriptions. Backtested on two years of the user's ING rows, pass 2 was
    //      99% correct when it matched and lifted overall accuracy from 51% to 79%.
    // The category is never guessed — it is inherited from the matched payee's category in the
    // database (see MmexDatabase.GetPayeeCategories: learned from the payee's latest transaction,
    // else its default), so ING transactions land in the same taxonomy as Macquarie ones. Anything
    // unmatched returns no payee (the caller falls back to the parser's cleaned description) and no
    // category (imported uncategorised until the user sets the payee's category in MMEX).
    public class PayeeResolver
    {
        public readonly record struct Resolution(string? PayeeName, long? CategoryId);

        // Existing-payee substring matching is skipped for names shorter than this to avoid a
        // short name (e.g. "ING") spuriously matching unrelated descriptions. The curated alias
        // file has no such limit: each alias there is deliberate.
        private const int MinPayeeNameMatchLength = 4;

        private readonly List<(string Lower, string Payee)> _aliases;
        private readonly Dictionary<string, string> _payeeByDescriptionKey;
        private readonly List<(string Lower, string Name)> _payeeNames;
        private readonly Dictionary<string, long> _payeeCategory; // name -> categid (may be -1)

        // descriptionHistory: (raw description, payee) pairs, oldest first, so the latest wins.
        public PayeeResolver(
            IEnumerable<CategoryRecords.Alias> aliases,
            IEnumerable<(string Description, string Payee)> descriptionHistory,
            IReadOnlyDictionary<string, long> payeeCategories)
        {
            _aliases = aliases
                .Select(a => (Lower: a.Pattern.ToLowerInvariant(), a.Payee))
                .OrderByDescending(a => a.Lower.Length)
                .ToList();

            _payeeByDescriptionKey = new Dictionary<string, string>();
            foreach (var (description, payee) in descriptionHistory)
            {
                var key = DescriptionKey(description);
                if (key.Length > 0)
                {
                    _payeeByDescriptionKey[key] = payee;
                }
            }

            _payeeCategory = new Dictionary<string, long>(payeeCategories, StringComparer.OrdinalIgnoreCase);

            _payeeNames = payeeCategories.Keys
                .Where(n => n.Length >= MinPayeeNameMatchLength)
                .Select(n => (Lower: n.ToLowerInvariant(), Name: n))
                .OrderByDescending(n => n.Lower.Length)
                .ToList();
        }

        public Resolution Resolve(string rawDescription)
        {
            var haystack = (rawDescription ?? string.Empty).ToLowerInvariant();

            foreach (var (lower, payee) in _aliases)
            {
                if (lower.Length > 0 && haystack.Contains(lower))
                {
                    return new Resolution(payee, CategoryFor(payee));
                }
            }

            var key = DescriptionKey(rawDescription ?? string.Empty);
            if (key.Length > 0 && _payeeByDescriptionKey.TryGetValue(key, out var learned))
            {
                return new Resolution(learned, CategoryFor(learned));
            }

            foreach (var (lower, name) in _payeeNames)
            {
                if (haystack.Contains(lower))
                {
                    return new Resolution(name, CategoryFor(name));
                }
            }

            return new Resolution(null, null);
        }

        // A description's merchant key: the text before ING's first " - " separator, lower-cased,
        // with every token containing a digit dropped (receipt numbers, card suffixes, CommBank's
        // "SEP26/00811257" references), so repeat purchases from one merchant share a key:
        // "Spotify P4796D867C - Visa Purchase - Receipt 165643..." -> "spotify".
        public static string DescriptionKey(string description)
        {
            var text = description.Trim().Trim('"').Trim();
            var cut = text.IndexOf(" - ", StringComparison.Ordinal);
            var head = cut > 0 ? text[..cut] : text;
            return string.Join(' ', head.ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !word.Any(char.IsDigit)));
        }

        private long? CategoryFor(string payeeName)
        {
            if (_payeeCategory.TryGetValue(payeeName, out var categId) && categId != -1)
            {
                return categId;
            }
            return null;
        }
    }
}
