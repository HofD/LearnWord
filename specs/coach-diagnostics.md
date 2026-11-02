# Private Coach diagnostics

Adds read-only observability to Epic3A without changing prompt coach-v2, provider result schema1, candidate selection, normal SRS or learner evidence. A model decision None remains valid. Diagnostics distinguish selection/pipeline facts, not internal model reasoning.

## Use

After deploying backend+frontend from feature/epic-3a-ai-coach and applying normal migrations, sign in and open `/coach-diagnostics/{sessionId}`. Download JSON from this page. For the reported production session: `/coach-diagnostics/21beb61d-a1d7-4da9-8c50-c1ae67bec514`. No database client, SSH export, copied token or repeated paid analysis is required. Future sessions can expose a debug-only completion link when launched as `/review/{collectionId}?today=true&coachDebug=true`; ordinary study flow has no diagnostics link.

GET `/api/learning/sessions/{sessionId}/coach-diagnostics` is gateway-authenticated and checked by Identity against active owned session/collection. Anonymous401; foreign/missing/deleted collection404. Both internal and facade responses use Cache-Control:no-store. No ownership parameter in request, no provider call or database write on GET. Existing intervention solution payloads are never exported. Authenticated private page uses existing HTTP interceptor, not manual credentials.

## Stored and reconstructed data

`CoachAnalysis.DiagnosticJson` is nullable text, added by migration `20261102000000_AddCoachDiagnostics`. New generation captures text-free context diagnostics durably before provider invocation, under analysis lock and generation fence. It updates pipeline status/result/error and duration after completion/failure; same completed cache returns same snapshot. Retry replaces the last-generation snapshot; it is not an append-only execution tracing system. No generated teaching text or card words/translations are newly retained. Diagnostic retention follows existing analysis soft-delete retention; physical erasure remains outside Epic3A.

- CapturedAtAnalysis: bounded evidence snapshot available to that generation. Survives later review, edits and tracker clear. Pending/crash state is visible when context was captured.
- ReconstructedNow: no stored snapshot (legacy, no analysis or interruption before capture). Read-only reconstruction from current data, explicitly not an exact historic request. Historical attempt list still stops at session completion. Current content revisions/cache state may have changed. Unknown runtime metrics/provider call remain null rather than invented.

Version1 diagnostic DTO exports session/analysis IDs, capturedAt/mode, status/decision/decisionSource, provider/model/prompt/schema, failureCode, generation/retry count; session planned/completed/success/failure; candidate and gap counts; at most20 planned-card metadata rows with IDs/revisions, selection reason, exact observed/historical counts, session outcome/timing, optional current state status/score/source/model version and at most6 recent current-revision outcomes/timestamps/durations. No attempt UUIDs are needed here. Runtime metadata: nullable providerCalled/inputCharacters/outputCharacters/durationMs and Truncated flag.

Sources: NoCandidates (deterministic skip), Provider (valid model result including None), Failed, Pending, Disabled, LegacyUnknown. NotRequested is a status when no analysis exists, not a model decision. Provider does not explain why the model abstained. ProviderCalled records abstraction invocation stage/preparation, especially in Pending; it does not prove HTTP reached the remote provider or a paid request occurred. InputCharacters counts serialized allowlisted context, not the HTTP request/system schema, tokens or billable usage. Duration includes context/persistence/provider generation until finalizing diagnostic metadata; it is not pure model latency.

Selection reasons: Included, RevisionMismatch, InsufficientEvidence, NoSessionAttempt, SessionNotProblematic, ContentUnavailable (including missing/deleted card), ContentTooLarge, CandidateLimit. Diagnostics inspect bounded full membership to explain omitted cards; the ordinary model candidate order/limit and original gap-counter window remain unchanged. Diagnosed state is metadata only, never saved to LearnerCardState.

Hard serialized snapshot bound48000 characters. Truncated=true when fallback drops oldest bounded attempt details. It is a diagnostic snapshot, not a complete user history. Structured operational logs contain only IDs, counts/stage, decision/failure and timing/character sizes. They exclude words, translations, model summary/prose, raw output, headers, credentials and configuration.

## Frontend

Lazy authenticated route supports EN/RU, loading/error/GET retry, explicit captured/reconstructed warning, compact key summary and full JSON via Angular interpolation. Download creates a local JSON Blob with fixed filename. No Analyze POST or extra LLM calls. At390px JSON wraps inside a separately scrollable keyboard-focusable panel. A failed/missing session shows localized error and Retry rather than blank output. Main completion remains independent.

## Limits

Legacy production records cannot retroactively acquire an exact original input; this page provides labelled reconstruction. Captured metadata intentionally omits content, so linguistic teaching-quality investigations may still require the owner to share selected card text separately. Model summary/raw output are not retained in diagnostic export and no model chain-of-thought is requested or claimed. Existing AI result API still exposes its validated user-facing summary. No admin-wide data access, telemetry service, automatic replay or forced intervention is introduced.
