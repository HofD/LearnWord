# QA — automated private Coach diagnostics

## Changes

Added `tests/LearnWord.WebApi.Tests/CoachDiagnosticsTests.cs` (25 diagnostic cases). Extended `CoachTestDatabase` with a SaveChanges call counter for meaningful read-only assertions. Extended `CoachIdentityServiceTests` with owner-only diagnostics forwarding, anonymous/foreign denial, upstream stub support, and explicit no-store response policy coverage (two new tests plus existing auth-test expansion). No production source edited by QA.

Coverage includes all eight selection reasons, bounded 20-card/6-attempt metadata, current/historical revision counts, immutable session counters, and explicit isolation from the existing model context. Tests verify absent word/translation/PII/auth/solution fields and UUID attempt IDs; model summary and invalid raw-output markers never enter the diagnostic snapshot. Existing strict Social DTO tests remain in the focused WebApi suite.

Persistence cases verify durable Pending metadata before provider invocation; captured snapshot stability after future normal evidence and card edits; NoCandidates versus actual Provider None; retained snapshots through typed failures, tracker clears, retry replacement and content revision races; obsolete successful/failed workers cannot overwrite a newer generation; interrupted workers leave readable Pending metadata. Captured and reconstructed GET paths invoke neither provider nor SaveChanges and leave ordinary SRS, LearningAttempt and learner caches unchanged. Legacy diagnostics remain explicitly ReconstructedNow/LegacyUnknown, including historical-prompt records after content edits; missing records never cause generation or aggregate-cache writes. Disabled diagnostics do not create records. Unknown reconstructed call statistics remain null.

## Focused Docker verification

```sh
docker run --rm \
  -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test tests/LearnWord.WebApi.Tests/LearnWord.WebApi.Tests.csproj \
  --disable-build-servers --logger 'console;verbosity=minimal'
```

**153 passed, 0 failed, 0 skipped.** Includes all prior WebApi/Coach regressions plus 25 new diagnostic cases.

Identity focused command used the same Docker mount/runtime and `tests/LearnWord.Identity.Tests/LearnWord.Identity.Tests.csproj`: **68 passed, 0 failed, 0 skipped**. `git -C LearnWord diff --check` passed at QA handoff. Root owns the final full backend suite, frontend suite/build, PostgreSQL migration, two-user gateway flows and browser acceptance.

## Limits

SQLite relational tests establish DTO filtering, nonmutation, generation fences and lifecycle persistence; PostgreSQL row locks and real gateway cache headers need root's acceptance checks. Provider handlers/Fake remain deterministic and make no paid model calls. `ProviderCalled` records entry to the abstraction invocation stage, not proof that OpenRouter delivered or billed an HTTP call. No diagnostic explanation of the model's hidden reasoning is inferred.
