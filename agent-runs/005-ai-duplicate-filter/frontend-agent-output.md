# Frontend Agent Output

## Owner And Scope

Frontend impact review for AI duplicate filtering.

## Decision

No Angular production change is required.

The collection page already displays the `cards` returned by `POST /api/collections/{collectionId}/ai/generate-cards` and saves selected suggestions through the existing `POST /api/cards` flow. Because duplicate filtering happens before the response reaches the frontend, the current UI naturally shows only the filtered list.

## Spec Impact

`specs/frontend-behavior.md` now notes that successful AI generation displays the backend-returned list, which is already filtered for existing collection words.

## Verification

No frontend build was required because templates, component logic, and DTO shape did not change.
