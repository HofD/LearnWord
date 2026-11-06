# Deterministic Coach ranking acceptance

Implemented on feature/epic-3a-ai-coach. Verification results below were recorded before commit. No push performed.

Selection: evaluate all bounded members using unchanged eligibility, build each eligible context once,
rank with the pure CoachCandidateRanker, select five. Nullable private severity category/rank/signals
are absent from the provider contract. Old diagnostic snapshots remain readable.

Exact ranking and counter semantics: specs/ai-coach.md, Deterministic candidate ranking v1.
The existing state cache remains authoritative when revision matches; refresh now uses session end,
not wall-clock query time. This preserves the existing cache design rather than claiming a historical
reconstruction of learner state after later reviews. Attempt evidence remains bounded and cut off at completion.

Verification:
- `LW_STANDARD_IMAGE_PULL=never ./deploy/local-up.sh`: Docker build/start succeeded.
  Initial default pull failed on macOS credential helper; cached-image run succeeded.
- `cd LearnWord && ./tests/run-all-tests.sh`: 531 passed, 14 gateway E2E skipped by environment,
  zero failures across nine projects. Initial sandbox MSBuild named-pipe denial resolved by running
  the same approved script outside sandbox. Initial new-fixture failure corrected: six equal top-category
  cards correctly excluded ID 10 by tie-breaker; final fixture has five high-severity cards.
- `python3 agent-runs/019-ai-coach/verify-coach.py`: six acceptance groups passed.
- `python3 agent-runs/020-coach-diagnostics/verify-diagnostics.py`: all five reported checks passed.
- `python3 agent-runs/029-coach-ranking/verify-ranking.py`: passed through real gateway, Fake provider.
  Ten members: five low-ID cards with Again then Hard; five high-ID cards with repeated Again.
  Exactly the high-ID five selected; low-ID five CandidateLimit; ranks 1..5 match context selection.
  Captured diagnostic evidence is ranking-results.json. Ordinary attempt history, current learner state
  and collection/card/SRS data were identical before and after analysis.
- `git diff --check`: passed.

No paid model requests. Container AiCoach__Provider was inspected and confirmed Fake before acceptance.
No frontend, migrations, SRS/calculator implementation, provider, prompt, output schema, cache lifecycle,
or Social Collections implementation changes. Existing Coach gateway tests also cover None, caching,
ownership, intervention lifecycle, normal evidence/SRS preservation and shared/fork DTO isolation.

Limitations: heuristic categories; six-attempt window; existing cached learner-state semantics; full
LEARNWORD_E2E=true route suite was not run (targeted Coach and diagnostics gateway checks were run).
Diagnostic rows retain stable CardId order; SeverityRank conveys ranking position.
