# Backend response metadata

Added nullable providerResponse/outputFormat diagnostic DTOs and zero-based JSON parser line/byte positions. Provider response carries only numeric HTTP/token counts and allowlisted normalized finish reasons; unknown strings become Other, malformed/missing counts remain null. Native finish reason, raw output, model field names/values, exception messages/paths and credentials are not retained. Six whitespace-trimmed boundary flags are informational and do not claim validity/completeness.

IAiCoachProvider keeps Analyze returning a string and gains a default AnalyzeWithDiagnostics method for existing providers/tests. Immutable per-call CoachProviderResponse carries metadata; OpenRouter and Configured forward it without shared last-response state. Service captures it before validation and persists under existing generation fence; retries create fresh snapshots. Metadata is available on empty/oversized content and readable malformed envelopes; JSON parser failure positions are bounded nonnegative numbers. Historical snapshots have null additions; no migration.

Changed production files: CoachDiagnosticsDtos, IAiCoachProvider, CoachContracts, CoachFailure, CoachOutputValidator, CoachService, ConfiguredAiCoachProvider, OpenRouterAiCoachProvider. Prompt coach-v3/schema1, model, budgets, strict validation, public errors, card generator and learning data remain unchanged. No automatic replay/repair.

Verification: git diff --check passed. Root owns Docker build/full test runner and authenticated gateway HTTP-stub acceptance; no host dotnet or live/paid request run here. QA owns tests and root owns specs. No commit/merge/push by specialist.

Limit: provider-reported length is evidence of its stop reason, not proof that a particular syntax error was caused by token exhaustion. No historical missing metadata can be recovered retrospectively. Snapshot Truncated remains unrelated to model completion.
