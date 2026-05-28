# Unified Add Card Panel

## User Request

On the collection details page, manual card creation currently appears after the full card list while AI generation appears at the top. Unify both entry points into one top block with a mode switch, optimized for mobile.

## Scope

- Frontend collection details UX.
- Existing manual card creation flow.
- Existing AI card generation and save flow.
- Frontend behavior specification.
- Delivery artifacts and final acceptance notes.

## Acceptance Criteria

- The collection details page shows one add-card block above the card list.
- The block has a mobile-friendly switch between AI and manual creation.
- AI is selected by default.
- The selected mode is remembered only while the current component instance remains alive; no backend, cookie, `localStorage`, or `sessionStorage` persistence is added.
- Manual creation uses the existing card creation API and updates the displayed collection.
- AI generation and selected-card saving keep their existing behavior.
- The old manual add-card form is removed from the bottom of the card list.
- Frontend behavior spec is updated.
- Frontend build and mobile/desktop visual checks are run when feasible.

## Out Of Scope

- Backend API changes.
- Authentication or ownership changes.
- Persistent user preferences.
- Redesigning card search, pagination, editing, or review flows.
