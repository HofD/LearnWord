# Backend QA — Epic 2

Owner: qa_learner. Scope: backend tests and repeatable local gateway acceptance only; production changes owned by backend agent.

## Coverage added

- `LearningHistoryTests`: all four persisted outcomes and actual before/after scheduling values; optional telemetry and inclusive valid bounds; invalid telemetry/outcome including numeric enum input; missing card and throwing scheduler; injected SaveChanges failure; actual SQLite ledger INSERT rejection with transaction rollback; stable retry versus reused identifier with conflicting input; recovery of a deleted model cache from immutable evidence without schedule changes; content edit/reset revisions and immutable retained evidence; soft-delete retention.
- `LearnerStateCalculatorTests`: New and conservative legacy bootstrap without fabricated evidence; score bounds, deterministic replay, increasing spaced recall, isolated Easy, recent Again regression/Weak, Hard quality, overdue decay, all Strong evidence gates, history ordering and revision reset.
- `LearningSessionPlannerTests`: deterministic due/weak/new priority and deduplication, due capacity protection, per-group caps, non-new reviewed/unlearnt content, side-effect-free recommendations; session plan membership/counters/one card per session, double completion across cleared request trackers with PostgreSQL microsecond precision, abandonment/expiry, reset during session; deleted content exclusion and private history pagination.
- `LearningIdentityServiceTests`: foreign collection/card/session denial, denied review attachment before mutation, owned forwarding/pagination, every anonymous learner controller action.
- Extended existing Social Collections fork regression: populated source attempt/model rows are absent from copied cards; copy has no sessions. Existing explicit public JSON allowlist tests remain unchanged.
- Extended real gateway anonymous auth matrix with all seven learner routes.

SQLite test fixture converts UTC timestamps to UTC ticks for SQL date ordering/comparison. This preserves chronological semantics while production uses PostgreSQL timestamptz. PostgreSQL concurrency is exercised separately by gateway acceptance.

## Repeatable acceptance

`python3 agent-runs/018-learner-model/verify-learning.py` (backend repo root) uses two local confirmed accounts, disposable collections, real gateway and Mailpit. It checks owner/visitor/anonymous matrix, nonmutating plan, invalid telemetry, foreign session attachment, three concurrent identical review retries, session counters/double completion, legacy outcome-only clients, Again/Good/Good/Easy interval chain and immutable history, private state, safe shared search DTO, fresh fork and source deletion independence. Collections are removed in `finally`; dedicated local accounts remain reusable. Tokens and passwords are never printed.

## Verification responsibility

Gateway script syntax validated with Python `ast.parse`. No direct sandbox dotnet commands run. Root analyst owns Docker build, complete backend test runner, actual gateway execution and final results; authoritative command results belong in `final-acceptance.md`. Root reported the initial complete nine-project suite passing and real concurrent gateway retries passing. Gateway then exposed first-completion versus persisted timestamp precision; backend normalized learner clock timestamps and QA added a reload/precision regression. Final rerun after precision and cache-recovery tests is owned by root and recorded in final acceptance.
