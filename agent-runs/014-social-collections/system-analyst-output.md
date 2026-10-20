# Analysis and implementation plan

## Audit

Two sibling Git repositories; backend .NET 8, EF Core/Npgsql 8, PostgreSQL, separate domain (`words`) and ownership (`identitywords`) schemas, authentication identity schema. Ocelot is public boundary. LearnWord.Identity validates JWT and ownership; internal LearnWord.WebApi owns domain CRUD/BL/DAL. Domain Collections contain Name, Cards, timestamps; no descriptions, categories, persistent language or tags. Card owns multiple words and SRS state. No existing collection import/export/fork flow found. AI uses ownership facade then internal Fake/OpenRouter generation with explicit accept into existing card API. Existing telemetry is frontend AnalyticsService/Yandex goals. Angular 17 standalone components, RxJS HttpClient/local component state, auth guard, RU/EN strings, existing Bootstrap/custom CSS; Jasmine/Karma frontend and xUnit/Moq/EF test projects backend. Docker helpers build/start and migrator applies three schema migration sets; production internal services are not published.

## Decisions

Preserve all existing owner CRUD routes and ownership links. All social reads require login, matching product access model; login returnUrl preserves share destination. UUID share identity avoids sequential-id link guessing and survives rename. Explicit public DTOs omit emails, ownership identifiers, timestamps and review state. Author is a stable pseudonymous label because there is no public display-name/profile model. No new taxonomy/description/profile fields. Fork is private, deep copied and resets SRS; integer provenance is intentionally not an FK so deleting the source leaves a valid historical reference. Page shared content as well as Explore; no unlimited catalogue download. Visibility must be reread server-side. Use existing analytics instead of a bus.

## Ownership

- backend_social: backend production models, migrations, BL, facade, gateway route contracts.
- frontend_social: frontend components, routing, translations, analytics and focused Jasmine tests.
- qa_social: backend automated regression and security tests.
- system analyst: contracts/specs, independent acceptance, Docker builds/tests, authenticated gateway E2E, browser desktop/mobile checks and final diff review.

## Verification

Docker-first local-up.sh, repository tests/run-all-tests.sh (Docker SDK environment as appropriate), frontend build and Jasmine ChromeHeadless. Validate migration against persisted legacy collections, gateway security matrix, visibility revocation, fork content and learning state, rename stable URL, source deletion, search/pagination and no private DTO leakage. Review both Git diffs. No production deployment requested.
