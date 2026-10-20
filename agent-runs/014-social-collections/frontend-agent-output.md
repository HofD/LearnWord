# Frontend delivery: Social Collections

Changed:
- Added authenticated `/explore` and `/shared/collections/:publicId` routes, header navigation, RU/EN interface copy, paginated Public collection search, and read-only shared cards.
- Shared views show safe author labels and card counts, owner editor link instead of self-fork, independent-copy success link, loading/empty/retry/unavailable states, and pending request guards. Shared content uses Angular text interpolation; no owner editing or review controls are rendered for another user's content.
- Owner editor now contains an isolated visibility panel for Private (0), Unlisted (1), Public (2), saved server state, pending/error/success feedback, Clipboard share links and a selectable URL fallback. Existing card creation/edit/review controls remain intact.
- Added typed social API service and owner DTO fields `visibility`, `publicId`, `forkedFromCollectionId` without changing existing collection constructor callers.
- Added success-only analytics `COLLECTION_PUBLISHED`, `COLLECTION_UNPUBLISHED`, `COLLECTION_VIEWED`, `COLLECTION_FORKED`. Publish/unpublish represent entering/leaving Public; shared view fires once per opened collection, not once per card page. No names, authors, words, or translations are sent in goals.
- Added 15 Jasmine cases: HTTP contracts (4), search/pending requests, safe rendering, owner/fork split, successful/failed fork, unavailable read, visibility pending/success/failure, shared empty/pagination, Russian text, and Clipboard denied fallback (11).

Changed frontend files:
- `src/app/social/`: service, three components, two templates, service and component tests.
- `src/app/app.routes.ts`, `header/header.component.html`, `i18n/ui-text.ts`, `shared/services/analytics-events.ts`.
- `collection/collection.ts`, `collection/collection.component.ts`, `collection/collection.component.html`.

Verified by frontend agent:
- `git diff --check` passed in LearnWordWebApp.
- Source-level review against `agents/frontend-ui/AGENT.md` and Epic 1 frontend/API contracts.
- Local npm dependency diagnostic could not complete: registry.npmjs.org DNS resolution returned ENOTFOUND. No dependency/framework/lockfile changes.

Handoff / remaining acceptance:
- System analyst owns Docker build, Jasmine execution, and authenticated browser acceptance at mobile/desktop widths. No frontend Docker or visual pass is claimed by this specialist report.
- No test account was created or used by this agent; root coordinates shared Docker acceptance.

## Docker test follow-up

Root's first full Docker Jasmine run compiled and executed all 43 cases; 33 passed and 10 failed. One new owner-view assertion incorrectly searched the whole page for fork text also present in the generic introduction. It now checks actual action buttons. Nine existing failures came from legacy test setup missing HttpClient/ActivatedRoute; added HttpClientTestingModule and provideRouter([]) to the seven affected legacy spec files (app, email-confirm, collections, header, register, login, home). Replaced the stale generated `Hello, lw-app` shell assertion with current localized brand and router-outlet assertions. These follow-up changes affect tests only. Full Docker rerun is coordinated by root; no green suite claim until its result is recorded.

Second full Docker run: 41/43 pass, including all 15 new Social cases. The remaining two legacy AppComponent tests constructed child MessagesComponent without starting its lifecycle, causing undefined subscriptions on test cleanup. Added `fixture.detectChanges()` to those two tests so normal initialization precedes destruction. Awaiting final root-coordinated rerun; production code unchanged.
