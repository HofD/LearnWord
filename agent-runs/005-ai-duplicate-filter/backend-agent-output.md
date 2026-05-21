# Backend Agent Output

## Owner And Scope

Backend production implementation for AI duplicate filtering in `LearnWord.WebApi`.

## Changed Files

- `LearnWord/LearnWord.WebApi/Abstractions/IAiCardGenerationService.cs`
- `LearnWord/LearnWord.WebApi/Controllers/CollectionsController.cs`
- `LearnWord/LearnWord.WebApi/Services/AiCardGenerationService.cs`

## Implementation Notes

- `GenerateCards` now receives `collectionId`.
- `AiCardGenerationService` injects `ICollectionService`.
- After the provider returns and response suggestions are validated/trimmed, the service loads the target collection, builds a trimmed case-insensitive `HashSet<string>` of existing word values, and keeps only suggestions whose normalized value can be newly added to the set.
- The same hash set also collapses duplicate values returned in one provider response.

## Spec Impact

Backend API behavior changed without changing request or response DTO shape. The backend spec now documents duplicate filtering.

## Verification

`./tests/run-all-tests.sh` from `LearnWord/LearnWord`: passed. All 9 backend test projects passed.

## Residual Risk

Manual card/word creation can still create duplicates; this change is scoped to AI generation suggestions.
