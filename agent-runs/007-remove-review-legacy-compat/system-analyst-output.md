# System Analyst Output

## Interpretation

The current review flow already has the new outcome-based contract. The requested change intentionally removes compatibility routes and service methods for the old binary `learn`/`forget` review model.

## Affected Contract

- Backend API contract changed.
- Frontend behavior contract did not need a change because it already calls `POST /api/review/cards/{cardId}/review` with `{ outcome }`.

## Direct Actions

- Removed legacy review actions from backend service interfaces and implementations.
- Removed old card-level review routes from WebApi and Identity controllers.
- Removed old gateway routes.
- Updated backend spec to document only the new review submission endpoint.
- Updated tests and stubs to use the outcome-based review contract.

## Verification Plan

- Search for leftover legacy route/method references.
- Validate gateway JSON.
- Run full backend regression script.
- Run Docker local stack build/start.
- Smoke gateway behavior for new and old routes.

## Known Risk

Clients still calling legacy endpoints now receive `404 Not Found` through the gateway and must migrate to the outcome-based review endpoint.
