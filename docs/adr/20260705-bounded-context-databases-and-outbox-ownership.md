# ADR-034: Bounded Context Databases and Outbox Ownership

## Status

Accepted.

## Context

Admin and Catalog are separate bounded contexts. Both need durable integration messaging. A shared
outbox database was considered, but PostgreSQL transactions are scoped to one database connection,
and one connection targets one database. A separate messaging database would require distributed
transaction coordination or a dual-write between business data and messaging data.

Transactional outbox guidance requires the outbox row to be committed in the same transaction as the
business data that caused the event.

## Decision

- Admin and Catalog use separate PostgreSQL databases on the same PostgreSQL server.
- Each bounded context owns its own `messaging` schema and outbox/inbox tables inside its database.
- Messaging tables are not shared between bounded contexts.
- A future broker or relay may be shared infrastructure, but it must not replace the owning
  bounded context outbox transaction.

## Consequences

- Admin can write Admin data and Admin outbox records atomically.
- Catalog can write Catalog data and Catalog outbox/inbox records atomically.
- The same table names can be used in each database without coupling migration ownership.
- Cross-context delivery remains asynchronous and idempotent.

## Links

- [Architecture Decisions index](../ARCHITECTURE_DECISIONS.md)

## References

- <https://microservices.io/patterns/data/transactional-outbox.html>
- <https://microservices.io/patterns/data/database-per-service.html>
- <https://learn.microsoft.com/en-us/dotnet/architecture/microservices/architect-microservice-container-applications/data-sovereignty-per-microservice>
