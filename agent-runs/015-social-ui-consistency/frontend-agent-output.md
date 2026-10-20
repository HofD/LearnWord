# Frontend: Social UI consistency

Changed:
- Owner collection heading now shows compact saved mode (`Access: Private` / `Доступ: Личная`) and a direct Clipboard action for shared modes. Removed the always-expanded access panel.
- Native dialog starts closed, uses browser modal focus containment, Escape/cancel and explicit focus restoration; unsaved selections reset on dismissal/reopen. Server save is guarded, status/error feedback accessible, and saved mode/link change only after success. Dismissal while saving is allowed; reopening is guarded until completion, with pending/error status in the heading. Clipboard failure retains selectable URL fallback.
- Extracted shared `PagerComponent`, `CollectionHeadingComponent`, `CardFrameComponent`, and `WordDisplayComponent`. Both owner and shared collections use the same presentational heading/frame/word rendering; shared content does not instantiate edit components, forms, delete modals, AI controls, or require artificial IDs/review data.
- Shared metadata and fork/editor/copy-success actions are compact in the heading rather than a large separate action card.
- Common pager uses existing ranges, grouped previous/current-total/next, one-page navigation hiding, and mobile layout. Owner retains client filtering and 25/50/100 paging/clamping. Explore/shared use server paging with 25/50 choices and page-one reset on search/size changes; API cap stays unchanged. Explore submits compact search from the pager row, with no requests per keystroke.
- Content anchors use neutral dark normal/visited/hover/focus styles, scoped away from Bootstrap button links and dark navbar navigation. Header actions retain 44px minimum height. Removed obsolete extracted owner/word CSS.
- Existing Social tests updated for default25 and new dialog/Clipboard state; added reusable pager boundary/size coverage, owner clamp regression, modal close/reset/focus/pending/error cases, social size/search reset and safe readonly presentation coverage. Social tests restore the previous saved language after each case.

Files:
- New `src/app/shared/ui/` pager, heading, frame, word display and pager tests.
- Updated owner collection/cards/words templates, imports, scoped styles and Cards test.
- Updated Social visibility/explore/shared components, service default sizes, existing tests, RU/EN dictionary and shared CSS.
- Existing pending run014 files and backend contracts preserved; no new dependency/library/framework.

Verification:
- `git diff --check` passed.
- Local dependencies unavailable; no direct npm build/test pass is claimed.
- Root owns Docker frontend build, full Jasmine suite and authenticated desktop/mobile browser acceptance. Implementation is compile-ready; results are to be recorded by root.
- This specialist did not create/use a test account or launch Docker.

## First Docker feedback

Root Docker compile succeeded. Full Jasmine execution ran 51 cases: 50 passed, one new search-input test dispatched before asynchronous NgModel setup completed. The test now waits for Angular stability before input and after dispatch, retaining the real DOM event and server-query assertions. Production code unchanged for this correction; final rerun/browser acceptance remains root-owned.

## Final root-coordinated verification and cache follow-up

- Full Docker Jasmine rerun: **51/51 passed**.
- Production frontend build: **passed**.
- Root browser check on 127.0.0.1 confirmed the new UI. Existing localhost browser tab retained an old development bundle with an unchanged filename.
- Small cache freshness fix: frontend Dockerfile now passes `--output-hashing=all` to Angular build for both development and production Docker configurations. Existing environment/configuration selection remains unchanged; no dependencies or backend changes. Root coordinates the webapp-only rebuild and original-localhost acceptance.

## Browser pager selection regression

Root browser acceptance found that recreating the shared pager after loading 50 cards showed 25 in the selector while the range/content were correct. The select's value binding ran before its repeated options existed. Each option now explicitly binds its selected state to pageSize, preserving the existing numeric event contract. Added a DOM regression covering initial/recreated size50, owner size100, and updates back to25. Root reruns the suite (now52 cases) and browser acceptance after rebuilding webapp.

## Analyst final result

Final Docker suite **52/52 passed**, final production Angular build passed, final hashed development webapp build passed. Analyst personally verified original localhost, modal/save/share, catalogue/shared/owner multiple pages, size50/100 selection after recreation, copied-word editing and independent source via gateway, desktop/mobile fit, dark links and readonly controls. Temporary fixtures cleaned. See final-acceptance.md and screenshots/ for evidence.
