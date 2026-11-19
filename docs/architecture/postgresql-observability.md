# PostgreSQL observability

## Scope and sources

The repository provides opt-in, advisory PostgreSQL index-health collection through
`SharedKernel.Observability.Npgsql`. `ViajantesTurismo.DatabaseObservability` applies the reusable host
once to both production databases: `admin-database` and `catalog-database`.

Assumptions:

- Locally tested with PostgreSQL 18.4; compatibility with other PostgreSQL versions requires validation.
- a dedicated, least-privilege monitoring `NpgsqlDataSource`
- PostgreSQL cumulative statistics are reset-dependent and are not workload history

Sources:

- [PostgreSQL statistics views](https://www.postgresql.org/docs/16/monitoring-stats.html)
- [PostgreSQL predefined roles](https://www.postgresql.org/docs/16/predefined-roles.html)
- [Npgsql data sources](https://www.npgsql.org/doc/basic-usage.html#data-source)

## PostgreSQL server connection metrics

The optional local LGTM stack can measure connections across the AppHost-owned PostgreSQL
server. The supported server major versions are 17 and 18, verified with 17.12 and 18.4.
The local Collector owns the 30-second scrape, using a single read-only connection to the
`postgres` maintenance database. The connection timeout is five seconds and the statement
timeout is five seconds.

| OTel metric | Prometheus series | Unit and use |
| --- | --- | --- |
| `postgresql.server.connections.used` | `postgresql_server_connections_used` | Connections, current sum of `pg_stat_database.numbackends` across databases |
| `postgresql.server.connections.limit` | `postgresql_server_connections_limit` | Connections, configured `max_connections` |

The server connection total includes the monitoring connection. Use the two gauges together
to assess headroom. Reserved connection slots mean an ordinary client can exhaust available
slots before the configured limit. Npgsql client pools and index-health advisories keep
their separate signals and operator questions.
[PostgreSQL table of cumulative statistics](https://www.postgresql.org/docs/18/monitoring-stats.html#PG-STAT-DATABASE-VIEW)

The Collector uses `sqlquery/postgresql_server` from the pinned Contrib image. It reads only
the two aggregate values and strips all resource and data-point attributes before adding
the fixed service name `postgresql-server`. Query logging stays disabled. The narrower query
avoids the pinned PostgreSQL receiver's per-database table, index, lock, size and replication
queries. The SQL Query receiver and its PostgreSQL driver are already bundled in the image.
Review the receiver and its exported series against this contract when changing the image.
[Pinned SQL Query receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/v0.130.0/receiver/sqlqueryreceiver/README.md) ·
[Pinned PostgreSQL receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/v0.130.0/receiver/postgresqlreceiver/scraper.go)

Set `ASPIRE_ENABLE_OBSERVABILITY_STACK=1` and configure the AppHost settings below.
Use a dedicated login with `CONNECT` to `postgres` and read access to the `datid` and
`numbackends` columns of `pg_catalog.pg_stat_database`. Keep credentials in the AppHost
secret store. Grant only the privileges the query uses. The database owner controls
implicit privileges inherited through `PUBLIC` and provisions the account outside AppHost.
This configuration uses the local container network. [PostgreSQL predefined roles](https://www.postgresql.org/docs/18/predefined-roles.html)

```text
Aspire:Features:PostgreSqlServerMetrics=true
Parameters:postgresql-server-metrics-username=<dedicated login>
Parameters:postgresql-server-metrics-password=<dedicated password>
```

The feature defaults off. Startup and ongoing application OTLP use the existing gateway.
If PostgreSQL denies access or becomes unavailable, the Collector continues accepting
OTLP and records `Error scraping metrics` for `sqlquery/postgresql_server`. Check that
`otelcol_scraper_scraped_metric_points_total` with
`receiver="sqlquery/postgresql_server"` advances while monitoring is enabled. A failed
connection can leave the errored-point count at zero because the query produced no
points. Inspect successful scrape progress before interpreting recently cached data.

For the real boundary tests, run the repository verifier against a synthetic loopback
PostgreSQL fixture and the pinned native Collector binary. Give the probe three consecutive
free loopback ports beginning at the third argument.

```bash
bash observability/tests/VerifyPostgreSqlServerMetrics.sh "${collector_bin}" 55418 56411 healthy
```

Use `VT_POSTGRES_SERVER_METRICS_USER` and `VT_POSTGRES_SERVER_METRICS_PASSWORD` for the
fixture login. Run `healthy` with an authorized role, `denied` with a role barred from
the maintenance database, `unavailable` against an unbound port, and `disabled` to check
the gateway alone. The verifier checks that normal OTLP export works in each case.

## Index-health metrics

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
