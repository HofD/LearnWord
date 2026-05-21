# AI Features

LearnWord's first AI feature is AI card generation from source text. It is designed as a portfolio-friendly feature because it touches product UX, backend contracts, LLM provider integration, validation, tests, and the agentic SDLC workflow.

## AI Card Generator

User flow:

```text
User opens a collection
      |
      v
Pastes source text
      |
      v
Requests AI suggestions
      |
      v
Reviews draft cards
      |
      v
Saves selected suggestions as normal cards
```

AI suggestions are drafts. They are not persisted until the user explicitly selects and saves them.

## Service Boundaries

`LearnWord.Identity` stays a lightweight authenticated facade:

- reads current user identity;
- checks collection ownership;
- proxies valid requests to the internal learning service.

`LearnWord.WebApi` owns AI behavior:

- request validation;
- prompt construction;
- LLM provider selection;
- OpenRouter API calls;
- structured output parsing;
- suggestion validation;
- fake-provider behavior for local/test-safe runs.

This keeps identity/auth concerns separate from AI provider concerns.

## Public Contract

```http
POST /api/collections/{collectionId}/ai/generate-cards
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "sourceText": "Yesterday I went to the market and bought fresh vegetables.",
  "sourceLanguage": "en",
  "targetLanguage": "ru",
  "level": "A2",
  "maxCards": 5
}
```

Response:

```json
{
  "cards": [
    {
      "value": "market",
      "transcription": "ˈmɑːrkɪt",
      "translation": "рынок",
      "example": "I went to the market.",
      "explanation": "A place where people buy and sell goods.",
      "difficulty": "A2"
    }
  ]
}
```

## OpenRouter Setup

OpenRouter is used through its OpenAI-compatible chat completions API:

```text
https://openrouter.ai/api/v1/chat/completions
```

Configuration should be supplied through backend service environment variables, not frontend code:

```text
AiCardGeneration__Provider=OpenRouter
AiCardGeneration__OpenRouter__ApiKey=<secret>
AiCardGeneration__OpenRouter__BaseUrl=https://openrouter.ai/api/v1/chat/completions
AiCardGeneration__OpenRouter__Model=google/gemma-4-26b-a4b-it:free
AiCardGeneration__MaxSourceTextLength=4000
AiCardGeneration__MaxCards=10
AiCardGeneration__OpenRouter__TimeoutSeconds=30
```

For local development and tests, use:

```text
AiCardGeneration__Provider=Fake
```

## Free Tier Notes

OpenRouter free models are acceptable for early development, demo, and low-traffic portfolio use. The model ID should remain configurable because free-model availability, quality, and structured-output support can change.

For a more stable small-production setup, keep the same integration but add a small credit balance and pin a suitable model explicitly.

## Safety And Validation

The backend should:

- limit `sourceText` length;
- limit `maxCards`;
- request structured JSON output;
- validate every returned suggestion;
- discard empty or malformed suggestions;
- avoid persisting suggestions automatically;
- avoid logging API keys or full user text at error level;
- return stable errors for validation and provider failure cases.

## Testing Strategy

Automated tests should not require OpenRouter. Use a fake provider to verify:

- validation blocks invalid requests before provider calls;
- ownership failures in `LearnWord.Identity` do not reach `LearnWord.WebApi`;
- successful fake generation returns draft suggestions;
- malformed provider output is rejected or sanitized.
