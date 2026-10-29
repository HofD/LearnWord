# Epic 3A — AI Coach: final factual report

Date: 2026-10-29. Decision: accepted for the implemented local vertical slice. Changes are saved in separate backend/frontend commits on `feature/epic-3a-ai-coach`, as requested after acceptance. Neither branch was merged into main; no production rollout was performed. Real provider transport is implemented; successful end-to-end teaching flows were tested with Fake, failure with local HTTP stub. Paid/live LLM output quality was not tested.

## Implemented

Completed adaptive session -> deterministic summary -> separate asynchronous Coach request -> None or bounded private suggestions -> explicit lesson/practice acceptance -> server-validated supplementary answers -> durable completion -> diagnostic follow-up from future normal reviews. None hides the Coach block. Failures, disabled feature and missing key leave ordinary learning successful. Tested in actual Docker application through authenticated gateway and browser.

## Architecture

New `CoachContextBuilder` -> versioned explicit `CoachSessionContext` -> `IAiCoachProvider` -> `CoachOutputValidator` -> `CoachService` -> additive durable storage. WebApi owns provider/domain behavior, Identity verifies collection ownership before calls, gateway authenticates all public paths. Separate analyze endpoint avoids any learning transaction spanning LLM latency. No distributed worker infrastructure or broad existing-provider refactor.

## AI Boundary

Coach writes only CoachAnalysis, CoachIntervention and InterventionAttempt. It cannot update Card scheduling/ownership/content, LearnerCardState or normal LearningAttempt. Existing scheduler, calculator, review/session implementation and Social DTOs were preserved. AI advice is not SRS scheduling. AI supplementary exercise is not LearningAttempt. AI intervention does not change mastery.

## Coach Context

Schema 1: language, completedAt, session counts, revisionMismatchCards, contentOmittedCards, candidates. Candidate: opaque card/revision, up to4 words/translations, current state/source/model version, exact current/historical evidence counts, latest6 current-revision attempts at/before completion, session attempt. Maximum5 candidates; only session members with repeated observed evidence and problematic session outcome/timing. Context snapshot uses PostgreSQL repeatable read. Current state is current at analysis time; attempts are bounded at session completion. Missing cache uses explicit BoundedObserved fallback, never saves a replacement aggregate.

Excluded: account/email/ownership IDs, collection names, unrelated collections, Social data, auth tokens/headers, provider config/secrets, full EF entities, scheduling fields. User-entered card text can itself contain PII; explicit allowlist does not sanitize arbitrary authored prose. Old revisions are never attributed to current text.

## Structured Output

Actual provider contract is defined in `CoachContracts.cs`, public DTO in `CoachDtos.cs`, detailed [schema/limits](../../specs/ai-coach.md). Root fields: decision, summary, observations, interventions. Observations contain card/revision, controlled type, evidenceAttemptIds, evidenceSummary, explanation. Interventions contain card/revision, controlled type, title, structured lesson or RecognitionChoice exercises. Provider supplies option IDs and expected option; server assigns intervention/exercise IDs and explicitly excludes solutions from public mapping.

Validation: strict known JSON members and required fields, exact enums, known/current card/revision/attempt references, duplicate observation cards/intervention card+type, evidence gates for each observation type, nonempty bounded content, option ID/text uniqueness, option count, answer consistency and pairwise source-word/translation grounding. Invalid response creates no partial interventions.

Limits: cards5, recent attempts/card6, words/card4, observations5, interventions3, exercises/intervention4, options2–4, examples3, tips3, title120, short fields240, prose800, prompt24000 characters, output16000 characters, HTTP envelope128000 bytes, output token limit3000. Provider timeout1–90 seconds, service timeout90 seconds. Limits centralized in CoachLimits.

## Prompt

Prompt version `coach-v1`, schema1. System instructions treat content as untrusted DATA, prefer None for weak evidence, require bounded grounded claims, distinguish known outcomes/timing/history from unknown actual answers/confusions/psychological causes, prohibit scheduling/mastery/history/content changes and causal-effectiveness claims. Plain text structured teaching fields. Malicious card instructions and literal HTML were tested.

## Persistence

Migration `20261029000000_AddAiCoach` applied to real local PostgreSQL. Tables: `words.CoachAnalyses` (session/collection, provenance, status/decision, first language, retry/generation, validated observation JSON), `words.CoachInterventions` (card/revision, lifecycle/timestamps, structured private payload), `words.InterventionAttempts` (exercise/card, result/time, optional response duration, opaque option ID). No raw provider output/context retained or logged.

Unique session/prompt and intervention/exercise indexes; restrictive FKs preserve retention. Session row lock claims one analysis; provider runs outside transaction; final revision checks under sorted card locks, then atomic validated save. Pending lease120s, generation fencing, failed retry cooldown5s, max3 retries after initial generation. Recovery is request-driven; no automatic background retry. Completed cache reused despite later UI language/model changes. Feature flag returns ephemeral Disabled without mutating records and reenable restores reuse.

## Intervention

Explanation and MicroLesson support structured explanation/examples/tips; ExtraPractice supports separate questions. Suggested -> Accepted -> Completed, or Suggested/Accepted -> Dismissed. Accept and lesson Done are idempotent. ExtraPractice completes only after all distinct exercises have answers, never via lesson completion endpoint. User can leave immediately after ordinary session completion.

## Exercises

RecognitionChoice only: supplied word prompt, 2–4 choices, expected translation matched against same source word. Public DTO has prompt/options, answered flag and correctness only after answer; no expected option/solution. POST answer validates accepted/current intervention, membership and option; writes only InterventionAttempt/lifecycle. Same option and timing retry replays; changed input conflicts. Fake deliberately generates two small questions with opposite option placement and a simple word-pair lesson; no live linguistic-quality claim.

## Revision Safety

Card edit/reset advances existing LearningRevision. Unfinished previous-revision work maps to Stale, payload hidden, mutations409. Already completed work remains historical Completed with payload hidden and evaluation Unknown for changed revision. Deleted card maps to Unavailable, title/payload hidden, mutation404. Deleted collection blocks analysis/intervention access404. History remains internally retained under existing soft-deletion policy.

## Provider

Separate Coach provider reuses `AiCardGeneration` provider selection, OpenRouterOptions (key/model/URL/timeout), HttpClient DI and existing typed failure conventions. Fake requires no real key. OpenRouter uses structured schema, bounded streaming response and sanitized failure codes. Missing key fails noncritically instead of silently substituting advice. `LW_AI_COACH_ENABLED` wired in local/prod Compose; prod configuration syntax checked, no deployment. Existing AI card generation unchanged and regression tested.

## Security & Privacy

Identity-derived owner, never body UserId. Anonymous401; foreign session/intervention/answer404 before provider/mutation. Internal scope lookup stays behind facade. All new routes have gateway authentication. Public/shared/Explore/Fork DTO allowlists remain content-only; fresh fork inherits no Coach or learner history. Real two-user gateway checks plus Identity unit tests cover access. Prompt injection text cannot modify server actions/schema; untrusted model teaching prose still requires quality review.

## Evaluation Foundation

Indexed intervention -> card/revision/completedAt -> future ordinary LearningAttempt query. Returns count, first outcome/time and Unknown/NoFollowUpEvidence/FollowUpSuccess/FollowUpFailure. Again is failure, other current vocabulary including Hard is success. Supplementary answers never contribute. This is descriptive follow-up, not causal proof and not mastery input. Real gateway verified first post-lesson Good -> FollowUpSuccess; automated tests include Again/Hard/Good/Easy and revision/time exclusions.

## API

All authenticated with prefix `/api/learning`:

| Method | Path | Purpose |
|---|---|---|
| POST | `/sessions/{sessionId}/coach-analysis` | Separate analyze/reuse/retry; `{language: en|ru}` |
| GET | `/sessions/{sessionId}/coach-analysis` | Durable result/current availability |
| GET | `/interventions/{id}` | Private safe payload/status/evaluation |
| POST | `/interventions/{id}/accept` | Explicit start/resume |
| POST | `/interventions/{id}/dismiss` | Not now |
| POST | `/interventions/{id}/complete` | Complete accepted lesson |
| POST | `/interventions/{id}/exercises/{exerciseId}/answer` | `{optionId,responseTimeMs?}`; server result |

Analysis returns Pending/Completed/Failed/Disabled DTO; provider failure is noncritical and typed inside DTO. Existing learning APIs unchanged. Internal WebApi-only `/learning/interventions/{id}/scope` resolves collection for ownership check and is not gateway-exposed.

## Frontend

Standalone Angular Coach component only after durable adaptive-session completion. Loading/None/Disabled/noncritical failure/Retry; optional suggestions and dismiss; dedicated compact structured lesson; separate practice with progress and server feedback; stable retry payload; stale/deleted conflict reread. Pending polls every2s up to15 reads; unsubscribe on navigation/input change. EN/RU strings, wrapping narrow layout, keyboard actions and intentional focus to heading. No HTML/Markdown rendering and no precise mastery-score dashboard. Legacy due-only review remains independent without Coach.

## Tests

Final backend command (Docker, not direct sandbox dotnet):

```sh
docker run --rm --network learnword-local_default \
  -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
  -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

**337 passed,0 failed,0 skipped,9 projects**: Authorization7, IdentityService26, gateway E2E14, middleware6, BL79, DAL7, Identity.DAL6, Identity66, WebApi126. New Coach coverage115 WebApi cases +4 Identity tests. Tests include revision gaps/bounds/PII allowlist, malicious data, None, invalid schema/enums/IDs/limits, provider429/config/timeout/unavailable, durable cache/lease/generation/retry cap, edit during generation, lifecycle/correct+incorrect answers/duplicate/ownership, feature flag and follow-up; existing scheduler/planner/social/card generation regressions remain green.

Final frontend command (existing local test image with dependencies and Chromium):

```sh
docker run --rm \
  -v /Users/alekseikarpov/GitHub/lw/LearnWordWebApp/lw-app:/source:ro \
  -v /private/tmp/learnword-coach-chromium:/tmp/coach-chromium:ro \
  -e CHROME_BIN=/tmp/coach-chromium learnword/frontend-tests:local sh -c \
  'cp -R /source/src /app/src && cp /source/angular.json /source/tsconfig.json /source/tsconfig.app.json /source/tsconfig.spec.json /app/ && npm test -- --watch=false --browsers=ChromeHeadless && npm run build'
```

Wrapper is `#!/bin/sh` followed by `exec /usr/bin/chromium-browser --no-sandbox "$@"`. **91/91 Jasmine passed; production build passed.** Warnings: initial bundle527.04kB versus500kB warning budget (1MB error budget), existing collection CSS2.79kB versus2kB warning. No package-lock/dependency changes in frontend; backend QA added SQLite package to WebApi test project for real relational fixture.

`./deploy/local-up.sh` final successful; both local/prod `docker compose ... config --quiet` passed; `git diff --check` passed both repositories. Final test source/log summary is in `verification-results.json`; full transient logs in `/private/tmp/learnword-epic3a-*.log`.

## Real Gateway Acceptance

Commands from backend repo:

```sh
python3 agent-runs/019-ai-coach/verify-coach.py
python3 agent-runs/018-learner-model/verify-learning.py
python3 agent-runs/014-social-collections/verify-social.py
```

Coach **6 grouped checks passed**, including A None; B repeated Again/lesson; C practice with both correct/incorrect, double answer protection and unchanged full normal history/state/SRS; D stale/deleted content; E two-user/anonymous all routes; F shared content/fresh fork; extra future normal-review effectiveness query. Three simultaneous analyze requests returned one analysis ID and one intervention set. Supplemental answers produced real InterventionAttempts; database final count8,8 unique intervention/exercise pairs across disposable acceptance/browser fixtures, with no duplicate rows.

G executed against real OpenRouter HTTP path with local network-only node stub (`provider-stub.js`): delayed HTTP503 -> persisted `ai_provider_unavailable`, ordinary completion successful; then missing key -> configuration failure; flag false -> Disabled; restored Fake -> same analysis ID Completed. `verify-provider.py` prepare/unavailable/missing-key/disabled/retry/cleanup validates unchanged SRS/history/state and completion time each step. Provider selection was temporarily overridden using Compose override files in `/private/tmp`; stub used only local dummy credential, no paid API. Restore base Compose after test and remove stub. Runtime **4 provider/config checks passed**. Prior Epic2 **5 groups**, Social **6 groups** passed.

Before migration captured Id/DueDate/IntervalDays/EaseFactor/ReviewCount/LastReviewedAt/LearningRevision for all189 existing Card rows. Immediately after migration and after all acceptance, all189 are exactly unchanged. The additive migration is present in public.__EFMigrationsHistory. All runtime services are running; PostgreSQL healthy.

Initial startup was blocked by full Docker disk (PostgreSQL53100). Only reproducible unused Docker build cache was removed (4.394GB), no DB volume/images/user data. Migration then succeeded. Initial focused provider tests found outgoing JSON schema exceeding parser MaxDepth; outgoing serialization was separated from bounded untrusted-response parsing and all provider tests passed after fix. No unresolved infrastructure blocker remains.

## Browser Acceptance

Personally exercised localhost:8088 via current authenticated local test account. Desktop1280x900 and mobile390x844:

- RU adaptive review, actual two Again answers, deterministic completion2/0/2, Coach suggestions.
- Explicit micro-lesson, structured text/tip, keyboard Enter completion and focus restored to Coach heading.
- Separate practice correct then incorrect, server feedback, progress0->1->2 and result1/2; no SRS grade buttons.
- EN UI and None after two Good reviews: summary2/2/0 with zero Coach region.
- Delayed OpenRouter503: successful completion visible while Coach loads, compact noncritical failure, Retry after restoring Fake produces suggestions; Not now dismisses practice.
- Literal `<img src=x onerror=alert(1)>` card passed through Fake lesson/title as text. Coach DOM has0 injected img elements; no execution. EN lesson safely wraps at390px.
- Document scrollWidth=clientWidth=390 for lesson/practice/failure/plain-text mobile states; desktop1280 likewise. Keyboard focus reaches active lesson/exercise heading and returns to Coach heading.

Screenshots under [screenshots](screenshots/), including desktop RU, mobile lesson/practice/correct/incorrect/completion, None EN, loading/unavailable/retry and literal HTML. Temporary viewport override reset. Four browser-created collections soft-deleted; baseline user collection untouched. Gateway scripts also clean their disposable collections; evidence remains internally retained per policy.

## Residual Risks

- No paid/live LLM success sampled: actual model teaching quality, distractor ambiguity and provider-specific strict schema acceptance need real-provider sampling. HTTP success construction/parsing/error handling are covered by deterministic handlers; local runtime refusal503 is verified.
- Machine grounding validates references and observation gates, not semantic truth of every generated sentence/example. Prompt constrains this; rendered prose remains advisory.
- Conservative candidate/timing thresholds and bounded fallback are product heuristics; no clinical/scientific mastery or causal-effectiveness claim. Per-card bounded/count queries need profiling at extreme history/collection sizes; no load stress test.
- No old-session/history UI or automatic pending recovery job; cached language fixed by first generation. Request-driven retry is capped; users may still see retry after cap but no extra paid calls occur.
- Validated private teaching content is retained on soft deletion; account erasure/hard-purge retention and arbitrary PII inside authored card content remain outside this run.
- Initial bundle exceeds warning threshold by27.04kB; build remains within error threshold. No production deployment/verification.

## Recommendations for Epic 3B

1. Introduce versioned objective ExerciseDefinition/Submission contracts with CardId/LearningRevision, type, source/provenance, bounded public prompt and server-side answer key. Preserve ordinary SRS review and supplementary intervention evidence boundaries during any migration.
2. Promote current RecognitionChoice into a deterministic Recognition mode; add Active Recall (typed answer) and Reverse Recall using explicit card word/translation pairs, then constrained Fill Gap with validated source sentence/answer slot. Treat unsupported/ambiguous content as unavailable rather than infer grading with an LLM.
3. Add exercise-specific deterministic answer normalization: Unicode normalization, whitespace/case rules by language and explicit punctuation/acceptable-variant policies. Store bounded submitted answer/normalization/grading version only with an intentional privacy/retention contract; use Correct/Incorrect/Ambiguous rather than fabricating Again/Hard/Good/Easy.
4. Collect actual chosen/typed alternatives before confusion detection. Current self-rated SRS outcomes and AI-generated distractors cannot establish what word the learner confused. Link hypotheses only to observed alternatives, revisions and repeated evidence.
5. Build exercise-specific objective evidence separately from LearningAttempt, then design/calibrate recognition versus productive recall aggregates. Only after sufficient measured evidence consider active/passive mastery or multidimensional skills; keep scores deterministic, rebuildable and versioned.
6. Evaluate intervention usefulness with future ordinary current-revision recall, sample sizes/time windows and comparison baselines; descriptive follow-up status alone is not causal attribution. Add explicit history/resume UX and production provider quality sampling before expanding types.


## Review branches

Saved on user request after acceptance, with one local feature branch per nested repository, both named `feature/epic-3a-ai-coach`. Backend main remains at `57f7df9`; frontend main remains at `3fd5789`. Frontend commit: `1b5e511` — Add optional post-session AI Coach experience. Backend commit contains the Coach implementation, migration, tests, specification and delivery evidence, including this report. No push, PR creation or merge was performed as part of this save.
