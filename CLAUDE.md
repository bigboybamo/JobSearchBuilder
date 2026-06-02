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

## AI Integration Roadmap

### Progress Tracker
- [x] Phase 1 — `feature/llm-provider`
- [x] Phase 2 — `feature/nl-profile-builder`
- [x] Phase 3 — `feature/prompt-caching`
- [x] Phase 4 — `feature/query-suggestions`
- [x] Phase 5 — `feature/query-review`
- [ ] Phase 6 — `feature/eval-pipeline`
- [ ] Phase 7 — `feature/batch-profiles`

Mark phases `[x]` as they are merged to master. When starting a session, Claude reads the first unchecked phase as the current one.

---

### Provider Architecture

All AI features call `ILlmProvider` — never a concrete provider directly. Switching provider means changing one config value and restarting; no service code changes.

```
ILlmProvider                        ← the only thing services know about
    │
    ├── AnthropicProvider            ← /v1/messages, tool_use, cache_control
    ├── OpenAiProvider               ← /v1/chat/completions, function calling
    └── GeminiProvider               ← /v1beta/models/.../generateContent
```

Active provider is set in `appsettings.json` under `Ai.Provider`. API keys are read from environment variables — never stored in any config file. `LlmProviderFactory.Create(settings)` is called in the `MainForm` constructor alongside existing service wiring.

**Required environment variables:**
```
ANTHROPIC_API_KEY=sk-ant-...
OPENAI_API_KEY=sk-...
GEMINI_API_KEY=...
```
Only the key for the active provider needs to be set. The others are silently empty.

**Model tiers** — services request a capability tier, not a hardcoded model string:
- `Balanced` — the only active tier; one model per provider configured in `appsettings.json`

Model strings are configured in `appsettings.json` under `Ai.Models[provider].Balanced`. When the active provider changes via the UI dropdown, the matching model is resolved automatically — service code stays the same.

**`appsettings.json` addition:**
```json
"Ai": {
  "Provider": "Anthropic",
  "Models": {
    "Anthropic": { "Balanced": "claude-sonnet-4-5-20250929" },
    "OpenAI":    { "Balanced": "gpt-4.1-mini" },
    "Gemini":    { "Balanced": "gemini-2.5-flash" }
  }
}
```
API keys are not stored here — they are read at startup from `ANTHROPIC_API_KEY`, `OPENAI_API_KEY`, and `GEMINI_API_KEY` environment variables.

---

### New Files and Where They Live

**Interfaces/**
- `ILlmProvider.cs` — `ProviderName`, `ModelId`, `SendAsync(LlmRequest)` → `LlmResponse`

**Services/Providers/**
- `AnthropicProvider.cs` — POSTs to `/v1/messages`; handles `cache_control`, `tool_use` blocks
- `OpenAiProvider.cs` — POSTs to `/v1/chat/completions`; handles `tool_calls` in response
- `GeminiProvider.cs` — POSTs to `/v1beta/models/{model}:generateContent`; handles `functionCall` parts
- `LlmProviderFactory.cs` — reads `Ai.Provider` from config, returns the correct `ILlmProvider`

**Services/**
- `NlProfileBuilderService.cs` — takes plain English role description, returns `QueryProfileResult`
- `QuerySuggestionService.cs` — takes current chips, returns up to 5 suggested terms (Fast tier)
- `QueryReviewService.cs` — takes assembled query string, returns issues and suggestions (Balanced tier)
- `BatchProfileBuilderService.cs` — processes multiple descriptions at once (Phase 7)
- `PromptLoader.cs` — reads XML prompt templates from `/prompts/` at repo root

**Models/**
- `LlmRequest.cs` — `SystemPrompt`, `UserMessage`, `Tools`, `ForceToolName`, `ModelTier`, `EnableCaching`
- `LlmResponse.cs` — `TextContent`, `ToolCallName`, `ToolCallArguments`, `InputTokens`, `OutputTokens`, `CacheReadTokens`, `CacheWriteTokens`
- `LlmToolDefinition.cs` — `Name`, `Description`, `InputSchema` (raw JSON string)
- `QueryProfileResult.cs` — `Role`, `Seniority`, `TechStack`, `RemoteTerms`, `TimezoneTerms`, `ExcludeTerms`

**prompts/ (repo root, outside .csproj)**
```
prompts/
  nl_profile_builder/
    v1.xml          ← active prompt
    CHANGELOG.md    ← what changed between versions and why
    rubric.yaml     ← Promptfoo LLM-as-judge grader
  query_suggestions/
    v1.xml / CHANGELOG.md / rubric.yaml
  query_review/
    v1.xml / CHANGELOG.md / rubric.yaml
  evals/
    nl_profile_builder/golden_set.json   ← 15-20 plain English inputs → expected chip values
    query_suggestions/golden_set.json
    query_review/golden_set.json
  promptfooconfig.yaml
```

**Tests/**
- `AnthropicProviderTests.cs` — verifies request serialization and response parsing against canned JSON; no real HTTP
- `NlProfileBuilderServiceTests.cs` — uses `InMemoryLlmProvider` (hand-written stub, same pattern as `InMemoryProfileStore`)
- `QuerySuggestionServiceTests.cs` — uses `InMemoryLlmProvider`

---

### Prompt Template Rules

- All prompts live in `/prompts/` — never hardcoded in service classes
- Loaded at runtime by `PromptLoader.cs`
- XML-structured with these tags: `<instructions>`, `<context>`, `<output_format>`, `<constraints>`
- Every prompt is paired with a `CHANGELOG.md` and a `rubric.yaml`
- When changing a prompt: copy `v1.xml` → `v2.xml`, run Promptfoo eval, compare scores, record result in `CHANGELOG.md` before merging

---

### Prompt Caching Rules (Anthropic only)

- Set `EnableCaching = true` on `LlmRequest` for calls with stable system prompts
- `AnthropicProvider` wraps the system block in `cache_control: { "type": "ephemeral" }` when this flag is true
- `OpenAiProvider` and `GeminiProvider` silently ignore `EnableCaching`
- Dynamic content (user input, query string) goes in `messages[]` only — never in the system block
- Any byte-level change to the system block breaks the cache — do not inject dynamic values into system prompts
- Cache hits visible in VS Output window via `Debug.WriteLine` on `LlmResponse.CacheReadTokens`

---

### Phase Details

#### Phase 1 — Provider Abstraction (`feature/llm-provider`)
Build `ILlmProvider`, all three provider implementations, `LlmProviderFactory`, and the request/response DTOs. Wire the factory into `MainForm` constructor. Add `InMemoryLlmProvider` test double for use in all AI service tests. Add a status label to the form footer showing active provider and model (e.g. "Anthropic · claude-sonnet-4-20250514").

Key implementation notes:
- `AnthropicProvider`: system prompt sent as an array `[{"type":"text","text":"..."}]` — not a plain string — so `cache_control` can be appended per-element
- `OpenAiProvider`: `tool_choice` forced call is `{"type":"function","function":{"name":"..."}}` — different shape from Anthropic
- `GeminiProvider`: tools go in `tools[0].functionDeclarations[]` — not a top-level `tools[]` array
- All three providers map back to the same `LlmResponse` — `CacheReadTokens` / `CacheWriteTokens` are zero on non-Anthropic providers

#### Phase 2 — Natural Language Profile Builder (`feature/nl-profile-builder`)
A `Describe Role` button on `MainForm` opens a short text input dialog. The user types a plain English description (e.g. "Senior .NET developer, fully remote, UTC+1 or UTC+2, no security clearance"). `NlProfileBuilderService` sends this to the active provider using the `nl_profile_builder/v1.xml` prompt and the `build_query_profile` tool. The tool result populates `QueryProfileResult`, which is then passed to `LoadProfileIntoUi()`. User reviews chips and hits Build Query.

`tool_choice`: always forced (`ForceToolName = "build_query_profile"`) — a plain text fallback has no use here. Model tier: `Balanced`. `EnableCaching = true`.

#### Phase 3 — Prompt Caching (`feature/prompt-caching`)
Set `EnableCaching = true` on all `NlProfileBuilderService` and `QueryReviewService` requests. Verify cache hits in the VS Output window. Document what breaks the cache in `prompts/nl_profile_builder/CHANGELOG.md`. No new UI — this is an API cost optimisation.

#### Phase 4 — Query Chip Suggestions (`feature/query-suggestions`)
`QuerySuggestionService` fires on a 300ms debounce from chip `TextBox` `TextChanged` events. Uses the `Fast` tier — no XML prompt structure needed, simple system message only. Deliberately test this task with extended thinking enabled on a Balanced/Smart tier model, measure the latency and token cost, confirm quality is unchanged, and document the result in `prompts/query_suggestions/CHANGELOG.md`. This is the concrete extended-thinking anti-pattern example.

#### Phase 5 — Query Review (`feature/query-review`)
A `Review Query` button near the query preview calls `QueryReviewService` with the assembled query string. Uses `Balanced` tier and `EnableCaching = true`. Response is plain JSON (no tool use) parsed into `QueryReviewResult` with `Issues[]` and `Suggestions[]`. UI shows a green label if clean, amber bullet list if issues found.

#### Phase 6 — Eval Pipeline (`feature/eval-pipeline`)
Install Promptfoo (`npm install -g promptfoo`). Build golden sets for `nl_profile_builder` and `query_review` (15–20 test cases each). Run `promptfoo eval` from `/prompts/` against both Anthropic and OpenAI providers. Establish a versioning discipline: always run evals before merging a prompt change, always record scores in `CHANGELOG.md`.

#### Phase 7 — Batch Profile Generation (`feature/batch-profiles`)
A `Bulk Describe` dialog accepts multiple plain English descriptions (one per line). `BatchProfileBuilderService` checks the active provider: Anthropic uses `/v1/messages/batches` (one HTTP call, polls until `processing_status === "ended"`); OpenAI/Gemini use `Task.WhenAll` for parallel async calls. Results shown in a list with `Apply to Profile` and `Save as New Profile` buttons per result.

---

### AI Integration — Always Do
- Call `ILlmProvider` from services — never instantiate a provider directly
- Load prompts from `/prompts/` via `PromptLoader` — never hardcode prompt text in a service
- Use `ModelTier` strings (`Fast`, `Balanced`, `Smart`) — never hardcode model IDs in services
- Set `EnableCaching = true` on any request whose system prompt does not change between calls
- Write a test using `InMemoryLlmProvider` for every new AI service method
- Run `promptfoo eval` before merging any change to a prompt file

### AI Integration — Never Do
- Inject dynamic user content into the system prompt block — it breaks caching
- Hardcode a model ID string (e.g. `"claude-sonnet-4-20250514"`) in a service class — use `ModelTier`
- Call a provider implementation directly from `MainForm` — always go through `ILlmProvider`
- Leave prompt text inline in C# — it belongs in `/prompts/*.xml`


*Last updated: 2026-04-06*  
*Maintained by: bigboybamo*
