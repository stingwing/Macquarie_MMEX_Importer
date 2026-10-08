# MoneyManagerExMAQ — notes for Claude

A Windows desktop app (C# WinForms, .NET 10, `net10.0-windows`) that imports bank CSV exports
into a **Money Manager Ex (MMEX) 1.9** database. It finds bank files in Downloads, works out
which MMEX account each belongs to, skips rows already in MMEX, assigns payees and categories
learned from the user's own history, pairs internal transfers, and writes the rest — with a
backup first.

## Build, test, run

```bash
dotnet build                          # app (repo-root MoneyManagerExMAQ.csproj)
dotnet test MoneyManagerExMAQ.Tests   # 58 xUnit tests, ~0.5 s
```

- Solution: `MoneyManagerExMAQ.slnx` (app + tests).
- The test project lives in a subfolder of the app project, so the app csproj excludes it via
  `DefaultItemExcludes` — keep that, or test files get compiled into the app.
- Tests build a throwaway MMEX-schema SQLite DB per test (`TestSupport.cs: TestDb`); they never
  touch the real database or settings. One write test returns early if MMEX is running.

## Safety rules (real money data)

- The live database is `C:\MoneyManager\Current.mmb` (plain SQLite). **Never write to it while
  experimenting.** Copy it to a scratch folder and point code at the copy; open the original
  read-only (`file:...?mode=ro` in Python, `readOnly: true` in `MmexDatabase.Open`).
- The app itself only writes when the user clicks Write, refuses while `mmex.exe` is running,
  and copies the `.mmb` to the backup folder first. Keep all three behaviours.
- The DB has **no foreign keys**. Never insert ids that may not exist (categories are validated
  at write time for this reason). Categories are never created by the importer — the user
  curates the taxonomy by hand and deletes categories deliberately.
- User settings live in `%APPDATA%\MoneyManagerExMAQ\settings.json` (accounts, fingerprints,
  own-transfer text, paths). Back it up before editing; prefer the Settings dialog.
- Test programs that create WinForms windows run on the user's desktop — any MessageBox they
  trigger pops up in front of the user. Don't call UI methods on closed/disposed forms.

## Source map

| File | Role |
|---|---|
| `BankCsv.cs` | Format detection + parsers for Macquarie, ING, CommBank; `BankTransaction` model |
| `ImportService.cs` | Scan Downloads, `Analyze` (account → dedupe → payees/categories), transfer pairing, batch write |
| `MmexDatabase.cs` | All SQL: schema check, ledger/dedupe, learned categories, category paths, description history, inserts |
| `PayeeResolver.cs` | Raw ING/CommBank description → payee (alias file → learned history → payee-name substring) |
| `TransferDetector.cs` | "Looks like a move between own accounts" (built-in bank wording + user's own-name markers) |
| `CategoryRecords.cs` | Loads the optional alias file (`pattern,payee` CSV) |
| `AppSettings.cs` | Settings model + tolerant JSON load/save |
| `MoneyManagerExMAQ.cs` / `.Designer.cs` | Main window (files grid, transactions grid, write buttons) |
| `SettingsForm.cs` / `.Designer.cs` | Settings dialog |
| `Assets/app.ico`, `Assets/make_icon.py` | App icon (blue MMEX-style coin + orange import arrow) and its generator |
| `MoneyManagerExMAQ.Tests/` | xUnit tests |

## Bank formats

- **Macquarie** — 11-column export with header `Transaction Date,Details,Account,Category,Subcategory,...`.
  Self-identifies its account via the `Account` column, mapped to an MMEX account by
  `BankAccountLabel` in settings. Carries the bank's own category.
- **ING** — header `Date,Description,Credit,Debit,Balance` (Credit before Debit; debits negative).
  No account column.
- **CommBank** — no header; rows like `16/09/2026,"-21.05","Description","0.00"` (exactly four
  columns, signed amount). Detected by that exact row shape so unrelated CSVs aren't swept up.
- ING/CommBank (`!BankCsv.HasAccountColumn(format)`) get their MMEX account from **fingerprints**:
  comma-separated tokens per account in settings (e.g. a card suffix), matched against
  descriptions with digit boundaries; the account matching the most rows wins, ties are reported.
  The user can also pick the account in the grid (`DetectedFile.AccountOverride`, preserved on re-analysis).
- Zero/blank-amount rows are skipped silently. Rows dated before the MMEX account's
  `INITIALDATE` are ignored (they're in the opening balance).

## How categorisation works (and why)

These rules were chosen by backtesting against the user's real ledger — don't "simplify" them
back without re-measuring.

- **Payee for ING/CommBank** (`PayeeResolver`): (1) alias file if configured, (2) the payee most
  recently recorded for the same *description key* — text before the first `" - "`, lower-cased,
  tokens containing digits dropped — learned from the last line of each transaction's NOTES,
  (3) longest existing payee name contained in the description (≥4 chars) as a fallback only.
  History was 99% right when it matched; pure name-substring matching was ~51% (generic payees
  like "Transfer"/"Refund" swallow everything). A "regenerate alias file from all payees" feature
  was deliberately removed for this reason.
- **Category** (`MmexDatabase.GetPayeeCategories`): the category on the payee's most recent
  transaction **by TRANSDATE** (95% in backtest), else the payee's default. Not by
  `LASTUPDATEDTIME` — bulk edits restamp thousands of rows. Applied to Macquarie rows too,
  overriding the bank's category; unknown payees keep the bank's category only if that path
  exists in MMEX, else fall back to the parent, else uncategorised (with an Issue).
- A category is usable only if it and all its ancestors are `ACTIVE`.
- Category/payee work runs after dedupe, on new rows only.

## Duplicates and transfers

- Dedupe key is (date, debit, credit) — descriptions are ignored; same-day repeats are counted.
- A row that looks like an internal transfer may match an existing MMEX Transfer up to 3 days
  apart (banks post the two sides on different days). Own-name wording is a weak signal: such a
  row may only match a Transfer whose notes also mention the user's name.
- Two loaded files for the same account (overlapping downloads) are deduped against each other
  at write time; paired debit/credit rows across accounts become one MMEX Transfer row.
- The main window re-analyzes all unwritten files after Settings are saved and after each write.

## WinForms conventions

- Layout is `TableLayoutPanel`/`FlowLayoutPanel`/`SplitContainer` with Dock/Anchor — no fixed
  pixel positions. `AutoScaleDimensions = 7F, 15F` (96-DPI baseline).
- Fill-mode grid columns use `FillWeight` and **no `MinimumWidth`**: minimums wider than the
  grid's tiny initial width make the DataGridView permanently rewrite the fill weights.
- Files-grid rows map to `_detectedFiles` by index, so columns are `NotSortable`.
- The account combo commits on change and defers re-analysis with `BeginInvoke` (doing it
  inside `CellValueChanged` is a reentrant call).
- Keep comments in the existing style: say *why*, especially for bank/MMEX quirks.

## Data quirks worth knowing

- Macquarie creates a new payee per merchant location ("Mtg Mate Lutwyche", "Mtg Mate Pty Ltd
  Banyo Aus"); a never-seen location gets the bank's category until the user fixes it once.
- Cross-bank transfers are worded with the user's name rather than bank phrases; the user's name
  variants are configured as "Own-transfer text" in Settings.
- The user makes many scripted DB edits, leaving `Current.mmb_before-<change>_<timestamp>.bak`
  files beside the database.

## Verifying changes

- Run the test suite; add tests next to the area you change.
- For behaviour against real data, copy `Current.mmb` to a scratch folder and run `Analyze`
  through a small console harness referencing the project, using the user's real bank CSVs from
  `C:\MoneyManager\Import`. Expect almost everything to be "already in MMEX" when the DB is current.
- For UI changes, render forms off-screen with `Control.DrawToBitmap` at the user's DPI and
  check several window sizes.
