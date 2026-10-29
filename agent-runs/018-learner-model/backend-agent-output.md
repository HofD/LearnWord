# Backend implementation — Epic 2

Production scope: owned Card reviews, private evidence/model/session/planning APIs and their Identity/gateway boundary. Existing social allowlist mappings and independent content forks are unchanged.

## Implemented

- `LearningAttempt`: server UTC evidence with the existing outcome enum, real scheduler before/after interval/ease/due, optional bounded response milliseconds, optional session/request IDs and content learning revision.
- `LearnerCardState`: disposable versioned counters/weighted evidence cache; pure calculator supports history replay and explicit-time refresh. Internal `LearningService.Rebuild(cardId, now)` repairs one card cache. Direct state read recovers a missing cache without mutating it; review recovers missing cache before incremental update.
- Existing Card SRS scheduling formula unchanged. Scheduler Reset additionally increments LearningRevision. Word edits and SRS resets share a production domain transaction/card lock, preserving old evidence while excluding old revisions from the current model.
- Reviews lock session then card in PostgreSQL; scheduler state, ledger, cache and session counters commit atomically. Same card/request ID retries return without a second review; changed payload conflicts. One card can contribute once per session; old clients remain compatible without optional fields. Without a client request ID, independent non-session submissions are intentionally treated as separate reviews.
- Today plan GET is read-only: due material first up to20, then at most5 weak and5 new using remaining capacity. Due/new groups distinguish meaningful current SRS evidence; empty-word/deleted content excluded; deterministic due/id ordering and no duplicates. Estimated time is30seconds/card rounded to minutes.
- Session start freezes plan/revisions in a repeatable-read PostgreSQL transaction. Review requires planned active card/revision and open session younger than24hours. Completion may close a partial session and is idempotent; unclosed abandoned sessions expire for new reviews after24hours and retain evidence.
- Collection summary loads compact current card/cache data in one query and refreshes time-dependent scores; history counts run in SQL. It never loads all attempts for all cards. This is O(cards) compact memory, with no word payload or N+1 queries.
- New authenticated collection today/summary/start, session read/complete, card state/paged history. Identity uses JWT-derived ownership; foreign collection/session404, foreign card403, anonymous401. Attaching a foreign session is checked before core review.
- Additive migrations `20261028000000_AddLearnerModel` and `20261028000001_AddLearnerPlanningIndex`; no legacy dates, intervals, ease or review counts rewritten and no fabricated attempts. Indexes cover card/due ordering, evidence card/time, session/card and card/request dedup, time counts, session collection/start and cached status.

## Model v1

Outcome evidence points Again0, Hard0.5, Good0.8, Easy1. All outcomes except Again count successful recall. For current-revision observed count n: q=points/n; e=min(1,n/8); i=min(1,max(interval,0)/30); f=clamp((ease-1.3)/1.7,0,1); s=min(1,successStreak/5); r=max(.5,1-max(0,overdueDays)/max(30,interval*2)). Mastery=clamp((.4q+.2e+.2i+.1f+.1s)*r*(latestAgain?.35:1),0,1). Difficulty=clamp(.7*failures/n+.3*(1-q),0,1).

Weak: latest Again, or n>=2 and mastery<.25. Strong: n>=4, successStreak>=3, interval>=7days, mastery>=.70. Otherwise Learning. No meaningful review evidence is New. Zero-ledger legacy progress is conservatively Learning with inferred mastery min(.4,reviewCount*.05), zero observed counters and explicit LegacyInferred source. Fresh/no-history source is NoHistory; mixed preexisting progress is ObservedWithLegacyBaseline. This is an explainable heuristic, not scientific validation.

## Delete/reset policy

Existing soft-delete remains authoritative. Evidence/session rows contain no word content and are retained internally; all learner reads require active owned card/collection. FK Restrict prevents accidental hard-delete of history-backed parents. Fork has independent new IDs, fresh SRS/revision and no cache/evidence/sessions. Editing invalidates planned old revisions with an explicit review conflict; historical evidence remains immutable.

## Verification / handoff

Agent did not run direct dotnet commands. Root owns Docker local-up, PostgreSQL migration/state comparison, full backend runner, real two-user gateway acceptance and browser acceptance. Root reported initial Docker build and213 backend tests passed while final source verification remains with root. `git diff --check` passed during implementation. Tests/specs are owned by separate QA/root agents.

Known limit: no durable session resume UX or background abandoned-session closure; completion is explicit/optional. History retention is internal soft-delete retention, not an account-erasure policy. Status/model thresholds and estimated minutes need future product calibration. Summary scans compact current-state rows per selected collection; larger collection scale may warrant SQL materialization or pagination. No LLM/coach/event infrastructure was introduced.
