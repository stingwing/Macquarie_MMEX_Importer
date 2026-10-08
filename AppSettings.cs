using System.Text.Json;

namespace MoneyManagerExMAQ
{
    public class AccountInfo
    {
        public string Name { get; set; } = string.Empty;

        // The "Account" column value in the bank's CSV export (e.g. "Macquarie Platinum Card"),
        // used to auto-detect which account a downloaded file belongs to.
        public string? BankAccountLabel { get; set; }

        // The ACCOUNTNAME in the MMEX database this bank account maps to (e.g. "MAC_Credit").
        public string? MmexAccountName { get; set; }

        // For ING/CommBank files (which carry no account column), one or more comma-separated tokens that,
        // when found in a transaction description, identify the file as this account — e.g. a card
        // suffix "8299" or an "Orange Everyday" account number. Empty for Macquarie accounts.
        public string? IngFingerprint { get; set; }
    }

    public class AppSettings
    {
        public List<AccountInfo> Accounts { get; set; } = new();

        // Path to the MoneyManagerEx .mmb database file.
        public string? MmbFilePath { get; set; }

        // Where timestamped .mmb backups are written before each write. Defaults to a
        // "Backups" folder beside the .mmb when empty.
        public string? BackupFolder { get; set; }

        // Where bank CSVs found in Downloads are archived after scanning.
        public string? ImportFolder { get; set; }

        // Path to the alias file (raw description -> clean payee) used for ING imports.
        public string? CategoryRecordsPath { get; set; }

        // Comma-separated extra text that marks a bank row as a transfer between the user's own
        // accounts — typically their own name as other banks print it ("timothy mollenha" also
        // covers Macquarie's truncated "Timothy Mollenha"). Used for transfer pairing and for
        // matching rows against existing MMEX transfers dated a few days apart.
        public string? TransferMarkers { get; set; }

        private static string SettingsPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MoneyManagerExMAQ", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return ParseSettings(json);
                }
            }
            catch (Exception)
            {
                // Fall back to defaults if the settings file is missing or corrupt.
            }

            return new AppSettings();
        }

        // Tolerant parse: unknown properties (e.g. from older versions of this app) are
        // ignored, so upgrading keeps whatever still applies (account names) and drops the rest.
        private static AppSettings ParseSettings(string json)
        {
            var settings = new AppSettings();
            using var doc = JsonDocument.Parse(json);

            settings.MmbFilePath = ReadString(doc.RootElement, "MmbFilePath");
            settings.BackupFolder = ReadString(doc.RootElement, "BackupFolder");
            settings.ImportFolder = ReadString(doc.RootElement, "ImportFolder");
            settings.CategoryRecordsPath = ReadString(doc.RootElement, "CategoryRecordsPath");
            settings.TransferMarkers = ReadString(doc.RootElement, "TransferMarkers");

            if (doc.RootElement.TryGetProperty("Accounts", out var accountsProp) &&
                accountsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in accountsProp.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var name = ReadString(item, "Name");
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    settings.Accounts.Add(new AccountInfo
                    {
                        Name = name,
                        BankAccountLabel = ReadString(item, "BankAccountLabel"),
                        MmexAccountName = ReadString(item, "MmexAccountName"),
                        IngFingerprint = ReadString(item, "IngFingerprint")
                    });
                }
            }

            return settings;
        }

        private static string? ReadString(JsonElement element, string property) =>
            element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;

        public void Save()
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
}
