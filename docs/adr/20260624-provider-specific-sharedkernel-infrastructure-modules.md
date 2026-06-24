# ADR-027: Provider-Specific SharedKernel Infrastructure Modules

## Context

SharedKernel abstractions are intended to be reusable across bounded contexts, but concrete
infrastructure implementations need external packages and provider-specific operational behavior.
Examples include PostgreSQL event stores, Redis idempotency stores, CloudEvents adapters, and future
inbox/outbox persistence.

Without a consistent boundary, provider-specific code can drift into bounded-context infrastructure
projects or into provider-neutral SharedKernel abstraction projects.

## Decision

Provider-neutral abstractions and provider-specific implementations must live in separate modules.

Use this naming pattern:

- `SharedKernel.<Capability>` for provider-neutral contracts and primitives.
- `SharedKernel.<Capability>.<Provider>` for reusable provider implementations.
- `<BoundedContext>.Infrastructure` for bounded-context composition, schema ownership, migrations,
  and read models.

Current examples:

- `SharedKernel.EventSourcing` owns `IEventStore`, `IProjectionCheckpointStore`, stream identifiers,
  expected revisions, and event envelopes.
- `SharedKernel.EventSourcing.PostgreSQL` owns reusable PostgreSQL event-store and projection
  checkpoint implementations plus provider telemetry names.
- `SharedKernel.IntegrationEvents` owns typed integration-event contracts.
- `SharedKernel.IntegrationEvents.CloudEvents` owns CloudEvents mapping as an adapter.
- `SharedKernel.Idempotency` owns idempotency contracts and value types.

Provider modules may reference provider packages such as `Npgsql`. Provider-neutral modules must not.

Bounded-context infrastructure remains responsible for:

- Choosing provider modules during composition.
- Owning schema names and migrations for context-owned data.
- Owning read-model tables and projection rebuild workflows.
- Enforcing context-specific retention, indexing, and operational policies.

ServiceDefaults may register stable telemetry source and meter names for provider modules, but it
should avoid direct project references to provider modules unless a future ADR explicitly accepts that
dependency.

## Consequences

- Reusable provider code has one home and can be tested independently.
- Provider-neutral contracts stay small and dependency-light.
- Bounded contexts keep ownership of schemas, read models, and operational decisions.
- ServiceDefaults can opt into provider telemetry without coupling application defaults to every
  provider implementation package.
- New provider modules need explicit names and telemetry contracts before becoming shared
  infrastructure.

## Candidate Inventory

| Capability | Neutral Module | Provider Module | Status |
| --- | --- | --- | --- |
| Event sourcing | `SharedKernel.EventSourcing` | `SharedKernel.EventSourcing.PostgreSQL` | Implemented |
| Integration-event envelope mapping | `SharedKernel.IntegrationEvents` | `SharedKernel.IntegrationEvents.CloudEvents` | Implemented |
| Idempotency | `SharedKernel.Idempotency` | `SharedKernel.Idempotency.PostgreSQL` or `SharedKernel.Idempotency.Redis` | Candidate |
| Inbox/outbox | Future `SharedKernel.Messaging` or focused contracts | PostgreSQL-backed stores | Candidate |
| Caching | Future cache contracts only if reused | Redis-backed cache adapters | Candidate |

## Alternatives

- Put all provider code in bounded-context infrastructure projects. This keeps dependencies local but
  duplicates reusable infrastructure and makes cross-context behavior inconsistent.
- Put provider implementations in neutral SharedKernel projects. This simplifies discovery but leaks
  provider dependencies into contracts and makes future provider replacement harder.
- Add provider references directly to ServiceDefaults. This simplifies source/meter registration but
  makes every application depend on providers it may not use.

## Status

Accepted.

## Links

- [ADR Index](../ARCHITECTURE_DECISIONS.md)
- [ADR-024: CloudEvents as Integration Event Adapter](20260621-cloudevents-as-integration-event-adapter.md)
- [ADR-025: Event Source Catalog Tour Presentation](20260621-event-source-catalog-tour-presentation.md)
- [ADR-026: Domain Materialization and SharedKernel Persistence Boundaries](20260621-domain-materialization-and-sharedkernel-persistence-boundaries.md)
- [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
