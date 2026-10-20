# Backend delivery: shared collection search

Added optional `search` through the existing shared-read repository, BL, internal controller, identity HTTP interface/adapter, facade and public controller. Existing calls remain valid; the facade appends search after userId. No migration, dependency or new abstraction.

The active-card SQL query matches any active word value OR translation using a trimmed case-insensitive substring before count/order/skip/take. A card appears once with all its active words. `cardsCount` stays the collection-wide count; `totalCount` counts matches. Empty search reuses existing unfiltered count. Authentication, visibility and owner checks remain unchanged; fork reads/copies the full source.

Tests added/updated:
- SQLite BL: matches beyond the original first page; translation/value, case/whitespace, stable filtered paging, counts, complete multiword cards without duplicates, deleted cards/words, foreign collection exclusion, transcription exclusion, empty/no results, full fork after search and deleted-source access.
- Identity: search propagation with Private/Unlisted/Public access matrix, anonymous controller denial, authenticated forwarding and unfiltered fork.
- Existing HTTP listener fixture: encoded Cyrillic and reserved characters preserve search/paging without query injection.

Changed production: SocialCollectionRepository, SocialCollectionService, both SocialCollectionsControllers, ISocialCollectionsHttpService, SocialCollectionsHttpService, SocialCollectionIdentityService. Changed tests: SocialCollectionServiceTests, SocialCollectionIdentityServiceTests, HttpUpstreamServiceTests.

Verification: `git diff --check` passed. No sandbox dotnet or Docker builds/tests launched by this agent; root coordinates `./deploy/local-up.sh` and `cd LearnWord && ./tests/run-all-tests.sh`. Compilation/regression/runtime results pending coordinator.

Residual risk: substring search uses the existing database lower/contains convention. SQLite case folding is narrower than PostgreSQL for non-ASCII text; runtime Unicode behavior should be verified on the Docker PostgreSQL stack.

Analyst verification: full Docker build passed; all9 backend projects passed158tests; real gateway/PostgreSQL late-card/uppercase Cyrillic/encoding/count/pagination/access smoke passed. See final-acceptance.md.
