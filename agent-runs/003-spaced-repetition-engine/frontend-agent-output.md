# Frontend Agent Output

## Changed

- Updated review HTTP service to submit `POST /api/review/cards/{cardId}/review` with `{ outcome }`.
- Replaced binary `Learn`/`Forget` actions with `Again`, `Hard`, `Good`, and `Easy`.
- Added compact SRS metadata chips for interval, review count, and due date when available.
- Switched review transcription display to the first word transcription.
- Updated English and Russian review copy.

## Changed Files

- `LearnWordWebApp/lw-app/src/app/review/review.http.service.ts`
- `LearnWordWebApp/lw-app/src/app/review/review.component.ts`
- `LearnWordWebApp/lw-app/src/app/review/review.component.html`
- `LearnWordWebApp/lw-app/src/app/review/review.component.css`
- `LearnWordWebApp/lw-app/src/app/i18n/ui-text.ts`

## Verified

- `docker build -t learnword-webapp-frontend-check .` from `LearnWordWebApp/lw-app`: passed, including production Angular build.
- `./deploy/local-up.sh`: passed after backend integration.
- `curl http://localhost:8088/`: returned `200`.

## Residual Visual Risk

- Authenticated browser walkthrough of the live review screen was not completed in this run.
