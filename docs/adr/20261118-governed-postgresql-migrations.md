# ADR-046: Governed PostgreSQL migrations

## Context

`MigrationService` applies the Admin, Catalog, Branding, and Management Security EF migrations
before the dependent hosts start. It also initializes the Catalog event store and bootstraps
Branding revisions. It seeds synthetic Admin data in Development. The shared PostgreSQL access
policy already calls for credentials scoped to each capability.
[Migration service](../../src/ViajantesTurismo.MigrationService/README.md) ·
[PostgreSQL access policy](20260707-postgresql-access-policy.md)

Production deployment needs reviewed SQL, compatibility across an application rollout, a record
of the exact executed bytes, and a reproducible recovery path. EF migration history establishes
which EF migrations a database has seen. Data backfill checkpoints, release compatibility, and
recovery rehearsal need separate records.

## Decision

Use versioned PostgreSQL scripts and a repository-owned .NET executor as the production
deployment input. Keep EF design-time factories, migrations, and snapshots for model scaffolding,
drift detection, and script provenance. Review generated SQL and provider-specific setup
together. Retain the existing Aspire migration and initialization flow for local development
and tests during adoption. The protected production deployment invokes the executor with
separate credentials before rolling out the matching application release.
[EF Core deployment strategies](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

### Migration manifest

Track a versioned manifest and scripts as reviewable source. Bind promotion to their paths and
SHA-256 digests in an attested release artifact. Validate the manifest, supported source release,
and exact contents of every listed script before contacting the database.

| Field | Contract |
| --- | --- |
| `schemaVersion`, `migrationId`, `context` | Versioned contract and stable migration identity within its owning context |
| `sourceReleases`, `targetRelease` | Explicit supported predecessors and the release this migration prepares |
| `steps[].phase`, `steps[].order` | Unique, ordered steps for expansion, data, verification, and contract |
| `omittedPhases[].phase`, `omittedPhases[].reason` | Reviewed omissions for phases with no step |
| `steps[].script`, `steps[].sha256` | Manifest-relative regular file inside the artifact and the digest of its exact bytes |
| `steps[].transaction`, `steps[].timeoutSeconds`, `steps[].lockTimeoutSeconds` | Atomic, batched, or online execution with bounded statement and lock waits |
| `steps[].checkpoint`, `steps[].verify` | Stable batch progress key for resumable data steps and a separately hashed verification script where needed |
| `steps[].onlineIndex` | Explicit index identity and ordered B-tree keys for concurrent index expansion |
| `steps[].destructive` | Explicit review classification. Destructive steps belong to the contract phase. |
| `recovery.kind`, `recovery.evidence`, `recovery.script` | Recovery method, content-addressed evidence, and required SQL for reversible or compensating recovery |

Expansion keeps the previous application release functional. Data steps can commit bounded batches.
Verification checks the expected data and application behavior. Contract steps require evidence
that the previous application version has been retired. A phase with no work is declared explicitly
with its reason. Verification is required on every manifest. Irreversible changes carry an approved
recovery procedure and a successful rehearsal before production execution.

The CLI provides a read-only plan before writes. Its journal records context, migration ID, step,
script digest, release and tool versions, attempt state, and sanitized timing. A transaction
commits atomic SQL and its completed state together. Each committed data batch records its
checkpoint with the data mutation. A permanent failure keeps the last completed checkpoint.
On retry, applied steps with matching digests are skipped, incomplete data resumes at the
checkpoint, and any digest drift stops the run.

Take one PostgreSQL advisory lock for the context before reading or writing its journal. Use a
dedicated direct session for execution across transactions. A disconnected executor loses its lock
and must acquire it again and reconcile state before continuing. The deployment job owns the
connection lifetime and refuses a second owner. Nontransactional DDL is an explicit exception with
a postcondition and recovery step. For example, concurrent index creation can leave an invalid
index and requires inspection before retry. [PostgreSQL advisory locks](https://www.postgresql.org/docs/18/explicit-locking.html#ADVISORY-LOCKS) ·
[Concurrent index creation](https://www.postgresql.org/docs/18/sql-createindex.html#SQL-CREATEINDEX-CONCURRENTLY)

### Recovery and release compatibility

| Class | Required recovery evidence |
| --- | --- |
| Reversible | A reviewed down script preserves data still needed by the previous release. Rehearse with both application versions. |
| Compensating | A new, reviewed forward migration repairs state while retaining the journal and audit trail. |
| Restore-based | Rehearse backup or point-in-time recovery into a separate target, then verify data, application reads, and cutover before promotion. |
| Forward-only | Record the recovery limits and a tested forward fix. An application-only rollback is allowed only while the schema remains compatible. |

Restoring to an earlier point can discard unrelated writes. The recovery plan specifies affected
databases, the recovery point and tolerable data loss, and how to reconcile or quiesce writes
before cutover. Coordinate Admin and Catalog restore points when integration-event progress
depends on both. A successful SQL exit alone is insufficient, since the old and new applications
must read the resulting state. [PostgreSQL continuous archiving and PITR](https://www.postgresql.org/docs/18/continuous-archiving.html)

### Ownership and risk controls

| Owner | Initial script inputs and verification |
| --- | --- |
| Admin | Write model, document constraints and triggers, holds, idempotency, outbox, and Admin-to-Catalog transport. Verify payment/document guards and readable bookings. |
| Catalog | Read-model migration, media constraints, inbox/checkpoint state, and event-store schema currently initialized by the provider. Verify event replay and consumer progress. |
| Branding | Branding schema, its qualified EF history and messaging storage, plus revision bootstrap as a data step. Verify revision lineage and projection visibility. |
| Management Security | Security persistence for OIDC, tickets, and token state. Verify store availability with synthetic records and protect token values in evidence. |

The schema-owner role is available only inside the authorized deployment job. Use narrowly
granted data and verifier roles for their phases and a runtime role with ordinary read/write
permissions. Runtime identities must lack schema ownership, membership in owner roles, and
`CREATE` on application schemas. Test those capabilities with the actual PostgreSQL principals.
Use the established secret provider and keep credentials and user data out of manifest, CLI
arguments, journal, and logs. [PostgreSQL privileges](https://www.postgresql.org/docs/18/ddl-priv.html)

Script digests detect drift. Authenticity comes from review, protected release configuration, and
verification of the release artifact before execution. Arbitrary SQL cannot be classified reliably
by matching a few keywords. Treat scripts as privileged code, require explicit destructive-change
approval, bind the selected action to its reviewed recovery declaration, and enforce role scope at
the database. Limit report fields to migration identifiers, digests, counts, timing, and outcomes.

### Adoption and rehearsal

1. Validate the four design-time factories, scaffold metadata, and extract reviewed baseline SQL.
   Inventory EF history tables, provider-managed event-store objects, and existing transport objects.
2. Deliver manifest validation and plan output before writable execution. Then prove PostgreSQL
   journaling, interruption, retry, checksum drift, and advisory lock ownership with synthetic data.
3. Baseline empty and existing targets separately. Preserve existing EF history and compare claimed
   history with schema objects and drift checks before recording the governed baseline. Unknown
   history must stop for explicit reconciliation.
4. Rehearse each supported source release, including interruption at schema and data boundaries,
   old/new compatibility, performance and lock budgets, and the declared recovery path. Attach
   report digests and application verification to the rehearsal result.
5. Adopt the executor for production only after all application schemas, role grants, and rehearsal
   checks pass. Keep local Aspire initialization available as a separate developer workflow.

## Alternatives

| Approach | Assessment |
| --- | --- |
| EF CLI, bundle, or runtime migration | Useful for local initialization and model scaffolding. Reviewed script content and a first-class data and recovery lifecycle are the production boundary. |
| DbUp | Supports SQL migration journals and configurable transaction modes. The proposed phased checkpoints, role checks, and recovery evidence would still need a separate orchestration layer. |
| Flyway | Has mature versioned migrations and checksum validation. The application-specific release gates and resumable data journal would remain custom, and deployment would add a separate runtime toolchain. |
| Liquibase | Provides changesets and a tracked changelog. The proposed production inputs already use PostgreSQL SQL and require custom data and recovery evidence. |
| Repository-owned .NET executor | Reuses the PostgreSQL client and release toolchain, with reviewable SQL and testable application-specific gates. The repository owns ordering, journaling, concurrency, and failure behavior. |

[DbUp journaling](https://dbup.readthedocs.io/en/latest/more-info/journaling/) ·
[Flyway validation](https://documentation.red-gate.com/flyway/reference/commands/validate) ·
[Liquibase changelog tracking](https://docs.liquibase.com/concepts/tracking-tables/databasechangelog-table.html)

## Consequences

EF changes continue to get model-drift coverage. The approved production identity applies only
the reviewed release artifacts. Every context owner must maintain scripts, explicit preconditions,
and recovery evidence. Runtime DDL removal depends on the baseline and rehearsal coverage above.

## Status

Proposed for review. The migration service provides the read-only planner and an executor
for atomic expansion, concurrent index creation, resumable data, and read-only verification
with forward-only recovery
evidence. The data login executes bounded DML and records each batch in the same transaction
through an attempt-scoped journal function. The context fence rejects successor owners while
a previous data transaction is active. The executor confirms the original owner session before
recording the checkpoint. A disconnected session causes rollback.
The online index step journals before dispatch, then checks the same index in PostgreSQL before
requiring its reviewed verifier to pass. A retry reconciles a ready index with its prior attempt.
Invalid and conflicting indexes require the reviewed recovery path. The contract gate, recovery
execution and rehearsal, and verified baseline scripts and roles for each context remain
prerequisites for production cutover.

## Links

- [Architecture decision index](../ARCHITECTURE_DECISIONS.md)
- [EF Core migration contribution guide](../CODING_GUIDELINES.md#ef-core-migrations)
- [Migration service and plan command](../../src/ViajantesTurismo.MigrationService/README.md#governed-migration-planning)
- [Initial migration executor and role provisioning](../../src/ViajantesTurismo.MigrationService/README.md#governed-migration-execution)
