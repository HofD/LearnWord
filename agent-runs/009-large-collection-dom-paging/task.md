# Large Collection DOM Paging

## User Request

Production collection pages with large collections remain slow even when the backend returns about 2000 cards in roughly 700 ms. The likely bottleneck is the frontend DOM size, not the API. Add frontend paging and reduce repeated delete modal DOM.

## Scope

- Collection details card rendering in the Angular frontend.
- Frontend behavior specification.
- Delivery record for the system analyst flow.

## Acceptance Criteria

- The backend contract stays unchanged: the frontend can still receive the full collection payload.
- The collection page renders only one client-side page of cards at a time.
- Manual card creation moves the card list to the last page.
- Saving AI-generated cards moves the card list to the last page.
- Card deletion uses one shared confirmation modal instead of one modal per card.
- Deleting cards keeps the current page in range.
- Frontend spec documents the new behavior.

## Out Of Scope

- Backend pagination.
- API DTO changes.
- Virtual scrolling or full list virtualization.
