# Asynchronous contracts

This page records the current asynchronous contract inventory, contract-documentation workflow, and
drift guard.

## Scope

The canonical contract artifact is [`asyncapi.json`](asyncapi.json). It covers current durable
integration events only. Domain events remain local to one bounded context and are documented as
implementation facts, not as transport contracts.

## Inventory

| Contract | Type | Producer | Consumer | Channel | Payload | Storage |
| --- | --- | --- | --- | --- | --- | --- |
| `admin.tour.created` v1 | Integration event | Admin | Catalog | `admin.tour.created` | `EventId`, `OccurredAt`, `AdminTourId`, `Identifier`, `Name` | Admin outbox, Catalog inbox/idempotency |
| `admin.tour.retired` v1 | Integration event | Admin | Catalog | `admin.tour.retired` | `EventId`, `OccurredAt`, `AdminTourId`, `RetirementId` | Admin outbox, Catalog retirement fence |
| `branding.settings.changed` v1 | Integration event | Branding | Admin | `branding.settings.changed` | Settings revision and optional logo fields | Branding outbox, Admin inbox/idempotency |
| `catalog.media-image.original-stored` v1 | Integration event | Catalog | Catalog | `catalog.media-image.original-stored` | `EventId`, `OccurredAt`, `MediaImageId`, `SourceObjectKey`, `ProcessingVersion` | Catalog outbox and in-process relay, Catalog inbox/idempotency |
| `catalog.tour.retirement-applied` v1 | Integration event | Catalog | Admin | `catalog.tour.retirement-applied` | `EventId`, `OccurredAt`, `AdminTourId`, `RetirementId` | Catalog outbox, Admin retirement status |

Current domain event to integration event mapping:

- `TourCreatedDomainEvent` maps to `AdminTourCreatedIntegrationEvent` through
  `TourIntegrationEventMappings.MapTourCreated`.
- `TourRetiredDomainEvent` maps to `AdminTourRetiredIntegrationEvent` through
  `TourIntegrationEventMappings.MapTourRetired`.

Current integration-event handlers:

- `AdminTourCreatedIntegrationHandler` consumes `admin.tour.created` for Catalog tour projection intake.
- `AdminTourRetiredIntegrationHandler` consumes `admin.tour.retired` to withdraw the Catalog tour and
  record the source fence.
- `BrandingSettingsChangedIntegrationHandler` consumes `branding.settings.changed` for the Admin Branding projection.
- `MediaImageOriginalStoredIntegrationHandler` consumes `catalog.media-image.original-stored` for Catalog
  public media processing.
- `CatalogTourRetirementAppliedIntegrationHandler` consumes `catalog.tour.retirement-applied` and
  records completion on the matching Admin tour retirement.

## Generation and drift checks

The repository CLI uses the same source-generated JSON metadata as the producer and consumer for each event.
Register each current durable event's serializer and topology in
[`AsyncApiDocument.cs`](../scripts/ViajantesTurismo.AsyncApi.Tool/AsyncApiDocument.cs).
Regenerate the artifact when that registration or its serializer changes.

```bash
dotnet run --project scripts/ViajantesTurismo.AsyncApi.Tool -- generate docs/asyncapi.json
dotnet run --project scripts/ViajantesTurismo.AsyncApi.Tool -- verify docs/asyncapi.json
```

The generated artifact carries `x-viajantes.provenance` with the source path and both commands.
Follow the [generated output metadata policy](DOCUMENTATION_GOVERNANCE.md#generated-output-metadata).

Local and CI validation path:

- `dotnet test --project tests/ViajantesTurismo.Admin.ContractTests/ViajantesTurismo.Admin.ContractTests.csproj`
  checks that the checked-in artifact exactly matches the generated document and that the registered
  serializer contexts are covered. CI runs the same tests.
- The CI Lint job validates documentation links (`sharedkernel-repo verify-links`) and Markdown
  (markdownlint) surrounding the artifact.

## Topology metadata

`docs/asyncapi.json` records the event topology in `x-viajantes`.

- producer context
- consumer contexts
- channel/topic name
- source contract type
- domain-event mapping when present
- outbox owner
- inbox/idempotency owner
- transport claimant for cross-context delivery
- handler or projection responsibility

Future diagram generation should prefer this metadata and then link generated event-flow diagrams back to
`docs/asyncapi.json`.

## Versioning

- Event type strings are stable names.
- `eventVersion` increments for breaking payload or semantic changes.
- Non-breaking additions must remain backward compatible for consumers.
- Retired event versions should stay documented until no persisted outbox or inbox messages can reference
  them.
