# Frontend — Learner Model vertical slice

Owner: frontend_learner. Scope: Angular production and frontend tests only; existing owner collection/review flows preserved.

## Delivered

- Collection shows a compact Today panel: server due/weak/new composition, estimated minutes, Start, and Strong/Learning/Weak/New summary with recent activity. Separate standalone component follows existing content-card/Bootstrap design, wraps naturally on mobile, and contains loading/error/empty/retry states.
- Start navigates to existing `/review/:collectionId?today=true`; that mode creates the server-planned learning session and reviews its returned cards. The original route without the query retains the original due-card selection endpoint and remains compatible.
- Review preserves existing single/multiword reveal and four outcomes. Displays completed-card progress and sends optional sessionId, random requestId, and responseTimeMs clamped to 0..600000. Server remains responsible for ReviewedAt.
- Duplicate clicks are blocked while a request is pending. Ambiguous failure retains the identical outcome/requestId/responseTimeMs for explicit safe retry; answer controls cannot submit a conflicting answer during that retry state. Successful reviews advance once.
- Last answer closes the session; completion uses persisted server counts. Failed close offers explicit retry without resubmitting answers. Abandoned pages do not depend on unload events; subscriptions are cleaned on destroy.
- Collection panel reloads after review navigation, card-count changes, and persisted word mutations via existing nested output-event pattern. No event bus, analytics content, dependency, routing rewrite, or public/social DTO change.
- All new UI messages have EN/RU variants. Existing optional analytics events retain their payload allowlist.

## Changed areas

`src/app/learning/`: typed HTTP service, Today panel, focused service/UI tests.
`src/app/review/`: optional telemetry service signature, session lifecycle/retry/progress/completion UI, backward-compatibility and session tests.
`src/app/collection/`: Today panel integration and refresh revision.
`src/app/words/`, `card/`, `cards/`: successful word mutation event propagation and focused assertions.
`src/app/i18n/ui-text.ts`: EN/RU messages.

## Verification

- `git -C LearnWordWebApp diff --check`: passed after final edits.
- Root reported first full Docker Jasmine suite and production build passed before final word-refresh additions. Root owns final full rerun, Docker app startup and desktop/mobile browser acceptance; exact final results belong in final-acceptance.md.
- No direct npm/node execution: local node_modules/runtime absent. No test account was created by this agent.

## Residual limits

- Legacy due-only route creates ledger attempts without a LearningSession; adaptive Today route supplies session identity.
- Reloading/abandoning a review can leave an open session; no unreliable unload close is attempted.
- Local response time is optional coarse elapsed display time and bounded; it is not claimed as exact recall latency and is not sent to analytics.
