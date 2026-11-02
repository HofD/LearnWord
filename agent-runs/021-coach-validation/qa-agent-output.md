# QA: precise Coach validation diagnostics

Added deterministic regressions for malformed/missing/null/enum/duplicate/unknown fields, nested malicious property names, typed GUIDs, bounded array indexes, evidence references/gates, and separate prompt/paired-translation mismatch codes. Tests assert exact sanitized stage/code/path and preserve the exact UpstreamServiceException/public error contract.

Added service coverage for durable failure metadata, read-only repeated export, cleared metadata on successful retry, no partial interventions or learner-state changes, HTTP status persistence, and old JSON compatibility. Updated cache test from historical coach-v2 to current coach-v3. Request tests verify require_parameters=true and schema bounds while keeping configured model, token budget and reasoning unchanged. Card-generation request remains without Coach routing parameters.

Changed only tests: CoachValidationDiagnosticsTests.cs (new), CoachDiagnosticsTests.cs, CoachServiceTests.cs, AiCoachProviderTests.cs, ConfiguredAiCardGenerationProviderTests.cs.

Verification: git diff --check passed. Root owns Docker full-suite and gateway-stub acceptance; no duplicate test process or paid provider call started by QA. Final Docker results are recorded in final-acceptance.md.

Residual risk: deterministic tests and HTTP stub validate failure classification, not the cause of the historical production Gemma reply whose raw content was not retained. A future sanitized production export is needed to identify its actual failing rule.
