# Final Acceptance

## Decision

Accepted.

## Changed Files

- `LearnWord/LearnWord.WebApi/Abstractions/IAiCardGenerationService.cs`
- `LearnWord/LearnWord.WebApi/Controllers/CollectionsController.cs`
- `LearnWord/LearnWord.WebApi/Services/AiCardGenerationService.cs`
- `LearnWord/tests/LearnWord.WebApi.Tests/AiCardGenerationServiceTests.cs`
- `specs/backend-api.md`
- `specs/frontend-behavior.md`
- `docs/ai-features.md`
- `agent-runs/005-ai-duplicate-filter/*`

## Verification

Command:

```bash
./tests/run-all-tests.sh
```

Run from `LearnWord/LearnWord`.

Result:

- All 9 backend test projects passed.
- `LearnWord.WebApi.Tests` passed with 11 tests, including the new duplicate filtering regression.
- `LearnWord.Api.E2E.Tests` remained skipped for its existing gateway tests, and the script reports the project as passed.

## Checks Not Run

- Full `./deploy/local-up.sh` Docker startup was not run because the behavior change is internal WebApi service logic with no route, DTO, migration, or frontend shape change, and the focused backend regression script passed.
- Frontend build/browser verification was not run because Angular code did not change.

## Remaining Risks

- The change filters AI suggestions only; manual card/word creation can still create duplicates.
- A race is still possible if another request saves the same word after generation but before the user saves selected suggestions.
- The provider may return fewer useful suggestions than requested if many generated words already exist in the collection.

## Recommended Next Step

If product policy becomes "no duplicate words in a collection at all", add enforcement to card/word creation as a separate backend behavior change.
