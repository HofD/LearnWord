# Epic 2 — Final acceptance

Decision: accepted locally on 2026-10-28. Both repositories are buildable and tested. The accepted result was saved in separate backend/frontend commits on 2026-10-29 at the user's request; no production deployment was performed. Local Docker stack remains running. Browser was returned to original collection5, RU language and default viewport.

## Implemented

Private append-oriented attempt ledger around the existing Card scheduler; deterministic bounded/rebuildable card model; New/Learning/Weak/Strong classification; collection-scoped private summary; bounded due/weak/new recommendation; durable session membership/progress/completion; existing review UI integration and compact EN/RU collection panel. Actual gateway and desktop/mobile user paths passed.

## Architecture

Current owned Card retains all scheduling fields. LearningAttempt is historical evidence; LearnerCardState is a replaceable aggregate; LearningSession and ordered membership describe actual planned work. All reside in the words database, allowing local atomic persistence with SRS. Identity remains authoritative for ownership. Social content and fork boundaries were preserved. No AI or distributed event infrastructure was introduced.

Scheduler scheduling answers when to review; learner scores summarize evidence for recommendations. Client analytics remain optional analytics, never ledger evidence.

## Data model

- Card adds LearningRevision, starting at0 and incremented on content/SRS reset.
- LearningAttempt: UUID Id, CardId, optional SessionId/RequestId, revision, existing outcome, server UTC ReviewedAt, optional duration and actual previous/new interval/ease/due fields.
- LearnerCardState: CardId primary key, revision/model version/source, mastery/difficulty, observed counters, consecutive counters, outcome points, last outcome/date and status.
- LearningSession: UUID Id, CollectionId, start/completion and planned/completed/success/failure counts.
- LearningSessionCard: session/card composite key, position and content revision.
- Indexes: card collection/due/id; attempt card/reviewed time, global reviewed time, unique card/request and session/card; state Status; session collection/start; membership CardId. History-bearing parents use restrictive physical-delete FKs.
- Additive migrations20261028000000_AddLearnerModel and20261028000001_AddLearnerPlanningIndex; snapshot updated.

## Mastery model

Exact formula and rationale: `specs/learner-model.md`.

Let q=mean points (Again0, Hard0.5, Good0.8, Easy1), e=min(1,n/8), i=min(1,max(interval,0)/30), f=clamp((ease-1.3)/1.7,0,1), s=min(1,consecutive successes/5), r=max(0.5,1-max(0,overdueDays)/max(30,interval*2)).

Mastery=clamp((0.4q+0.2e+0.2i+0.1f+0.1s)*r*(last Again?0.35:1),0,1).

Difficulty=clamp(0.7*failures/n+0.3*(1-q),0,1).

Weak: latest Again, or n>=2 and mastery<0.25. Strong: n>=4, consecutive successes>=3, interval>=7days, mastery>=0.70. Otherwise Learning. Fresh cards with no meaningful review/SRS evidence are New. A single Easy cannot be Strong. This is an explainable v1 heuristic, not a scientifically validated recall probability.

## SRS compatibility

Outcome names and scheduler interval/ease calculations remain unchanged. Reset only adds the learning revision increment alongside the existing schedule reset. Old outcome-only review requests still work. Actual gateway Again→Good→Good→Easy sequence produced intervals1→3→7→21 and chained immutable before/after history. Existing scheduler regression suite remains green.

## API

All authenticated under `/api/learning`:

- GET `/collections/{collectionId}/today` and `/summary`.
- POST `/collections/{collectionId}/sessions`.
- GET `/sessions/{sessionId}`; POST `/sessions/{sessionId}/complete`.
- GET `/cards/{cardId}/state` and `/attempts?page=1&pageSize=50` (cap100).

Existing POST `/api/review/cards/{cardId}/review` adds optional responseTimeMs0..600000, sessionId and requestId. Server-generated timestamps use PostgreSQL microsecond UTC precision. No owner selection via body/query.

Today is read-only, excludes empty/deleted cards and duplicate IDs, caps total20; studied due material is first ordered by due/id, weak max5, genuinely new max5. Due work can consume the full limit. Estimated duration=ceil(cards*0.5) minutes. Session start freezes plan/revisions; review validates membership/revision and24h lifetime; completion safely closes partial sessions and is idempotent.

## Frontend

Compact collection panel shows plan groups, estimated minutes, Start, knowledge categories, current studied count and trailing-seven-day attempt activity. `/review/:collectionId?today=true` starts the planned durable session; legacy due-only route remains supported. Existing translation and all-word presentation are preserved. Code-level submission guard, disabled pending buttons, stable retry payload/UUID, acknowledged progress and explicit completion retry/summary are implemented. Persisted card/word mutations refresh the panel. No precision score dashboard or gamification.

## Security

Authenticated JWT/facade ownership: foreign collection/session404, foreign card403, anonymous401. A user's card cannot be attached to another user's session. Deleted content is inaccessible through learner endpoints. Public/shared/Explore responses retain explicit content-only allowlists; search remains word/translation content-only. Fork has new private independent cards, fresh SRS and zero inherited attempts/states/sessions. Two-user gateway checks and old social acceptance passed.

## Migration and edit/delete policy

Real local PostgreSQL applied both migrations. Before migration, saved169 existing Card rows' Id/DueDate/IntervalDays/EaseFactor/ReviewCount/LastReviewedAt; immediately after migration they were byte-identical. Rechecked by original IDs after acceptance: all169 unchanged.

No attempts or dates/outcomes were fabricated. Legacy progress infers Learning with mastery capped0.4 and explicit LegacyInferred source, zero observed counters and unknown last outcome/date. New observed evidence on legacy cards is marked ObservedWithLegacyBaseline.

Word changes/reset share a domain transaction and card lock, advance revision and invalidate older session membership while preserving old attempts. Soft deletion retains content-free evidence internally and excludes deleted content from reads/plans/summaries. Physical purge requires an explicit retention policy. totalCardsStudied is current SRS-evidenced active cards; reset makes a card New. Activity/attempt counts retain actual old-revision evidence for active cards.

## Tests

Commands were run from the relevant repository directories with Docker access. Exact backend runner:

```sh
./deploy/local-up.sh
docker run --rm --network learnword-local_default \
  -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
  -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

Final result: **218 passed,0 failed,0 skipped**, all9 projects: Authorization7, IdentityService.WebApi26, gateway E2E14, middleware6, BL79, DAL7, Identity.DAL6, Identity62, WebApi11. Includes transactional rollback on attempt insert failure, scheduler failures, all outcomes/telemetry boundaries, model bounds/progression/recency/status/legacy/rebuild, planner priority/caps/dedup, session lifecycle/revisions, ownership and fork/social regressions.

Frontend ran in node:20-alpine with Chromium installed and npm ci dependencies, mounted final source copied to isolated `/app`. CHROME_BIN wrapper invoked `/usr/bin/chromium-browser --no-sandbox` for the container:

```sh
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

Final result: **76/76 Jasmine tests passed**; Angular production build passed. Full-stack local-up built all runtime services/frontend, migrator completed. Existing collection CSS budget warning remains2.79kB against2kB; build succeeds. Existing dependency/nullable warnings remain; no package or lockfile changes.

Authenticated real gateway acceptance:

```sh
python3 agent-runs/018-learner-model/verify-learning.py
python3 agent-runs/014-social-collections/verify-social.py
```

Results:5 Epic2 scenario groups and6 previous Social Collections groups passed. Three concurrent identical review requests produced one attempt/SRS increment. Repeated completion DTOs match after fixing PostgreSQL microsecond precision. Scripts clean their disposable collections.

Reviewed both tracked diffs and new files; no unrelated source/dependency/environment changes found. `git -C LearnWord diff --check` and `git -C LearnWordWebApp diff --check` passed from workspace root.

## Browser acceptance

Personally exercised existing local test-account login, collection panel EN/RU at1280x900 and390x844, Start, translation reveal, four actual answers (Again/Hard/Good/Easy on four cards), pending disabled controls, progress0→4 and completion reviewed4/success3/need-work1. Returned to collection: Learning3/Weak1/New0, recommendation weak1 and activity4. Edited the failed card's translation: panel immediately changed to Learning3/Weak0/New1 while activity remained4.

Gateway readback of browser-created data confirmed completed session-linked evidence and unchanged old attempt after edit, new revision1/current attempts0/New state. At390px document width equalled390px. Screenshots are under `screenshots/`. Browser fixtures were soft-deleted; original collections retained. Error/retry branches were covered by automated frontend tests; no live browser network-failure injection was performed.

## Residual risks

- Model weights/status gates and estimated duration require product calibration; no scientific validation claimed.
- Summary scans compact per-collection card/cache rows; extreme collection sizes need profiling. No production load/concurrency stress test was performed.
- Old standalone clients omitting requestId cannot get general retry deduplication. Adaptive sessions additionally enforce one attempt per planned card.
- Abandoned sessions retain evidence and expire for further reviews after24h; no durable browser-resume UX or automatic completion job.
- Evidence is retained through soft deletion; account-erasure/hard-purge retention policy is future work.
- Attempts carry content revision, not historical word-text snapshots; a future coach must not treat old-revision evidence as an answer to current edited content.
- No production deployment or production migrations were requested/performed. Separate local backend/frontend commits were subsequently requested and saved on 2026-10-29.

## Recommendations for Epic 3

Consume an authorized, versioned read model combining recent attempts, current-revision learner state and session aggregates with current content only when revisions match. Include inferred/observed source, model version and evidence gaps. Keep AI advice separate and explain its evidence; AI must never mutate history, ownership or deterministic SRS/mastery. Evaluate recommendations against future observed recall before granting them scheduling influence.
