# System Analyst Output

## Requirement Interpretation

The first AI feature should be an AI card generator. The user explicitly rejected placing LLM logic inside `LearnWord.Identity`, so the implementation keeps identity as a thin ownership-checking facade and places LLM integration in `LearnWord.WebApi`.

OpenRouter is acceptable for the initial stage. The implementation should support free or low-cost OpenRouter models through configuration, but the code must not depend on a real LLM call for tests.

## Contract

Public endpoint:

```http
POST /api/collections/{collectionId}/ai/generate-cards
Authorization: Bearer <access-token>
```

Facade endpoint:

```http
POST /collections/{collectionId}/ai/generate-cards
```

Internal endpoint:

```http
POST /collections/{collectionId}/ai/generate-cards
```

The public and facade endpoints verify authentication and collection ownership. The internal endpoint performs AI request validation, provider calls, output validation, and returns draft suggestions.

## Specialist Assignments

### backend-dev

Implement backend contract and LLM provider plumbing in `LearnWord.WebApi`, plus facade proxying in `LearnWord.Identity` and gateway routing.

Guardrail:

```text
Do not run direct dotnet restore/build/test in the sandbox. Use ./deploy/local-up.sh for build/startup verification and cd LearnWord && ./tests/run-all-tests.sh for focused backend regression checks. If that cannot run, report the blocker instead of trying alternate sandbox builds.
```

Return output suitable for `agent-runs/002-ai-card-generator/backend-agent-output.md`.

### frontend-ui

Implement collection-page AI generation UI in `../LearnWordWebApp/lw-app`, using the new public endpoint for suggestions and existing card creation for persistence.

Return output suitable for `agent-runs/002-ai-card-generator/frontend-agent-output.md`.

### qa-backend

Add or verify focused backend coverage for ownership guard, request validation, provider failure behavior, fake-provider behavior, and no real OpenRouter dependency in tests.

Guardrail:

```text
Do not run direct dotnet restore/build/test in the sandbox. Use ./deploy/local-up.sh for build/startup verification and cd LearnWord && ./tests/run-all-tests.sh for focused backend regression checks. If that cannot run, report the blocker instead of trying alternate sandbox builds.
```

Return output suitable for `agent-runs/002-ai-card-generator/qa-agent-output.md`.

## Verification Plan

- Update backend and frontend specs first.
- Use fake provider for deterministic local/test behavior unless OpenRouter configuration is explicitly supplied.
- Prefer `./deploy/local-up.sh` for full local verification.
- Use `cd LearnWord && ./tests/run-all-tests.sh` for backend regression checks.
- Use browser verification against `http://localhost:8088` for frontend behavior if the stack can run locally.

## Known Risks

- OpenRouter free models have rate and availability limits.
- Structured output support varies by model, so the model must remain configurable.
- Frontend and backend repositories are separate git repositories; final status should report both.
