# Observability Signal Catalog

This catalog lists every repository-owned custom signal, the question it answers, where it is used, and
whether it is kept. It is the source of truth for signal inventory;
[OPEN_TELEMETRY.md](OPEN_TELEMETRY.md) keeps the registration, privacy, and span/log contract rules.

## The value test

A signal is kept when it has a concrete use: a dashboard panel with a written description, an alert
with a runbook, or a documented diagnostic or correlation role. Alerts are optional. A signal with no
use is a removal candidate. Verdicts:

- `keep`: has a documented use.
- `remove`: has no use; delete it and call out the change for operators.
- `gap`: wanted by an operator question but not emitted or not yet verified; tracked by an issue.

## Meters

| Meter | Registration | Dashboard |
| --- | --- | --- |
| `SharedKernel.Mediator` | `AddSharedKernelMediatorMetrics` | none yet, added with the first production dispatch |
| `ViajantesTurismo.Catalog` | `AddCatalogMetrics` | `catalog-pipeline`, `media-accessibility` |
| `ViajantesTurismo.Branding` | `AddBrandingMetrics` | `branding-cache` |
| `PandaBear.AspNetCore` | `AddSharedKernelAspNetCoreMetrics` | `http-platform` |
| `PandaBear.Blazor` | `AddSharedKernelBlazorMetrics` | `http-platform` |
| `SharedKernel.EventSourcing.Npgsql` | `AddSharedKernelProviderMetrics` | `event-store` |
| `SharedKernel.Observability.Npgsql` | `DatabaseObservabilityHostConfiguration` | `postgresql-observability` |

Platform meters (ASP.NET Core, HttpClient, Kestrel, .NET runtime, Npgsql, AWS SDK) are consumed
through the `http-platform` and `runtime` dashboards. Collector self-telemetry (`otelcol_*`) is consumed
by `collector-gateway`.

## Metrics

Prometheus names follow the Collector exporter rules: dots become underscores, counters gain `_total`,
and `ms` or `s` units become `_milliseconds` or `_seconds`.

| Metric | Meter | Unit | Dimensions | Question answered | Used by | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| `catalog.integration_event` | `ViajantesTurismo.Catalog` | `{event}` | `catalog.outcome`, event type | Are Admin events handled? | `catalog-pipeline`, alert `catalog-event-errors` | keep |
| `catalog.idempotency.operation` | `ViajantesTurismo.Catalog` | `{operation}` | idempotency outcome | How many deliveries are duplicates? | `catalog-pipeline` | keep |
| `catalog.tour.stream_update` | `ViajantesTurismo.Catalog` | `{operation}` | `catalog.outcome` | Are tour streams written? | `catalog-pipeline` | keep |
| `catalog.projection.event` | `ViajantesTurismo.Catalog` | `{event}` | projection name | Are projections consuming events? | `catalog-pipeline` | keep |
| `catalog.projection.batch` | `ViajantesTurismo.Catalog` | `{batch}` | projection name, outcome | Do batches complete or fail? | `catalog-pipeline` | keep |
| `catalog.media_object.reconciliation.object` | `ViajantesTurismo.Catalog` | `{object}` | operation, outcome | Is orphan cleanup finding or failing? | `catalog-pipeline` | keep |
| `catalog.media_accessibility.generation.attempts` | `ViajantesTurismo.Catalog` | `{attempt}` | bounded | Are generations attempted? | `media-accessibility` | keep |
| `catalog.media_accessibility.generation.completed` | `ViajantesTurismo.Catalog` | `{attempt}` | bounded | Do generations finish? | `media-accessibility` | keep |
| `catalog.media_accessibility.generation.duration` | `ViajantesTurismo.Catalog` | `s` | bounded | Is generation slow? | `media-accessibility` | keep |
| `catalog.media_accessibility.evaluation.cases` | `ViajantesTurismo.Catalog` | `{case}` | `catalog.outcome` (pass, fail) | Is generated text passing evaluation? | `media-accessibility` | keep |
| `catalog.media_accessibility.publication.blocked` | `ViajantesTurismo.Catalog` | `{image}` | `catalog.outcome` (missing_alt, unreviewed) | What blocks publication? | `media-accessibility` | keep |
| `catalog.media_accessibility.review.decisions` | `ViajantesTurismo.Catalog` | `{decision}` | `catalog.outcome` (approved, edited, rejected, failed, missing_alt) | Are editors working the queue? | `media-accessibility` | keep |
| `catalog.media_accessibility.review.queue_age` | `ViajantesTurismo.Catalog` | `s` | none | How long do items wait for review? | `media-accessibility` | keep |
| `branding.cache.requests` | `ViajantesTurismo.Branding` | `{request}` | outcome | Is the cache effective? | `branding-cache` | keep |
| `branding.cache.invalidations` | `ViajantesTurismo.Branding` | `{invalidation}` | outcome | Do invalidations succeed? | `branding-cache`, alert `branding-invalidation-failures` | keep |
| `branding.settings.source_revision` | `ViajantesTurismo.Branding` | revision | instance | Do instances converge on one revision? | `branding-cache` | keep |
| `unhandled_exceptions_total` | `PandaBear.AspNetCore` | `{exception}` | error type | Is a request failing with an unhandled exception? | `http-platform`, alert `unhandled-exceptions` | keep |
| `authentication_events_total` | `PandaBear.AspNetCore` | `{event}` | outcome | Are sign-ins failing? | `http-platform` | keep |
| `rate_limit_rejections_total` | `PandaBear.AspNetCore` | `{rejection}` | bounded | Are clients throttled? | `http-platform` | keep |
| `handled_ui_failures_total` | `PandaBear.Blazor` | `{failure}` | `error_type`, `operation` | Which UI operation shows an error? | `http-platform` | keep |
| `eventsourcing.postgresql.append.duration` | `SharedKernel.EventSourcing.Npgsql` | `ms` | outcome | Is append slow? | `event-store` | keep |
| `eventsourcing.postgresql.load.duration` | `SharedKernel.EventSourcing.Npgsql` | `ms` | outcome | Is load slow? | `event-store` | keep |
| `eventsourcing.postgresql.checkpoint.duration` | `SharedKernel.EventSourcing.Npgsql` | `ms` | outcome | Is checkpointing slow? | `event-store` | keep |
| `eventsourcing.postgresql.event.appended` | `SharedKernel.EventSourcing.Npgsql` | `{event}` | schema | How much is written? | `event-store` | keep |
| `eventsourcing.postgresql.event.loaded` | `SharedKernel.EventSourcing.Npgsql` | `{event}` | schema | How much is read? | `event-store` | keep |
| `eventsourcing.postgresql.conflict` | `SharedKernel.EventSourcing.Npgsql` | `{conflict}` | schema | Do writers collide? | `event-store` | keep |
| `postgresql.index_health.collections` | `SharedKernel.Observability.Npgsql` | `{collection}` | outcome | Does index-health collection work? | `postgresql-observability` | keep |
| `postgresql.index_health.assessments` | `SharedKernel.Observability.Npgsql` | `{assessment}` | action, reason | Is any index worth human review? | `postgresql-observability` | keep |
| `AdminContractsClientTelemetry` outcomes | Admin contracts client | n/a | outcome | Which inter-service client calls fail? | none yet | gap |

## Pending dispatch

`SharedKernel.Mediator` (meter and `ActivitySource`, `mediator.*` metrics and spans) stays registered in
`ServiceDefaults`. No production code dispatches through it yet, so the live stack shows no `mediator_*`
series and no `mediator.*` spans. Admin commands and queries are being moved onto the mediator; the
`mediator` dashboard and the per-metric rows land with the first production dispatch. Verdict: `gap`.

## Spans

| Span | Source | Use | Verdict |
| --- | --- | --- | --- |
| `eventsourcing.postgresql.append`, `.load`, `.checkpoint` | `SharedKernel.EventSourcing.Npgsql` | `traces` search | keep |
| `DatabaseInitialization` | `ViajantesTurismo.MigrationService.DatabaseInitializationWorker` | startup diagnosis in Aspire; no metric yet | gap |

## Log events

| Event | Source | Use | Verdict |
| --- | --- | --- | --- |
| `EventId` 4001 (unhandled exception) | `PandaBear.AspNetCore` | `logs` warnings and errors | keep |
| `EventId` 4101 (authentication event) | `PandaBear.AspNetCore` | `logs` warnings and errors | keep |
| `EventId` 4102 (rate-limit rejection) | `PandaBear.AspNetCore` | `logs` warnings and errors | keep |
| `EventId` 7001 (handled UI failure) | `PandaBear.Blazor` | `logs` warnings and errors | keep |

## Drift guard

`ObservabilityAssetsTests` in `tests/ViajantesTurismo.ArchitectureTests` fails when the meters in the Meters table differ
from the `AddMeter` calls in `ServiceDefaults` (plus the meters registered by host configuration), when a
catalog row lacks a verdict, or when a dashboard or alert uses a repository metric the catalog does not
list. It also checks the dashboard and alert contracts. It
runs with the architecture tests in CI:

```bash
dotnet test --project tests/ViajantesTurismo.ArchitectureTests -- --filter-class "*ObservabilityAssetsTests"
```
