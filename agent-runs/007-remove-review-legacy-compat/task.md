# Remove Review Legacy Compatibility

## User Request

Remove backward compatibility from review behavior. The product should use only the new review contract.

## Scope

- Backend public gateway/API review contract.
- Identity facade and internal WebApi card/review endpoints.
- Backend API specification.
- Focused backend tests.

## Acceptance Criteria

- `POST /api/review/cards/{cardId}/review` remains the only public review submission endpoint.
- Legacy binary review endpoints are no longer routed or implemented:
  - `POST /api/cards/{id}/learn`
  - `POST /api/cards/{id}/forget`
  - `POST /api/review/cards/{cardId}/learn`
  - `POST /api/review/cards/{cardId}/forget`
- Service interfaces no longer expose legacy `Learn`/`Forget` review actions.
- Specs and tests describe the new-only contract.

## Out Of Scope

- Frontend UI changes, because the frontend already uses the new outcome-based endpoint.
- Review scheduling algorithm changes.
