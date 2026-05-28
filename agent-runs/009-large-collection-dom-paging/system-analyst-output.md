# System Analyst Output

## Requirement Interpretation

The bottleneck is frontend DOM pressure on `/collections/:id`: a large collection caused Angular to render every card, nested word UI, and one Bootstrap delete modal per card. Backend response time is acceptable enough to avoid changing API contracts in this iteration.

## Chosen Approach

- Keep `GET /api/collections/{id}` unchanged.
- Add client-side pagination in `CardsComponent`.
- Render only the visible page.
- Keep one shared delete modal for the selected card.
- Move to the last page after manual card creation and AI card saves.

## Affected Specs

- `specs/frontend-behavior.md` now documents client-side card pagination, page-size options, last-page behavior after creation, and the shared delete modal.

## Verification Plan

- Inspect generated diffs for the frontend and spec.
- Run frontend production build if dependencies are available.
- If a local browser environment is available, smoke test a large collection and confirm the DOM only contains the current page of card components.

## Known Risks

- This is not backend pagination; memory still holds the full collection payload.
- Build and browser checks depend on local Node/npm dependencies being installed.
