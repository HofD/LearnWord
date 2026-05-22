# Final Acceptance

## Decision

Accepted for image build; pending full stack runtime verification.

## Changed Files

- `LearnWord/Dockerfile.migrations`
- `LearnWord/LearnWord.sln`
- `LearnWord/LearnWord.Migrator/LearnWord.Migrator.csproj`
- `LearnWord/LearnWord.Migrator/Program.cs`
- `deploy/docker-compose.build.yml`
- `deploy/docker-compose.local.yml`
- `deploy/docker-compose.prod.yml`
- `deploy/env/deploy.env.example`
- `deploy/env/local.env.example`
- `deploy/local-up.sh`
- `deploy/local-up.ps1`
- `deploy/deploy.sh`
- `deploy/deploy.ps1`
- `deploy/README.md`

## Checks

- Static review confirms production no longer overrides the migrations entrypoint with `dotnet ef`.
- Static review confirms the final migrations image stage is `mcr.microsoft.com/dotnet/aspnet:8.0`.
- Docker build of `LearnWord/Dockerfile.migrations` succeeded for `learnword/migrations:bundle-check`.
- `docker history` shows the migrator payload layer is about `7.93MB`; the reported total image size includes the shared `aspnet:8.0` base layer already used by the .NET service images.
- `docker run --rm learnword/migrations:bundle-check` reaches the migrator entrypoint and fails fast with `ConnectionStrings__LwConnection is required.` when no connection string is provided.

## Checks Not Run

- Full local stack verification was not run yet in this record.

## Remaining Risks

- EF bundle execution must be verified against Docker because bundle generation/runtime behavior is build-tool sensitive.
- Production database credentials must be valid before the one-shot migration service can complete.

## Recommended Next Step

Run `./deploy/local-up.sh` locally or build `learnword/migrations` with Docker, then deploy after the hosting-side database password issue is resolved.
