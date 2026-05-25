# Task

## User Request

Add a quick MVP product monitoring solution using the user's existing Yandex Metrica account to understand whether LearnWord is used and how actively.

## Scope

- Frontend-only Yandex Metrica MVP.
- Track successful registration, login, content creation, AI card generation, and review activity.
- Document the analytics event contract and privacy boundaries.
- Keep backend unchanged.

## Acceptance Criteria

- Yandex Metrica is configurable by environment and disabled locally by default.
- Events are sent only after successful user actions.
- No email, tokens, words, translations, transcriptions, source text, or other user-entered learning content is sent to analytics.
- The frontend spec documents the goals and active-user definitions.
- A focused frontend build verifies TypeScript and bundling.

## Out Of Scope

- Backend analytics tables or server-side event collection.
- Admin dashboard inside LearnWord.
- Yandex Metrica account setup beyond documenting the required JavaScript goals and counter id configuration.
