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

The `execute` command prepares schema expansion, concurrent indexes, resumable data batches,
and read-only verification. Two explicit commands handle later contract steps and the
executable recovery declared by the migration. Keep production promotion gated on the
complete lifecycle rehearsal in the
[governed migration decision](../../docs/adr/20261118-governed-postgresql-migrations.md#adoption-and-rehearsal).

The protected release job supplies `VT_MIGRATION_OWNER_CONNECTION` and
`VT_MIGRATION_VERIFIER_CONNECTION` using the secret provider. Preparation with a `data` step
also requires `VT_MIGRATION_DATA_CONNECTION`. All connections name the same database on a
single direct PostgreSQL primary endpoint and use separate scoped logins. The advisory lock
requires a dedicated PostgreSQL session across transactions.

```bash
dotnet run --project src/ViajantesTurismo.MigrationService -- \
  execute path/to/manifest.json --from 2026.11.1
```

Provision the `governed_migration` schema in the target database with the deployment owner as its
owner. The executor creates `governed_migration.attempts` and its indexes there. Scope the owner
to the context's application schema and this journal. All deployment logins need `CONNECT`
and `TEMPORARY` for the session-local functions. Give the verifier only the schema usage and
SELECT permissions its gates need. Grant the data login only schema usage and the DML its
backfills need. It uses the executor-installed checkpoint function without direct journal table
privileges. Keep application runtime identities out of the deployment roles and the journal.
Use a separately provisioned database per context for this journal model.

Use reviewed PostgreSQL SQL in UTF-8 without a byte order mark, up to 16 MiB per script. The
executor checks the original bytes before connecting and checks the exact bytes again immediately
before their step. Expansion SQL runs in one transaction with the completed journal entry.
Verification runs with the verifier login in a read-only transaction and must return one row and
one PostgreSQL Boolean column with value `true`. Failed or interrupted verification can run again
without repeating completed expansion. The context lock rejects a concurrent executor.

A retry uses the same manifest bytes, selected source release, and script digests. A previous
session's open attempt becomes failed with code `interrupted` after the retry acquires the context
lock. The journal keeps the old attempts. Process status 0 means every step selected by the
command completed, 1 means validation or execution failed, 2 means incorrect usage, and 130 means the operator interrupted
the command. The source release is supplied by the protected deployment job.

### Online index expansion

An `expand` step with transaction policy `online` declares `onlineIndex` and a separately
hashed `verify` script. The declaration specifies the target schema and table, the index
name, one to 32 ordered and distinct B-tree key columns, and whether the index is unique.
Each identifier uses lowercase ASCII letters, digits, and underscores, begins with a
letter or underscore, and has at most 63 characters.

```json
{
  "schema": "migration_fixture",
  "table": "entries",
  "name": "entries_lookup",
  "columns": ["category", "position"],
  "unique": false
}
```

The SQL artifact is the one statement below for this declaration. Quote every identifier,
use `CREATE UNIQUE INDEX` when `unique` is true, and include the final semicolon. The
artifact may end with one LF newline. The planner and executor check the declaration
against those exact bytes before the executor connects.

```sql
CREATE INDEX CONCURRENTLY "entries_lookup" ON "migration_fixture"."entries" USING btree ("category", "position");
```

The attempted operation is journaled before dispatch. The schema owner executes the
statement outside a transaction block on the session that holds the context lock. The
verifier checks the index definition and its PostgreSQL validity, then runs the reviewed
read-only `verify` script. Both checks must pass before the owner completes the attempt.

A first attempt requires the index name to be vacant. A retry with the same manifest
rechecks an index previously attempted by that migration. It reruns the catalog check
and verifier on a matching ready index, and can start another build if the name is vacant.
An invalid or conflicting object stops the run and stays available for inspection.
Use the reviewed recovery procedure before retrying after an invalid build. Read-only
verification and the remaining ordered migration steps follow the completed online expansion.

### Resumable data scripts

A `data` step declares transaction policy `batched`, a stable `checkpoint` key, and a
separately hashed, read-only `verify` script. A batch script uses one reviewed SQL statement.
Parameter `$1` is the last committed `bigint` cursor, initially zero. Parameter `$2` is
the maximum batch size as an `integer`, currently 1,000. Use a stable ordering and an ordinal
cursor that contains no personal data. Keep the selection within the supplied row limit and
the declared timeout.

The statement returns exactly one row with a `bigint` next cursor, an `integer` affected
count, and a `boolean` that marks completion. An advancing cursor requires a positive
affected count within the limit. An empty terminal batch returns the original cursor and
zero affected rows. An invalid result or a nonterminal batch without progress rolls back.

Each batch changes application data and appends its cursor, affected count, and completion
state together in PostgreSQL. The executor revalidates the batch artifact before each
dispatch and confirms the original owner connection before recording the checkpoint. A retry
skips committed batches and starts from the journaled cursor. A terminal
batch still requires the step's verification script to return one true PostgreSQL Boolean.
A failed gate leaves the data attempt incomplete, so the retry rechecks that gate without
repeating the batch writes. After that gate passes, the manifest continues to its ordered
verification phase.

### Contract after application retirement

A manifest can include ordered atomic `contract` steps. The `execute` command leaves them
pending while the previous application remains compatible. The protected deployment job
runs `contract` after it verifies retirement of every declared source release and a
successful recovery rehearsal for the target. The job authenticates its retirement receipt
and supplies the digest through the same protected configuration that selects the database.
The executor verifies the receipt's bytes, release and target bindings, and completion of
the prior migration phases before dispatching any contract SQL.

```bash
dotnet run --project src/ViajantesTurismo.MigrationService -- \
  contract path/to/manifest.json --from 2026.11.1 \
  --retirement path/to/retirement.json --sha256 <receipt-sha256>
```

Use a UTF-8 JSON receipt no larger than 64 KiB. It contains `schemaVersion` with value `1`,
`context`, `migrationId`, `manifestSha256`, `targetRelease`, all `retiredSourceReleases`,
`deploymentTarget`, `database`, `recoveryEvidenceSha256`, and `recoveryRehearsalPassed`
with value `true`. The protected job sets `VT_MIGRATION_DEPLOYMENT_TARGET` to the receipt's
target identifier. The receipt's database name must match the configured owner connection.
The journal stores the receipt SHA-256 with each dispatched contract step. The contract SQL
and completed attempt commit together under the context lock.

### Declared recovery

For `reversible` or `compensating` recovery, the manifest names reviewed SQL in `recovery.script`,
a separately hashed read-only gate in `recovery.verify`, and the reviewed rehearsal evidence
in `recovery.evidence`. `recovery.timeoutSeconds` and `recovery.lockTimeoutSeconds` default to
60 and 30 seconds. Restore-based and forward-only procedures run through the protected
operator procedure with their own validation. They provide their recovery evidence by digest.

```bash
dotnet run --project src/ViajantesTurismo.MigrationService -- \
  recover path/to/manifest.json --from 2026.11.1
```

`recover` checks the original exact manifest and source release and requires prior mutation
history. It records the recovery script and its completion with the mutation in one
transaction. The verifier then checks the recovery gate on its separate read-only session.
A retry can finish the gate without repeating SQL that already committed. Once recovery
starts, preparation and contract for the same manifest are fenced. Apply a separately
reviewed forward migration to continue from the recovered state. Production rehearsal
includes reads by the application versions and grants as well as the SQL gate.

## Dependencies

- **Admin, Catalog, Branding, and Management Security Infrastructure**: Database contexts and migrations
- **ViajantesTurismo.ServiceDefaults**: Service discovery and telemetry
- **Entity Framework Core**: Migration execution
