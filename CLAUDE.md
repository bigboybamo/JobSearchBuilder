# CLAUDE.md — JobSearchBuilder

---

## Project Overview

**Project name:** JobSearchBuilder  
**Solution file:** `JobSearchBuilder.sln`  
**Target framework:** .NET Framework 4.8  
**Project type:** Windows Desktop — WinForms  
**Primary language:** C# (.NET Framework 4.8 — no nullable reference types, no file-scoped namespaces)  
**Description:** A Windows desktop application that builds targeted Google search queries for job hunting. It generates `site:`-scoped queries that search directly inside the career portals of popular Applicant Tracking Systems (Greenhouse, Lever, Workable, Ashby, Workday, and more), combining tech stack, role, seniority, location, visa, timezone, and remote preferences into a single query.

---

## Repository Structure

```
JobSearchBuilder/               # Main WinForms project (net4.8, WinExe)
  Interfaces/
    IProfileStore.cs            # Profile persistence contract
  Models/
    AtsSourceGroup.cs           # A named set of ATS domains
    SearchProfile.cs            # All criteria for one search
    SearchQuery.cs              # Query result model
  Services/
    AppSettingsLoader.cs        # Reads appsettings.json (via Newtonsoft.Json)
    IDbConnectionFactory.cs     # Database connection abstraction
    SqlConnectionFactory.cs     # SQL Server implementation
    SqlProfileStore.cs          # Persists profiles via ADO.NET
    InMemoryProfileStore.cs     # In-memory store used in unit tests
    QueryBuilder.cs             # Assembles the Google query string
    CountryService.cs           # Loads country list (used in location picker)
  Scripts/
    CreateTables.sql            # One-time DB setup + seed script
  Images/                       # UI images/icons
  MainForm.cs / MainForm.Designer.cs  # Main WinForms UI
  appsettings.json              # Runtime configuration (ATS groups, defaults)
  appsettings.local.json        # Gitignored — overrides ConnectionString locally
JobSearchBuilder.Installer/     # Installer project
JobSearchBuilder.Tests/         # NUnit test project (net48)
.github/workflows/
  dotnet-desktop.yml            # CI: build and test on push/PR
scripts/                        # Build/setup scripts
integrations/                   # Integration config (Azure DevOps, GitHub, Jira, Slack)
```

---

## Architecture

### Pattern
Simple single-project WinForms application with a Services/Interfaces/Models folder structure. There is no Clean Architecture layering, no CQRS, and no MediatR.

**Key design points:**
- `IProfileStore` / `IDbConnectionFactory` — interface + implementation pattern for testability
- `QueryBuilder` is a pure service with no I/O (easy to unit test without mocks)
- `AppSettingsLoader` reads `appsettings.json` at startup and returns a plain `AppSettings` bag
- `SqlProfileStore` uses raw ADO.NET (`IDbConnection` / `IDbCommand`) — no ORM
- `InMemoryProfileStore` is a test double for `IProfileStore`
- `MainForm` wires everything in its constructor — this is the composition root

### No Clean Architecture — do not apply layered rules
Do not reorganise code into Domain/Application/Infrastructure projects. The existing flat structure is intentional for a small desktop tool.

---

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes, interfaces | PascalCase | `SqlProfileStore`, `IProfileStore` |
| Interfaces | `I` prefix | `IProfileStore`, `IDbConnectionFactory` |
| Methods | PascalCase | `LoadProfileIntoUi`, `RebuildQuery` |
| Parameters, locals | camelCase | `profile`, `chipPanel` |
| Private fields | `_` prefix + camelCase | `_store`, `_queryBuilder` |
| Async methods | `Async` suffix | `LoadCountriesAsync` |
| Event handlers | `{control}_{eventName}` | `lstProfiles_SelectedIndexChanged` |

---

## Coding Standards

### General (.NET Framework 4.8)
- **Block namespaces** — file-scoped namespaces are not used (C# 10+ feature, incompatible with net4.8 syntax style here)
- `var` is used when the type is obvious from the right-hand side
- Private fields use `_` prefix
- No nullable reference types — the project targets .NET Framework 4.8
- Standard `using` blocks, not `using` declarations
- Null checks use manual `if (x == null)` or `ArgumentNullException` — no `ThrowIfNull` (net4.8)

### Async / await
- UI-initiated async work uses `async void` event handlers or `async void` lifecycle methods (acceptable in WinForms)
- `Task.Run` is used to push blocking work off the UI thread (see `LoadCountriesAsync`)
- Do not use `.Result` or `.Wait()` on the UI thread

### Error handling
- Exceptions are let through to display in the UI (MessageBox) or swallowed with a comment where silent fallback is intentional (e.g. `LoadCountriesAsync`)
- `QueryBuilder` throws `ArgumentNullException` for null inputs — callers catch at the boundary

### LINQ
- Method syntax used throughout
- `FirstOrDefault()` used (not `First()`)

---

## Database

**Driver:** Raw ADO.NET via `Microsoft.Data.SqlClient` — **no EF Core, no Dapper**  
**Database:** SQL Server (any edition — Express, Developer, LocalDB)  
**Schema:** Three tables — `SearchProfiles`, `ProfileKeywords` (keywords by category), `ProfileSourceGroups`

Setup script: `JobSearchBuilder/Scripts/CreateTables.sql`  
Run once: `sqlcmd -S .\SQLEXPRESS -i Scripts\CreateTables.sql`

Connection string key: `ConnectionString` (top-level key in `appsettings.json`)

---

## Testing

**Framework:** NUnit 3.14  
**Mock library:** None — `InMemoryProfileStore` is a hand-written test double for `IProfileStore`; SQLite (`System.Data.SQLite`) provides a real in-process database for `SqlProfileStore` tests  
**Assertion library:** NUnit built-in (`Assert.That`)

### Test conventions
- Test class name: `{ClassUnderTest}Tests` (e.g. `QueryBuilderTests`)
- Method name: `{MethodName}_{Scenario}_{ExpectedOutcome}` (e.g. `Build_EmptyProfile_ReturnsEmptyQuery`)
- Use `[TestFixture]` on the class and `[Test]` on each method
- `[SetUp]` initialises shared objects
- `[TestCase]` for parameterised tests

### What not to do in tests
- Do not mock `IDbConnection` or `IDbCommand` — use `InMemoryProfileStore` or SQLite instead
- Do not use `Thread.Sleep`

### Running tests
```powershell
dotnet test JobSearchBuilder.Tests/JobSearchBuilder.Tests.csproj
```
No SQL Server needed — tests use SQLite.

---

## NuGet Packages

| Purpose | Package |
|---|---|
| JSON parsing | `Newtonsoft.Json` 13.0.4 |
| SQL Server client | `Microsoft.Data.SqlClient` 6.1.4 |
| Azure authentication | `Azure.Identity` 1.17.1, `Azure.Core` 1.50.0 |
| MSAL | `Microsoft.Identity.Client` 4.80.0 |
| JSON (stdlib backport) | `System.Text.Json` 8.0.6 |
| Testing | `NUnit` 3.14, `NUnit3TestAdapter` 4.5, `Microsoft.NET.Test.Sdk` 17.8 |
| Test database | `System.Data.SQLite` 1.0.119 |

> When suggesting new packages, check the existing `packages.config` first. Prefer packages compatible with `net472`/`net48` since the main project targets .NET Framework 4.8.

---

## Configuration

**appsettings.json** — committed to source, contains:
- `ConnectionString` — SQL Server connection string (defaults to `.\SQLEXPRESS`)
- `AtsSourceGroups` — array of ATS platform groups (id, name, domains)
- `Defaults` — suggestion lists for seniority, roles, visa terms, remote terms, locations, exclude terms, timezone terms

**appsettings.local.json** — gitignored, can override `ConnectionString` for local dev.

**Required configuration:**
```
ConnectionString=  # SQL Server connection string
                   # e.g. Server=.\SQLEXPRESS;Database=JobSearchBuilder;Integrated Security=true;TrustServerCertificate=True;
```

No user-secrets, no environment variables — configuration is file-based.

---

## Build & Local Commands

```powershell
# Restore packages for the main WinForms project (packages.config style)
nuget restore JobSearchBuilder/JobSearchBuilder.csproj -PackagesDirectory packages

# Restore packages for the test project (PackageReference style)
dotnet restore JobSearchBuilder.Tests/JobSearchBuilder.Tests.csproj

# Build the test project (also builds the main project as a reference)
dotnet build JobSearchBuilder.Tests/JobSearchBuilder.Tests.csproj --configuration Release

# Run all tests (no SQL Server required — uses SQLite)
dotnet test JobSearchBuilder.Tests/JobSearchBuilder.Tests.csproj --configuration Release

# Build the main WinForms project with MSBuild
msbuild JobSearchBuilder.sln /p:Configuration=Release

# Set up the database (run once against your SQL Server instance)
sqlcmd -S .\SQLEXPRESS -i JobSearchBuilder/Scripts/CreateTables.sql
```

---

## Git Workflow

- **Main branch:** `master` (protected — no direct pushes)
- **Branch naming:** `feature/{short-description}` (e.g. `feature/Timezone`, `feature/newSearches`)
- **Commit messages:** plain English, descriptive (e.g. `Add Timezone Implementation`, `New searches`)
- **Pull requests:** always target `master`

### CI/CD
GitHub Actions: `.github/workflows/dotnet-desktop.yml`  
Triggers on push to `master`, `main`, and `feature/**` branches, and on PRs to `master`/`main`.  
Steps: NuGet restore → dotnet restore → build test project → run tests.

---

## Desktop App Notes (WinForms)

- **No MVVM** — this is a code-behind WinForms app; business logic lives in `Services/`, not in `MainForm`
- `MainForm` is the composition root: it creates `AppSettings`, `IProfileStore`, and `QueryBuilder` directly in its constructor
- UI state is managed via `_isDirty` and `_isLoading` flags to prevent event re-entrancy during profile loads
- Chip-based keyword input: each keyword category renders as a `FlowLayoutPanel` of small `Panel` chips with an inline `TextBox` and suggestion buttons
- `_cboLocationPicker` is a searchable `ComboBox` populated asynchronously from `CountryService`
- All UI interactions eventually call `MarkDirtyAndRebuild()` to keep the query preview live

---

## Claude Behaviour for This Project

### Always do
- Read the full file before editing it
- Keep changes inside `Services/`, `Models/`, `Interfaces/`, or `MainForm` — match the existing flat structure
- Check `.NET Framework 4.8` compatibility before suggesting language features (no records, no file-scoped namespaces, no `ArgumentNullException.ThrowIfNull`)
- Suggest a test in `JobSearchBuilder.Tests` for any new service method
- Use `Newtonsoft.Json` for JSON — it is already the project's JSON library

### Never do
- Introduce EF Core — the project uses raw ADO.NET by design
- Add `async Task` return type to WinForms event handlers — use `async void` (WinForms standard)
- Use `Console.WriteLine` for logging — use `Debug.WriteLine` for diagnostics or surface errors via `MessageBox`
- Leave `TODO` comments in committed code
- Add NuGet packages without confirming they target `net472`/`net48`

### When you are unsure
- Ask before changing the database schema — there is no migration system; DDL changes require updating `Scripts/CreateTables.sql` manually
- Ask before adding a new keyword category — it touches `SearchProfile`, `ProfileKeywords` category column, `QueryBuilder`, `MainForm` UI, and `SqlProfileStore` in tandem

---

## MCP Integrations

| Integration | Purpose |
|---|---|
| GitHub | Read issues, create PRs, review diffs |

> Claude must never send emails, post Slack messages, or merge PRs without explicit user approval in the session.

---

*Last updated: 2026-04-06*  
*Maintained by: bigboybamo*
