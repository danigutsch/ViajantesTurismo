# ADR-034: Bounded Context Databases and Outbox Ownership

## Status

Accepted.

Extended for co-hosted Branding by
[ADR-040](20260724-branding-module-co-hosting-and-state-distribution.md).

## Context

Admin and Catalog are separate bounded contexts. Both need durable integration messaging. A shared
outbox database was considered, but PostgreSQL transactions are scoped to one database connection,
and one connection targets one database. A separate messaging database would require distributed
transaction coordination or a dual-write between business data and messaging data.

Transactional outbox guidance requires the outbox row to be committed in the same transaction as the
business data that caused the event.

## Decision

- Admin and Catalog use separate PostgreSQL databases on the same PostgreSQL server.
- Admin and Catalog currently use the SharedKernel default `messaging` schema/table mappings inside
  their respective databases; the same default name does not imply one physical shared table.
- Outbox/inbox ownership is never shared between bounded contexts. A default transport table is an
  explicit producer-database handoff queue partitioned by `ConsumerName`, not either consumer's inbox
  or source-of-truth state.
- A co-hosted logical module that shares a physical database still owns a context-qualified outbox
  mapping/table. ADR-040 applies this rule to Branding in the `branding` schema rather than sharing
  Catalog's outbox.
- Transport is distinct from source outbox ownership. Admin-to-Catalog uses Admin-database
  `messaging.transport_messages`; ADR-040 assigns Branding-to-Admin envelopes to the Catalog
  database's default `messaging.transport_messages` table while Branding retains its qualified outbox.
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
