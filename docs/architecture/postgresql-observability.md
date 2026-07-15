# PostgreSQL observability

## Scope and sources

Epic #983/#984 provides opt-in, advisory PostgreSQL index-health collection through
`SharedKernel.Observability.Npgsql`. `ViajantesTurismo.DatabaseObservability` applies the reusable
host once to both production databases: `admin-database` and `catalog-database`.

Assumptions:

- Locally tested with PostgreSQL 18.4; compatibility with other PostgreSQL versions is tracked in #1052.
- a dedicated, least-privilege monitoring `NpgsqlDataSource`
- PostgreSQL cumulative statistics are reset-dependent and are not workload history

Sources:

- [PostgreSQL statistics views](https://www.postgresql.org/docs/16/monitoring-stats.html)
- [PostgreSQL predefined roles](https://www.postgresql.org/docs/16/predefined-roles.html)
- [Npgsql data sources](https://www.npgsql.org/doc/basic-usage.html#data-source)

## Metrics

Service defaults subscribe to these meters:

| Meter | Metric | Tags |
| --- | --- | --- |
| `SharedKernel.Observability.Npgsql` | `postgresql.index_health.collections` (`{collection}` counter) | `outcome` |
| `SharedKernel.Observability.Npgsql` | `postgresql.index_health.assessments` (`{assessment}` counter) | `action`, `reason` |

`postgresql.index_health.collections` has only these `outcome` values:

- `outcome`: `collected`, `permission_denied`, `unsupported`, `unavailable`

`postgresql.index_health.assessments` has only these values:

- `action`: `insufficient_evidence`, `review_creation`, `review_modification`
- `reason`: `statistics_unavailable`, `statistics_window_too_short`, `table_too_small`,
  `protected_index`, `unsupported_index_shape`, `per_object_statistics_window_unavailable`,
  `high_index_read_volume`, `high_sequential_scan_volume`, `insufficient_activity`

No schema, table, index, role, tenant, connection-string, SQL, parameter, or error-text value is a
metric tag.

## Read-only collection

`PostgreSqlIndexHealthCollector` uses fixed PostgreSQL catalog `SELECT` statements with a command
timeout and cooperative cancellation. It reads `pg_stat_database`, `pg_stat_user_indexes`,
`pg_stat_user_tables`, `pg_index`, and `pg_constraint`; it never uses locking clauses or executes
DDL, DML, `ANALYZE`, `VACUUM`, or index-maintenance commands.

Grant the monitoring role only `pg_read_all_stats` when it must observe all database objects. Do not
grant DDL, DML, ownership, or application-role privileges. Ensure the database does not grant the
role `TEMPORARY` through `PUBLIC`; revoke that privilege from `PUBLIC` and do not grant it to the
monitoring role.

## Evidence and outcomes

Collection is advisory only. A candidate needs:

- at least seven days since `stats_reset`
- table statistics from `ANALYZE` or auto-analyze
- an estimated table size of at least 10,000 rows

Primary, unique, and constraint-backed indexes are protected. Invalid, unready, dead, partial, and
expression indexes are excluded. The collector also verifies that the latest analyze timestamp is no
older than the database statistics reset.

Further review thresholds are deliberately conservative:

- high index-read volume: at least 100 index scans and tuple reads at least ten times the estimated
  row count
- high sequential-scan volume: at least 1,000 sequential scans and tuple reads at least ten times
  the estimated row count

`ReviewCreation` and `ReviewModification` require human review and a separately managed migration.
They never apply a schema change. Removal recommendations are intentionally withheld: PostgreSQL can
reset statistics for one table or index without changing `pg_stat_database.stats_reset`, so a single
collection cannot establish a safe per-index observation window. Statistics are cumulative estimates,
may reset, and do not prove query cost, selectivity, workload intent, or a safe schema change;
incomplete, protected, or unsupported evidence yields `InsufficientEvidence`.

Permission failure yields `PermissionDenied`; unsupported capability yields `Unsupported`; connection
and timeout failures yield `Unavailable`. These outcomes emit bounded metrics, return no assessments,
and do not stop the host. Caller-requested cancellation is rethrown.

## Opt-in composition

The AppHost omits the database-observability host by default. Configure its user-secret store or
deployment configuration with the feature flag and both secret parameters to enable collection:

```text
Aspire:Features:DatabaseObservability=true
Parameters:admin-index-health-connection-string=<admin monitoring-role connection string>
Parameters:catalog-index-health-connection-string=<catalog monitoring-role connection string>
```

Defaults:

- polling interval: one hour; accepted range: one minute through one day
- command timeout: 30 seconds; accepted range: greater than zero through five minutes

When monitoring is enabled, invalid, missing, or malformed configuration fails startup with a
connection-string-free validation message. Disabled monitoring registers no collector. A collection
cycle visits both databases sequentially and emits only aggregate bounded dimensions; it never emits
a database, schema, or object identifier. Each subsequent cycle begins only after the preceding
cycle completes and the polling interval elapses; missed cadence never queues catch-up cycles.

## Excluded data and non-goals

Excluded from telemetry and structured logs:

- connection strings and credentials
- SQL text and parameters
- roles, tenants, schemas, tables, indexes, and object definitions
- PostgreSQL error text
- per-object assessment evidence

Object names remain in-memory advisory evidence only.

Non-goals:

- automatic index creation, removal, rebuild, or modification
- index-removal advice until durable per-object observation snapshots establish a safe window
- query-plan analysis, query capture, SQL logging, or workload profiling
- replacing PostgreSQL administration, capacity monitoring, or human performance review
- cross-database aggregation or tenant-level telemetry

## Focused test plan

- Unit-test seven-day, 10,000-row, protected-index, unsupported-shape, zero-scan, and high
  sequential-scan policy outcomes.
- Integration-test collection against PostgreSQL with a role granted only `pg_read_all_stats`.
- Verify collection returns `Collected`, exports only allowed telemetry tags, and does not alter index
  definitions.
- Verify the monitoring role cannot create database objects.
- Verify the reusable host registers one collector for both dedicated database monitoring connections
  and rejects duplicate registration.
