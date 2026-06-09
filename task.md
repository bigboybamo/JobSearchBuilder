# Phase 7 — Batch Profile Generation

**Branch:** `feature/batch-profiles`

---

## Goal

Add a "Bulk Describe" button that accepts multiple plain-English role descriptions (one per line), calls the LLM in the most efficient way for the active provider, then shows a results dialog where the user can apply or save each result as a new profile.

---

## Files to Create

### 1. `JobSearchBuilder/Models/BatchProfileResult.cs` ✅ DONE

Plain data bag: `Description`, `Profile` (QueryProfileResult), `IsError`, `ErrorMessage`.

---

### 2. `JobSearchBuilder/Services/BatchProfileBuilderService.cs`

**Constructor:**
```
BatchProfileBuilderService(
    ILlmProvider provider,
    PromptLoader promptLoader,
    AppSettings settings,
    HttpMessageHandler handler = null,
    int pollIntervalMs = 5000)
```
- `handler` injected for testability (same pattern as `AnthropicProvider`)
- `pollIntervalMs` defaults to 5000; pass 0 in tests to skip actual delay

**Public method:**
```
Task<List<BatchProfileResult>> BuildBatchAsync(IList<string> descriptions)
```
- Validates: throws `ArgumentNullException` if null, returns empty list if empty
- Branches on `_provider.ProviderName == "Anthropic"`:
  - `"Anthropic"` → `BuildWithAnthropicBatchAsync`
  - Anything else → `BuildInParallelAsync`

**`BuildInParallelAsync`:**
- Shares one `NlProfileBuilderService` instance across all tasks
- Launches `Task<BatchProfileResult>` per description via `BuildSingleSafeAsync`
- `BuildSingleSafeAsync` wraps `NlProfileBuilderService.BuildAsync` in try/catch; returns `IsError=true` on any exception
- Awaits `Task.WhenAll`, returns ordered list

**`BuildWithAnthropicBatchAsync`:**

Step 1 — Submit batch (`POST /v1/messages/batches`):
- Headers: `x-api-key`, `anthropic-version: 2023-06-01`, `anthropic-beta: message-batches-2024-09-24`
- Body:
  ```json
  {
    "requests": [
      {
        "custom_id": "0",
        "params": {
          "model": "<from settings>",
          "max_tokens": 2048,
          "system": [{ "type": "text", "text": "<prompt>", "cache_control": { "type": "ephemeral" } }],
          "messages": [{ "role": "user", "content": "<description>" }],
          "tools": [{ "name": "build_query_profile", "description": "...", "input_schema": {...} }],
          "tool_choice": { "type": "tool", "name": "build_query_profile" }
        }
      }
    ]
  }
  ```
- Parse `id` from response; throw if missing

Step 2 — Poll until ended:
- `GET /v1/messages/batches/{id}` in loop with `await Task.Delay(_pollIntervalMs)`
- Break when `processing_status == "ended"`

Step 3 — Fetch JSONL results:
- `GET /v1/messages/batches/{id}/results`
- Pass content string to `ParseBatchResults(descriptions, jsonlContent)`

**`ParseBatchResults` (private static):**
- Split JSONL on `\n`, skip blank/malformed lines
- Parse `custom_id` as int index; parse `result.type`
  - `"succeeded"`: walk `result.message.content[]`, find first `type=="tool_use"` block, serialize `input` → `ParseProfile`
  - `"errored"`: `IsError=true`, message from `result.error.message`
- Build ordered list indexed 0..N; any missing index → `IsError=true, ErrorMessage="No result returned."`

**Private static helpers (duplicated from `NlProfileBuilderService` — acceptable, keeps services decoupled):**
- `ParseProfile(string argumentsJson)` → `QueryProfileResult`
- `ReadStringList(JObject root, string key)` → `List<string>`
- `GetToolSchema()` → same JSON string literal

---

### 3. `JobSearchBuilder.Tests/BatchProfileBuilderServiceTests.cs`

Uses `[TestFixture]`, `[SetUp]`, `[TearDown]` — same temp-dir pattern as `NlProfileBuilderServiceTests`.

**SetUp:**
- Create temp prompt dir with `nl_profile_builder/v2.xml` stub
- `InMemoryLlmProvider` with valid `build_query_profile` tool-call response
- `AppSettings` with Anthropic config: api key `"test-key"`, model `"claude-sonnet-4-5-20250929"`

**Parallel-path tests (ProviderName == "InMemory" ≠ "Anthropic"):**

| Test | Scenario | Assert |
|---|---|---|
| `BuildBatchAsync_NullDescriptions_ThrowsArgumentNullException` | null | `ThrowsAsync<ArgumentNullException>` |
| `BuildBatchAsync_EmptyList_ReturnsEmptyList` | `[]` | `Count == 0` |
| `BuildBatchAsync_TwoDescriptions_ReturnsTwoResults` | 2 descriptions | `Count == 2`, neither `IsError` |
| `BuildBatchAsync_ValidResponse_ParsesProfileCorrectly` | 1 description | `Role=="Developer"`, `Seniority=="Senior"`, `TechStack==["C#",".NET"]` |
| `BuildBatchAsync_ProviderReturnsWrongTool_WrapsErrorInResult` | `NextResponse` has no `ToolCallName` | `results[0].IsError==true`, `ErrorMessage` not empty |

**Anthropic-path tests — inner class `FakeBatchHandler : HttpMessageHandler`:**

Handler logic (decides response by HTTP method + URL):
- `POST` → capture `LastBatchRequestBody`, return `{"id":"msgbatch_test","processing_status":"in_progress"}`
- `GET` not ending in `/results` → return `{"processing_status":"ended"}`
- `GET` ending in `/results` → return the JSONL string supplied at construction

Use `AnthropicProvider(settings, new FakeNeverCalledHandler())` as `ILlmProvider` so `ProviderName=="Anthropic"` triggers the batch code path. `FakeNeverCalledHandler` throws `InvalidOperationException` if `SendAsync` is ever called (it shouldn't be in the batch path).

All Anthropic tests pass `pollIntervalMs: 0` to avoid real delay.

| Test | Scenario | Assert |
|---|---|---|
| `BuildBatchAsync_AnthropicProvider_PostsBatchWithCorrectRequestCount` | 2 descriptions | body contains `"custom_id":"0"` and `"custom_id":"1"` |
| `BuildBatchAsync_AnthropicProvider_IncludesCorrectModel` | 1 description | body contains `"claude-sonnet-4-5-20250929"` |
| `BuildBatchAsync_AnthropicProvider_ForcesBuildQueryProfileTool` | 1 description | body contains `"build_query_profile"` and `"type":"tool"` |
| `BuildBatchAsync_AnthropicProvider_ParsesSucceededJsonlResult` | JSONL with succeeded entry | `IsError==false`, `Profile.Role=="Engineer"` |
| `BuildBatchAsync_AnthropicProvider_ParsesErroredJsonlResult` | JSONL with errored entry | `IsError==true`, `ErrorMessage` contains error text |
| `BuildBatchAsync_AnthropicProvider_HandlesGapInResults` | JSONL missing index 1 (2 descriptions submitted) | `results[1].IsError==true` |

---

## Files to Modify

### 4. `JobSearchBuilder/JobSearchBuilder.csproj`

Add two `<Compile>` entries inside the existing `<ItemGroup>` that lists source files (after `Models\QueryReviewResult.cs` and `Services\NlProfileBuilderService.cs`):

```xml
<Compile Include="Models\BatchProfileResult.cs" />
<Compile Include="Services\BatchProfileBuilderService.cs" />
```

---

### 5. `JobSearchBuilder/MainForm.cs`

**In `PostInitialize()`** — add one call directly after `AddDescribeRoleButton()`:
```csharp
AddBulkDescribeButton();
```

**`AddBulkDescribeButton()` private method:**
- Same visual style as `AddDescribeRoleButton` (purple BackColor, white bold text, FlatStyle.Flat)
- `Text = "Bulk Describe"`, `Size = (126, 28)`, `Anchor = Top | Right`
- `Location = new Point(btnSaveProfile.Left - 266, btnSaveProfile.Top)`
  - Calculation: 132 (space for Describe Role) + 126 (Bulk Describe width) + 8 (gap) = 266
- Add to `pnlEditor.Controls`, bring to front
- Wire `Click += btnBulkDescribe_Click`

**`btnBulkDescribe_Click` — `async void`:**
```
1. Guard: if _provider == null → MessageBox warning, return
2. string raw = ShowBulkDescribeInputDialog()
3. If null or whitespace → return
4. Split raw on '\n', trim each, filter blank → List<string> descriptions
5. If descriptions.Count == 0 → return
6. Disable button, set Cursor.Current = Cursors.WaitCursor
7. try:
     var service = new BatchProfileBuilderService(_provider, _promptLoader, _config)
     var results = await service.BuildBatchAsync(descriptions)
     ShowBatchResultsDialog(results)
   catch (Exception ex):
     MessageBox error
   finally:
     restore cursor, re-enable button
```

**`ShowBulkDescribeInputDialog()` — `private static string`:**
- Inline `Form` in `using` block (same pattern as `ShowRoleDescriptionDialog`)
- `ClientSize = (520, 340)`, FixedDialog, no maximize/minimize
- Label: "Enter one role description per line:"
- Multiline `TextBox`, size `(496, 252)`, vertical scroll bars
- "Build Profiles" OK button + "Cancel" button
- Returns `txtDescriptions.Text` or `null` on cancel/empty

**`ShowBatchResultsDialog(List<BatchProfileResult> results)` — `private void`:**
- Inline `Form` in `using`, `Size = (700, 560)`, `Sizable`, `MinimumSize = (560, 340)`
- `Text = "Bulk Build Results — " + results.Count + " description(s)"`

Footer (Dock=Bottom, 44px, light grey):
- Left: summary label `"{successCount} built, {errorCount} failed"`
- Right: "Close" button (`DialogResult = Cancel`)

Scroll area (Dock=Fill, AutoScroll=true, Padding=8):
- Contains `FlowLayoutPanel` (TopDown, AutoSize, Width=656)
- Per result: `CreateBatchResultRow(result)` → appended to flow

Show with `dialog.ShowDialog(this)`.

**`CreateBatchResultRow(BatchProfileResult result)` — `private Panel`:**
- `Width = 652`, `Height = 88`, `Margin = Padding(0,0,0,4)`
- `BackColor`: error → `FromArgb(255, 248, 248)`, success → `FromArgb(248, 250, 255)`
- `Paint` event draws 1px border (light red for error, light blue-grey for success)

Inside the row:

*Description label* (top-left, italic 9pt, Width=472, truncated):
```csharp
string desc = result.Description.Length > 80 ? result.Description.Substring(0, 80) + "..." : result.Description;
```

*Profile / error label* (below description, 8.5pt, Width=472):
- Success: `"{Seniority} {Role} · {string.Join(", ", TechStack.Take(3))}"` in blue
- Error: `"Error: {ErrorMessage}"` in red

*Action buttons* (right side, success only):
- "Apply" button at `(498, 10)`, size `(70, 26)` — calls `ApplyQueryProfileResult(result.Profile, false)` (dialog stays open)
- "Save New" button at `(498, 44)`, size `(78, 26)` — calls `ApplyQueryProfileResult(result.Profile, true)`
- Both: FlatStyle, no border, white text; Apply=blue, Save=green

---

## Implementation Order

1. ✅ `BatchProfileResult.cs`
2. `BatchProfileBuilderService.cs`
3. `JobSearchBuilder.csproj` — add 2 `<Compile>` entries
4. `MainForm.cs` — button + click handler + 2 dialog methods + row helper
5. `BatchProfileBuilderServiceTests.cs`
6. `dotnet test JobSearchBuilder.Tests/JobSearchBuilder.Tests.csproj` — all pass

---

## Key Constraints (from CLAUDE.md)

- No new interfaces — `BatchProfileBuilderService` checks `_provider.ProviderName` directly
- .NET Framework 4.8 — no records, no file-scoped namespaces, no `ThrowIfNull`
- `async void` for WinForms event handlers
- `Newtonsoft.Json` for all JSON — not `System.Text.Json`
- `InMemoryLlmProvider` for parallel-path tests; `FakeBatchHandler` for Anthropic-path tests
- Prompt loaded via `PromptLoader.Load("nl_profile_builder", "v2")` — not hardcoded
- No hardcoded model IDs in service code — always `_settings.Ai.GetModelId("Anthropic", "Balanced")`
