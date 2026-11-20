# ViajantesTurismo.MigrationService

Database initialization service for application startup.

## Purpose

Short-lived process that runs Entity Framework Core migrations in every environment and atomically
initializes synthetic Admin data only in Development. It ensures database schemas are up to date
before the APIs start.

## Responsibilities

- **Migrations**: Apply pending EF Core migrations to database
- **Development data initialization**: Atomically populate the complete synthetic Admin data set only
  in Development
- **Startup Order**: Runs before API service starts (via `WaitForCompletion()`)
- **EF Core Tooling**: Each migration-owning project provides a design-time factory

## Design-time EF Core

Use the owning project's design-time factory for scaffolding, model validation, and SQL
generation. The factory uses a deterministic placeholder, so these operations run without
AppHost configuration or database credentials.

```bash
project=src/ViajantesTurismo.Admin.Infrastructure
context=AdminWriteDbContext
dotnet tool run dotnet-ef -- migrations list --no-connect \
  --project "$project" --startup-project "$project" --context "$context"
dotnet tool run dotnet-ef -- migrations has-pending-model-changes \
  --project "$project" --startup-project "$project" --context "$context"
dotnet tool run dotnet-ef -- migrations script 0 \
  --project "$project" --startup-project "$project" --context "$context" --output admin.sql
```

| Owning project under `src/` | Context |
| --- | --- |
| `ViajantesTurismo.Admin.Infrastructure` | `AdminWriteDbContext` |
| `ViajantesTurismo.Catalog.Infrastructure` | `CatalogDbContext` |
| `ViajantesTurismo.Branding.Infrastructure` | `BrandingDbContext` |
| `ViajantesTurismo.Management.Security` | `ManagementSecurityDbContext` |

The read and transport contexts share schemas and history with their owners. The Catalog
provider initializes the event-store schema separately. The
[governed migration decision](../../docs/adr/20261118-governed-postgresql-migrations.md)
describes the production script adoption and rehearsal.

The history was reset before launch: each context has one `Initial*` migration. A local database built from the old
history must be recreated; see "Migration history reset before launch" in `docs/CODING_GUIDELINES.md`.

## Execution

Starts the Generic Host, runs `DatabaseInitializationWorker` through `MigrationProcess`, stops the host,
and returns an explicit process status. Dependent services wait for successful completion before starting.

## Governed migration planning

Use the native plan command to validate a release manifest and its artifacts before the database
phase. The command runs without starting the service host or opening a database connection.

```bash
dotnet run --project src/ViajantesTurismo.MigrationService -- \
  plan path/to/manifest.json --from 2026.11.1
```

The [manifest contract](../../docs/adr/20261118-governed-postgresql-migrations.md#migration-manifest)
defines the phases, recovery modes, and artifact fields. Use a UTF-8 JSON manifest no larger
than 1 MiB and give each artifact a relative path and SHA-256 digest. Every phase needs ordered
steps or an explicit omission. The verification phase always needs a step. The command prints
the validated identifiers, ordered phases, and digests on success. It returns 1 for an invalid
manifest or artifact and 2 for incorrect arguments.

## Governed migration execution

The initial executor supports atomic expansion and read-only verification with forward-only
recovery evidence. It rejects data, online, contract, and executable recovery operations before
opening a connection. Keep production promotion gated on the complete lifecycle rehearsal
in the [governed migration decision](../../docs/adr/20261118-governed-postgresql-migrations.md#adoption-and-rehearsal).

The protected release job supplies `VT_MIGRATION_OWNER_CONNECTION` and
`VT_MIGRATION_VERIFIER_CONNECTION` using the secret provider. Both connection strings must name
the same database on a single direct PostgreSQL primary endpoint and use separate scoped
logins. The advisory lock requires a dedicated PostgreSQL session across transactions.

```bash
dotnet run --project src/ViajantesTurismo.MigrationService -- \
  execute path/to/manifest.json --from 2026.11.1
```

Provision the `governed_migration` schema in the target database with the deployment owner as its
owner. The executor creates `governed_migration.attempts` and its indexes there. Scope the owner
to the context's application schema and this journal. Both deployment logins need `CONNECT`
and `TEMPORARY` for the session-local functions. Give the verifier only the schema usage and
SELECT permissions its gates need. Keep application runtime identities out of the deployment
roles and the journal. Use a separately provisioned database per context for this journal model.

Use reviewed PostgreSQL SQL in UTF-8 without a byte order mark, up to 16 MiB per script. The
executor checks the original bytes before connecting and checks the exact bytes again immediately
before their step. Expansion SQL runs in one transaction with the completed journal entry.
Verification runs with the verifier login in a read-only transaction and must return one row and
one PostgreSQL Boolean column with value `true`. Failed or interrupted verification can run again
without repeating completed expansion. The context lock rejects a concurrent executor.

A retry uses the same manifest bytes, selected source release, and script digests. A previous
session's open attempt becomes failed with code `interrupted` after the retry acquires the context
lock. The journal keeps the old attempts. Process status 0 means every step completed, 1 means
validation or execution failed, 2 means incorrect usage, and 130 means the operator interrupted
the command. The source release is supplied by the protected deployment job.

## Dependencies

- **Admin, Catalog, Branding, and Management Security Infrastructure**: Database contexts and migrations
- **ViajantesTurismo.ServiceDefaults**: Service discovery and telemetry
- **Entity Framework Core**: Migration execution
