# Final acceptance — Shared collection search

Decision: accepted locally, 2026-10-20. Existing run014/015 pending work preserved; no production deployment or commits performed.

## Delivered

Shared preview now includes the common compact pager search row, existing RU/EN card-search labels and submit via Enter/Search. It searches the full accessible collection on the server, never only the downloaded page. Matching checks word value OR translation using trimmed case-insensitive substrings before count/order/skip/take. A matching card is returned once with all active words. cardsCount remains source total; totalCount is filtered card count. Optional search parameter is forwarded through both controllers, facade, HTTP transport, BL and DAL; existing unfiltered GET callers stay compatible. No DTO/schema/dependency/auth changes. Fork remains full source independent of applied query.

Search resets page1; paging/size/retry retain applied query; route change resets search; clear restores all cards. Found N of M and zero-match card-search state distinguish filtered absence from empty source. Neutral RU/EN empty-state hint now avoids suggesting creation inside readonly view. Heading/pager/search remain mounted while loading, input readonly preserves focus, pending/error fork is blocked and inaccessible source clears content.

## Verification

- `./deploy/local-up.sh`: full .NET/Angular Docker build and stack startup passed; migrator completed existing migrations. Final webapp-only Compose rebuild passed after empty-state text correction; hashed bundles serve current source.
- Repository `cd LearnWord && ./tests/run-all-tests.sh` in .NET8 SDK Docker container on local Compose network, LEARNWORD_E2E=true: all9 projects passed, **158 tests**, none skipped. New SQL-backed SQLite tests cover late-page matches, filtering/count/paging, multiword dedup/full context, deleted/foreign content, empty/whitespace and full fork; facade/controller auth/search forwarding and real HTTP encoded-query tests passed.
- Full Chromium Jasmine/Karma suite in Docker: **58/58 passed**. Production `npm run build` passed on final source. Existing unchanged collection.component.css warning remains2.79kB vs2kB budget; no compile/type errors. No lint task configured.
- Personal authenticated gateway/PostgreSQL test used60-card temporary public source plus a separate Private control. Found a word on original card60 using trimmed mixed-case `NEEDLE &`, with both words of the card preserved. Uppercase Cyrillic translation `ИСКОМЫЙ ПЕРЕВОД` matched. Query match produced29cards,25+4 across2pages; no-match0 and whitespace60. Foreign Private search returned404; anonymous search401. Confirms PostgreSQL Unicode/encoding behavior beyond SQLite case folding.
- Personal browser acceptance: login returns to shared URL; typing alone leaves original1–25/60; Enter applies late-card query and Found1/60; original search input remains focused. Filtered pages show26–29/29 then size50 resets1–29/29 and correctly selected50. No-match shows neutral helper and Reset Search; reset restores1–50/60. Russian translation search returns matching whole card. At390x844 document width=390 and no owner editing controls; desktop1280x900 final screenshot captured. Existing shared collection5 is left open with working search.
- Removed both temporary test collections after acceptance; original collections retained.
- `git diff --check` passed in both repositories. No migrations, auth changes, new packages or lockfile edits. Source review confirmed filter is before pagination, safe DTO unchanged and fork remains unfiltered.

## Areas changed

Backend: existing social Get repository/BL/internal controller/facade/interface/HTTP controller chain; focused BL/identity/HTTP tests. Frontend: SocialHttpService optional query, SharedCollectionComponent state/template/Forms import/states, focused component/HTTP tests and neutral shared card-search helper text. Analyst updated backend/frontend specs and run016 evidence/reports.

## Notes

Search uses existing database lower/contains convention rather than a new search engine/index or diacritic normalization layer. Current server page cap50 and owner local paging25/50/100 are unchanged. Production rollout and load testing are outside this narrow search follow-up.
