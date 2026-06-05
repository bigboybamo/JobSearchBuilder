# query_review Prompt Changelog

## Phase 6 — Eval Pipeline Baseline (2026-06-05)

**Eval run:** Not captured this session — eval was not run against the expanded 19-case golden set.

Golden set expanded in Phase 6 from 17 → 19 cases:
- Added well-formed query with humanitarian org site filters (careers.un.org, jobs.unicef.org, wfp.org/careers) to confirm the reviewer does not flag valid international org domains as malformed
- Added bad query where excluding "international development" defeats the purpose of targeting international org career portals

**Baseline score: pending.** Run `promptfoo eval --config query_review/eval.yaml` from `prompts/` to establish it.

## v1
- Initial prompt for Phase 5 query review.
- Reviews generated Google job-search queries for concrete quality issues.
- Returns raw JSON with `issues` and `suggestions` arrays.
- Uses a stable XML system prompt so Anthropic prompt caching can be enabled.
