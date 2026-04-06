# Plan: Company Career Pages Integration

## Context

The app builds Google `site:` filter queries targeting ATS platforms. The user wants to also target specific company career pages directly — sourced from the llms.txt directory at https://directory.llmstxt.cloud (tech-forward companies: AI, dev tools, infrastructure, etc.). The company JSON data is available at:

`https://raw.githubusercontent.com/thedaviddias/llms-txt-hub/main/data/websites.json`

Each entry has `name`, `domain`, `category`. There are 400+ companies — users must pick specific ones (Google query length limits prevent using all).

## Approach

Autocomplete company picker (mirrors the location picker pattern) → chips store domain in Tag → domains merged into the existing `site:` block alongside ATS groups.

---

## Files to Create

### `JobSearchBuilder/Models/CompanyEntry.cs`
Simple POCO: `Name`, `Domain`, `Category` properties. `ToString()` returns `Name`.

### `JobSearchBuilder/Services/CompanyService.cs`
Follow `CountryService.cs` exactly:
- Fetch from GitHub raw JSON URL using `WebClient.DownloadString()`
- Cache to `companies.json` in `AppDomain.CurrentDomain.BaseDirectory`
- Return `List<CompanyEntry>` sorted by Name
- Constructor takes `(string cacheFilePath, Func<string> fetchJson)` for testability
- `ParseApiResponse(string json)` — handle both `JArray` root and `{ "websites": [...] }` root defensively
- `ParseCache(string json)` — reads the slimmed-down cached format `[{name, domain, category}]`

---

## Files to Modify

### `JobSearchBuilder/Models/SearchProfile.cs`
- Add `public List<string> CompanyDomains { get; set; }`
- Initialize in constructor: `CompanyDomains = new List<string>();`

### `JobSearchBuilder/Services/QueryBuilder.cs`
Change `BuildSiteBlock` signature:
```csharp
private string BuildSiteBlock(List<int> groupIds, List<string> companyDomains)
```
Inside: collect ATS domains first, then append company domains (dedup via `StringComparer.OrdinalIgnoreCase`). Update call site in `Build()`:
```csharp
string siteBlock = BuildSiteBlock(profile.SourceGroupIds, profile.CompanyDomains);
```

### `JobSearchBuilder/Services/SqlProfileStore.cs`
- **Read** (`LoadProfiles` switch): add `case "CompanyDomain": p.CompanyDomains.Add(keyword); break;`
- **Write** (`InsertKeywordsAndGroups`): add `insertCategory("CompanyDomain", profile.CompanyDomains);`
- No schema changes needed — `ProfileKeywords.Category` already accepts any string

### `JobSearchBuilder/MainForm.Designer.cs`
Add three controls in `flpEditor`, positioned after the ATS groups spacer and before the Stack section:
```
lblCompaniesHeader  (Label, matches other section headers)
flpCompanies        (FlowLayoutPanel, matches other chip panels)
flpCompaniesAddRow  (FlowLayoutPanel, matches other add-row panels)
```
Add field declarations at the bottom of the partial class.

### `JobSearchBuilder/MainForm.cs`

**New fields:**
```csharp
private ComboBox _cboCompanyPicker;
private List<CompanyEntry> _allCompanies;
```

**`PostInitialize()`** additions:
- `ConfigureSectionHeader(lblCompaniesHeader, "COMPANY CAREER PAGES")`
- `ConfigureChipPanel(flpCompanies)`
- Build `_cboCompanyPicker` (same style as `_cboLocationPicker`): `Width=300`, `DropDownStyle=DropDown`, `AutoCompleteMode=SuggestAppend`, `AutoCompleteSource=CustomSource`
- Wire `KeyDown` (Enter) → `AddCompanyChip` + `MarkDirtyAndRebuild`
- Wire `SelectionChangeCommitted` → `AddCompanyChip(entry.Name, entry.Domain)` + `MarkDirtyAndRebuild`
- Add picker to `flpCompaniesAddRow`

**New `AddCompanyChip(string displayName, string domain = null)`:**
- If `domain == null`: lookup in `_allCompanies` by name, fallback to using `displayName` as domain
- **Key difference from `AddChip`**: `chip.Tag = domain` so `GetChips(flpCompanies)` returns domains directly
- Dedup by `chip.Tag` (domain)
- Chip colors: `BackColor = Color.FromArgb(235, 240, 255)`, `ForeColor = Color.FromArgb(30, 60, 120)`

**New `LoadCompaniesAsync()`** (mirrors `LoadCountriesAsync`):
```csharp
private async void LoadCompaniesAsync()
{
    try
    {
        _allCompanies = await Task.Run(() => new CompanyService().GetCompanies());
        var source = new AutoCompleteStringCollection();
        source.AddRange(_allCompanies.Select(c => c.Name).ToArray());
        _cboCompanyPicker.AutoCompleteCustomSource = source;
        _cboCompanyPicker.Items.AddRange(_allCompanies.Cast<object>().ToArray());
    }
    catch { /* fail silently — user can type domain manually */ }
}
```
Call from constructor after `LoadCountriesAsync()`.

**`LoadProfileIntoUi()`** — after `ClearChips(flpExclude)`:
```csharp
ClearChips(flpCompanies);
foreach (string domain in profile.CompanyDomains ?? Enumerable.Empty<string>())
{
    string name = _allCompanies?.FirstOrDefault(
        c => string.Equals(c.Domain, domain, StringComparison.OrdinalIgnoreCase))?.Name ?? domain;
    AddCompanyChip(name, domain);
}
```

**`ReadProfileFromUi()`** — add to profile initializer:
```csharp
CompanyDomains = GetChips(flpCompanies),
```

---

## Implementation Order

1. `CompanyEntry.cs` — no dependencies
2. `CompanyService.cs` — self-contained, testable in isolation
3. `SearchProfile.cs` — trivial property addition
4. `QueryBuilder.cs` — extend `BuildSiteBlock`, update call site
5. `SqlProfileStore.cs` — add read case + write call
6. `MainForm.Designer.cs` — add control declarations
7. `MainForm.cs` — wire everything together

Steps 1–5 can be built and tested before touching the UI.

---

## Key Reused Patterns

| Pattern | Source File |
|---|---|
| Fetch + local cache service | `Services/CountryService.cs` |
| Autocomplete ComboBox picker | `MainForm.cs` → `_cboLocationPicker` block |
| ProfileKeywords category discriminator | `Services/SqlProfileStore.cs` → `insertCategory()` |

---

## Verification

1. **Unit tests**: Add `CompanyServiceTests.cs` (parse, cache round-trip, API fallback). Extend `QueryBuilderTests` (company domains only, merged with ATS, dedup). Extend `SqlProfileStoreTests` (save/load `CompanyDomains`).
2. **Cold start**: Delete `companies.json` → run app → picker populates from network.
3. **Warm start**: Run again → cache is used.
4. **Add company**: Type "Anthropic" → chip shows "Anthropic", query preview shows `site:anthropic.com`.
5. **Combined**: Check an ATS group + add a company → single `site:` block with both.
6. **Save/reload**: Save profile → restart → chips reappear with names resolved.
7. **Freeform domain**: Type `stripe.com` (not in list) → chip added with domain as both name and tag.
