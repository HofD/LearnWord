# Private Coach diagnostics

Adds read-only observability to Epic3A with current prompt coach-v5, unchanged provider result schema1, candidate selection, normal SRS and learner evidence. A model decision None remains valid. Diagnostics distinguish selection/pipeline facts, not internal model reasoning.

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

## Precise failure diagnostics (coach-v3)

New captured failures add nullable `analysis.failureStage`, `validationCode`, `validationPath` and `providerHttpStatus`. The existing `failureCode` and ordinary Coach error UI remain compatible. Older stored/reconstructed snapshots cannot recover the exact rejection rule and leave these fields null. A new explicit analysis after upgrade uses the v3 cache key; GET diagnostics never retries a paid request.

Validation paths refer only to known schema fields and bounded array indexes. Unknown field names map to their containing known object. Neither exception messages nor model values/property names are copied into these fields. Structured logs use the same safe metadata. `exercise_prompt_mismatch` identifies a prompt that is not a literal supplied word value; `exercise_answer_mismatch` identifies a correct option that does not match the translation paired with that exact value. These are distinct from JSON/schema, evidence, reference and limit errors. Transport/configuration/envelope failures are identified separately where known; a numeric provider HTTP rejection status may be retained without its body.

The Coach request requires structured-output parameter support through `provider.require_parameters=true`. The prompt and schema clarify literal word prompts, paired translations, evidence eligibility, and lesson/exercise alternatives. The flag does not replace semantic validation or guarantee a useful intervention. Model selection, card generation, reasoning and token budgets are unchanged. Raw model output remains unretained, so an old `ai_provider_invalid_response` cannot retroactively be attributed to a particular rule.

## Provider response and parser metadata

Captured generations additionally expose nullable root `providerResponse`: numeric `httpStatus`, allowlisted normalized `finishReason` (unknown string becomes `Other`, missing/invalid stays null), nonnegative integer `promptTokens`, `completionTokens`, `totalTokens`, and `reasoningTokens`. Fields are copied independently from the envelope; missing/invalid/negative/overflowing counts stay null rather than being estimated. No native provider finish strings, provider errors, cost/account metadata or generated reasoning text are retained. Envelope fields follow the [OpenRouter Chat Completions reference](https://openrouter.ai/docs/api_reference/overview).

Nullable `outputFormat` contains only six boundary flags after whitespace trim: `startsWithJsonObject`, `startsWithJsonArray`, `endsWithJsonObject`, `endsWithJsonArray`, `startsWithCodeFence`, `endsWithCodeFence`. They describe surface syntax only: matching braces do not prove valid/complete JSON, and a missing boundary does not itself diagnose the cause. No output repair, fence removal or acceptance relaxation is performed.

Syntax failures additionally expose nullable `analysis.jsonLineNumber` and `analysis.jsonBytePositionInLine`: bounded, zero-based System.Text.Json parser coordinates (UTF-8 byte position within the line, not character index). No JsonException message/path or failing character is retained. The existing `validationCode=json_malformed` remains compatible.

Provider metadata survives output validation failure, and is also recorded on successful responses. Safely parsed metadata may remain available for empty/oversized content errors; malformed HTTP envelopes may leave it unknown. Retry replaces all response metadata; existing generation fencing still applies. Immutable per-call provider results prevent concurrent requests exchanging metadata. Old snapshots have null additions, no backfill or automatic paid replay. Prompt remains coach-v3, model/budgets and card generation unchanged; no new migration or frontend change. For an existing failed session, explicitly request retry (subject to existing retry cap), or analyze a fresh completed session after backend deployment. Opening/downloading diagnostics alone does not rerun analysis.

The snapshot `truncated` flag still means diagnostic storage trimming only. `finishReason=length` is provider-reported termination metadata; inspect it with token counts and parser position rather than inferring model truncation from output character count. `stop` does not guarantee valid JSON. Provider HTTP status is retained in providerResponse even when subsequent local validation fails; analysis.providerHttpStatus also falls back to this known status for local validation failures.


coach-v4 now sends mutually exclusive intervention schema alternatives, matching the existing lesson/practice semantic rules. The same failure codes/paths remain for mixed content. The prompt/description one-observation-per-card rule is still backed by semantic duplicate detection. Historical v3 diagnostic snapshots and evidence remain unchanged; new explicit generations use v4 cache provenance. Existing response metadata and parser diagnostics remain available.


coach-v5 explicitly adds nonempty minima for mandatory text while retaining intended empty fields and semantic whitespace checks. Rejections still export text_empty plus the same known JSON field path; stored historical v4 snapshots are unchanged. No new diagnostic fields or migration.
