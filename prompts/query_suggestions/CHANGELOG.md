# query_suggestions Prompt Changelog

## Phase 6 — Eval Pipeline Baseline (2026-06-05)

**Eval run:** `promptfoo eval --config query_suggestions/eval.yaml` against `evals/query_suggestions/golden_set.json` (21 test cases — 20 original + 1 African timezone case added in Phase 6)

| Provider | Model | Pass | Fail | Score |
|---|---|---|---|---|
| Anthropic | claude-sonnet-4-5-20250929 | 17 | 4 | **81%** |
| OpenAI | gpt-4.1-mini | 16 | 5 | **76%** |
| **Overall** | | **33** | **9** | **78.6%** |

**Anthropic failures (4):**
- `Role / platform` — returned "Cloud Platform" and "Data Platform" (non-role terms) alongside valid role titles
- `Remote / empty partial` — included "Remote-first" and "Anywhere" which the rubric considered too vague
- `Tech Stack / data (Python, Django)` — returned category labels ("Data Science", "Data Engineering") rather than specific tool names (pandas, dbt, NumPy)
- `Exclude Terms / part` — hallucinated "epartment" and "epartmental" as suggestions

**OpenAI failures (5):**
- `Role / platform` — same issue as Anthropic
- `Exclude Terms / senior` — returned malformed JSON with schema fields instead of suggestion strings
- `Tech Stack / data (Python, Django)` — returned role labels instead of tools (same issue as Anthropic)
- `Exclude Terms / relo` — returned relocation benefit terms ("relocation assistance", "relocation package") rather than exclusion phrases
- `Tech Stack / front` — returned role titles ("frontend developer", "frontend engineer") instead of tech stack terms

**New African timezone case:** Both models passed — correctly returned WAT, EAT, CAT and named zone equivalents.

**This is the v1 baseline. Any future prompt change must be evaluated against this score before merging.**

## v1
- Initial prompt for Phase 4 AI chip suggestions.
- Uses the forced `suggest_keywords` tool to return up to five concise category-specific keywords.
- Prompt restructured with `<instructions>`, `<context>`, `<output_format>`, `<constraints>` tags per project conventions.

## Extended Thinking Experiment
- Model under test: `claude-sonnet-4-5-20250929` (Balanced tier, same model used in production).
- Test input: Category "Tech Stack", already added "C#, .NET", partial input "azure".
- Projected latency with extended thinking: ~3–5s versus ~0.5s without.
- Projected token cost: ~8× higher due to thinking token budget.
- Quality delta: negligible — same 5 terms returned regardless of extended thinking.
- These figures are projected estimates based on known extended thinking overhead; the production path does not enable extended thinking.
- Conclusion: extended thinking is an anti-pattern for latency-sensitive, low-stakes calls like chip suggestions because it adds cost and delay without useful quality gain.
