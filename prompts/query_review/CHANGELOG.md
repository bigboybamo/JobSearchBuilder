# query_review Prompt Changelog

## v1
- Initial prompt for Phase 5 query review.
- Reviews generated Google job-search queries for concrete quality issues.
- Returns raw JSON with `issues` and `suggestions` arrays.
- Uses a stable XML system prompt so Anthropic prompt caching can be enabled.
