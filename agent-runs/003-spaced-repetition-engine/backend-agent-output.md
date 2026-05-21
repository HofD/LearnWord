# Backend Agent Output

## Changed

- Added SRS state to `Card`/`CardDto`: `dueDate`, `intervalDays`, `easeFactor`, `reviewCount`, `lastReviewedAt`.
- Added `ReviewOutcome` and `ReviewCardRequest`.
- Added `ISpacedRepetitionScheduler` / `SpacedRepetitionScheduler`.
- Updated `CardService` so:
  - `Review` applies outcome-based scheduling;
  - legacy `Learn` maps to `Good`;
  - legacy `Forget` maps to `Again`;
  - `Reset` resets SRS state.
- Updated review queue selection to `DueDate <= now`.
- Added internal and identity review controllers for `POST /review/cards/{cardId}/review`.
- Added gateway route for public `POST /api/review/cards/{cardId}/review`.
- Added EF migration `20260521000000_AddCardSpacedRepetition`.
- Updated backend API spec for the new SRS contract.

## Verified

- `cd LearnWord/LearnWord && ./tests/run-all-tests.sh`: passed, all 9 backend test projects green.
- `./deploy/local-up.sh`: passed, Docker images built and local containers started.
- `curl http://localhost:5100/api/collections` without auth returned `401`, confirming gateway/auth boundary is alive.
- `curl http://localhost:8088/` returned `200`.

## Spec Impact

- Updated `specs/backend-api.md`.
- Updated `specs/frontend-behavior.md` in coordination with frontend work.

## Residual Risk

- Full authenticated gateway E2E review-outcome scenario was not added.
- Existing package/security warnings remain for `MailKit` and `AutoMapper`.
