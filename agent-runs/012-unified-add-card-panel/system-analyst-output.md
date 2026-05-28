# System Analyst Output

## Requirement Interpretation

The requested product change is a frontend UX consolidation: manual and AI card creation are two modes of one "add card" panel at the top of the collection details page. AI must be the initial mode. Mode choice is transient in Angular component state only.

## Affected Specs

- Updated `specs/frontend-behavior.md` to move manual creation ownership from `CardsComponent` to the collection details add-card panel.
- Backend API spec is unaffected because both creation paths continue to use existing endpoints.

## Agent Assignment

Assigned frontend implementation to the frontend UI worker with ownership of:

- `LearnWordWebApp/lw-app/src/app/collection/collection.component.*`
- `LearnWordWebApp/lw-app/src/app/cards/cards.component.*`
- relevant i18n text and focused component specs if needed.

Backend and backend QA agents were not assigned because the requested behavior uses existing frontend calls and does not change API contracts.

## Verification Plan

- Review frontend worker patch against acceptance criteria.
- Run focused frontend build from `LearnWordWebApp/lw-app`.
- Prefer Docker local stack and browser checks at desktop and mobile widths if startup is available.
- Confirm no lower duplicate manual add-card form remains.

## Known Risks

- Moving the manual creation event out of `CardsComponent` requires preserving the "go to last page after adding" behavior through the collection component's `ViewChild(CardsComponent)`.
