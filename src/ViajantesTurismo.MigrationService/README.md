# ViajantesTurismo.MigrationService

Database migration and seeding service for application startup.

## Purpose

Background service that runs Entity Framework Core migrations and seeds initial data. Ensures database schema is
up-to-date before the API starts.

## Responsibilities

- **Migrations**: Apply pending EF Core migrations to database
- **Seeding**: Populate initial data for development/testing
- **Startup Order**: Runs before API service starts (via `WaitForCompletion()`)

## Execution

Runs as a hosted service, executes migrations and seeding, then completes. The API service waits for this service to
finish before accepting requests.

## Dependencies

- **ViajantesTurismo.Admin.Infrastructure**: Database context and migrations
- **ViajantesTurismo.ServiceDefaults**: Service discovery and telemetry
- **Entity Framework Core**: Migration execution
