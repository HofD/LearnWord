# Backend delivery — Epic 3A AI Coach

## Implemented

Additive private advisory layer in WebApi (`CoachContextBuilder`, `CoachService`, independent `IAiCoachProvider` and fake/configured/OpenRouter providers). Identity remains owner-checking facade; six explicit authenticated Ocelot route templates added to each gateway config. Public API matches `specs/ai-coach.md`, plus internal-only intervention scope lookup for ownership checks before content reads.

`CoachAnalysis`, `CoachIntervention`, `InterventionAttempt` store durable provenance, validated observations, bounded private lesson/exercise payload, lifecycle and separate exercise responses. Additive `20261029000000_AddAiCoach` migration and updated EF model snapshot. Existing SRS/normal history/learner services unchanged.

Analysis is separate from session completion. Initial durable claim uses session lock; provider call runs outside DB transaction; finalize locks analysis and sorted cards then rechecks active content/revisions. One session/prompt analysis, completed cached, pending reused; 120-second expired lease recovery, 90-second provider ceiling, generation fencing, max three retries with five-second cooldown. Raw provider output and content/error bodies never logged or retained.

Context uses repeatable-read snapshot, completed session membership only, maximum five candidate cards, six current-revision attempts/card anchored to completion, current aggregate explicitly named, old revision count separate. At least two observations and an actual difficult/slow session answer required before paid call. Oversized/empty content omitted with explicit count. If current cache is absent/mismatched, bounded deterministic fallback is labeled `BoundedObserved`; it is not claimed as full authoritative cache. No identity ids, email, credentials, social metadata or unrelated collections.

Output parser rejects invalid JSON, unknown members, missing required fields, enum/type errors, excessive fields/counts, duplicate observations/card-type interventions, unknown/revision-mismatched mappings, invalid evidence references and unsupported grounding. RecognitionChoice prompt must exactly match a supplied word with its corresponding correct translation. Exercise IDs generated server-side, answers omitted from all public DTOs, server validates opaque option IDs. Lesson completion and exercise answers never invoke scheduler, create LearningAttempt or write LearnerCardState.

Interventions support Explanation, MicroLesson and ExtraPractice; only RecognitionChoice exercises. Suggested→Accepted→Completed or Dismissed; repeat actions/answers idempotent, differing submitted answer conflicts. Card edits/reset make unfinished older work Stale; deleted card makes it Unavailable; payload hidden and mutations rejected. Completed older work remains historical. Completion links ordinary future current-revision attempts to diagnostic follow-up count/first outcome/date; diagnostic only.

OpenRouter reuses existing `AiCardGenerationOptions.OpenRouter`, credentials/model/timeout/endpoint and typed failure conventions. Separate domain provider avoids forcing coach into card-extraction interface. Structured JSON schema, bounded HTTP envelope and content sizes, request timeout, safe logging. Existing card generation code unchanged. Fake produces deterministic bounded lesson + two exercises for repeated Again evidence. `AiCoach:Enabled` (`LW_AI_COACH_ENABLED` in local Compose/example) disables independently. Disabled analysis POST/GET return ephemeral Disabled DTOs, hide cached suggestions and perform no provider call or persistence; re-enabling preserves/reuses completed analyses. Existing intervention/history routes remain available. Missing key is configuration failure, not silently replaced with fake.

## Verification

Root owns final Docker startup/migration/full-suite/gateway/browser acceptance. QA's focused Docker WebApi run reported 126 passed, zero failed/skipped at handoff (including current revisions, input validation, provider failures, lifecycle, answer isolation and diagnostic evaluation); includes expired lease recovery, generation fencing, retry exhaustion and card edits during provider calls. `git diff --check` passed. Outgoing schema serializer depth issue discovered by QA and fixed; untrusted response parser retains depth16.

## Residual limits

Observation semantics cannot fully prove arbitrary generated prose; references/type grounding and prompt constrain it. Generated distractor pedagogical quality remains model-dependent. Follow-up status is descriptive and does not establish causation. AI input excludes account PII, but user-controlled learning text itself may contain personal content. Soft-delete retention and account purge remain separate policy. Fixed conservative limits/heuristics may omit useful advice; None is intentional.
