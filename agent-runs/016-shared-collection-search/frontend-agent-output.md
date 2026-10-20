# Frontend: shared collection search

Changed:
- Shared preview now projects compact card search into the existing common pager, using existing RU/EN card labels/placeholders and Search/Enter submit. Typing alone does not call the API.
- `SocialHttpService.get(publicId, page, pageSize, search = '')` appends encoded optional search without reordering existing arguments. Server `totalCount` drives matching ranges; `cardsCount` remains the full source count and appears in Found N of total.
- Applied query is separate from draft input. Search/size reset page one; paging/size/retry keep applied query. Route changes cancel stale reads and clear both search states/page. Empty input submission restores the whole collection; zero-match state also offers explicit clear action.
- Shared heading, pager and search input remain mounted during pending reads and recoverable errors; readonly input preserves focus/draft while requests are pending. Old cards are hidden pending/error, and fork actions are guarded/disabled for pending or failed reads. 404 removes unavailable content. Fork URL/body remain independent of search and copy the full collection.
- Zero matches reuse card search empty copy rather than source-empty wording. True empty source keeps existing no-cards copy. Safe public DTO/auth/presentation and owner behavior unchanged.
- Added five focused shared-search tests (real input/submit/no keystroke calls/focus retention, paging-size-query-retry-clear, zero-vs-empty, route cancellation/reset and unfiltered fork, RU/unavailable state) and one HTTP encoding test. Updated older get-argument/readonly expectations and removed duplicated invalid language-storage cleanup.

Files:
- `src/app/social/shared-collection.component.ts/.html`
- `src/app/social/social.http.service.ts/.spec.ts`
- `src/app/social/social-components.spec.ts`
- New `src/app/social/shared-search.component.spec.ts`
- `src/app/i18n/ui-text.ts` adds clear-search RU/EN copy.

Verification:
- `git diff --check` passed.
- Compile-ready handoff sent to root. No Docker launched and no local dependency install/build attempted by this specialist; root owns Docker production build, full suite (expected 58 cases) and browser acceptance.
- No test-account changes or backend/spec edits by this agent. Existing run014/015 implementation preserved.

Copy follow-up agreed with root: neutralized existing `cards.searchEmptyIntro` in RU/EN to “Попробуйте другое слово или перевод.” / “Try another word or translation.” so readonly preview does not promise an unavailable add-card action; owner search remains accurate with the same shared key. Source remains compile-ready.

Analyst verification: final Chromium suite58/58, production/dev Docker builds passed; personally checked late-card query, translation, server pages/size, clear/empty/focus, readonly/mobile and existing shared URL. See final-acceptance.md/screenshots.
