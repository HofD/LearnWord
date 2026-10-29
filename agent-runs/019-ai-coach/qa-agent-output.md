# Backend QA — Epic 3A AI Coach

## Scope and changed files

Added `CoachTestDatabase.cs`, `CoachContextTests.cs`, `CoachBoundaryTests.cs`, `CoachOutputValidatorTests.cs`, `AiCoachProviderTests.cs`, `CoachServiceTests.cs` in `tests/LearnWord.WebApi.Tests`; added SQLite package there to use isolated relational tests. Added `CoachIdentityServiceTests.cs` in `tests/LearnWord.Identity.Tests`. No production files edited by QA.

115 new WebApi test cases cover versioned allowlist/privacy and strict Social DTO boundaries; current-revision evidence, old-revision counts, bounded card/history/content selection, session membership, incomplete/deleted sessions, malicious card data; None and bounded grounded lesson/practice validation; unknown enums/IDs/revisions/attempt references, duplicate observations/options/interventions, unsupported exercise types, missing schema fields, oversized/nested output, exact word-to-translation recognition pairing; configured/Fake/OpenRouter behavior, malformed envelopes, response/prompt bounds, key/config/network/timeout/rate-limit errors, data/instruction separation; cached analysis, independent session completion, controlled retry, Pending reuse/lease recovery, obsolete generation suppression, retry cap and concurrent content edit; feature flag suppresses cached analysis without deleting it, disabled fresh reads/analysis make no record/call, reenable reuses cached analysis or generates fresh, and legacy Disabled records recover without consuming retry allowance; durable accept/dismiss/complete, stale/deleted content, correct/incorrect server answers, duplicate/conflicting answer semantics, no answer solution in exercise DTO, unchanged SRS/ordinary ledger/learner aggregate, separate InterventionAttempt persistence and revision-aware normal follow-up evaluation.

Four new Identity tests cover all seven anonymous controller actions without upstream access, foreign session analysis/read denial, foreign intervention/read/accept/dismiss/complete/exercise denial before content or mutation, and owned identifiers/language/answer forwarding.

## Commands and results

Focused final run through Docker SDK (repository's required backend execution surface):

```sh
docker run --rm \
  -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test tests/LearnWord.WebApi.Tests/LearnWord.WebApi.Tests.csproj \
  --disable-build-servers --logger 'console;verbosity=minimal'
```

**126 passed, 0 failed, 0 skipped** (115 new Coach cases and 11 existing WebApi cases). `git -C LearnWord diff --check` passed at QA handoff. Root owns final nine-project regression run, real PostgreSQL migration/gateway flows and browser acceptance; their final results are recorded in final-acceptance.md.

First focused run exposed a real OpenRouter defect: outgoing schema exceeded the untrusted parser's shared MaxDepth16 before HTTP. Backend owner separated outgoing request serialization from bounded result parsing. Final provider tests confirm request construction and typed transport paths work with deterministic HTTP handlers and no API key/network dependency.

## Limits

SQLite exercises relational persistence/transactions and service generation checks, but does not prove PostgreSQL row-lock behavior. Root real gateway tests cover duplicate requests and ownership with actual PostgreSQL. HTTP handlers validate transport/error handling without a paid real-model call; linguistic usefulness and provider schema compatibility need later real-provider sampling. No external provider output/secrets are retained in tests or reports.
