# Final Acceptance

## Decision

Accepted.

The AI card generator is implemented as a draft-suggestion flow. `LearnWord.Identity` remains a lightweight authenticated facade that checks collection ownership and proxies to `LearnWord.WebApi`; LLM/provider logic lives in `LearnWord.WebApi`.

## Changed Areas

- Backend AI DTOs and WebApi AI generation service/provider stack.
- OpenRouter-compatible provider with fake-provider fallback.
- Identity facade ownership-checked proxy endpoint.
- Gateway route for `POST /api/collections/{collectionId}/ai/generate-cards`.
- Local and production AI configuration examples.
- Angular collection-page AI generation UI.
- Backend and frontend specs.
- AI feature documentation.
- Recorded agent run artifacts.

## Verification

```bash
./deploy/local-up.sh
```

Passed. Docker images built and the local stack started.

```bash
cd LearnWord
./tests/run-all-tests.sh
```

Passed 9/9 backend test projects.

Gateway smoke checks:

- `POST /api/collections/{id}/ai/generate-cards` without bearer token returned `401`.
- Authenticated user created a collection through the gateway.
- Authenticated user generated fake AI suggestions for an owned collection through the gateway and received `200 OK`.

Browser checks against `http://localhost:8088`:

- Logged in as `agent-ui-test@example.com`.
- Opened the collection details page.
- Confirmed the AI generation panel appears.
- Generated draft suggestions.
- Saved selected suggestions.
- Confirmed saved cards appeared in the collection.
- Checked mobile viewport `390x844`; AI panel and form controls remained accessible.

## Checks Not Run

- Live OpenRouter call was not run because no real API key was configured.
- Screenshot capture in the in-app browser timed out once, so acceptance used DOM/browser interaction checks instead of a saved screenshot.

## Residual Risk

- OpenRouter model quality and structured-output behavior still need a live configured-key check.
- Current automated coverage is mostly service/provider-level, not full HTTP model-binding integration for `LearnWord.WebApi`.
- Existing warnings remain for nullable annotations and known package vulnerabilities in `AutoMapper` and `MailKit`.

## Follow-Up UI Refinement

The AI generation form was refined after initial acceptance:

- the form is collapsed by default and toggled from the panel header;
- source and target languages use fixed select lists with 10 common languages;
- language level uses fixed CEFR values from A1 to C2.

## Follow-Up Provider Error Handling

Production testing showed that the OpenRouter free route can return `429` for real generation while a tiny model ping succeeds. The follow-up change keeps paid/free model choice configurable and improves the product behavior:

- OpenRouter `429` is returned as ProblemDetails code `ai_provider_rate_limited`;
- temporary provider failures use `ai_provider_unavailable`;
- provider credential or route failures use `ai_provider_configuration_error`;
- `LearnWord.Identity` preserves ProblemDetails from `LearnWord.WebApi` instead of replacing it with a generic upstream error;
- the frontend maps AI provider errors to retry-later messages;
- after all selected AI suggestions are saved successfully, the AI form and suggestion list are cleared.

Follow-up verification:

```bash
cd LearnWord
./tests/run-all-tests.sh
```

Passed 9/9 backend test projects after adding regression coverage for OpenRouter `429` mapping and Identity ProblemDetails propagation.

```bash
docker build --target build-source -t lw-app-ai-error-handling-build-check .
```

Passed Angular production build through the frontend Docker build stage.

## Next Step

Use the paid OpenRouter route for production if the free route returns provider-side `429` errors during real generation. Keep the model configurable through `LW_AI_OPENROUTER_MODEL`.
