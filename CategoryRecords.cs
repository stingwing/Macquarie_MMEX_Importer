namespace MoneyManagerExMAQ
{
    // The alias file (historically "CatagoryRecords.csv"): a two-column, header-less CSV mapping
    // a raw-description fragment to a clean payee name — e.g.  HIA ONE PTY LTD,IGA
    // Categories are NOT stored here; they come from the resolved payee's category in the MMEX
    // database, so the ING and Macquarie sides always share one category taxonomy.
    public static class CategoryRecords
    {
        public readonly record struct Alias(string Pattern, string Payee);

        // Reads the alias file. Missing file returns an empty list (not an error). Blank lines and
        // rows without both columns are skipped.
        public static List<Alias> Load(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new List<Alias>();
            }
            return Parse(BankCsv.ReadAllLinesShared(path));
        }

        private static List<Alias> Parse(IEnumerable<string> lines)
        {
            var aliases = new List<Alias>();
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }
                var comma = raw.IndexOf(',');
                if (comma <= 0)
                {
                    continue;
                }
                var pattern = raw[..comma].Trim();
                var payee = raw[(comma + 1)..].Trim();
                if (pattern.Length == 0 || payee.Length == 0)
                {
                    continue;
                }
                aliases.Add(new Alias(pattern, payee));
            }
            return aliases;
        }
    }
}
