# Multi-store consistency audit

Scope: source-controlled flows that touch more than one durable system, publish or consume
events, call external side effects, or run asynchronous processing.

## Summary

- Media upload and processing currently touch database rows and media object storage.
- Admin tour creation writes the Admin database and EF outbox rows in the same `SaveChanges` unit.
- Catalog integration handling writes the Catalog event store behind a durable idempotency wrapper, but
  broker transport is not wired in the current runtime.
- Catalog projection processing reads the event store, writes read models, then writes a checkpoint.
- Public content, public theme, Catalog media metadata API, Admin booking/customer/tour mutations,
  and HTTP API clients are single-store or caller-side flows in the current implementation.

## Inventory

### Media original upload intake

- Trigger: `MediaImageUploadIntake.Accept`.
- Code: `src/ViajantesTurismo.Catalog.Application/Media/MediaImageUploadIntake.cs`.
- Systems touched: malware scanner, media object storage, Catalog database metadata.
- Failure window: object stored, then metadata persistence fails.
- Existing mitigation before this audit: deterministic object key; cleanup only when domain metadata
  validation fails before persistence.
- Gap proven by test: `Accept_deletes_stored_object_when_metadata_persistence_fails` first failed
  because the original object remained after metadata persistence failure.
- Change made: cleanup now deletes the stored original object if metadata persistence throws.
- Recommendation: compensation now; add reconciliation later if storage moves to a remote provider
  where delete can fail independently.

### Media original processing

- Trigger: `MediaImageOriginalStoredIntegrationHandler.Handle`.
- Code: `src/ViajantesTurismo.Catalog.Application/Media/MediaImageOriginalStoredIntegrationHandler.cs`.
- Systems touched: media object storage and Catalog database metadata.
- Failure window: variants stored, then metadata update fails.
- Existing mitigation before this audit: deterministic variant keys; cleanup only when processed
  domain state was invalid before persistence.
- Gap proven by test: `Handle_deletes_stored_variants_when_metadata_persistence_fails` first failed
  because generated variants remained after metadata persistence failure.
- Change made: cleanup now deletes generated variants if metadata persistence throws.
- Recommendation: compensation now; reconciliation later for orphaned variants and stale metadata.

### Admin tour creation to Catalog event stream

- Trigger: `CreateTourCommandHandler.Handle`.
- Code: `src/ViajantesTurismo.Admin.Application/Tours/CreateTour/CreateTourCommandHandler.cs`.
- Systems touched: Admin write database and `messaging.outbox_messages`.
- Failure window: transport publication is not wired yet; committed outbox rows can accumulate until a
  publisher exists.
- Existing mitigation: Admin aggregate rows and outbox rows are persisted in the same `SaveChanges`
  transaction through the shared EF Core outbox provider.
- Remaining risk: outbox rows are durable but not yet published to Catalog.ApiService.
- Recommendation: add a background publisher/transport consumer; do not publish directly from
  `SavingChanges`.

#### SaveChanges failure semantics

EF Core applies all changes in one `SaveChanges` call in a transaction when the provider supports
transactions. If any change fails, the transaction rolls back and the database is left unmodified. If a
transaction is already active, EF Core creates a savepoint before saving and rolls back to it on failure.

The Admin write path therefore uses this rule:

- Dispatch domain events before the database write only to enqueue local outbox rows into the same
  `DbContext` transaction.
- Persist aggregate changes and outbox rows in the same `SaveChanges` call.
- Clear aggregate domain events only after `SavedChanges` confirms success.
- Do not clear domain events in `SaveChangesFailed`; discard the DbContext and rebuild state from the
  database before retrying. Retrying the same tracked unit of work can re-dispatch events while
  previously-added outbox entities are still tracked.
- Do not publish integration events directly from `SavingChanges`; external publication happens later
  from durable outbox rows.

Sources: EF Core default transaction behavior, savepoints, `ISaveChangesInterceptor.SaveChangesFailed`,
and connection-resiliency guidance on transaction commit ambiguity:
<https://learn.microsoft.com/ef/core/saving/transactions>,
<https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.isavechangesinterceptor.savechangesfailed>,
<https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#transaction-commit-failure-and-the-idempotency-issue>.

### Catalog Admin tour-created integration handler

- Trigger: `AdminTourCreatedIntegrationHandler.Handle`.
- Code: `src/ViajantesTurismo.Catalog.Application/Tours/AdminTourCreatedIntegrationHandler.cs`.
- Systems touched: inbound integration event and Catalog event store.
- Failure window: duplicate event delivery after a handler success or transient failure.
- Existing mitigation: `ExpectedStreamRevision.NoStream` prevents duplicate stream creation for the
  same Admin tour id.
- Remaining risk: duplicate delivery is modeled as a stream revision conflict, not as an explicit
  idempotent success. This is acceptable only while dispatch is in-process/test-like.
- Recommendation: route broker-delivered events through `IdempotentIntegrationHandler<TIntegrationEvent>`
  and the EF Core idempotency store.

### Catalog integration idempotency wrapper

- Trigger: `IdempotentIntegrationHandler<TIntegrationEvent>.Handle`.
- Code: `src/ViajantesTurismo.Catalog.Application/IntegrationEvents/IdempotentIntegrationHandler.cs`.
- Systems touched: idempotency store plus inner handler side effects.
- Failure window: inner handler succeeds, then `Complete` fails; retry can re-run side effects after
  the lock expires.
- Existing mitigation: lock duration and completion fingerprint.
- Current runtime status: `SharedKernel.EntityFrameworkCore` provides `EfIdempotencyStore<TContext>` and
  maps entries to `messaging.idempotency_keys`; no broker wiring consumes through this wrapper yet.
- Recommendation: keep handler side effects idempotent by stable keys plus safe conflict handling, then
  use the durable idempotency row as the delivery guard when real message ingress exists.

### Catalog event-store projection runner

- Trigger: `CatalogProjectionRunner.Project`.
- Code: `src/ViajantesTurismo.Catalog.Application/Projections/CatalogProjectionRunner.cs`.
- Systems touched: event store, projection read model store, projection checkpoint store.
- Failure window: read model write succeeds, then checkpoint write fails.
- Existing mitigation: checkpoint is written after applying events; retry replays the same events.
  Current read-model upsert uses event position, so repeated projection is intended to be safe.
- Recommendation: no change now. If projections gain non-idempotent side effects, add a projection
  inbox/checkpoint transaction or make each projection idempotent per event position.

### Catalog media metadata management API

- Trigger: `CatalogEndpoints.UpsertMediaImage`.
- Code: `src/ViajantesTurismo.Catalog.ApiService/CatalogEndpoints.cs`.
- Systems touched: Catalog read model lookup and media metadata database store.
- Failure window: none across durable systems; both operations are database reads/writes in Catalog.
- Existing mitigation: validates every tour link before saving metadata.
- Recommendation: no change.

### Public content and public theme management

- Trigger: `CatalogEndpoints.UpsertPublicContent`, `CatalogEndpoints.UpsertPublicTheme`.
- Code: `src/ViajantesTurismo.Catalog.ApiService/CatalogEndpoints.cs`.
- Systems touched: Catalog database only.
- Failure window: single durable store.
- Recommendation: no change.

### Admin customer, booking, tour, and import mutations

- Trigger: Admin application command handlers except tour-create integration dispatch.
- Code: `src/ViajantesTurismo.Admin.Application/**`.
- Systems touched: Admin write database only.
- Failure window: single durable store.
- Recommendation: no change for multi-store consistency.

### Migration service seeding

- Trigger: `SeederWorker.ExecuteAsync`.
- Code: `src/ViajantesTurismo.MigrationService/SeederWorker.cs`.
- Systems touched: Catalog migrations plus Admin seeding through `ISeeder`.
- Failure window: startup seeding can partially apply if later seed steps fail.
- Existing mitigation: intended local/startup migration workflow; EF migrations are idempotent.
- Recommendation: no production outbox/inbox. Keep seed methods idempotent and re-runnable.

### Frontend and contract HTTP API clients

- Trigger: web frontend calls through contract-owned typed clients.
- Code: `src/ViajantesTurismo.Management.Web/Program.cs`, `src/*Contracts/*ApiClient.cs`.
- Systems touched: caller process and remote HTTP API; no local durable write in the same flow.
- Failure window: caller-visible HTTP failure only.
- Recommendation: no multi-store pattern. Keep timeout/retry policy in shared HTTP client defaults.

## SharedKernel candidates

- Reusable compensation helper: added as `SharedKernel.BuildingBlocks.Compensation` because request-path
  compensation is a common host-agnostic workflow primitive and now has two media cleanup callers.
- Outbox/inbox primitives: implemented as small EF Core provider code in
  `SharedKernel.EntityFrameworkCore`; storage-neutral contracts remain in `SharedKernel.IntegrationEvents`
  and `SharedKernel.Idempotency`.
- Shared EF messaging tables use the `messaging` schema to group asynchronous delivery and idempotency
  infrastructure separately from domain tables.

## Follow-up implementation candidates

1. Add an outbox publisher and Catalog transport consumer for durable cross-service event delivery.
2. Add a media reconciliation job after object storage becomes remote or delete failures need durable
   repair outside the request path.
