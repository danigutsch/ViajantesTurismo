# Observability Signal Catalog

This catalog lists every repository-owned custom signal, the question it answers, where it is used, and
whether it is kept. It is the source of truth for signal inventory;
[OPEN_TELEMETRY.md](OPEN_TELEMETRY.md) keeps the registration, privacy, and span/log contract rules.

## The value test

A signal is kept when it has a concrete use: a dashboard panel with a written description, an alert
with a runbook, or a documented diagnostic or correlation role. Alerts are optional. A signal with no
use is a removal candidate. Verdicts:

- `keep`: has a documented use.
- `idle-ok`: emitted by the library at no cost, but the system has no traffic for it yet;
  silence is expected.
- `remove`: has no use; delete it and call out the change for operators.
- `gap`: wanted by an operator question but not emitted or not yet verified; tracked by an issue.

## Meters

| Meter | Registration | Dashboard |
| --- | --- | --- |
| `SharedKernel.Mediator` | SharedKernel defaults, `SharedKernel.*` | `mediator` |
| `ViajantesTurismo.Catalog` | `AddViajantesTurismoTelemetry`, `ViajantesTurismo.*` | `catalog-pipeline`, `media-accessibility`, `operation-health` |
| `ViajantesTurismo.Admin.CustomerImport` | `AddViajantesTurismoTelemetry`, `ViajantesTurismo.*` | `operation-health` |
| `ViajantesTurismo.Admin.Documents` | `AddViajantesTurismoTelemetry`, `ViajantesTurismo.*` | `document-operations` |
| `ViajantesTurismo.Branding` | `AddViajantesTurismoTelemetry`, `ViajantesTurismo.*` | `branding-cache` |
| `PandaBear.AspNetCore` | SharedKernel defaults, `PandaBear.*` | `http-platform` |
| `PandaBear.AspNetCore.Authentication.Bff` | SharedKernel defaults, `PandaBear.*`, after approved BFF producer adoption | `operation-health` |
| `PandaBear.Blazor` | SharedKernel defaults, `PandaBear.*` | `http-platform` |
| `SharedKernel.EventSourcing.Npgsql` | SharedKernel defaults, `SharedKernel.*` | `event-store` |
| `SharedKernel.Observability.Npgsql` | `DatabaseObservabilityHostConfiguration` | `postgresql-observability` |
| `ViajantesTurismo.MigrationService` | `AddViajantesTurismoTelemetry`, `ViajantesTurismo.*` | `platform-smoke` |
| `Npgsql` | `AddViajantesTurismoTelemetry` | none yet; pool panels are planned |
| `SharedKernel.MalwareScanning` | SharedKernel defaults, `SharedKernel.*` | Prometheus scan diagnosis |
| `SharedKernel.Scheduling` | SharedKernel defaults, `SharedKernel.*` | Prometheus poller diagnosis |

Platform meters (ASP.NET Core, HttpClient, Kestrel, .NET runtime, AWS SDK) are consumed
through the `http-platform` and `runtime` dashboards. Collector self-telemetry (`otelcol_*`) is consumed
by `collector-gateway`.

## Metrics

Prometheus names follow the Collector exporter rules: dots become underscores, counters gain `_total`,
and `ms` or `s` units become `_milliseconds` or `_seconds`.

| Metric | Meter | Unit | Dimensions | Question answered | Used by | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| `admin.document.operation` | `ViajantesTurismo.Admin.Documents` | count | `document.operation`, `document.outcome` | Which document requests and retention batches succeed, reject, fail, or cancel? | `document-operations`, alert `document-operation-failures` | keep |
| `migration.database_initialization.duration` | `ViajantesTurismo.MigrationService` | `s` | `migration.outcome` (success, error, cancelled) | Did database initialization finish, and how long did it take? | `platform-smoke`, Recent database initializations and Recent database initialization duration, alert `migration-initialization-failed` | keep |
| `catalog.integration_event` | `ViajantesTurismo.Catalog` | `{event}` | `catalog.outcome`, event type | Are Admin events handled? | `catalog-pipeline`, alert `catalog-event-errors` | keep |
| `catalog.idempotency.operation` | `ViajantesTurismo.Catalog` | `{operation}` | idempotency outcome | How many deliveries are duplicates? | `catalog-pipeline` | keep |
| `catalog.tour.stream_update` | `ViajantesTurismo.Catalog` | `{operation}` | `catalog.outcome` | Are tour streams written? | `catalog-pipeline` | keep |
| `catalog.projection.event` | `ViajantesTurismo.Catalog` | `{event}` | projection name | Are projections consuming events? | `catalog-pipeline` | keep |
| `catalog.projection.batch` | `ViajantesTurismo.Catalog` | `{batch}` | projection name, outcome | Do batches complete or fail? | `catalog-pipeline` | keep |
| `catalog.media_object.reconciliation.object` | `ViajantesTurismo.Catalog` | `{object}` | operation, outcome | Is orphan cleanup finding or failing? | `catalog-pipeline` | keep |
| `catalog.media_accessibility.generation.attempts` | `ViajantesTurismo.Catalog` | `{attempt}` | outcome, supported language | Are generations attempted? | `media-accessibility` | keep |
| `catalog.media_accessibility.generation.completed` | `ViajantesTurismo.Catalog` | `{attempt}` | outcome, supported language | Do generations finish? | `media-accessibility` | keep |
| `catalog.media_accessibility.generation.duration` | `ViajantesTurismo.Catalog` | `s` | outcome, supported language | Is generation slow? | `media-accessibility` | keep |
| `catalog.media_accessibility.evaluation.cases` | `ViajantesTurismo.Catalog` | `{case}` | `catalog.outcome` (pass, fail) | Is generated text passing evaluation? | `media-accessibility` | keep |
| `catalog.media_accessibility.publication.blocked` | `ViajantesTurismo.Catalog` | `{image}` | `catalog.outcome` (missing_alt, unreviewed) | What blocks publication? | `media-accessibility` | keep |
| `catalog.media_accessibility.review.decisions` | `ViajantesTurismo.Catalog` | `{decision}` | `catalog.outcome` (approved, edited, rejected, failed, missing_alt), supported language | Are editors working the queue? | `media-accessibility` | keep |
| `catalog.media_accessibility.review.queue_age` | `ViajantesTurismo.Catalog` | `s` | outcome, supported language | How long do items wait for review? | `media-accessibility` | keep |
| `branding.cache.requests` | `ViajantesTurismo.Branding` | `{request}` | outcome | Is the cache effective? | `branding-cache` | keep |
| `branding.cache.invalidations` | `ViajantesTurismo.Branding` | `{invalidation}` | outcome | Do invalidations succeed? | `branding-cache`, alert `branding-invalidation-failures` | keep |
| `branding.settings.source_revision` | `ViajantesTurismo.Branding` | revision | instance | Do instances converge on one revision? | `branding-cache` | keep |
| `unhandled_exceptions_total` | `PandaBear.AspNetCore` | `{exception}` | error type, route template, handled | Is a request failing with an unhandled exception? | `http-platform`, alert `unhandled-exceptions` | keep |
| `authentication_events_total` | `PandaBear.AspNetCore` | `{event}` | event, reason, scheme | Are sign-ins failing? | `http-platform` | keep |
| `rate_limit_rejections_total` | `PandaBear.AspNetCore` | `{rejection}` | policy | Are clients throttled? | `http-platform` | keep |
| `handled_ui_failures_total` | `PandaBear.Blazor` | `{failure}` | `error_type`, `operation` | Which UI operation shows an error? | `http-platform` | keep |
| `mediator.requests` | `SharedKernel.Mediator` | `{request}` | `mediator.request.name`, `mediator.outcome` | How often do dispatched requests return, cancel, or throw? Returned failures such as a booking 404 count as `success` for this metric. The Failed mediator results panel identifies them through `mediator.result`. | `mediator` | keep |
| `mediator.request.duration` | `SharedKernel.Mediator` | `ms` | `mediator.request.name`, `mediator.outcome` | Which request type is slow? | `mediator` | keep |
| `mediator.notifications` | `SharedKernel.Mediator` | `{notification}` | `mediator.notification.name`, `mediator.outcome` | Admin publishes no mediator notifications, so there is no fan-out outcome to monitor. | none | remove |
| `mediator.notification.duration` | `SharedKernel.Mediator` | `ms` | `mediator.notification.name`, `mediator.outcome` | Admin publishes no mediator notifications, so there is no fan-out latency decision. | none | remove |
| `mediator.streams` | `SharedKernel.Mediator` | `{request}` | `mediator.request.name`, `mediator.outcome` | Admin dispatches no mediator streams, so there is no stream outcome to monitor. | none | remove |
| `eventsourcing.postgresql.append.duration` | `SharedKernel.EventSourcing.Npgsql` | `ms` | schema, outcome | Is append slow? | `event-store` | keep |
| `eventsourcing.postgresql.load.duration` | `SharedKernel.EventSourcing.Npgsql` | `ms` | schema, operation, outcome | Is load slow? | `event-store` | keep |
| `eventsourcing.postgresql.checkpoint.duration` | `SharedKernel.EventSourcing.Npgsql` | `ms` | schema, operation, outcome | Is checkpointing slow? | `event-store` | keep |
| `eventsourcing.postgresql.event.appended` | `SharedKernel.EventSourcing.Npgsql` | `{event}` | schema | How much is written? | `event-store` | keep |
| `eventsourcing.postgresql.event.loaded` | `SharedKernel.EventSourcing.Npgsql` | `{event}` | schema, operation | How much is read? | `event-store` | keep |
| `eventsourcing.postgresql.append.conflict` | `SharedKernel.EventSourcing.Npgsql` | `{conflict}` | schema | Do writers collide? | `event-store` | keep |
| `malware_scan.requests` | `SharedKernel.MalwareScanning` | `{scan}` | engine, transport, mode, outcome, error type | Are scans rejecting content or failing to run? | Prometheus, filter `malware_scan_outcome` | keep |
| `malware_scan.duration` | `SharedKernel.MalwareScanning` | `s` | engine, transport, mode, outcome, error type | Is the scanner delaying uploads? | Prometheus scan latency diagnosis | keep |
| `scheduling.polling.cycle.duration` | `SharedKernel.Scheduling` | `s` | service, outcome | Which background poller is slow or failing? | Prometheus, group by `scheduling_service_name` | keep |
| `scheduling.polling.batch` | `SharedKernel.Scheduling` | `{batch}` | service | Is the poller completing batches? | Prometheus poller diagnosis | keep |
| `scheduling.polling.item` | `SharedKernel.Scheduling` | `{item}` | service | Is the poller making progress? | Prometheus poller diagnosis | keep |
| `scheduling.polling.failure` | `SharedKernel.Scheduling` | `{failure}` | service, error type | Is the poller repeatedly failing with the same error? | Prometheus poller diagnosis | keep |
| `postgresql.index_health.collections` | `SharedKernel.Observability.Npgsql` | `{collection}` | outcome | Does index-health collection work? | `postgresql-observability` | keep |
| `postgresql.index_health.assessments` | `SharedKernel.Observability.Npgsql` | `{assessment}` | action, reason | Is any index worth human review? | `postgresql-observability` | keep |
| `admin.customer_import.attempts` | `ViajantesTurismo.Admin.CustomerImport` | `{attempt}` | `import.operation`, `import.outcome`, `import.size_bucket` | Are import workflows succeeding? | `operation-health`, alert `customer-import-failures` | keep |
| `admin.customer_import.duration` | `ViajantesTurismo.Admin.CustomerImport` | `s` | `import.operation`, `import.outcome`, `import.size_bucket` | Are imports slow for a particular size bucket? | `operation-health`, alert `customer-import-latency` | keep |
| `catalog.media_upload.attempts` | `ViajantesTurismo.Catalog` | `{attempt}` | `upload.outcome` | Are media uploads succeeding or being rejected? | `operation-health`, alert `catalog-media-upload-failures` | keep |
| `catalog.media_upload.stage.duration` | `ViajantesTurismo.Catalog` | `s` | `upload.stage` | Is scanning or writing originals slow? | `operation-health`, alert `catalog-media-upload-latency` | keep |
| `bff.session_ticket.failures` | `PandaBear.AspNetCore.Authentication.Bff` | `{failure}` | `operation` | After producer adoption, are protection or cache errors disrupting sessions? | `operation-health`, alert `management-session-ticket-failures` | keep |

## Activity sources

| ActivitySource | Owner | Verdict |
| --- | --- | --- |
| `ViajantesTurismo.Admin.Contracts.Clients` | Admin HTTP contracts | keep |
| `ViajantesTurismo.Admin.Documents` | Admin document workflows | keep |
| `ViajantesTurismo.Catalog` | Catalog application | keep |
| `ViajantesTurismo.MigrationService.DatabaseInitializationWorker` | Migration service | keep |
| `SharedKernel.Mediator` | SharedKernel Mediator | keep |
| `SharedKernel.EventSourcing.Npgsql` | SharedKernel PostgreSQL event store | keep |
| `SharedKernel.HttpClients.ContractOutcomes` | SharedKernel typed HTTP client outcomes | keep |
| `SharedKernel.MalwareScanning` | SharedKernel ClamAV scanner | keep |
| `SharedKernel.ObjectStorage.S3` | SharedKernel S3 object store | keep |
| `SharedKernel.Scheduling` | SharedKernel polling services | keep |

## Spans

The trace backend reports elapsed duration for each span. Numeric counts and positions are span
attributes for diagnosis. Keep metric labels bounded independently of those trace attributes.

| Span | Source | Attributes | Question answered | Used by | Verdict |
| --- | --- | --- | --- | --- | --- |
| `admin.document.operation` | `ViajantesTurismo.Admin.Documents` | operation, outcome, server-generated correlation | Which dependency contributed to a failed document request or retention batch? | Tempo search by `document.correlation` | keep |
| `mediator.send` | `SharedKernel.Mediator` | request type, handler, pipeline depth, outcome | Which command dispatch is slow or throws? | `traces`, Mediator spans | keep |
| `mediator.stream` | `SharedKernel.Mediator` | request type, outcome | Admin has no mediator stream dispatch to diagnose. | none | remove |
| `mediator.publish` | `SharedKernel.Mediator` | notification type, handler count, outcome | Admin has no mediator notification fan-out to diagnose. | none | remove |
| `mediator.notification.handle` | `SharedKernel.Mediator` | notification type, handler, outcome | Admin has no mediator notification handler failure to diagnose. | none | remove |
| Request type (for example `CompleteBookingCommand`) | `SharedKernel.Mediator` pipeline with Admin `ResultTagBehavior` | request and response types, runtime outcome, `mediator.result` | Which request failed a domain rule, returned not found, or threw? | `traces`, Mediator spans and Failed mediator results | keep |
| `eventsourcing.postgresql.append` | `SharedKernel.EventSourcing.Npgsql` | schema, operation, event count, expected revision mode and value, actual revision, outcome, error type | Did the append write events or hit a revision conflict? | `traces`, Event store spans | keep |
| `eventsourcing.postgresql.load` | `SharedKernel.EventSourcing.Npgsql` | schema, operation, event count, outcome, error type | Was reading the stream slow or unsuccessful? | `traces`, Event store spans | keep |
| `eventsourcing.postgresql.checkpoint` | `SharedKernel.EventSourcing.Npgsql` | schema, operation, projection, checkpoint position, outcome, error type | Is this projection checkpoint advancing? | `traces`, Event store spans | keep |
| `catalog.integration_event.handle` | `ViajantesTurismo.Catalog` | bounded context, integration event type and version, idempotency and operation outcomes, error type | Was the delivery retried, skipped or applied? | `traces`, Catalog pipeline spans | keep |
| `catalog.tour.stream_update` | `ViajantesTurismo.Catalog` | bounded context, integration event type and version, event count, outcome, error type | Did the Admin delivery update the tour stream? | `traces`, Catalog pipeline spans | keep |
| `catalog.projection.process` | `ViajantesTurismo.Catalog` | projection, checkpoint position, event count, outcome, error type | Is a Catalog projection advancing? | `traces`, Catalog pipeline spans | keep |
| `catalog.media_accessibility.generate` | `ViajantesTurismo.Catalog` | configured provider and requested model, adapter/prompt/configuration versions, reported model placeholder, outcome | Did drafting fail after a model or configuration change? | `traces`, Catalog pipeline spans | keep |
| `DatabaseInitialization` | `ViajantesTurismo.MigrationService.DatabaseInitializationWorker` | operation type, worker type, error type | Did migration and local data initialization complete? | Aspire startup diagnosis and `traces`, Recent error traces | keep |
| `customers.create` | `ViajantesTurismo.Admin.Contracts.Clients` | Admin API area, operation, HTTP status, command outcome kind | Did the customer command fail validation, conflict, or get an unexpected response? | Tempo search by scope and command outcome | keep |
| `tours.create` | `ViajantesTurismo.Admin.Contracts.Clients` | Admin API area, operation, HTTP status, command outcome kind | How did the tour create command end? | Tempo search by scope and command outcome | keep |
| `bookings.create` | `ViajantesTurismo.Admin.Contracts.Clients` | Admin API area, operation, HTTP status, command outcome kind | How did the booking create command end? | Tempo search by scope and command outcome | keep |
| `http.client.outcome` | `SharedKernel.HttpClients.ContractOutcomes` | HTTP method, status, generic outcome kind | Did transport, decoding, or outcome mapping fail for a typed HTTP client? | Tempo search by `span.http.outcome.kind` | keep |
| `malware_scan.clamav.scan` | `SharedKernel.MalwareScanning` | engine, transport, mode, outcome, error type | Did scanning reject the upload or fail? | Tempo search by operation and outcome | keep |
| `object_storage.put` | `SharedKernel.ObjectStorage.S3` | operation | Did storing an object delay the request? | Tempo object-store diagnosis | keep |
| `object_storage.open_read` | `SharedKernel.ObjectStorage.S3` | operation | Did opening a stored object delay the request? | Tempo object-store diagnosis | keep |
| `object_storage.exists` | `SharedKernel.ObjectStorage.S3` | operation | How long did the existence check take? | Tempo object-store diagnosis | keep |
| `object_storage.list` | `SharedKernel.ObjectStorage.S3` | operation | Did listing objects delay reconciliation? | Tempo object-store diagnosis | keep |
| `object_storage.create_upload_url` | `SharedKernel.ObjectStorage.S3` | operation | How long did issuing an upload URL take? | Tempo object-store diagnosis when presigned uploads run | idle-ok |
| `object_storage.delete` | `SharedKernel.ObjectStorage.S3` | operation | Did deleting an object delay cleanup? | Tempo object-store diagnosis | keep |
| `object_storage.health` | `SharedKernel.ObjectStorage.S3` | operation | Is the object-store health check slow? | Tempo object-store diagnosis | keep |
| `poll` | `SharedKernel.Scheduling` | service, outcome, item count, error type | Did a projection, reconciliation, or retention poller finish its drain cycle? | Tempo search by `span.scheduling.service.name` | keep |

Use the Admin client spans to distinguish the command and its typed result from the generic
`http.client.outcome` spans in the same trace. For example, search Tempo with
`{ instrumentation:name = "ViajantesTurismo.Admin.Contracts.Clients" && span.viajantes.admin_command.outcome = "UnexpectedStatus" }`.
These spans cover expected business outcomes too. The client warning events below cover unexpected
outcomes. Neither diagnostic carries request bodies, entity identifiers, or response contents.

## Log events

Explicit EventIds are scoped by declaring owner. The unit is one log record at the listed level.
Operational panels use severity and application resource; the owner and event ID identify the event.

| Owner/EventId | Event (level) | Fields | Question answered | Used by | Verdict |
| --- | --- | --- | --- | --- | --- |
| `PandaBear.AspNetCore/4001` | Unhandled exception (error) | exception type, route template, status, trace correlation | Which request template threw? | `logs`, Recent warnings and errors | keep |
| `PandaBear.AspNetCore/4101` | Authentication event (warning) | event, reason, scheme, trace correlation | Why was authentication refused? | `logs`, Recent warnings and errors | keep |
| `PandaBear.AspNetCore/4102` | Rate-limit rejection (warning) | policy, trace correlation | Which policy throttled requests? | `logs`, Recent warnings and errors | keep |
| `PandaBear.Blazor/7001` | Handled UI failure (error) | operation, exception type, trace correlation | Which UI operation failed? | `logs`, Recent warnings and errors | keep |
| `ApplicationVersionLogger/1` | Application version (information) | application, version | Which version of a worker or host produced the surrounding signals? | `logs`, Recent logs | keep |
| `PermanentRedirectExtensions/1` | Redirect target rejected (warning) | none | Did a redirect rule return a target outside the permitted local path form? | `logs`, Recent warnings and errors | keep |
| `PollingBackgroundService/1` | Polling work failed (error) | service, error type | Which background service will retry work? | `logs`, Recent warnings and errors | keep |
| `ResultTransactionBehavior/1` | Transaction hook failed (error) | command and hook types, hook kind, error type | Did a follow-up step fail after transaction completion? | `logs`, Recent warnings and errors | keep |
| `PostgreSqlIntegrationEventTransportConsumer/1` | Delivery retry scheduled (warning) | consumer, transport message ID, attempt | Which transport delivery failed and will retry? | `logs`, Recent warnings and errors | keep |
| `PostgreSqlIntegrationEventTransportConsumer/2` | Delivery poisoned (error) | consumer, transport message ID, attempt | Which delivery exhausted its retries? | `logs`, Recent warnings and errors | keep |
| `BookingsApiClient/1` | Unexpected create outcome (warning) | status, outcome kind | Did booking creation return an unexpected outcome? | `logs`, Recent warnings and errors | keep |
| `CustomersApiClient/1` | Unexpected create outcome (warning) | status, outcome kind | Did customer creation return an unexpected outcome? | `logs`, Recent warnings and errors | keep |
| `ToursApiClient/1` | Unexpected create outcome (warning) | status, outcome kind | Did tour creation return an unexpected outcome? | `logs`, Recent warnings and errors | keep |
| `CustomerImportLog/1` | Import refused after scanner failure (warning) | error type | Did scanner failure prevent an import? | `logs`, Recent warnings and errors | keep |
| `BrandingCacheLog/1` | Public cache invalidated (information) | cache area | Did the Branding write invalidate its cache? | `logs`, Recent logs | keep |
| `BrandingCacheLog/2` | Public cache invalidation failed (warning) | cache area, error type | Why is a Branding value still cached? | `logs`, Recent warnings and errors | keep |
| `CatalogCacheLog/1` | Public cache invalidated (information) | cache area | Did the Catalog write invalidate its cache? | `logs`, Recent logs | keep |
| `CatalogCacheLog/2` | Tour projection pending (warning) | error type | Did a committed Catalog write fall behind its projection? | `logs`, Recent warnings and errors | keep |
| `CatalogCacheLog/3` | Public cache invalidation failed (warning) | cache area, error type | Which Catalog cache area may be stale? | `logs`, Recent warnings and errors | keep |
| `DatabaseInitializationWorkerLogger/1` | Initialization started (information) | none | Did the migration worker start? | Aspire startup logs | keep |
| `DatabaseInitializationWorkerLogger/2` | Initialization completed (information) | none | Did the migration worker finish? | Aspire startup logs | keep |
| `DatabaseInitializationWorkerLogger/3` | Initialization failed (error) | error type | What kind of failure prevented initialization? | Aspire startup logs | keep |
| `DatabaseInitializationWorkerLogger/4` | Initialization cancelled (information) | none | Did initialization stop on cancellation? | Aspire startup logs | keep |
| `DocumentRetentionLog/1` | Held documents skipped (information) | retention service, count held | Is an active disposition hold preventing expiry? | `logs`, Recent logs | keep |
| `DocumentRetentionLog/2` | Unattributed entries kept (warning) | retention service, count unattributed | Is document lineage missing for expired entries? | `logs`, Recent warnings and errors | keep |
| `DocumentOperationTelemetry/10` | Operation completed (information or error) | operation, outcome, server-generated correlation | Which trace and audit entry explain a failed document operation? | `logs`, Recent logs | keep |
| `MediaObjectReconciliationService/1` | Orphan deletion failed (warning) | error type | Is a storage failure preventing orphan cleanup? | `logs`, Recent warnings and errors | keep |

Retention events expose service names and aggregate counts. Document lineage and audit data stay
in their application stores.

## Removal candidates

Admin dispatches requests through the mediator. The pinned
`PandaBear.SharedKernel.Mediator` package still defines the unused stream and notification
signals. The notification panel has been removed. Reassess stream and notification dispatch
when adopting a new mediator candidate, then remove any definitions still unused in a
SharedKernel follow-up coordinated with its other consumers.

## Customer import measurements

An attempt starts after ASP.NET Core has bound the multipart form on the Admin API. Authentication,
rate limiting, malformed requests, and body-read time continue to use the HTTP and upload transport
signals. The counter is recorded once per terminal attempt and the histogram measures the same work.

- `import.operation` identifies the `import` or `commit` route.
- `import.outcome` is `completed` when processing returns without a full rejection, including
  partial row success. It is `rejected` for validation, scan rejection, or a result with no success
  and conflicts or row errors. Dependency and unhandled workflow errors are `failed`.
  Cooperative request cancellation is `canceled`.
- `import.size_bucket` is `unknown`, `up_to_64_kib`, `up_to_256_kib`, `up_to_1_mib`, or
  `over_limit`. The server's multipart file length is classified before export. Raw byte sizes,
  customer identifiers, content, file names, and exception messages are excluded.

The [Operation Health dashboard](../observability/grafana/dashboards/operation-health.json) shows
attempt rates by route and outcome and p95 processing time by size bucket. Failures and sustained
latency have separate [operator runbooks](OBSERVABILITY_RUNBOOKS.md#customer-import-failures).

## Media intake measurements

The Catalog intake records one result per completed attempt. `upload.outcome` is one of
`accepted`, `invalid_file`, `scan_rejected`, `scanner_unavailable`, `gallery_conflict`, or
`storage_failed`. A request that cancels intake does not count as a rejection. A stage that
starts records its elapsed seconds with just `upload.stage` set to `scan` or `store`.
The scan measures the scanner call and the store measures the original object write.
File names, media IDs, object keys, text, and exception messages are excluded.

The [Operation Health dashboard](../observability/grafana/dashboards/operation-health.json)
shows intake outcomes and p95 scan and store times. Alerts cover
[dependency failures](OBSERVABILITY_RUNBOOKS.md#catalog-media-upload-failures) and
[sustained stage latency](OBSERVABILITY_RUNBOOKS.md#catalog-media-upload-latency).

## Session ticket measurements

The Management Web ticket store emits this counter after ViajantesTurismo adopts the
separately approved SharedKernel BFF producer. Until adoption, the provisioned panel
permits an empty result and Grafana treats the absent alert series as OK.

Once active, the counter uses one dimension. `operation` is `protect`, `unprotect`,
`cache_get`, `cache_set`, or `cache_remove`. Ordinary misses, valid expiration, and
cooperative cancellation do not increment the counter. Ticket keys, claims, encrypted
payloads, and exception messages are excluded.

The [Operation Health dashboard](../observability/grafana/dashboards/operation-health.json)
shows failures by operation. The [session ticket runbook](OBSERVABILITY_RUNBOOKS.md#management-session-ticket-failures)
distinguishes key-ring and cache checks.

## Drift guard

`ObservabilityAssetsTests` in `tests/ViajantesTurismo.ArchitectureTests` fails when the meters in the Meters table differ
from the registered names and patterns, when an ActivitySource or a stable application log event lacks
a catalog key, when a signal row lacks a verdict, or when a dashboard or alert uses a repository metric
the catalog does not list. The checks read explicit registrations in ServiceDefaults and
DatabaseObservability, plus the pinned SharedKernel registration patterns. They run in CI:

```bash
dotnet test --project tests/ViajantesTurismo.ArchitectureTests -- --filter-class "*ObservabilityAssetsTests"
```
