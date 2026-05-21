# Agent Run 002: AI Card Generator

## Task

Implement the first AI feature for LearnWord: generate draft learning card suggestions from user-provided source text.

## Architecture Decision

`LearnWord.Identity` remains a lightweight authenticated facade. It verifies that the current user owns the collection and proxies the AI generation request to `LearnWord.WebApi`.

LLM integration, prompt construction, provider configuration, structured output parsing, and AI-specific validation live in `LearnWord.WebApi`.

## Scope

- Add a public gateway endpoint for AI card generation.
- Add an authenticated facade endpoint in `LearnWord.Identity`.
- Add an internal AI generation endpoint and service in `LearnWord.WebApi`.
- Add OpenRouter-compatible LLM provider support with configuration.
- Add a fake provider for local/test-safe behavior.
- Add frontend UI on the collection details page for generating draft cards.
- Save selected suggestions through the existing card creation flow.
- Update backend and frontend specs.
- Add focused tests where practical.

## Acceptance Criteria

- Authenticated users can request draft card suggestions for an owned collection.
- Requests for a foreign collection are rejected before any LLM/provider call.
- The source text and `maxCards` are validated before provider calls.
- AI suggestions are returned as drafts and are not persisted automatically.
- Users can select suggestions and save them as regular cards.
- OpenRouter API key is read only from backend service configuration and is never exposed to the frontend.
- Tests do not require a real OpenRouter call.
- Specs document the new backend contract and frontend behavior.

## Out Of Scope

- Spaced repetition changes.
- Persisting AI generation history.
- Streaming LLM output.
- Production-grade rate limiting dashboard.
- A separate AI microservice.
