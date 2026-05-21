# Task: Spaced Repetition Engine

## User Request

Implement the portfolio Variant 2 feature: evolve the current review flow into a real Spaced Repetition Engine and record the agent delivery run.

## Scope

- Replace the binary review model with outcome-based review scheduling.
- Persist per-card scheduling state:
  - `dueDate`
  - `intervalDays`
  - `easeFactor`
  - `reviewCount`
  - `lastReviewedAt`
- Add review outcomes:
  - `Again`
  - `Hard`
  - `Good`
  - `Easy`
- Update backend API, frontend behavior, and automated coverage.
- Preserve authenticated ownership boundaries.
- Record system analyst, backend, frontend, QA, and final acceptance notes.

## Acceptance Criteria

- `GET /api/collections/{collectionId}/review` returns cards due for review using `dueDate <= now`.
- New cards are due immediately.
- `POST /api/review/cards/{cardId}/review` accepts `{ "outcome": "Again|Hard|Good|Easy" }`.
- Review outcome updates `intervalDays`, `easeFactor`, `reviewCount`, `lastReviewedAt`, and `dueDate`.
- Angular review UI presents four outcome actions instead of binary Learn/Forget.
- Backend and frontend specs describe the new contract.
- Backend regression coverage validates due-card selection, representative outcome scheduling, and ownership boundaries.

## Out Of Scope

- Full review history/audit table.
- LLM tutor mode or AI explanation.
- User-configurable scheduler settings.
- Push notifications or reminder emails.
