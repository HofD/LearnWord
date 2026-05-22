# Compact Migration Bundles

## User Request

Production image/archive size grew sharply after adding automatic EF Core migrations. Investigate why and replace the SDK-heavy migrations image with a compact runtime approach.

## Scope

- Backend deployment repository.
- Production and local Docker migration service.
- Deployment documentation and process notes.

## Acceptance Criteria

- Production still runs all three EF Core migration sets before application services start.
- The final `migrations` image no longer ships the .NET SDK/source-tree `dotnet ef` execution environment.
- Local Docker startup and production deployment use the same migration execution model.
- Deployment docs explain the migration image design and runtime identifier.

## Out Of Scope

- Database credential recovery or hosting-side PostgreSQL fixes.
- Changing application service images.
- Changing frontend behavior.
