# nl_profile_builder Prompt Changelog

## v3 — Location and visa extraction (2026-06-15)

**Changes from v2:**
1. Added two new tool fields, `locations` and `visa_terms`, to the `build_query_profile` schema (and to `QueryProfileResult`). Previously the Describe Role workflow never extracted these, so the location and visa chips kept whatever defaults were already loaded in the UI regardless of the typed description.
2. Added `<context>` definitions for locations and visa terms.
3. Added constraints:
   - `locations` only when the user explicitly names a geographic place; empty otherwise. Do not infer from timezone, remote preference, or nationality.
   - `visa_terms` only when the user wants to *match* a work-authorization phrase. A visa requirement stated as something to *avoid* ("no visa sponsorship") goes to `exclude_terms`, mirroring the existing remote/exclusion rule.
4. `ApplyQueryProfileResult` now fully drives `LocationFilters`/`VisaFilters` from the result (cleared when unmentioned), matching how role/stack/remote/timezone/exclude already behave.
5. Added a timezone-expansion constraint: a US-timezone group reference ("US timezones") expands to the specific identifiers EST, CST, MST, PST and the bare label is dropped. Other regions (Europe, Africa, Asia, …) are *not* expanded — they return a single `"<Region> timezones"` label. Targets the long-standing OpenAI failure (v1 baseline) where "US timezones" was kept literal; also makes Anthropic's expansion deliberate rather than inferred.
6. Tightened role normalization: when a description leads with a technology ("Python data engineer"), only the clean job title goes in `role` and the technology moves to `tech_stack`; job-title words are never duplicated into `tech_stack`. Targets the OpenAI category-split failure where `role: ["Python data engineer"]` and `tech_stack: ["Python", "data engineer"]` slipped past the lenient rubric.
7. Added a seniority-stripping constraint: when a seniority level word appears in the description it is extracted into `seniority` and removed from `role` ("Senior software architect" -> role "Software Architect", seniority "Senior"). Targets the OpenAI failure (seen across all test cases) where the seniority word was kept in the role string, e.g. `role: ["Senior Software Architect"]`, causing rubric mismatches against the bare-title expectation. Anthropic already stripped it correctly.

**Eval results vs v2 baseline:** Overall pass rate improved over the v2 baseline on the expanded 26-case golden set (added location/visa, US-timezone expansion, European/African regional-label, role-normalization, and seniority-stripping cases). Exact per-provider counts not recorded for this run. A few failures remain — chiefly gpt-4.1-mini structural-compliance issues (scalar fields returned as arrays) that prompt wording alone does not resolve.

**v3 is the active prompt.** Confirmed no regression on the v2 cases; the constraint additions (items 5–7) lifted the previously failing US-timezone, role-split, and seniority-in-role cases.

---

## v2 — Seniority and remote_terms constraint fixes (2026-06-05)

**Changes from v1:**
1. Clarified that seniority uses explicit level words only (Junior, Mid, Senior, Lead, Principal, Staff) — "otherwise return Any"
2. Added explicit rule: management/leadership titles (Engineering Manager, Director, VP, Head of) are roles, not seniority levels — always return `seniority: "Any"` unless a seniority qualifier is also stated
3. Added explicit rule: only populate `remote_terms` when the user explicitly states a work-arrangement preference — do not infer from a negative exclusion; put "no on-site" / "no relocation" in `exclude_terms` instead

**Eval results vs v1 baseline:**

| Provider | v1 | v2 | Change |
|---|---|---|---|
| Anthropic | 16/20 (80%) | **19/20 (95%)** | +15pp |
| OpenAI | 11/20 (55%) | **12/20 (60%)** | +5pp |
| Overall | 27/40 (67.5%) | **31/40 (77.5%)** | +10pp |

**What improved:**
- Both models now correctly return `seniority: "Any"` for Engineering Manager
- Anthropic now correctly puts "no on-site" in `exclude_terms` instead of inventing `remote_terms`
- OpenAI Staff engineer "no on-site" case also now passes

**Remaining Anthropic failure (1):**
- `C# backend developer` — still adding `remote_terms: ["remote"]` when input only states "no clearance, no relocation". The constraint is partially effective; this edge case may need a more explicit example in the prompt.

**Remaining OpenAI failures (8):**
- Field category confusion (e.g. `seniority` returned as array instead of string, `"data engineer"` in `tech_stack`)
- Truncated exclusion phrases ("visa sponsorship" instead of "visa sponsorship required")
- Still occasionally invents `remote_terms` from exclusions (`Senior software architect` case)
- These are structural compliance issues with the schema; prompt wording alone has limited impact on gpt-4.1-mini

**v2 is the new active prompt.** The 80%→95% Anthropic improvement confirms both constraint fixes were correct.

---

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
