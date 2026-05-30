# Phase 1 — Provider Abstraction: Implementation Plan

## Goal
Build `ILlmProvider`, three provider implementations (Anthropic, OpenAI, Gemini),
`LlmProviderFactory`, request/response DTOs, and wire everything into `MainForm`.
Add `InMemoryLlmProvider` test double and `AnthropicProviderTests`.

---

## Files to Create

### Models (JobSearchBuilder/Models/)

#### `LlmRequest.cs`
```csharp
public class LlmRequest
{
    public string SystemPrompt { get; set; }
    public string UserMessage { get; set; }
    public List<LlmToolDefinition> Tools { get; set; }
    public string ForceToolName { get; set; }
    public string ModelTier { get; set; }      // "Fast" | "Balanced" | "Smart"
    public bool EnableCaching { get; set; }
}
```

#### `LlmResponse.cs`
```csharp
public class LlmResponse
{
    public string TextContent { get; set; }
    public string ToolCallName { get; set; }
    public string ToolCallArguments { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheReadTokens { get; set; }
    public int CacheWriteTokens { get; set; }
}
```

#### `LlmToolDefinition.cs`
```csharp
public class LlmToolDefinition
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string InputSchema { get; set; }    // raw JSON string
}
```

#### `QueryProfileResult.cs`
```csharp
public class QueryProfileResult
{
    public string Role { get; set; }
    public string Seniority { get; set; }
    public List<string> TechStack { get; set; }
    public List<string> RemoteTerms { get; set; }
    public List<string> TimezoneTerms { get; set; }
    public List<string> ExcludeTerms { get; set; }
}
```

---

### Interfaces (JobSearchBuilder/Interfaces/)

#### `ILlmProvider.cs`
```csharp
public interface ILlmProvider
{
    string ProviderName { get; }
    string ModelId { get; }
    Task<LlmResponse> SendAsync(LlmRequest request);
}
```

---

### Services/Providers/ (new folder)

#### `AnthropicProvider.cs`
- POST to `https://api.anthropic.com/v1/messages`
- Headers: `x-api-key`, `anthropic-version: 2023-06-01`, `anthropic-beta: prompt-caching-2024-07-31`
- System prompt sent as array `[{"type":"text","text":"..."}]` — NOT a plain string
- When `EnableCaching = true`, append `"cache_control": {"type":"ephemeral"}` to the system block
- Tool choice forced: `{"type":"tool","name":"..."}`
- Response: parse `content[]` for `type=text` -> `TextContent`, `type=tool_use` -> `ToolCallName` + `ToolCallArguments`
- Usage: `input_tokens`, `output_tokens`, `cache_read_input_tokens` -> `CacheReadTokens`, `cache_creation_input_tokens` -> `CacheWriteTokens`
- `Debug.WriteLine` cache hit when `CacheReadTokens > 0`
- Constructor accepts optional `HttpMessageHandler handler = null` for test injection
- `ModelId` returns `_settings.GetModelId("Balanced")`

#### `OpenAiProvider.cs`
- POST to `https://api.openai.com/v1/chat/completions`
- Header: `Authorization: Bearer {apiKey}`
- System prompt goes in `messages[0]` with `"role":"system"`
- Tool choice forced: `{"type":"function","function":{"name":"..."}}`
- Schema key is `"parameters"` (not `"input_schema"`)
- Response: `choices[0].message.content` -> `TextContent`, `choices[0].message.tool_calls[0].function` -> name/arguments
- Usage: `prompt_tokens` -> `InputTokens`, `completion_tokens` -> `OutputTokens`
- `CacheReadTokens` / `CacheWriteTokens` always 0
- Constructor accepts optional `HttpMessageHandler handler = null`

#### `GeminiProvider.cs`
- POST to `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}`
- No auth header — API key is a query parameter
- System prompt: `"system_instruction": {"parts": [{"text":"..."}]}`
- Tools: `"tools": [{"functionDeclarations": [...]}]` — NOT top-level `tools[]`
- Tool config forced: `{"function_calling_config": {"mode":"ANY","allowed_function_names":["..."]}}`
- Schema key is `"parameters"` (same shape as OpenAI)
- Response: `candidates[0].content.parts[]` — check for `text` or `functionCall`
- Usage: `usageMetadata.promptTokenCount` -> `InputTokens`, `candidatesTokenCount` -> `OutputTokens`
- Constructor accepts optional `HttpMessageHandler handler = null`

#### `LlmProviderFactory.cs`
```csharp
public static class LlmProviderFactory
{
    // providerOverride: if supplied, ignores settings.Ai.Provider and uses this name instead.
    // This is what the UI dropdown calls when the user switches providers at runtime.
    public static ILlmProvider Create(AppSettings settings, string providerOverride = null)
    // throws InvalidOperationException if Ai section missing or provider unknown
}
```

---

### Services/ (existing folder)

#### `InMemoryLlmProvider.cs`
Test double — same pattern as `InMemoryProfileStore`:
```csharp
public class InMemoryLlmProvider : ILlmProvider
{
    public string ProviderName => "InMemory";
    public string ModelId => "test-model";
    public LlmResponse NextResponse { get; set; }
    public LlmRequest LastRequest { get; private set; }
    public Task<LlmResponse> SendAsync(LlmRequest request) { ... }
}
```

---

## Files to Modify

### `Services/AppSettingsLoader.cs`

**Add `AiSettings` class** (same file, below `AppSettings`):
```csharp
public class AiSettings
{
    public string Provider { get; set; }
    public Dictionary<string, string> Models { get; set; }
    public Dictionary<string, string> ApiKeys { get; set; }

    public string GetModelId(string tier) { ... }    // returns "" if not found
    public string GetApiKey(string provider) { ... } // returns "" if not found
}
```

**Add to `AppSettings`:**
```csharp
public AiSettings Ai { get; set; }
```

**In `AppSettingsLoader.Load()`:**
1. Extend local-settings merge to also overlay `Ai.ApiKeys` from `appsettings.local.json`
2. Parse the `Ai` section: `Provider`, `Models` dict, `ApiKeys` dict

---

### `appsettings.json`

Add the `Ai` block at the top level — no API keys here:
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

API keys are read from environment variables at startup:
- `ANTHROPIC_API_KEY`
- `OPENAI_API_KEY`
- `GEMINI_API_KEY`

Only the key for the active provider needs to be set.

---

### `MainForm.cs`

**Add fields** (`_provider` is NOT readonly — it is reassigned when the user switches):
```csharp
private ILlmProvider _provider;
private Label _lblModelId;           // updated whenever _provider changes
```

**Add usings:**
```csharp
using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Services.Providers;
```

**In constructor**, after `_queryBuilder = ...`:
```csharp
try
{
    _provider = LlmProviderFactory.Create(_config);
}
catch (Exception ex)
{
    Debug.WriteLine("AI provider init failed: " + ex.Message);
    _provider = null;
}
```

**In `PostInitialize()`**, at the end — replace the single status label with a footer panel
containing a provider dropdown:
```
┌────────────────────────────────────────────────────────────────────────────┐
│  Provider:  [ Anthropic ▼ ]   claude-sonnet-4-20250514          (right-pad)│
└────────────────────────────────────────────────────────────────────────────┘
```

```csharp
Panel pnlFooter = new Panel
{
    Dock = DockStyle.Bottom,
    Height = 28,
    BackColor = Color.FromArgb(240, 242, 248),
    Padding = new Padding(8, 0, 12, 0)
};

Label lblProviderCaption = new Label
{
    Text = "Provider:",
    AutoSize = true,
    Font = new Font("Segoe UI", 8f),
    ForeColor = Color.FromArgb(100, 100, 120),
    TextAlign = ContentAlignment.MiddleLeft
};
lblProviderCaption.Top = (pnlFooter.Height - lblProviderCaption.PreferredHeight) / 2;
lblProviderCaption.Left = 8;

ComboBox cboProvider = new ComboBox
{
    DropDownStyle = ComboBoxStyle.DropDownList,
    Font = new Font("Segoe UI", 8f),
    Width = 110
};
cboProvider.Items.AddRange(new object[] { "Anthropic", "OpenAI", "Gemini" });
// Select whichever provider is currently active
int providerIdx = cboProvider.Items.IndexOf(_provider != null ? _provider.ProviderName : "Anthropic");
cboProvider.SelectedIndex = providerIdx >= 0 ? providerIdx : 0;
cboProvider.Top = (pnlFooter.Height - cboProvider.Height) / 2;
cboProvider.Left = lblProviderCaption.Left + lblProviderCaption.PreferredWidth + 4;

_lblModelId = new Label
{
    AutoSize = true,
    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
    ForeColor = Color.FromArgb(100, 100, 120),
    Text = _provider != null ? _provider.ModelId : string.Empty
};
_lblModelId.Top = (pnlFooter.Height - _lblModelId.PreferredHeight) / 2;
_lblModelId.Left = cboProvider.Left + cboProvider.Width + 10;

cboProvider.SelectedIndexChanged += (s, e) =>
{
    string selected = cboProvider.SelectedItem.ToString();
    try
    {
        _provider = LlmProviderFactory.Create(_config, selected);
        _lblModelId.Text = _provider.ModelId;
    }
    catch (Exception ex)
    {
        Debug.WriteLine("Provider switch failed: " + ex.Message);
        _provider = null;
        _lblModelId.Text = "unavailable";
    }
};

pnlFooter.Controls.Add(lblProviderCaption);
pnlFooter.Controls.Add(cboProvider);
pnlFooter.Controls.Add(_lblModelId);
this.Controls.Add(pnlFooter);
// Added after tblMain (DockStyle.Fill) — WinForms docks Bottom before Fill
```

**Why `_provider` is not readonly:** The user may switch providers multiple times in one session.
All API keys for all three providers are loaded at startup from `appsettings.local.json`, so no
file I/O is needed on switch — only a new provider object is instantiated.

---

### `JobSearchBuilder.csproj`

Add `<Compile Include="...">` entries inside the existing `<ItemGroup>` with the other Compile entries:
```xml
<Compile Include="Interfaces\ILlmProvider.cs" />
<Compile Include="Models\LlmRequest.cs" />
<Compile Include="Models\LlmResponse.cs" />
<Compile Include="Models\LlmToolDefinition.cs" />
<Compile Include="Models\QueryProfileResult.cs" />
<Compile Include="Services\InMemoryLlmProvider.cs" />
<Compile Include="Services\Providers\AnthropicProvider.cs" />
<Compile Include="Services\Providers\GeminiProvider.cs" />
<Compile Include="Services\Providers\LlmProviderFactory.cs" />
<Compile Include="Services\Providers\OpenAiProvider.cs" />
```

---

## Files to Create (Tests)

### `JobSearchBuilder.Tests/AnthropicProviderTests.cs`

Uses two internal fake handlers (no real HTTP):
- `FakeHandler` — returns canned JSON with HTTP 200
- `FakeCapturingHandler` — same, but also captures the raw request body for assertion

**Test cases:**
1. `SendAsync_SimpleTextRequest_ReturnsTextContent`
2. `SendAsync_ToolUseResponse_ReturnsToolCallDetails`
3. `SendAsync_CacheHit_ReportsCacheReadTokens`
4. `SendAsync_EnableCaching_IncludesCacheControlInRequest`
5. `SendAsync_DisabledCaching_OmitsCacheControl`

---

## Implementation Order

1. Models — LlmRequest, LlmResponse, LlmToolDefinition, QueryProfileResult
2. ILlmProvider interface
3. AppSettingsLoader — AiSettings class + Ai parsing + local merge
4. appsettings.json — add Ai block
5. AnthropicProvider, OpenAiProvider, GeminiProvider
6. LlmProviderFactory
7. InMemoryLlmProvider
8. MainForm.cs — field, constructor wiring, PostInitialize status label
9. JobSearchBuilder.csproj — add Compile entries
10. AnthropicProviderTests.cs

---

## Key Constraints (from CLAUDE.md)

- Block namespaces only — no file-scoped `namespace X;` syntax
- No nullable reference type annotations
- No `ArgumentNullException.ThrowIfNull` — manual null checks
- Use `Newtonsoft.Json` (JObject/JArray) for all JSON — NOT System.Text.Json
- `async Task` on provider `SendAsync`; `.ConfigureAwait(false)` on all awaits
- `Debug.WriteLine` for diagnostics — never `Console.WriteLine`
- All three providers map to the same `LlmResponse` shape
- `CacheReadTokens` / `CacheWriteTokens` are 0 on non-Anthropic providers
- Private fields: `_` prefix + camelCase
