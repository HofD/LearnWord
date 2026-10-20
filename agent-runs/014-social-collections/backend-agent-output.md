# Backend delivery: Social Collections

Implemented the existing facade/BL/DAL architecture without replacing owner CRUD or scheduler behavior.

- Added `CollectionVisibility` (Private default), stable UUID `PublicId`, nullable non-FK `ForkedFromCollectionId`; mapped owner DTOs and added PostgreSQL backfill/index migration plus EF snapshot.
- Added authenticated gateway/facade Explore, paged shared reads, owner visibility updates and fork. Shared DTOs expose only the documented name, pseudonymous author, counts and word content; owner collection id appears only for its owner.
- Explore filters Public and applies search, ordering, count, skip and take in SQL. Shared content uses a bounded SQL page and a coherent transaction snapshot.
- Fork rechecks current visibility in a serializable transaction, creates independent Private collections/cards/words with fresh SRS state, and stores provenance without a relationship to the source.
- Identity links for the new collection and every card are written in one `SaveChanges` transaction. Failure triggers compensating removal of the core copy; cleanup failure is logged and the original error preserved.
- Existing card creation already validates parent ownership in `CardIdentityService`; retained that behavior.

Changed production areas: Models/BL DTOs and mapping; DAL context and social repository; BL social service; Identity social HTTP adapter/service/controller and collection visibility action; identity ownership repository; internal WebApi social controller/visibility action; both DI registrations; both Ocelot configs; migration and snapshot.

Verification: no direct sandbox dotnet build/restore/test executed. Root coordinates `./deploy/local-up.sh`, backend regression and runtime acceptance to avoid concurrent Docker builds. Root reported production Docker compilation succeeded on the first pass and all 24 BL checks passed. Initial container startup encountered Docker disk exhaustion; root freed old build cache and is rerunning local-up. Full regression and runtime acceptance remain root-owned pending checks.

Residual risk: cross-service compensation is best effort if both identity link persistence and core cleanup fail. There is no distributed transaction or retry queue. A partially created copy remains Private and without usable owner links in that case. Existing in-flight reads/forks use snapshot consistency; later requests recheck current access.
