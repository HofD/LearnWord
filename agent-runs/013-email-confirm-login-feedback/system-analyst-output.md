# System Analyst Output

## Requirement Interpretation

The bug is a user guidance gap in the account lifecycle. Backend already rejects login for unconfirmed email with `403`; the frontend only showed an explicit alert for `401`, so the user could fail login without actionable guidance.

Backend registration already resends a confirmation email when an existing unconfirmed email is registered again, so the current recovery path can be described without adding a backend contract.

## Affected Specs

- `specs/backend-api.md`: no change needed. The existing contract already documents `403` for unconfirmed email.
- `specs/frontend-behavior.md`: updated to document the login `403` alert and the failed-confirmation recovery guidance.

## Ownership And Assignments

Handled directly as a small frontend/spec correction. Specialist handoff was not used because the backend contract remained unchanged and the frontend change was limited to auth error handling, localized copy, one template link, and focused test coverage.

## Verification Plan

- Inspect backend registration/login implementation and specs.
- Add frontend regression coverage for `AuthService.login` receiving `403`.
- Run frontend build/test fallback if dependencies are available.
- Prefer Docker/browser acceptance for final verification when the local stack can run.

## Known Risks

- Host shell lacks `npm` and frontend `node_modules`, so direct host `npm test` was not available.
- Docker frontend build and browser acceptance covered the changed UI behavior.
