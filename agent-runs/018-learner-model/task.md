# Epic 2 — Learning history and deterministic learner model

Implement an independent private learning layer around the existing owned Card and spaced repetition scheduler. Preserve Epic 1 social content, independent forks, explicit public DTO allowlists and Identity-authoritative ownership.

## Acceptance

- Atomic actual-before/after review ledger, optional validated response time, server UTC timestamps, retry/concurrency protection.
- Rebuildable deterministic bounded learner state, conservative legacy inference, explicit reset generations and no invented attempts.
- Collection-scoped private summary, bounded due/weak/new plan, persisted sessions and safe completion.
- Existing review UX with Today entry, progress, completion and concise knowledge summary; RU/EN and mobile support.
- Tests for scheduling/model/history/session/security/reset/fork invariants; Docker, real PostgreSQL migrations, two-user authenticated gateway checks and desktop/mobile browser acceptance.
- Backend/frontend specs and exact verification evidence. No AI, shared mutable SRS, content-model rewrite, gamification or production deployment.

## Implementation plan

1. Audit current Card/scheduler/review/ownership/deletion/social and UI/spec/run history.
2. Agree collection-scoped API contract; backend specialist owns production models/services/facade/migration, frontend specialist owns existing collection/review UI, QA specialist owns backend regression and gateway scenarios.
3. Review integration and failure paths, build/start via local Docker, execute complete suites and production Angular build.
4. Independently exercise actual authenticated user paths and inspect PostgreSQL persistence and migration preservation.
5. Review both diffs and record exact acceptance and residual limits.
