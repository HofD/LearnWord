# QA: response metadata and JSON parser diagnostics

Added24 deterministic cases in CoachResponseMetadataTests.cs. Covers allowlisted stop/length/content_filter and safe Other finish reason; missing/invalid/negative/overflow token usage; usage and HTTP metadata retained for empty/oversized content and malformed choices; zero-based numeric JSON positions including UTF8 byte offsets; default interface compatibility; Configured forwarding and concurrent per-request metadata isolation.

Service cases cover successful and malformed responses, whitespace-aware object/array/fence flags, stored/read-only diagnostic round trips, retry clearing of old usage/positions/fence flags, unknown legacy fields, provider-content failure metadata, no raw text/key leakage, no partial interventions and unchanged normal learning state/history. Existing generation-fence tests remain in the full runner.

Verification: git diff --check passed. No direct host dotnet, duplicate Docker test process or paid provider call. Root owns ./deploy/local-up.sh, full Docker ./tests/run-all-tests.sh and authenticated HTTP-stub/gateway acceptance; final results belong in final-acceptance.md.

Residual risk: these tests establish observability and safe persistence. They cannot recover finish_reason/usage/raw syntax from the historical production response, and format flags alone do not prove why generation ended.
