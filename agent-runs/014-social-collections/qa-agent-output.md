# Backend QA — Social Collections

Tested by new automated coverage:
- Stable numeric visibility contract; new collections default to Private with unique UUIDs stable through rename.
- Public-only Explore, filtering before paging, name search, active content pagination and visibility changes.
- Private/Unlisted/Public shared access; Private fork denied; own fork conflicts; owner-only visibility writes.
- Independent fork IDs/content, fresh SRS, all ownership links, and surviving provenance/content after source deletion.
- Public DTO property allowlists exclude user/email/card IDs/review state; author labels contain no email.
- Missing/undefined visibility and invalid pagination rejected. Existing owner CRUD and foreign card-parent/word ownership coverage retained.
- Four additional gateway anonymous-auth scenarios (Explore, shared view, fork, visibility).

Files:
- `LearnWord/tests/LearnWord.BL.Tests/SocialCollectionServiceTests.cs` (new)
- `LearnWord/tests/LearnWord.Identity.Tests/SocialCollectionIdentityServiceTests.cs` (new)
- `LearnWord/tests/LearnWord.BL.Tests/CollectionCardWordServiceTests.cs`
- `LearnWord/tests/LearnWord.Identity.Tests/OwnershipIntegrationTests.cs`
- `LearnWord/tests/LearnWord.Api.E2E.Tests/GatewayAuthBoundaryTests.cs`

Commands/result:
- QA agent did not run build/test commands; orchestrator owns `./deploy/local-up.sh` and `cd LearnWord && ./tests/run-all-tests.sh` to avoid simultaneous builds.
- Test execution result must be taken from the orchestrator's verification log.

Residual risk:
- SQLite-backed tests use EnsureCreated, so they do not prove PostgreSQL existing-row migration/backfill. Verify Private default and unique public UUIDs on preexisting rows in Docker.
- Gateway tests require LEARNWORD_E2E=true and running Docker; otherwise they are explicitly skipped.
- Human UI acceptance and concurrent cross-service visibility changes require runtime verification.
