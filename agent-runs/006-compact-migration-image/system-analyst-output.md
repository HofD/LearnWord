# System Analyst Output

## Requirement Interpretation

The size increase was caused by adding a `migrations` image based directly on `mcr.microsoft.com/dotnet/sdk:8.0`, copying the backend source tree, installing `dotnet-ef`, and saving that image into the production archive. The desired behavior is to keep automatic production migrations while removing SDK/source payload from the final migrations image.

## Affected Specs And Docs

- `deploy/README.md` documents the compact migration bundle design.
- `agent-runs/006-compact-migration-bundles/` records the deployment change.

No backend API or frontend behavior spec change is required because public runtime behavior and API contracts do not change.

## Direct Actions

- Reworked `LearnWord/Dockerfile.migrations` into a multi-stage image:
  - SDK build stage publishes a small `LearnWord.Migrator` executable.
  - final stage uses `mcr.microsoft.com/dotnet/aspnet:8.0`, which is already shared by the production .NET application images.
- Added `LearnWord.Migrator`, which runs `Database.MigrateAsync()` sequentially for the three DbContexts using `ConnectionStrings__LwConnection`.
- Added the migrator project to `LearnWord.sln`.
- Updated local and production Compose files to rely on the image entrypoint instead of overriding it with `dotnet ef`.

## Verification Plan

- Build the migrations image with Docker.
- Run the local Docker stack or at least the migration service against the local PostgreSQL container.
- Confirm the final image no longer contains SDK tooling as the execution path.

## Known Risks

- Verification requires Docker daemon access; if unavailable in the current environment, run `./deploy/local-up.sh` or the production deploy script on a machine with Docker.
- The migrator is framework-dependent and uses the same `aspnet:8.0` base family as the other .NET services.
