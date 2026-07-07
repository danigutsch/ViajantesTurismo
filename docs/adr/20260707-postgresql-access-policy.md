# ADR-035: PostgreSQL Access Policy

## Status

Accepted.

## Context

Catalog uses PostgreSQL for relational read models, public content, integration outbox records,
event streams, and projection checkpoints. EF Core fits tables owned by a `DbContext`, migrations,
LINQ query composition, and change tracking. Raw Npgsql fits append-only event streams,
checkpoints, hot SQL paths, batching, and provider-specific operations that should not pretend to
be relational aggregates.

Npgsql recommends one app-wide `NpgsqlDataSource` per connection string. The data source is
thread-safe and owns the connection pool. EF Core `DbContext` instances remain short-lived units of
work and are not thread-safe.

## Decision

- Application projects define persistence ports for use cases. Infrastructure projects provide
  provider adapters.
- Do not add a generic repository abstraction over EF Core or Npgsql.
- Use EF Core for schema-owned relational state, migrations, outbox records, and read models where
  query composition or change tracking is useful.
- Use raw Npgsql for event stores, projection checkpoints, hot-path SQL, and provider-specific
  append or claim operations.
- Share one `NpgsqlDataSource` per bounded-context database connection string between EF Core and
  raw Npgsql adapters when both are active in the same process.
- A migration service owns event-sourcing schema initialization instead of adding extra hosted
  initialization services.
- When both EF Core and raw Npgsql adapters implement the same Application port, keep them
  swappable through explicit DI composition and add contract tests that prove behavioral parity.

## Consequences

- PostgreSQL connection pools are not duplicated for Catalog EF Core and raw event-sourcing stores.
- `DbContext` lifetime and transaction behavior stay EF Core-specific.
- Event-sourcing and checkpoint operations can use provider-specific SQL without leaking Npgsql into
  Application or Domain projects.
- Future raw Npgsql replacements for EF Core stores require a real port, explicit composition, and
  parity tests before becoming interchangeable.

## Links

- [Architecture Decisions index](../ARCHITECTURE_DECISIONS.md)

## References

- <https://www.npgsql.org/doc/basic-usage.html>
- <https://www.npgsql.org/efcore/>
- <https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/>
- <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/>
