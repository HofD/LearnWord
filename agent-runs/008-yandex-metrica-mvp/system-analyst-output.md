# System Analyst Output

## Requirement Interpretation

The MVP should use Yandex Metrica as a lightweight product analytics layer. The first delivery should answer:

- are users visiting the app;
- are users logging in;
- are users creating learning content;
- are users starting and completing review sessions.

## Affected Specs

- `specs/frontend-behavior.md` gains the Yandex Metrica provider contract, goal list, allowed parameters, and definitions for active learning/content users.
- `specs/backend-api.md` is unchanged because this MVP does not alter backend contracts.

## Direct Actions

- Implement frontend analytics service and event constants.
- Add production/local environment configuration.
- Emit goals after successful API-backed actions.
- Keep local analytics disabled unless a production counter id is configured.
- Install the production counter through the static production HTML snippet, not through a second Angular runtime script loader.

## Verification Plan

- Run a focused Angular production build from `LearnWordWebApp/lw-app`.
- Review changed event payloads for privacy-sensitive fields.

## Known Risks

- Browser-side analytics can be blocked by ad blockers or user privacy settings.
- MVP counts successful frontend-observed actions, not authoritative server-side database events.
- Changing the Yandex Metrica counter requires updating both `src/index.prod.html` and `src/environments/environment.prod.ts`.
