# ViajantesTurismo.MigrationService

Database migration and seeding service for application startup.

## Purpose

Short-lived process that runs Entity Framework Core migrations and seeds initial data. Ensures database schema is
up-to-date before dependent services start.

## Responsibilities

- **Migrations**: Apply pending EF Core migrations to database
- **Seeding**: Populate initial data for development/testing
- **Startup Order**: Runs before API service starts (via `WaitForCompletion()`)
- **EF Core Tooling**: Serves as startup project for `dotnet ef` commands

## Creating Migrations

This project **must** be used as the `--startup-project` when running EF Core commands:

```powershell
# From repository root:
dotnet ef migrations add MigrationName --project src/ViajantesTurismo.Admin.Infrastructure --startup-project src/ViajantesTurismo.MigrationService --context AdminWriteDbContext
dotnet ef migrations add MigrationName --project src/ViajantesTurismo.Catalog.Infrastructure --startup-project src/ViajantesTurismo.MigrationService --context CatalogDbContext
dotnet ef migrations add MigrationName --project src/ViajantesTurismo.Branding.Infrastructure --startup-project src/ViajantesTurismo.MigrationService --context BrandingDbContext
dotnet ef migrations add MigrationName --project src/ViajantesTurismo.Management.Security --startup-project src/ViajantesTurismo.MigrationService --context ManagementSecurityDbContext
```

## Execution

Starts the Generic Host, executes migrations and seeding through `MigrationRunner`, stops the host, and returns an
explicit process status. Dependent services wait for successful completion before starting.

## Dependencies

- **Admin, Catalog, Branding, and Management Security Infrastructure**: Database contexts and migrations
- **ViajantesTurismo.ServiceDefaults**: Service discovery and telemetry
- **Entity Framework Core**: Migration execution
