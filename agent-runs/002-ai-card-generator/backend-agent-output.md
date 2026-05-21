# Backend Agent Output

## Changed Files

- Added shared AI DTOs in `LearnWord/LearnWord.BL.Models/Dto`.
- Added WebApi AI abstractions, options, validation service, fake provider, configured provider selector, and OpenRouter-compatible provider under `LearnWord/LearnWord.WebApi`.
- Added internal WebApi endpoint: `POST /collections/{collectionId}/ai/generate-cards`.
- Added Identity facade endpoint and ownership-checked proxy, with no LLM logic in Identity.
- Added Gateway routes for `POST /api/collections/{collectionId}/ai/generate-cards` in `ocelot.json` and `ocelot-dev.json`.
- Added AI config env examples and compose variables for local/prod.
- Added focused Identity ownership regression coverage for AI generation.

## Verification

```bash
./deploy/local-up.sh
```

Passed. Images built and local stack started.

```bash
cd LearnWord && ./tests/run-all-tests.sh
```

Passed all 8 test projects.

Existing warnings remain: nullable warnings and known package vulnerability warnings for `AutoMapper` and `MailKit`.

## Spec Impact

Implementation matches the drafted contract:

- suggestions are drafts only and are not persisted automatically;
- OpenRouter configuration and API key stay inside `LearnWord.WebApi`;
- if provider is `Fake` or OpenRouter API key is absent, `LearnWord.WebApi` uses the fake provider.

## Residual Risk

- OpenRouter live behavior is not exercised without a real API key.
- Structured output support can vary by model, so the model remains configurable.
