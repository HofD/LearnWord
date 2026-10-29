# Frontend — Epic 3A AI Coach

## Implemented

- Added standalone `coach/coach.component` with dedicated HTTP service and explicit public DTO interfaces. It is mounted only after a durable adaptive session has completed; deterministic completion remains visible while analysis loads or fails. Legacy due-only review remains independent.
- None and Disabled produce no empty panel. Pending is polled every 2 seconds at most 15 times; leaving/changing sessions cancels subscriptions and timers. Failed/network responses show a compact noncritical retry with no technical error detail.
- Suggested lessons/practice require explicit acceptance. Accepted work is rechecked on resume through idempotent accept; dismissal, lesson Done and practice completion use backend lifecycle. Stale/deleted conflicts reload current status and remove unavailable actions.
- Structured lessons use Angular text interpolation (no HTML/Markdown). RecognitionChoice practice sends opaque option IDs for server validation, displays server correct/incorrect feedback, progress and final correct count. Failed ambiguous answers retain the same option/timing payload for retry; other answer choices remain disabled until acknowledged.
- RU/EN UI strings follow I18nService. Buttons wrap and maintain 44px targets; content wraps within narrow layouts. Semantic headings and live feedback are present, focus moves to lesson/exercise headings after explicit navigation and back to the Coach heading on close.

## Files

- `LearnWordWebApp/lw-app/src/app/coach/*` (component/template/styles/models/service and unit/HTTP tests).
- Review component imports/template and regression tests.
- `src/app/i18n/ui-text.ts`.
- No dependencies/package-lock changes.

## Verification

Executed in existing Docker image `learnword/frontend-tests:local`, copying final source/config into `/app` to reuse installed dependencies:

```sh
docker run --rm \
  -v /Users/alekseikarpov/GitHub/lw/LearnWordWebApp/lw-app:/source:ro \
  -v /private/tmp/learnword-coach-chromium:/tmp/coach-chromium:ro \
  -e CHROME_BIN=/tmp/coach-chromium learnword/frontend-tests:local sh -c \
  'cp -R /source/src /app/src && cp /source/angular.json /source/tsconfig.json /source/tsconfig.app.json /source/tsconfig.spec.json /app/ && npm test -- --watch=false --browsers=ChromeHeadless && npm run build'
```

Chromium wrapper contents:

```sh
#!/bin/sh
exec /usr/bin/chromium-browser --no-sandbox "$@"
```

Result: **91/91 Jasmine tests passed**, **production Angular build passed**. Includes independent completion under Coach failure/loading, HTTP contracts without user-selected identity or local solution, None/Disabled, localized failure and retry, bounded polling/cancellation/session changes, explicit lesson acceptance/plain text/Done, dismissal, correct/incorrect practice/server feedback, stable retries/duplicate submission prevention, stale content, and accepted resume.

Build warnings: initial bundle 527.04kB vs 500kB warning (1MB error threshold); existing collection CSS 2.79kB vs 2kB warning. No new dependencies. Frontend `git diff --check` passed.

One source-only type correction after the verified copied snapshot aligns `schemaVersion` with backend integer DTO (1); it does not affect rendering/runtime behavior. Coordinator's final Docker stack build verifies final integrated source.

## Handoff / limitations

Final real gateway + desktop/mobile browser acceptance is owned by the coordinating agent against the rebuilt full Docker stack; this report does not claim it. No UI for reopening an old session after page refresh is added (Epic 2 currently has no persisted browser session resume). Supplementary attempts never call the review API or calculate local mastery/SRS.
