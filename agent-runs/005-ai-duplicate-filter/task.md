# Task

## User Request

Add duplicate protection to AI card generation so generated suggestions do not include words that already exist in the target collection. Use a hash-set based check after the AI provider returns suggestions.

## Scope

- Backend AI generation flow in `LearnWord.WebApi`.
- Backend tests for duplicate filtering.
- Backend and frontend specs/docs that describe the behavior.
- Delivery artifacts for the agent workflow.

## Acceptance Criteria

- AI generation still validates request input before provider calls.
- The provider is still called normally for valid requests.
- After provider output validation, suggestions whose `value` already exists in the collection are removed.
- Matching trims surrounding whitespace and is case-insensitive.
- Duplicate values within a single provider response are also collapsed by the same hash set.
- The public request/response DTO shape does not change.
- Existing frontend flow continues to work without UI changes.
- Focused backend regression checks pass or any blocker is recorded.

## Out Of Scope

- Database uniqueness constraints for manually added words.
- Prompt changes that send existing words to the LLM.
- Frontend duplicate warnings or skipped-suggestion counters.
