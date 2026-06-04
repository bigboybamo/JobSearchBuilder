# nl_profile_builder Prompt Changelog

## Phase 6 — Eval Pipeline Baseline (2026-06-04)

**Eval run:** `promptfoo eval` against `evals/nl_profile_builder/golden_set.json` (20 test cases)

| Provider | Model | Pass | Fail | Score |
|---|---|---|---|---|
| Anthropic | claude-sonnet-4-5-20250929 | 16 | 4 | **80%** |
| OpenAI | gpt-4.1-mini | 11 | 9 | **55%** |
| **Overall** | | **27** | **13** | **67.5%** |

**Anthropic failures (4):**
- `Staff engineer` — inferred `remote_terms: ["remote"]` even though user only said "no on-site", not "remote"
- `C# backend developer` — missed `C#` in `tech_stack` (it was in the role description, not listed separately)
- `Senior software architect` — invented `remote_terms: ["remote", "WFH"]` when user specified no preference
- `Engineering manager` — returned `seniority: "Manager"` instead of `"Any"` (Manager is a role title, not a seniority level)

**OpenAI failures (9):**
- `Junior React developer` — role not normalised; exclusion phrasing truncated
- `Python data engineer` — put `"data engineer"` in `tech_stack`; kept `"US timezones"` literal instead of expanding to EST/CST/PST
- `Senior iOS developer` — excluded `"visa sponsorship"` instead of `"visa sponsorship required"`
- `Staff engineer` — returned `seniority: "Senior"` instead of `"Staff"`; invented `remote_terms`
- `C# backend developer` — `tech_stack` only contained `"Azure"`, missing `"C#"`
- `Senior software architect` — invented `remote_terms` (same issue as Anthropic)
- `Mid-level mobile developer` — put `"hybrid"` in `tech_stack` instead of `remote_terms`
- `Engineering manager` — `seniority: "Manager"` instead of `"Any"` (same issue as Anthropic)
- `Senior ML engineer` — truncated exclusion to `"PhD"` instead of `"PhD required"`

**Key findings:**
- Anthropic substantially outperforms OpenAI on field normalisation and category mapping
- Both models mishandle `Engineering manager` seniority — prompt needs to clarify that management titles are not seniority levels
- Both models invent `remote_terms` when the user implies remote via an exclusion ("no on-site") rather than stating it directly — prompt constraint needs tightening
- OpenAI more often puts values in the wrong category field (e.g. work arrangement in tech_stack)

**This is the v1 baseline. Any future prompt change must be evaluated against this score before merging.**

## Phase 3 - Prompt Caching
- Anthropic prompt caching is active when the stable system prompt XML is sent with `EnableCaching = true`.
- The cache is tied to the exact system block bytes; editing `v1.xml`, switching providers, or toggling `EnableCaching` prevents reuse of the cached block.
- Dynamic user descriptions stay in `messages[]` and must not be injected into the system prompt, otherwise each request becomes a new cache entry.
- `QueryReviewService` caching is deferred to Phase 5 because that service does not exist yet.
- Token savings observed: pending manual provider run; first request writes cache tokens, repeated matching requests should report cache read tokens in the VS Output window.

## v1
- Initial structured prompt for the Phase 2 Describe Role workflow.
- Forces `build_query_profile` tool output for direct UI application.
