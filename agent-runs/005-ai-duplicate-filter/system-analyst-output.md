# System Analyst Output

## Requirement Interpretation

The requested behavior is a backend generation refinement: keep the current AI card generation API shape, but remove suggestions that duplicate words already stored in the selected collection. The selected implementation point is after the AI provider returns and after provider response validation/trim, using a hash set for fast membership checks.

## Agent Plan

### system-analyst

- Update backend and frontend specs to reflect the new accepted behavior.
- Keep the public API contract stable.
- Record the delivery under `agent-runs/005-ai-duplicate-filter/`.
- Perform final acceptance against the user request.

### backend-dev

Scope:

- `LearnWord.WebApi/Abstractions/IAiCardGenerationService.cs`
- `LearnWord.WebApi/Controllers/CollectionsController.cs`
- `LearnWord.WebApi/Services/AiCardGenerationService.cs`

Assignment:

- Pass `collectionId` into `IAiCardGenerationService`.
- Inject `ICollectionService` into `AiCardGenerationService`.
- After the provider returns and `ValidateResponse` trims cards, load the target collection and build a trimmed, case-insensitive hash set of existing word values.
- Filter returned suggestions through that hash set so existing collection words and duplicate generated values are removed.

Guardrail:

```text
Do not run direct dotnet restore/build/test in the sandbox. Use ./deploy/local-up.sh for build/startup verification and cd LearnWord && ./tests/run-all-tests.sh for focused backend regression checks. If that cannot run, report the blocker instead of trying alternate sandbox builds.
```

### qa-backend

Scope:

- `tests/LearnWord.WebApi.Tests/AiCardGenerationServiceTests.cs`

Assignment:

- Update existing service tests for the new `collectionId` service signature.
- Add regression coverage proving provider suggestions already present in the collection are filtered after provider response.
- Cover case-insensitive and trim-based matching, plus duplicate values inside the provider response.

Guardrail:

```text
Do not run direct dotnet restore/build/test in the sandbox. Use ./deploy/local-up.sh for build/startup verification and cd LearnWord && ./tests/run-all-tests.sh for focused backend regression checks. If that cannot run, report the blocker instead of trying alternate sandbox builds.
```

### frontend-ui

No production frontend changes assigned. The current UI displays whatever `cards` the backend returns and persists selected suggestions through the existing card creation flow, so backend filtering is sufficient for this change.

## Verification Plan

- Run focused backend regression through `./tests/run-all-tests.sh` from `LearnWord/LearnWord`.
- If the local Docker stack is already available or cheap to start, use `./deploy/local-up.sh` for broader build/startup verification; otherwise record the residual risk.

## Known Risks

- This does not prevent duplicates created manually outside AI generation.
- This does not prevent a race where another request creates the same word between generation and saving.
- Returning fewer suggestions than `maxCards` is expected when provider output contains duplicates.
