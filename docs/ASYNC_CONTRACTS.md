# Asynchronous contracts

This page records the current asynchronous contract inventory, contract-documentation workflow, and
drift guard for Epic #827.

## Scope

The canonical contract artifact is [`asyncapi.json`](asyncapi.json). It covers current durable
integration events only. Domain events remain local to one bounded context and are documented as
implementation facts, not as transport contracts.

## Inventory

| Contract | Type | Producer | Consumer | Channel | Payload | Storage |
| --- | --- | --- | --- | --- | --- | --- |
| `admin.tour.created` v1 | Integration event | Admin | Catalog | `admin.tour.created` | `eventId`, `occurredAt`, `adminTourId`, `identifier`, `name` | Admin outbox, Catalog inbox/idempotency |
| `catalog.media-image.original-stored` v1 | Integration event | Catalog | Catalog | `catalog.media-image.original-stored` | `eventId`, `occurredAt`, `mediaImageId`, `sourceObjectKey`, `processingVersion` | Catalog outbox, Catalog inbox/idempotency |

Current domain event to integration event mapping:

- `TourCreatedDomainEvent` maps to `AdminTourCreatedIntegrationEvent` through
  `TourIntegrationEventMappings.MapTourCreated`.

Current integration-event handlers:

- `AdminTourCreatedIntegrationHandler` consumes `admin.tour.created` for Catalog tour projection intake.
- `MediaImageOriginalStoredIntegrationHandler` consumes `catalog.media-image.original-stored` for Catalog
  public media processing.

## Tooling decision

Use a hand-authored AsyncAPI JSON artifact for now.

Rejected for this slice:

- Transient npm execution or global AsyncAPI CLI installs. This conflicts with the local tool security
  preference to avoid ad hoc npm tooling when repository-owned checks can cover the current need.
- A custom generator. Current event count is small, and a generator would add maintenance surface before
  metadata and diagram needs are stable.

Local and CI validation path:

- `dotnet test --project tests/ViajantesTurismo.Admin.ContractTests/ViajantesTurismo.Admin.ContractTests.csproj`
  checks that the artifact contains current event types, versions, payload fields, consumer names, and
  topology metadata.
- `bash scripts/lint-all.sh` validates documentation links and Markdown surrounding the artifact.

## Topology metadata

`docs/asyncapi.json` uses `x-viajantes` metadata to make event topology discoverable without adding a
new code abstraction:

- producer context
- consumer contexts
- channel/topic name
- source contract type
- domain-event mapping when present
- outbox owner
- inbox/idempotency owner
- handler or projection responsibility

Future diagram generation should prefer this metadata and then link generated event-flow diagrams back to
`docs/asyncapi.json`.

## Versioning

- Event type strings are stable names.
- `eventVersion` increments for breaking payload or semantic changes.
- Non-breaking additions must remain backward compatible for consumers.
- Retired event versions should stay documented until no persisted outbox or inbox messages can reference
  them.

## Gaps

No additional implementation issue is required for the current durable event set. Future event-flow diagram
automation can extend the same `x-viajantes` metadata instead of introducing a parallel registry.
