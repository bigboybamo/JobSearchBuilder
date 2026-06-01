# query_suggestions Prompt Changelog

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
