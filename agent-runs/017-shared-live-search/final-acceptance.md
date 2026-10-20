# Final acceptance — Shared automatic search

Decision: accepted locally, 2026-10-20. Corrected run016 manual-submit mismatch with owner-card UX. No backend/API changes, deployment or commits.

Delivered: shared preview has a field-only card-search control, no Find button or form/Enter requirement. Input triggers whole-source search after300ms idle debounce; empty input and Reset restore immediately. Superseded timers and in-flight reads are unsubscribed immediately, protecting latest input from stale results/errors. Input stays mounted/editable/focused while pending. Pending query prevents pagination, size changes and fork; route/destroy cancel outstanding work. Existing counts, empty state, page reset/query persistence and full-source fork remain intact. Server filtering/paging is retained so automatic search still covers the entire collection without downloading it all; owner/Explore are unchanged.

Verification:
- `./deploy/local-up.sh` full Docker build/start passed; frontend hashed development bundle and unchanged backend/migrator startup passed.
- Full Chromium Jasmine suite **62/62 passed**. Replaced five manual-submit tests with nine fakeAsync cases: rapid typing300ms, editable focused pending field, immediate HTTP cancellation and stale response/error suppression, query paging/size/retry, immediate/native clear, zero/reset, route/full-source fork, destruction, RU unavailable state.
- Production `npm run build` passed. Existing unchanged collection CSS warning2.79kB/2kB persists; no compile errors. No dependencies/lockfiles added. No backend test rerun needed for frontend trigger-only change; run016 records158 passed backend tests for unchanged search API.
- Personally checked existing authenticated shared collection5 in browser: typing a nonexistent value automatically shows Found0/1 without Enter, rapid r/ri/river resolves to Found1/1, field remains focused/editable, DOM has zero forms/submit buttons. Clearing immediately restores unfiltered count; Russian translation input automatically matches. At390x844 document width390; screenshot saved. Desktop1280x900 checked and captured. Viewport override reset; existing source/data unchanged.
- `git diff --check` passed in both repositories. Existing run014-016 changes preserved.

Changed areas: shared collection TS/HTML (Forms removed, timer lifecycle and input-only presentation), shared-search fakeAsync tests, readonly DOM test adjustment, frontend behavior spec, run017 artifacts. No backend, owner, Explore, common pager or security changes.
