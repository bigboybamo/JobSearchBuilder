# nl_profile_builder Prompt Changelog

## Phase 3 - Prompt Caching
- Anthropic prompt caching is active when the stable system prompt XML is sent with `EnableCaching = true`.
- The cache is tied to the exact system block bytes; editing `v1.xml`, switching providers, or toggling `EnableCaching` prevents reuse of the cached block.
- Dynamic user descriptions stay in `messages[]` and must not be injected into the system prompt, otherwise each request becomes a new cache entry.
- `QueryReviewService` caching is deferred to Phase 5 because that service does not exist yet.
- Token savings observed: pending manual provider run; first request writes cache tokens, repeated matching requests should report cache read tokens in the VS Output window.

## v1
- Initial structured prompt for the Phase 2 Describe Role workflow.
- Forces `build_query_profile` tool output for direct UI application.
