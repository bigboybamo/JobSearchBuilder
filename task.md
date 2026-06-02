# Phase 5 — Query Review: Implementation Plan

## Context
`pnlPreview` is docked Bottom inside `pnlEditor`. It contains `lblPreviewHeader` (Top), `txtQueryPreview` (Fill), and `flpPreviewButtons` (Bottom, right-to-left). A review result panel added with `DockStyle.Bottom` in `PostInitialize` slots naturally between `txtQueryPreview` and `flpPreviewButtons`. WinForms skips hidden controls during docking, so the panel is invisible until a review runs with no layout side effects.

Phase 5 also completes the deferred Phase 3 item — `EnableCaching = true` on `QueryReviewService`.

---

## Step 1 — `QueryReviewResult` (`Models/QueryReviewResult.cs`)

```csharp
public class QueryReviewResult
{
    public List<string> Issues { get; set; }
    public List<string> Suggestions { get; set; }

    public QueryReviewResult()
    {
        Issues = new List<string>();
        Suggestions = new List<string>();
    }
}
```

---

## Step 2 — `QueryReviewService` (`Services/QueryReviewService.cs`)

```csharp
public async Task<QueryReviewResult> ReviewAsync(string query)
```

- Throws `ArgumentException` for null/whitespace query
- `ModelTier = "Balanced"`, `EnableCaching = true`
- **No tool use** — plain JSON parsed from `response.TextContent`
- No `ForceToolName`, no `Tools` list
- System prompt from `PromptLoader.Load("query_review", "v1")`
- User message: the raw query string (never in the system block)
- Returns `new QueryReviewResult()` (empty lists) on any exception — never throws to the UI

**Expected JSON shape from model:**
```json
{ "issues": ["...", "..."], "suggestions": ["...", "..."] }
```

**Markdown fence stripping** — models sometimes wrap JSON in ```json ```; strip before parsing:
```csharp
private static string StripMarkdownFence(string text)
{
    string trimmed = (text ?? string.Empty).Trim();
    if (!trimmed.StartsWith("```")) return trimmed;
    int newline = trimmed.IndexOf('\n');
    if (newline >= 0) trimmed = trimmed.Substring(newline + 1);
    if (trimmed.EndsWith("```"))
        trimmed = trimmed.Substring(0, trimmed.Length - 3).TrimEnd();
    return trimmed;
}
```

---

## Step 3 — `prompts/query_review/v1.xml`

Full XML structure with `<instructions>`, `<context>`, `<output_format>`, `<constraints>`. Instructs the model to:
- Analyse the Google search query for potential problems (URL length, conflicting terms, overly broad terms, missing operators)
- Return **only** raw JSON — no markdown, no prose
- `issues`: concrete problems with the query
- `suggestions`: actionable improvements

Companion files: `CHANGELOG.md`, `rubric.yaml`, `prompts/evals/query_review/golden_set.json`

---

## Step 4 — `MainForm.cs` changes

**New fields:**
```csharp
private Panel _pnlReviewResult;
private Button _btnReviewQuery;
```

**`PostInitialize()` — add after footer panel setup:**

```csharp
// Review result panel — docked Bottom inside pnlPreview, hidden until review runs
_pnlReviewResult = new Panel
{
    Dock = DockStyle.Bottom,
    Height = 36,
    BackColor = Color.FromArgb(30, 30, 45),
    Padding = new Padding(12, 4, 12, 4),
    Visible = false
};
pnlPreview.Controls.Add(_pnlReviewResult);

// Review Query button — added to flpPreviewButtons (right-to-left, appears leftmost)
_btnReviewQuery = new Button
{
    Text = "Review Query",
    BackColor = Color.FromArgb(50, 100, 55),
    ForeColor = Color.White,
    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
    FlatStyle = FlatStyle.Flat,
    Cursor = Cursors.Hand,
    Size = new Size(120, 28),
    Margin = new Padding(0, 0, 6, 0)
};
_btnReviewQuery.Click += btnReviewQuery_Click;
flpPreviewButtons.Controls.Add(_btnReviewQuery);
```

**`btnReviewQuery_Click` (async void):**
```csharp
private async void btnReviewQuery_Click(object sender, EventArgs e)
{
    if (_provider == null)
    {
        MessageBox.Show("AI provider is unavailable. Check your provider settings and API key.",
            "Review Query", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }
    if (_lastQueryResult == null || string.IsNullOrWhiteSpace(_lastQueryResult.RawQuery))
        return;

    _btnReviewQuery.Enabled = false;
    Cursor previousCursor = Cursor.Current;
    Cursor.Current = Cursors.WaitCursor;
    try
    {
        QueryReviewService svc = new QueryReviewService(_provider, _promptLoader);
        QueryReviewResult result = await svc.ReviewAsync(_lastQueryResult.RawQuery);
        ShowReviewResult(result);
    }
    catch (Exception ex)
    {
        Debug.WriteLine("Query review failed: " + ex.Message);
    }
    finally
    {
        Cursor.Current = previousCursor;
        _btnReviewQuery.Enabled = true;
    }
}
```

**`ShowReviewResult(QueryReviewResult result)`:**
- Clears `_pnlReviewResult.Controls`
- If `Issues.Count == 0`: dark green background, single "Query looks good" label in green
- Otherwise: dark amber background, one label per issue (amber text) + one label per suggestion (dimmer text), panel height expands to fit content
- Sets `_pnlReviewResult.Visible = true`

**`HideReviewResult()`:**
```csharp
private void HideReviewResult()
{
    if (_pnlReviewResult != null)
        _pnlReviewResult.Visible = false;
}
```

**`RebuildQuery()` modification** — add `HideReviewResult()` at the top so any stale result is cleared whenever the query changes:
```csharp
private void RebuildQuery()
{
    HideReviewResult();
    // ... existing logic
}
```

---

## Step 5 — Tests (`QueryReviewServiceTests.cs`)

5 tests using `InMemoryLlmProvider`:

1. `ReviewAsync_ValidQuery_SendsBalancedCachedRequest` — verify `ModelTier = "Balanced"`, `EnableCaching = true`, no `ForceToolName`, no `Tools`, user message equals the query string
2. `ReviewAsync_CleanResponse_ReturnsEmptyIssues` — `{"issues":[],"suggestions":[]}` → both lists empty
3. `ReviewAsync_IssuesResponse_ReturnsPopulatedLists` — issues and suggestions arrays mapped correctly
4. `ReviewAsync_MarkdownFencedJson_ParsesCorrectly` — ```json\n{...}\n``` stripped before parse
5. `ReviewAsync_NullQuery_ThrowsArgumentException`

---

## Files changed

| File | Change |
|---|---|
| `Models/QueryReviewResult.cs` | New |
| `Services/QueryReviewService.cs` | New |
| `prompts/query_review/v1.xml` | New |
| `prompts/query_review/CHANGELOG.md` | New |
| `prompts/query_review/rubric.yaml` | New |
| `prompts/evals/query_review/golden_set.json` | New |
| `MainForm.cs` | `_btnReviewQuery`, `_pnlReviewResult`, `btnReviewQuery_Click`, `ShowReviewResult`, `HideReviewResult`, `RebuildQuery` stale-result clear |
| `JobSearchBuilder.csproj` | Add Compile entries for new model + service |
| `JobSearchBuilder.Tests/QueryReviewServiceTests.cs` | New |
| `CLAUDE.md` | Phase 5 `[x]` |
| `task.md` | Overwrite with this plan |
