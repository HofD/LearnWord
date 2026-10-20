# Final acceptance — Social Collections

Decision: accepted locally, 2026-10-19. Complete vertical slice implemented in both sibling repositories. No production deployment performed.

## Implemented

Private/Unlisted/Public visibility with owner settings and clipboard share; stable UUID shared URL; authenticated safe read-only collection page; independent private fork with fresh cards/words/SRS and historical provenance; database-paginated Public-only Explore with name search; RU/EN states and existing analytics goals for published/unpublished/viewed/forked.

## Architecture and data

Preserved .NET 8/Angular 17/EF Core/PostgreSQL/Ocelot architecture and existing owner API routes. New social BL/repository and identity facade services expose explicit public allowlist DTOs. Migration `20261019000000_AddSocialCollections` adds Visibility (Private default), unique PublicId (database UUID default) and nullable ForkedFromCollectionId (no FK). Indexes cover share identity and visibility/id listing. No description/taxonomy/profile fields exist, so none were invented. Author uses a stable non-email pseudonym. User identity comes from validated JWT, never request-body owner ids.

Public API additions:
- PUT `/api/collections/{id}/visibility`
- GET `/api/social/collections?page=1&pageSize=20&search=...`
- GET `/api/social/collections/{publicId}?page=1&pageSize=50`
- POST `/api/social/collections/{publicId}/fork`

Frontend additions: authenticated `/explore`, `/shared/collections/:publicId` and isolated visibility component in existing owner editor. All public/shared reads require login, preserving the product authentication model and returnUrl. Public means discoverable to signed-in users; Unlisted means link-only for signed-in users. Owner mutation rules remain authoritative server-side. Private foreign reads/forks return 404; anonymous social calls return 401; own-source fork returns 409.

## Verification actually performed

- `./deploy/local-up.sh`: Docker build/start success for all services, frontend development build and migrator. Initial startup failed because Docker VM disk was full; removed only unused build cache, preserved images/volumes/data and successfully reran.
- `cd LearnWord && ./tests/run-all-tests.sh` inside .NET 8 SDK Docker container, same local Compose network, `LEARNWORD_E2E=true`, gateway `http://gateway:8080`: all 9 projects passed, 144 tests passed, none skipped. Includes 7 live gateway authentication-boundary cases. Initial gateway tests ran before services were available; corrected environment and full rerun passed.
- Full Jasmine/Karma suite in Docker Chromium: 43/43 pass, including 15 new Social service/UI tests. Repaired legacy HTTP/router/lifecycle test setup and isolated test language storage to avoid order-dependent failures; production behavior was not altered for test repairs.
- `npm run build`: production Angular build passed. Existing unchanged `collection.component.css` exceeds 2kB warning budget by 810 bytes; no type/build errors. No lint script is configured.
- Real PostgreSQL migration applied to two seeded pre-migration rows: original ids/names preserved, Visibility=0, distinct nonzero PublicId, null provenance. Rerunning migrator completed successfully.
- Personally executed `python3 agent-runs/014-social-collections/verify-social.py` against real gateway with two registered/confirmed local users: six scenario groups passed (private/default/anonymous matrix; owner-only writes; Unlisted safe DTO and Explore exclusion; Public search/pagination; deep private fork/fresh SRS/ownership/own409; stable rename, immediate privacy revocation, source deletion independence). Script cleans its test collections.
- Personally executed browser flow at desktop and 390x844: local test-account login, owner editor/shared view, Clipboard success, visitor login return to Explore, foreign read-only view, fork success and copied editor Private, Unlisted save and return Private/link removal, RU catalogue and empty search. Screenshots under `screenshots/`.
- Reviewed both Git diffs and untracked production/tests; `git diff --check` passed in both repositories. Existing working trees were clean before changes. No libraries/frameworks or lockfiles changed.

## Changed areas

Backend: domain Collection/enum, DTOs/errors, EF context/migration/snapshot, social repository/BL, mapper, social HTTP proxy/identity facade/controllers/DI, ownership-link repository, both gateway configs. Tests: new BL/identity social suites plus owner lifecycle/default identity/auth-boundary regressions. Frontend: social components/service/tests, routes, navigation, owner visibility insertion/model additions, RU/EN text and analytics events; legacy test setup repairs. Contracts, README and architecture docs updated. Specialist reports are in this run directory.

## Residual risk and deferred scope

Cross-service fork writes use atomic domain copy, atomic identity-link SaveChanges and compensating delete. If identity write and compensation both fail, an inaccessible Private unlinked copy may remain and is logged; durable reconciliation/outbox is follow-up. Snapshot isolation protects consistent in-flight read/fork; no distributed lock or retroactive revocation of already returned content is claimed. Frontend analytics hooks are existing optional client telemetry, not durable backend events. Concurrency stress/load testing, production rollout and public author profiles remain follow-up. Likes/comments/followers/subscriptions/payments/recommendations and Learner Model/Knowledge Graph remain out of scope.

## Epic 2 recommendation

Separate content identity and per-user learning state before allowing multiple learners to study one shared content object. SRS currently belongs to owned Card rows, and fork intentionally resets it. Preserve facade ownership boundaries, keep learner calculations in backend services and learner/private state out of social DTOs. Treat historical fork ids as provenance, not an always-existing graph edge; current client analytics should not become the authoritative learner-history ledger.
