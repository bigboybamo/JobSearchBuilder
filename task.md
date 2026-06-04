# Session Handoff — Phase 6 Eval Pipeline

**Branch:** `feature/eval-pipeline`  
**Last session date:** 2026-06-04

---

## What Was Done This Session

### Eval pipeline built and running
- Expanded all three golden sets to Promptfoo-native format (`vars` + `llm-rubric assert`):
  - `prompts/evals/nl_profile_builder/golden_set.json` — 20 cases
  - `prompts/evals/query_review/golden_set.json` — 17 cases
  - `prompts/evals/query_suggestions/golden_set.json` — 20 cases
- Added `correct_mapping` criterion to `prompts/nl_profile_builder/rubric.yaml`
- Created `messages.yaml` per prompt (Promptfoo needs system + user message structure):
  - `prompts/nl_profile_builder/messages.yaml`
  - `prompts/query_review/messages.yaml`
  - `prompts/query_suggestions/messages.yaml`
- Fixed `promptfooconfig.yaml` — dropped broken scenarios format; now runs `nl_profile_builder` by default
- Created separate `eval.yaml` for the other two prompts:
  - `prompts/query_review/eval.yaml`
  - `prompts/query_suggestions/eval.yaml`
- **Ran the nl_profile_builder eval successfully — baseline scores recorded**

### Baseline eval results (nl_profile_builder, 2026-06-04)
| Provider | Model | Score |
|---|---|---|
| Anthropic | claude-sonnet-4-5-20250929 | **80%** (16/20) |
| OpenAI | gpt-4.1-mini | **55%** (11/20) |
| Overall | | 67.5% (27/40) |

Full findings in `prompts/nl_profile_builder/CHANGELOG.md`.

---

## What Needs To Be Done Next (in order)

### TASK 1 — Fix `nl_profile_builder/v1.xml` (two prompt bugs found by the eval)

**Bug 1 — Engineering manager seniority**
Both models return `seniority: "Manager"` instead of `"Any"`.
Fix: add a constraint to `v1.xml` clarifying that management titles (Engineering Manager, Director, VP) are roles, not seniority levels. Seniority should stay `"Any"` unless Junior/Mid/Senior/Lead/Principal/Staff is explicitly stated.

**Bug 2 — Inferring remote from a negative exclusion**
Both models add `remote_terms: ["remote"]` when the user says "no on-site" — but the user didn't say remote, they just excluded on-site.
Fix: add a constraint stating do not populate `remote_terms` based on an exclusion alone. Only populate it when the user explicitly states a preference (remote, hybrid, fully remote, etc.).

**Process:**
1. Copy `v1.xml` → `v2.xml` in `prompts/nl_profile_builder/`
2. Apply both constraint fixes in `v2.xml`
3. Update `prompts/nl_profile_builder/messages.yaml` — change `v1.xml` reference to `v2.xml`
4. Run `promptfoo eval` from `prompts/` and compare score vs baseline (80% / 55%)
5. If v2 scores higher on both providers → record new scores in `CHANGELOG.md`, keep v2 as active
6. If not → investigate failing cases and iterate

### TASK 2 — Add remaining humanitarian/international org domains to `appsettings.json`

International org sets #14, #15, #16 already exist:
- Set #14: AfDB, World Bank, UNDP, UNOPS
- Set #15: UNICEF, WFP, ITU, WIPO
- Set #16: WHO, IMF, IOM, UN, IDB Invest

User has a list of additional organisations to paste. Add them into existing sets or create Set #17+ as needed. Each entry is just a domain string under the matching `"Domains"` array.

### TASK 3 — Add humanitarian test cases to the golden sets

Once the org list is finalised, add humanitarian-flavoured test cases:
- `nl_profile_builder` golden set: e.g. "Programme Officer, WASH sector, East Africa, no relocation" or "M&E Specialist, UN agencies, remote, UTC+2 or UTC+3"
- `query_review` golden set: queries using humanitarian site: filters (site:careers.un.org, site:jobs.unicef.org etc.) to validate the reviewer handles them correctly
- `query_suggestions` golden set: Role category cases with humanitarian titles (M&E Officer, Field Coordinator, Programme Manager)

Then re-run all three evals and record the new baseline.

### TASK 4 — Merge Phase 6 PR and mark done

Once Tasks 1–3 are complete:
1. Open PR from `feature/eval-pipeline` → `master`
2. Update `CLAUDE.md` progress tracker: change Phase 6 from `[ ]` to `[x]`

### TASK 5 — Start Phase 7 — Batch Profile Generation (`feature/batch-profiles`)

New branch: `feature/batch-profiles`

- A `Bulk Describe` dialog accepts multiple plain English descriptions (one per line)
- `BatchProfileBuilderService` checks active provider:
  - Anthropic → `/v1/messages/batches` (one HTTP call, poll until `processing_status === "ended"`)
  - OpenAI / Gemini → `Task.WhenAll` for parallel async calls
- Results shown in a list with `Apply to Profile` and `Save as New Profile` buttons per result

---

## How to Run the Eval

```powershell
# Set API keys first (if not already in system environment)
$env:ANTHROPIC_API_KEY = "sk-ant-..."
$env:OPENAI_API_KEY = "sk-..."

# From the prompts/ directory:
cd prompts

promptfoo eval                                         # nl_profile_builder (20 cases x 2 providers)
promptfoo eval --config query_review/eval.yaml         # query_review (17 cases x 2 providers)
promptfoo eval --config query_suggestions/eval.yaml    # query_suggestions (20 cases x 2 providers)
```

---

## Key Files Reference

| File | Purpose |
|---|---|
| `prompts/promptfooconfig.yaml` | Master eval entry point (runs nl_profile_builder) |
| `prompts/nl_profile_builder/v1.xml` | Active system prompt — needs v2 with two bug fixes |
| `prompts/nl_profile_builder/CHANGELOG.md` | Baseline scores + failure analysis |
| `prompts/nl_profile_builder/messages.yaml` | Promptfoo message structure (update to v2.xml after fix) |
| `prompts/evals/nl_profile_builder/golden_set.json` | 20 test cases |
| `prompts/query_review/eval.yaml` | Eval config for query_review |
| `prompts/query_suggestions/eval.yaml` | Eval config for query_suggestions |
| `JobSearchBuilder/appsettings.json` | ATS + humanitarian source groups (sets #14–16 exist) |
| `CLAUDE.md` | Phase tracker — Phase 6 is `[ ]` pending completion |
