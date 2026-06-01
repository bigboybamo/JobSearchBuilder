# Phase 3 — Prompt Caching: Implementation Plan

## Current State

| Item | Status |
|---|---|
| `NlProfileBuilderService` — `EnableCaching = true` | Done (Phase 2) |
| `AnthropicProvider` — `cache_control` + beta header | Done (Phase 2) |
| `AnthropicProvider` — parse `CacheReadTokens` / `CacheWriteTokens` | Done |
| `Debug.WriteLine` for cache hits | Done (`AnthropicProvider.cs:133`) |
| `Debug.WriteLine` for cache writes | Missing |
| `QueryReviewService` — `EnableCaching = true` | Deferred — service doesn't exist until Phase 5 |
| CHANGELOG: what breaks the cache | Missing |

Phase 3 is small — caching was already wired in Phase 2. Two deliverables remain.

---

## Step 1 — Add cache write logging (`AnthropicProvider.cs`)

`AnthropicProvider.cs:133` already logs hits. Add a parallel `Debug.WriteLine` for writes directly below it:

```csharp
if (mapped.CacheReadTokens > 0)
    Debug.WriteLine("Anthropic cache hit tokens: " + mapped.CacheReadTokens);
if (mapped.CacheWriteTokens > 0)
    Debug.WriteLine("Anthropic cache write tokens: " + mapped.CacheWriteTokens);
```

---

## Step 2 — Document cache behaviour (`prompts/nl_profile_builder/CHANGELOG.md`)

Add a section that records:

- What activates the cache: stable system prompt XML + `EnableCaching = true` on the request
- What **breaks** the cache: any byte-level change to the system block — editing `v1.xml`, changing provider, toggling `EnableCaching`
- That dynamic content (user description) always goes in `messages[]`, never in the system block
- That `QueryReviewService` caching is deferred to Phase 5
- Token savings observed on first run (placeholder until tested)

---

## Step 3 — Update CLAUDE.md and create PR

- Mark Phase 3 `[x]` in `CLAUDE.md`
- Branch: `feature/prompt-caching`
- One commit: two files changed (`AnthropicProvider.cs` + `prompts/nl_profile_builder/CHANGELOG.md`) + `CLAUDE.md`
- PR targeting `master`

---

## Out of Scope

- `QueryReviewService` — built in Phase 5; `EnableCaching = true` will be set there
- OpenAI / Gemini — they already silently ignore `EnableCaching`; no change needed
- New tests — existing `AnthropicProviderTests.cs` "caching on / caching off" tests already cover this path
