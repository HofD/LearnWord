# Learning history and Learner Model v1

## Boundaries

The privately owned Card remains the unit of study and the existing scheduler remains authoritative for DueDate, IntervalDays, EaseFactor, ReviewCount, LastReviewedAt and Learnt. LearningAttempt records evidence; LearnerCardState derives knowledge indicators; LearningSession groups actual work. The model is deterministic and heuristic, not scientifically validated. No AI participates in scoring or selection.

LearnWord.Identity resolves the validated JWT identity and checks the existing ownership links. No learning API accepts a userId to select an owner. Private Card history and state require card ownership; collection summary/planning/session start require collection ownership; session review and completion check its collection ownership and card membership. The internal WebApi remains a trusted service behind the facade.

## Authenticated API

All paths below are public gateway paths and require bearer authentication:

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/learning/collections/{collectionId}/today` | Read-only bounded recommendation |
| GET | `/api/learning/collections/{collectionId}/summary` | Private collection knowledge/activity counts |
| POST | `/api/learning/collections/{collectionId}/sessions` | Start session from a freshly computed plan |
| GET | `/api/learning/sessions/{sessionId}` | Read an owned session |
| POST | `/api/learning/sessions/{sessionId}/complete` | Idempotently close a session, including partial work |
| GET | `/api/learning/cards/{cardId}/state` | Private derived state |
| GET | `/api/learning/cards/{cardId}/attempts?page=1&pageSize=50` | Paginated private evidence |

`POST /api/review/cards/{cardId}/review` continues to accept `{ "outcome": "Good" }`. New optional fields are `responseTimeMs`, `sessionId` and `requestId`. Outcomes remain Again, Hard, Good and Easy. Server UTC determines ReviewedAt; browser time only measures optional response duration. Frontend retries retain the same requestId and outcome until the server acknowledges the attempt.

Today returns `totalCount`, `estimatedMinutes`, `groups: { due, weak, new }` and private `cards`. Starting a session returns its stable UUID, collectionId, startedAt/completedAt, planned/completed counts, successful/failed counts and the selected cards. Summary returns `new`, `learning`, `weak`, `strong`, `totalCardsStudied`, `attemptsCount`, `recentAttemptsCount`.

`responseTimeMs` is an optional integer in 0..600000. Empty UUIDs and unknown/case-mismatched outcomes are rejected. History pagination defaults to 50, caps at 100 and validates offset overflow. Recent activity means the trailing seven days. Successful means Hard/Good/Easy; Again needs more work.

`totalCardsStudied` counts active cards with current SRS evidence; resetting content makes that card New and removes it from this current-state count. Attempt/activity counts retain observed evidence across content revisions for active cards. They are not fabricated lifetime estimates for legacy users. New learner UTC timestamps use PostgreSQL microsecond precision so persisted completion retries return stable timestamps.

## Evidence and derived state

LearningAttempt stores UUID Id, CardId, optional SessionId/RequestId, LearningRevision, existing ReviewOutcome, server ReviewedAt, optional ResponseTimeMs and previous/new IntervalDays, EaseFactor and DueDate. Capture previous values before scheduler invocation and final values afterward. No word content or user email is copied into history.

LearnerCardState stores CardId, LearningRevision, ModelVersion=1, Source, bounded MasteryScore/DifficultyScore, attempts/success/failure counts, consecutive success/failure counts, accumulated OutcomePoints and last outcome/time/status. It is an aggregate cache, not independent evidence. The calculator accepts an explicit current time and can replay current-revision attempts in deterministic time/ID order. Read-time refresh applies recency using current Card data. Internal `LearningService.Rebuild(cardId, now)` replays and replaces a card aggregate under lock; it is not a public mutation API. Individual state reads and reviews recover missing caches from that card's evidence.

## Mastery formula v1

For observed current-revision attempts, let:

- `n` = attempts; successful outcomes are Hard/Good/Easy, failures are Again.
- `q` = mean outcome points: Again=0, Hard=0.5, Good=0.8, Easy=1.
- `e = min(1, n / 8)`; `s = min(1, max(0, IntervalDays) / 30)`.
- `f = clamp((EaseFactor - 1.3) / 1.7, 0, 1)`.
- `c = min(1, ConsecutiveSuccesses / 5)`.
- `o = max(0, days(now - DueDate))`.
- `r = max(0.5, 1 - o / max(30, 2 * IntervalDays))`.

`MasteryScore = clamp((0.40*q + 0.20*e + 0.20*s + 0.10*f + 0.10*c) * r * penalty, 0, 1)` where `penalty=0.35` when the last outcome is Again, otherwise 1.

`DifficultyScore = clamp(0.70 * FailedAttempts/n + 0.30 * (1-q), 0, 1)`.

Outcome quality carries the greatest weight; repeated evidence and demonstrated spacing each contribute 20%. Ease and consecutive recall contribute 10% each. Recency has a grace period through the existing due date and then decays gradually to a 0.5 floor. A recent Again immediately applies a substantial penalty. Response time does not change mastery or scheduling.

Status rule precedence:

1. No observed attempts and no prior SRS signals: New.
2. Last outcome Again, or at least two observed attempts with mastery below 0.25: Weak.
3. At least four observed attempts, at least three consecutive successes, interval at least seven days and mastery at least 0.70: Strong.
4. Otherwise: Learning.

Strong requires sustained evidence and cannot follow a single Easy. Explicit evidence/streak/interval gates reduce unstable score-only categorization. This is a transparent product heuristic, not a probability of recall or a validated cognitive model.

## Legacy migration and reset policy

Migration `20261028000000_AddLearnerModel` only adds tables/indexes and Card.LearningRevision with default 0. It does not alter due dates, intervals, ease, counts or review dates, and inserts no attempts. Follow-up additive migration `20261028000001_AddLearnerPlanningIndex` adds Card `(CollectionId, DueDate, Id)` for bounded due selection.

Without observed history, any ReviewCount>0, LastReviewedAt, positive IntervalDays or Learnt marks Source=LegacyInferred and Status=Learning. Conservative mastery is `min(0.4, ReviewCount*0.05)` when ReviewCount>0, otherwise zero; attempts and outcome counters remain zero and last outcome/time remain unknown. Untouched cards use NoHistory/New. Observed reviews on a legacy baseline identify ObservedWithLegacyBaseline; genuine new-card evidence uses Observed. No legacy card becomes Strong without new observed evidence.

Word-content changes follow the existing SRS reset path and advance LearningRevision. Production word edits and reset share a transaction and the same card lock as review writers. Old attempts remain immutable and retrievable, while current model calculation excludes earlier revisions. Session cards capture their revision so edited content cannot silently receive an answer under an old plan.

Card/collection deletion remains soft deletion. Content-free evidence and session records are retained; private learning reads require active content and ownership, and summaries/plans exclude deleted cards/collections. Foreign keys restrict physical deletion of referenced evidence; an eventual retention/purge policy must explicitly remove evidence before physical content removal. There is no new public historical-content API.

## Sessions, selection and persistence

Today is read-only. The planner selects active nonempty cards, at most 20 total: due/overdue studied cards ordered by DueDate then Id first; then at most five cached Weak cards by Id; then at most five truly New cards by Id. Each later group excludes earlier IDs. Due work can consume the full limit. A New card has ReviewCount=0, no LastReviewedAt, IntervalDays=0 and Learnt=false; fresh immediate DueDate alone does not make it due-study material. Estimated minutes is `ceil(totalCount * 0.5)` and is an estimate.

Starting persists a stable session and ordered card/revision membership from a newly calculated plan. Reviews update completed/success/failure counts. Completion is idempotent and may close partially completed work. Abandoned sessions retain evidence and reject further reviews after 24 hours; closing a browser tab does not fabricate completion.

PostgreSQL review writers lock session then card, and atomically save scheduler changes, new attempt, learner aggregate and session counters in the same words-database transaction. Unique `(CardId, RequestId)` and `(SessionId, CardId)` indexes prevent duplicate evidence. Identical retry IDs replay safely; reuse with changed input returns conflict. Optional IDs preserve old clients; standalone callers omitting an ID cannot receive a general network-retry deduplication guarantee.

Indexes cover `(CardId, ReviewedAt)`, global ReviewedAt, unique card/request and session/card, session `(CollectionId, StartedAt)`, session-card CardId and derived Status. Planner caps selection in SQL and does not load attempt histories. Summary reads compact current-card/cache rows in one query, then database aggregate counts. Collection-sized current-state scans remain a scaling consideration for unusually large collections.

## Social isolation

PublicCollectionSummary, PublicCollectionDetails, PublicCardDto and PublicWordDto use their existing explicit allowlists. No mastery, status, attempt count, review date, response duration or session ID is added. Shared search remains content-only. Fork constructs independent private Card/Word rows with fresh SRS and no learning attempts, cached states or sessions. Deleting its source does not affect the copy.

## Future AI consumption

Future coach features may consume authorized, versioned snapshots of attempts, derived state and sessions. Preserve provenance, learning revision, model version, inferred legacy source and evidence gaps. AI output should be advisory and separately stored; it must not rewrite attempts, ownership, SRS or deterministic scores. Evaluation should compare recommendations against observed future recall with explicit consent and minimal telemetry.
