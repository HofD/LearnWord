# Frontend: automatic shared card search

Changed (shared component/template and tests only):
- Removed manual Find/Search button and submit form. Shared preview now uses the same compact input-only appearance as owner card search, keeping existing RU/EN card-search text.
- Input schedules trimmed whole-collection server search after 300ms. Every new query immediately cancels the previous timer/read; unsubscribe prevents stale results/errors overwriting the current query. Source pagination/API filtering remain server-side; no full-source download or downloaded-page filtering.
- Search field stays editable and focused while debounce/HTTP are pending; heading/pager remain mounted. Pending search disables paging/size/fork until the applied query resolves. Paging, size changes and retry preserve that applied query.
- Native empty input and Reset Search restore the full collection immediately, canceling pending work and resetting page one. Route changes reset both query states/page and cancel timers/reads; destruction cancels outstanding work.
- Full-source fork, safe readonly DTO, auth, matching ranges/Found counts and empty-vs-no-match states preserved. Explore, owner UI, common pager, API/backend unchanged.
- Replaced five manual-submit shared-search tests with nine fakeAsync automatic-search cases: rapid typing; editable/focused pending input; immediate unsubscribe and ignored stale results/errors; paging/size/retry/query; native clear; zero-match reset; route cancellation and unfiltered fork; destruction; RU/unavailable. Existing readonly test now asserts no forms and a search input.

Verification:
- `git diff --check` passed.
- Compile-ready handoff sent to root for Docker production build, full suite (expected62 cases), and browser acceptance. No Docker, local dependency install, or test-account changes by this specialist.
- No local execution pass claimed; final Docker/browser evidence is root-owned.

Analyst final verification: all62 frontend tests passed in Docker Chromium; production and local-stack builds passed. Personally verified automatic input, rapid typing, clear, focus/no submit, mobile and desktop on existing shared collection. See final-acceptance.md/screenshots.
