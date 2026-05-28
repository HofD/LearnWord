# Review All Words

## User Request

During card review, the UI currently shows only the first word in a card. Show all words instead, while preserving the existing visual appearance for cards that contain exactly one word.

## Scope

- Angular review page behavior and styling.
- Frontend behavior specification.
- Focused frontend regression coverage for single-word and multi-word review cards.

## Acceptance Criteria

- A review card with one word keeps the existing large single-word layout.
- A review card with multiple words shows every word and each available transcription.
- Revealing the translation shows all translations in matching word order.
- Multi-word cards remain usable on desktop and mobile; review actions are not visually displaced by normal multi-word content.
- Backend API contracts remain unchanged.

## Out Of Scope

- Per-word review scoring.
- Spaced-repetition scheduling changes.
- Backend DTO or endpoint changes.
