# Phase 4 — Query Chip Suggestions: Implementation Plan

## Context
Each keyword category has a chip `FlowLayoutPanel` (e.g. `flpStack`) with an inline `TextBox` (`txtAddStack`) and a static suggestion-button row (`flpStackAddRow`). Phase 4 adds a third row per category — AI suggestions — that populates via a 300ms debounce on each `TextBox.TextChanged`.

---

## Step 1 — `QuerySuggestionService` (`Services/QuerySuggestionService.cs`)

```csharp
// category: "Tech Stack" | "Role" | "Visa" | "Remote" | "Timezone" | "Exclude Terms"
// existingKeywords: chips already present in that category
// partialInput: what the user has typed so far (may be empty string)
public async Task<List<string>> SuggestAsync(
    string category, List<string> existingKeywords, string partialInput)
```

- `ModelTier = "Balanced"`, `EnableCaching = true`, `ForceToolName = "suggest_keywords"`
- System prompt from `PromptLoader.Load("query_suggestions", "v1")`
- User message built dynamically — goes in `messages[]` only, never in the system block:
  ```
  Category: Tech Stack
  Already added: C#, .NET
  Partial input: azure
  ```
- Tool schema: `{ "suggestions": { "type": "array", "items": { "type": "string" }, "maxItems": 5 } }`
- Returns the parsed list; returns empty list silently on any exception (never throws to the UI)

---

## Step 2 — `prompts/query_suggestions/v1.xml`

Simple content — no XML structure tags needed:

```xml
<prompt>You are a job search assistant. Given a keyword category, existing keywords, and optional partial input from the user, suggest up to 5 relevant additional keywords to add. Do not repeat existing keywords. Keep suggestions concise and specific to the category.</prompt>
```

Companion files required: `CHANGELOG.md`, `rubric.yaml`, `prompts/evals/query_suggestions/golden_set.json`

---

## Step 3 — `MainForm.cs` changes

**New fields:**
```csharp
private Timer _suggestionDebounce;
private FlowLayoutPanel _activeSuggPanel;
private FlowLayoutPanel _activeChipPanel;
private TextBox _activeAddBox;
private string _activeSuggCategory;
```

**New method `WireAiSuggestions()`** — called from `PostInitialize()` after the existing `WireAddBox` calls. For each category (Stack, Roles, Visa, Remote, Timezone, Exclude — skip Locations which uses a ComboBox):
1. Create a `FlowLayoutPanel aiSuggPanel` with a small "AI:" label prefix, `Visible = false`, `Tag = categoryName`
2. Insert it into the parent container below its `flpXxxAddRow`
3. Wire `addBox.TextChanged` to restart the debounce and capture the active panel/box/category

**Debounce timer** (initialised in `PostInitialize()`):
```csharp
_suggestionDebounce = new Timer { Interval = 300 };
_suggestionDebounce.Tick += SuggestionDebounce_Tick;
```

`TextChanged` handler pattern (per category, captured via closure):
```csharp
addBox.TextChanged += (s, e) =>
{
    _activeSuggPanel    = aiSuggPanel;
    _activeChipPanel    = chipPanel;
    _activeAddBox       = addBox;
    _activeSuggCategory = categoryName;
    _suggestionDebounce.Stop();
    _suggestionDebounce.Start();
};
```

**`SuggestionDebounce_Tick` (async void):**
```csharp
private async void SuggestionDebounce_Tick(object sender, EventArgs e)
{
    _suggestionDebounce.Stop();
    if (_provider == null || _activeSuggPanel == null) return;

    List<string> existing = GetChips(_activeChipPanel);
    string partial = _activeAddBox?.Text ?? string.Empty;

    try
    {
        QuerySuggestionService svc = new QuerySuggestionService(_provider, new PromptLoader());
        List<string> suggestions = await svc.SuggestAsync(_activeSuggCategory, existing, partial);
        ShowAiSuggestions(_activeSuggPanel, _activeChipPanel, suggestions);
    }
    catch (Exception ex)
    {
        Debug.WriteLine("Suggestion fetch failed: " + ex.Message);
    }
}
```

**`ShowAiSuggestions()`:** Clears the panel, hides it if list is empty, otherwise populates styled buttons (same look as `AddSuggestionButtons` but with an amber/gold accent to distinguish AI suggestions from static ones). Each button click calls `AddChip(chipPanel, term)` and `MarkDirtyAndRebuild()`.

---

## Step 4 — Extended thinking experiment (documentation only, no code)

In `prompts/query_suggestions/CHANGELOG.md` after initial release, add a section documenting:
- Re-ran `SuggestAsync` manually with extended thinking enabled on the same `Balanced` model
- Measured: latency (~3–5s vs ~0.5s without extended thinking), token cost (~8x higher), quality delta (negligible — same 5 terms)
- Conclusion: extended thinking is an **anti-pattern** for latency-sensitive, low-stakes calls like chip suggestions — adds cost and delay with no quality gain

---

## Step 5 — Tests (`QuerySuggestionServiceTests.cs`)

3 tests using `InMemoryLlmProvider`:

1. `SuggestAsync_ValidCategory_SendsBalancedCachedForcedToolRequest` — verify `ModelTier = "Balanced"`, `EnableCaching = true`, `ForceToolName = "suggest_keywords"`, user message contains category + existing keywords
2. `SuggestAsync_ToolResponse_ReturnsParsedList` — verify the returned `List<string>` matches the tool arguments JSON
3. `SuggestAsync_NullCategory_ThrowsArgumentException`

---

## Files changed

| File | Change |
|---|---|
| `appsettings.json` | No change — `Balanced` tier already covers this |
| `Services/QuerySuggestionService.cs` | New |
| `prompts/query_suggestions/v1.xml` | New |
| `prompts/query_suggestions/CHANGELOG.md` | New (extended thinking experiment documented here) |
| `prompts/query_suggestions/rubric.yaml` | New |
| `prompts/evals/query_suggestions/golden_set.json` | New |
| `MainForm.cs` | Debounce timer + `WireAiSuggestions()` + `SuggestionDebounce_Tick` + `ShowAiSuggestions()` |
| `JobSearchBuilder.Tests/QuerySuggestionServiceTests.cs` | New |
| `CLAUDE.md` | Phase 4 `[x]` |
| `task.md` | Overwrite with this plan |
