# System Analyst Output

## Requirement Interpretation

The current review flow is a simple binary learning flag with a fixed 7-day threshold. The requested portfolio feature changes this into a Spaced Repetition Engine with persistent scheduling state and outcome-based review actions.

The intended product story:

> Implemented a spaced repetition review engine with persistent scheduling state and review outcome tracking.

## Affected Specs

- `specs/backend-api.md`
- `specs/frontend-behavior.md`

Both specs must move from:

- `Learnt`
- `ShowedAt`
- fixed 7-day threshold
- `Learn` / `Forget`

to:

- `dueDate`
- `intervalDays`
- `easeFactor`
- `reviewCount`
- `lastReviewedAt`
- `Again` / `Hard` / `Good` / `Easy`

## Assignments

### Backend Dev

Owner: backend production implementation.

Scope:

- card SRS persistence fields;
- scheduler service;
- outcome DTO/enum;
- review endpoint through internal service, identity facade, and gateway;
- due-card selection;
- backend contract updates as needed.

### Frontend UI

Owner: Angular review experience.

Scope:

- review service API call for outcome-based review;
- four outcome buttons;
- compact SRS metadata;
- localized English/Russian text;
- frontend behavior spec updates as needed.

### QA Backend

Owner: backend regression coverage.

Scope:

- due-card selection tests;
- outcome scheduling tests;
- ownership boundary tests;
- focused backend regression run.

## Verification Plan

- Prefer `./deploy/local-up.sh` for Docker build/startup and gateway verification.
- Use `cd LearnWord && ./tests/run-all-tests.sh` for backend regression.
- Use Angular build or Docker frontend inspection for UI changes when feasible.

## Known Risks

- EF migration generation may require local tooling availability.
- Existing `learn`/`forget` endpoints may need backward-compatible behavior while the new review endpoint becomes the primary flow.
- Gateway route updates must be mirrored in both local and production Ocelot config files.
