# Backend — automated private Coach diagnostics

## Delivered

Added owner-only authenticated `GET /api/learning/sessions/{sessionId}/coach-diagnostics`, with explicit routes in both Ocelot configs, Identity session ownership check and internal WebApi read. Both controllers set no-store/no-cache response policy. No analysis POST or paid call is made by diagnostics GET.

Added nullable `CoachAnalysis.DiagnosticJson` and additive migration `20261102000000_AddCoachDiagnostics`; EF snapshot updated. Existing rows remain null and all SRS, attempts and learner aggregates remain untouched. Normal Coach/public/social DTOs are unchanged. Prompt stays `coach-v2`, model schema stays1; diagnostics use their own version1.

`CoachContextBuilder.BuildWithDiagnostics` returns an internal envelope containing the existing provider context and a separate metadata-only diagnostic context. `Build` still returns only the original provider context. Selection still follows card ID ordering and includes at most five candidates. Diagnostics evaluate at most20 planned members with at most six recent current-revision attempts each, SQL counts rather than loading unbounded history, and explicit reasons: Included, RevisionMismatch, InsufficientEvidence, NoSessionAttempt, SessionNotProblematic, ContentUnavailable, ContentTooLarge, CandidateLimit. Missing/deleted membership is ContentUnavailable with nullable current revision. CandidateLimit means a later card met all eligibility/content checks. Diagnostic counters cover all bounded members; provider context counters preserve the original window ending at the fifth included candidate, so diagnostics do not change prompt input.

Snapshot is captured and committed before provider invocation, fenced by analysis ID/generation/Pending status. The provider-invocation stage is committed before entering the provider. Success/failure updates status/source/counts/timings without losing the snapshot after `ChangeTracker.Clear`; superseded workers cannot overwrite newer generation diagnostics. Retries replace the previous generation snapshot. Per-generation structured logs contain only analysis/session IDs, generation, source/decision, candidate count, provider-stage flag, duration/character counts and failure code.

## Actual JSON fields

`schemaVersion`, `sessionId`, nullable `analysisId`, `mode`, `capturedAt`, `analysis`, `context`, nullable `providerCalled`, `inputCharacters`, `outputCharacters`, `durationMs`, and `truncated`.

`analysis`: status, decision, decisionSource, provider, model, promptVersion, schemaVersion, failureCode, generation, retryCount. **No summary or other free model prose** is exported or retained in diagnostics.

`context`: planned, completed, successful, failed, candidateCount, revisionMismatchCards, contentOmittedCards, cards. Each card contains cardId/sessionRevision/currentRevision, selectionReason, current/historical revision evidence counts, session outcome/timing, current deterministic status/mastery/source/modelVersion, and bounded recent attempt outcome/date/timing/revision metadata. No attempt UUIDs, words, translations, transcriptions, prompts, raw output, option answers, identity IDs, credentials or auth/config values are included.

Decision sources are factual: NoCandidates (backend short-circuit), Provider (including valid provider None), Failed, Pending, Disabled or LegacyUnknown. No invented model reason is generated. Captured output never changes None into an intervention.

## Legacy and metric semantics

Captured records return `CapturedAtAnalysis`. A record without DiagnosticJson, or a session without an analysis, returns `ReconstructedNow` using present revision/aggregate data and the existing session-completion evidence cutoff. This is not claimed to be the old context. No-analysis status is NotRequested, or Disabled if the feature is currently disabled. Completed legacy records use LegacyUnknown rather than inferring whether a provider was called. Known durable Failed/Pending/Disabled statuses remain labelled as such. Unrecorded provider/timing/character metrics are null. GET does not save reconstruction.

`providerCalled` records entry/preparation of the provider invocation stage. In a Pending/interrupted request it is not proof of HTTP delivery, model execution or billing. `inputCharacters` measures serialized allowlisted context only, not the full provider HTTP request/system prompt/schema. `outputCharacters` measures returned provider text length before validation. `durationMs` covers context building/provider/validation and diagnostic persistence after the durable claim, not the whole gateway request.

Diagnostic JSON is hard-bounded to48000 characters. Normal20×6 metadata fits; defensive trimming drops oldest recent metadata and explicitly sets `truncated=true`. Existing content-free counters remain. Reconstructed results are similarly bounded by20×6 typed fields; no diagnostic state is passed into the model.

## Verification and limits

Root owns full Docker migration, backend/frontend suites, two-user gateway and browser acceptance. QA focused Docker results: WebApi153 passed and Identity68 passed, zero failures/skips. Coverage includes all reasons, snapshots, privacy, failed/None/intervention metadata, legacy read-only behavior and generation fencing. `git diff --check` passed at handoff. No direct sandbox dotnet commands run.

Historical snapshots intentionally retain content-free review metadata after card edits/deletion; collection ownership/active-session checks still gate reads. Current feature configuration or current content may differ from the captured generation; the mode/timestamp make the distinction explicit. Account purge/retention remains the existing separate policy. No production deployment or prompt reanalysis performed by this change.
