# Final acceptance — Social UI consistency

Decision: accepted locally, 2026-10-20. All three approved review areas implemented. Existing pending run014 work preserved. No commits or production deployment requested/performed.

## Delivered

- Owner access settings are closed by default in a native modal dialog. Compact saved-mode Access/Доступ button is projected into the shared collection heading, with a direct copy-link action for Unlisted/Public. Cancel/Escape resets unsaved selection and returns focus; pending saves cannot be duplicated/reopened; a dismissed pending failure remains visible; clipboard rejection offers a selectable URL.
- Owner cards, Explore and shared collections now use the same PagerComponent and established card-pager visual language: ranges, compact sizes, grouped previous/current-total/next, no navigation on one page, existing mobile layout. Owner keeps client filtering/pagination and25/50/100. Social uses server pagination with25/50, consistent with unchanged API cap50; size/search reset to page1.
- Owner/shared views reuse CollectionHeadingComponent, CardFrameComponent and WordDisplayComponent. Shared author/count/actions moved into the compact heading; large separate metadata panel removed. No owner editor widgets, mutation modals, fake card IDs or extra private DTO fields are introduced on shared pages.
- Content links use dark-neutral text/visited/hover/focus colors while Bootstrap button/nav/footer contrast remains intact.
- Docker Angular builds use output hashing for development and production so refreshed local pages request current bundles. Environment selection and API URLs unchanged.

## Verification performed by analyst

- `./deploy/local-up.sh` succeeded: all .NET services built without errors, Docker Angular development build passed, existing migrator completed, local stack started. Follow-up webapp-only Compose rebuilds passed after cache and pager corrections.
- Docker Chromium/Jasmine full suite: **52/52 passed**. Added range/boundary/size, recreated-select, modal lifecycle/failure/focus, real-input search/no-keystroke-requests, server page-size reset and safe readonly-presentation regression checks. First new search test needed awaiting Angular NgModel setup; fixed test lifecycle. Browser acceptance then caught a real recreated-select display bug, now fixed and covered by a DOM regression for50/100/25.
- Production `npm run build` passed on final source in Docker. Existing unchanged collection.component.css still emits the preexisting 2kB warning (2.79kB); no type/build errors. No lint script is configured; no dependency/lockfile/framework changes.
- Browser checked desktop1280x900 and mobile390x844, EN/RU. Original localhost tab now receives new hashed bundle after rebuild. Verified compact owner header with dialog closed; real Escape cancellation/reset/focus; Unlisted save/direct clipboard success; Private save/share removal. Source collection6 restored Private.
- Real server catalogue fixture:27 public temporary collections; search matched27, page2 displayed26–27 and2/2; change50 reset to page1 and hid one-page navigation. Content link computed color rgb(33,37,41).
- Real shared fixture:52 cards; pages1/2/3 displayed1–25,26–50,51–52 with correct boundaries. Change50 reset to1–50/52 and select displayed50 after pager recreation (the browser-detected regression). Shared page DOM contained zero owner controls (`app-words`, word actions, card-delete controls, mutation modals); mobile document width equalled390px viewport.
- Personally forked from the new heading, opened resulting Private owner copy, paged to2/3, selected100 (range1–52, no navigation), filtered to river52, edited translation and saved through the UI. Gateway read confirmed original unchanged and copied word independently updated.
- Removed all27 temporary catalogue/source collections and the temporary fork after checks. Existing initial collections/accounts/data retained. Returned browser to original `/collections/6`; viewport override reset.
- `git diff --check` passed in both repositories. Backend production/test files unchanged by this follow-up; no backend unit suite rerun was necessary for this UI-only change. Full current backend builds and real authenticated gateway flows were checked instead. Run014 records prior backend144-test acceptance.

## Files/areas

Frontend shared/ui: heading/card-frame/word-display and pager TS/CSS/spec. Owner collection heading insertion; cards wrapper/pager integration/spec and dead-style removal; words presentation extraction; social visibility/dialog, Explore/shared templates/controllers/services/tests; RU/EN labels, neutral global content links; Dockerfile bundle hashing. Analyst updated frontend behavior spec and run015 artifacts. No backend API, auth, migration or domain changes in run015.

## Remaining notes

Social size options intentionally stop at50 while owner local cards retain100. This is documented and does not alter the agreed common visual language. Raising the API cap would be a separate backend change. Automated success/failure coverage uses existing frameworks; runtime browser checked the successful flows, keyboard dismissal and real pagination. Production rollout, load testing and other social product changes remain outside this visual follow-up.
