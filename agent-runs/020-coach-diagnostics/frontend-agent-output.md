# Frontend — private Coach diagnostics

## Implemented

- Added standalone lazy-loaded `/coach-diagnostics/:sessionId` route protected by the existing `authGuard`.
- Added `CoachHttpService.diagnostics(sessionId)`, a GET-only read of `/api/learning/sessions/{sessionId}/coach-diagnostics` using the existing HttpClient/auth interceptor. The diagnostics page never invokes analyze, paid provider calls or learner mutations, and handles no credentials/tokens manually.
- Summary shows session, snapshot time, status, decision, decision source, provider-called state (including unknown), and candidate count. CapturedAtAnalysis and ReconstructedNow have explicit EN/RU explanations; legacy reconstruction is not presented as original evidence. Free-prose analysis summary is intentionally excluded from the diagnostic contract/view.
- Full JSON is rendered through Angular interpolation in a focusable, wrapped/scrollable preformatted block. Local Blob download uses the fixed filename `learnword-coach-diagnostics.json` and revokes the object URL. No external upload/export endpoint.
- Added loading, generic ownership/unavailable error and retry, cancellation on route changes/navigation, and download-error fallback retaining copyable JSON. Mobile layout wraps identifiers/JSON and uses 44px download target.
- Added a secondary completion link only when the review route explicitly has `coachDebug=true`, the adaptive session exists, and completion succeeded. Normal and legacy review UI do not expose the technical link.

## Files

- `LearnWordWebApp/lw-app/src/app/coach/coach-diagnostics.component.{ts,html,css,spec.ts}`.
- `coach.http.service.ts`, `coach.http.service.spec.ts`, `coach.models.ts`.
- `app.routes.ts`, `i18n/ui-text.ts`, review component/template/spec.

## Verification

Ran full Jasmine suite and production Angular build in existing local Docker test image:

```sh
docker run --rm \
  -v /Users/alekseikarpov/GitHub/lw/LearnWordWebApp/lw-app:/source:ro \
  -v /private/tmp/learnword-coach-chromium:/tmp/coach-chromium:ro \
  -e CHROME_BIN=/tmp/coach-chromium learnword/frontend-tests:local sh -c \
  'cp -R /source/src /app/src && cp /source/angular.json /source/tsconfig.json /source/tsconfig.app.json /source/tsconfig.spec.json /app/ && npm test -- --watch=false --browsers=ChromeHeadless && npm run build'
```

Chromium wrapper is `exec /usr/bin/chromium-browser --no-sandbox "$@"` with `/bin/sh` shebang.

Result: **105/105 tests passed**, production build passed. Fourteen added tests cover authenticated lazy route, GET-only HTTP/no analysis calls, capture/reconstruction labels and RU text, plain-text JSON, loading/retry/cancellation, local download/fixed filename/URL revocation, and strict completion-link gating.

Build warnings: initial bundle 533.78kB against 500kB warning (below 1MB error); existing collection CSS 2.79kB against 2kB warning. Diagnostics lazy chunk is 6.77kB raw / 2.04kB estimated transfer. No package/dependency changes. `git diff --check` passed.

## Handoff

Coordinator owns rebuilt full-stack gateway and desktop/mobile browser verification. No production deployment, commits, pushes or branch changes performed. Frontend remains on `feature/epic-3a-ai-coach`; main unchanged. No additional known frontend blockers.
