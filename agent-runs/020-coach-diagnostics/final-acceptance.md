# Automated Coach diagnostics — final acceptance, 2026-11-02

Accepted locally. User needed repeated production investigation without manual SQL/database-client exports. Implemented on existing feature/epic-3a-ai-coach in both repositories; main is unchanged. No production deployment, paid LLM calls, forced intervention or prompt/schema change.

## Delivered

- Owner-only authenticated GET `/api/learning/sessions/{sessionId}/coach-diagnostics`, Identity scope check + explicit gateway routes, no-store headers. No provider invocation or writes on read; no analysis regeneration.
- Text-free bounded evidence/filter snapshot captured before provider stage, durably fenced to current generation. Updates completion/failure/timing metadata; survives tracker clear, later review/edit and request interruption. Nullable `DiagnosticJson`, additive `20261102000000_AddCoachDiagnostics` migration, real PostgreSQL applied successfully.
- Distinguishes NoCandidates/Provider/Failed/Pending/Disabled/LegacyUnknown. Provider+None means model abstained; diagnostics do not invent its internal reason. CapturedAtAnalysis versus honest ReconstructedNow for legacy/no snapshot. Runtime metrics unknown when unrecorded.
- All8 selection reasons, at most20 planned cards and6 recent current-revision attempts/card, exact current/historical counts, revisions/session outcome/state source/model version, nullable provider-stage flag and char/timing metrics. 48000-character snapshot bound; Truncated flag marks fallback removal.
- No card words/translations, free model summary/prose, raw output, tokens/keys/PII/config or exercise solutions. Metadata is still private learner data; ordinary ownership/retention restrictions apply. Minimal structured operational logs contain IDs/stages/counts/timing only.
- Lazy authenticated `/coach-diagnostics/:sessionId` page, RU/EN, captured/reconstructed explanation, readable summary + interpolated JSON, local Download JSON, retry/error states. No Analyze POST; existing authentication interceptor reused. Technical completion link only when review URL contains `coachDebug=true`.

Full contract/limitations: [specs/coach-diagnostics.md](../../specs/coach-diagnostics.md).

## Production use after deployment

Deploy both updated feature branches with normal migration workflow, sign in on learnword.online and open:

`https://learnword.online/coach-diagnostics/21beb61d-a1d7-4da9-8c50-c1ae67bec514`

Download JSON and attach it to investigation. The reported old analysis will be reconstructed now, not claimed as its exact historical input. For future sessions launch review with `?today=true&coachDebug=true`; completion contains a diagnostics link even when Coach is hidden for None. No DB client/SSH export/copied token is needed.

## Verification

Full local Docker stack rebuilt with `./deploy/local-up.sh` (final log `/private/tmp/lw-diag-local-up-final.log`), PostgreSQL migration completed. No production changes.

Full backend command:

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

**366 passed,0 failed,0 skipped across9 projects**: Authorization7, IdentityService26, E2E14, middleware6, BL79, DAL7, Identity.DAL6, Identity68, WebApi153. New25 WebApi diagnostics cases +2 Identity tests. Covers all selection reasons/bounds, no provider-contract pollution, source/summary/raw/secret/solution exclusions, snapshot before provider, failures/retries/fences/crash, legacy reconstruction/no SaveChanges on GET, immutable metadata after edit, foreign/anonymous/no-store, no normal learning mutation. Existing Coach/scheduler/learner/social tests remain green. Log `/private/tmp/lw-diag-all-tests.log`.

Frontend ran full `npm test -- --watch=false --browsers=ChromeHeadless` and `npm run build` in existing Docker `learnword/frontend-tests:local` image with Chromium no-sandbox wrapper (full command in frontend-agent-output.md). **105/105 Jasmine tests passed**, production build passed. No package/dependency changes. Warning: initial533.78kB versus500kB warning threshold (1MB error); existing collection CSS2.79kB versus2kB warning. Diagnostics lazy chunk6.77kB raw/2.04kB transfer.

`python3 agent-runs/020-coach-diagnostics/verify-diagnostics.py`: **5 grouped gateway checks passed**. Deterministic no-candidate None; repeated Again -> Provider/Intervention; repeated Hard -> Provider/None despite candidate; owner/foreign/anonymous, repeated reads stable/text-free/no SRS/history/state mutations, captured snapshot unchanged after normal review/content edit. Initial Hard fixture was corrected to freeze a fresh-card plan first: standalone Hard is neither Weak nor immediately due. This was test preparation, not a product change. Final log `/private/tmp/lw-diag-gateway-final.log`.

`python3 agent-runs/019-ai-coach/verify-coach.py`: all6 previous Coach gateway groups passed after diagnostics changes. Log `/private/tmp/lw-diag-coach-regression.log`.

Created one old analysis before migration, read after migration: ReconstructedNow/LegacyUnknown. New analysis fixtures returned CapturedAtAnalysis with Provider or NoCandidates. Actual gateway Cache-Control:no-store verified for all3; schema contains Truncated=false. New GET remains useful even when ordinary None Coach block is hidden.

## Browser acceptance

Personally used local test account via normal login on localhost:8088, desktop1280x900 and mobile390x844. Captured mode in EN, reconstructed legacy in RU with explicit warning, deterministic None diagnostics, read-only JSON download and debug-gated completion link. Downloaded actual `learnword-coach-diagnostics.json`, parsed it as JSON and verified marker content absent. Browser UI did not invoke a new analysis on read/download. Completed an actual Good review under coachDebug=true; ordinary Coach hid for None, diagnostics link led to NoCandidates/SessionNotProblematic page. Unknown session produced localized error+retry rather than a blank result. Mobile document width/scrollWidth390; JSON wraps and scrolls internally. Screenshot evidence in screenshots/. Temporary viewport reset.

No paid provider call or production account used. Three disposable browser collections soft-deleted after checks; existing local collection preserved. Gateway fixtures clean their collections. Downloaded local test JSON is in user Downloads; it contains only synthetic diagnostic metadata.

## Practical limitations

- Legacy context cannot be recovered exactly retrospectively. ReconstructedNow uses current revisions/cache and completion-bounded history; do not treat it as original model input.
- Text-free metadata explains selection/pipeline/evidence, not teaching semantics or model reasoning. Selected word text may still be needed separately for a linguistic-quality issue.
- ProviderCalled is invocation-stage metadata, especially for interrupted Pending; it is not proof remote HTTP/billing occurred. InputCharacters is context length, not prompt tokens/cost. Retry keeps last-generation snapshot only.
- Private diagnostics retention follows analysis retention; no new account-erasure policy or administrative cross-user endpoint. Debug page is intentionally outside normal navigation and requires deployment of both repos.
- Initial bundle warning remains. Production diagnosis of the reported session still requires deployment and the owner's JSON export; this run does not claim to have read production data.
