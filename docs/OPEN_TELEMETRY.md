# OpenTelemetry Custom Telemetry Surfaces

This document lists repository-defined custom telemetry (ActivitySource and Meter), where each
surface is registered, and how to verify the emitted signals locally.

## Custom telemetry surfaces

| Surface type | Name | Defined in | Notes |
| --- | --- | --- | --- |
| ActivitySource | `SharedKernel.Mediator` | `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs` | Used by generated and runtime mediator spans. |
| Meter | `SharedKernel.Mediator` | `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs` | Emits mediator request/notification/stream metrics. |
| ActivitySource | `ViajantesTurismo.Catalog` | `src/ViajantesTurismo.Catalog.Application/CatalogTelemetry.cs` | Emits Catalog integration event, stream update, and projection spans. |
| Meter | `ViajantesTurismo.Catalog` | `src/ViajantesTurismo.Catalog.Application/CatalogTelemetry.cs` | Emits Catalog integration event, idempotency, stream update, and projection metrics. |
| ActivitySource | `SharedKernel.EventSourcing.PostgreSQL` | `src/SharedKernel/SharedKernel.EventSourcing.PostgreSQL/PostgreSqlEventSourcingTelemetry.cs` | Emits PostgreSQL event-store append/load/checkpoint spans. |
| Meter | `SharedKernel.EventSourcing.PostgreSQL` | `src/SharedKernel/SharedKernel.EventSourcing.PostgreSQL/PostgreSqlEventSourcingTelemetry.cs` | Emits PostgreSQL event-store duration, count, and conflict metrics. |
| ActivitySource | `ViajantesTurismo.MigrationService.SeederWorker` | `src/ViajantesTurismo.MigrationService/SeederWorker.cs` | Emits database seeding span (`DatabaseSeeding`). |

## Telemetry contract documentation rules

Each repository-owned telemetry surface should document:

- stable ActivitySource and Meter names
- span and metric names
- units for duration/count metrics
- tag names, allowed values, and outcome values
- privacy and cardinality limits for every tag or metric dimension
- registration point in service defaults or explicit service startup
- compatibility notes for additive versus breaking telemetry changes

Telemetry names, tag names, units, and outcome values are consumer-facing contracts. Additive spans,
metrics, or tag values are normally safe. Renames, unit changes, or cardinality expansions are
breaking and should be called out in release notes or ADRs when they affect operators.

Do not put personal data, customer-entered content, raw identifiers, or unbounded values into tags
or metric dimensions. Prefer low-cardinality outcome, operation, area, and provider values.

## Consumer documentation template

Per-surface docs should answer these questions before adding dashboard-specific JSON:

- Developers: which source or meter should be enabled, and which operation emits each signal?
- Operators: which operational question does each metric or span answer?
- Dashboard authors: which dimensions are safe to group by without cardinality risk?
- Compatibility reviewers: which names and dimensions are stable contracts?
- Backend users: how can the signal be queried without binding the repository to one vendor?

## Registration points

### Shared defaults registration

`ViajantesTurismo.ServiceDefaults` centralizes shared registration used by services calling
`builder.AddServiceDefaults()`.

- Metrics registration:
    - `src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs`
    - `AddSharedKernelMediatorMetrics()` -> `metrics.AddMeter(SharedKernel.Mediator.MediatorTelemetry.Name)`
    - `AddCatalogMetrics()` -> `metrics.AddMeter(ViajantesTurismo.Catalog.Application.CatalogTelemetry.Name)`
    - `AddSharedKernelProviderMetrics()` -> `metrics.AddMeter("SharedKernel.EventSourcing.PostgreSQL")`
- Tracing registration:
    - `src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs`
    - `AddSharedKernelMediatorTracing()` -> `tracing.AddSource(SharedKernel.Mediator.MediatorTelemetry.Name)`
    - `AddCatalogTracing()` -> `tracing.AddSource(ViajantesTurismo.Catalog.Application.CatalogTelemetry.Name)`
    - `AddSharedKernelProviderTracing()` -> `tracing.AddSource("SharedKernel.EventSourcing.PostgreSQL")`
- Applied in pipeline:
    - `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
    - `ConfigureOpenTelemetry()` calls the custom registration helpers.

### Migration service registration

The migration service adds its custom source explicitly:

- `src/ViajantesTurismo.MigrationService/Program.cs`
- `.WithTracing(tracingBuilder => tracingBuilder.AddSource(SeederWorker.ActivitySourceName))`

## Exporter configuration

Exporter wiring remains service-default driven and exporter-neutral:

- `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
- OTLP exporter is enabled only when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.

## Repository custom span contract

Repository-owned custom spans should follow one explicit contract across success,
cancellation, and failure paths.

| Path | Activity status | Status description | Exception event | Repo-specific tags |
| --- | --- | --- | --- | --- |
| Success | `ActivityStatusCode.Ok` | `null` | Do not record one | Keep the surface's success/outcome tags when that surface defines them |
| Cancellation | Leave status unset | `null` | Do not record one | Keep the surface's cancellation/outcome tags when that surface defines them |
| Failure | `ActivityStatusCode.Error` | Use the thrown exception message | Record exactly one exception event | Keep the surface's error/outcome tags when that surface defines them |

### Status description rules

- Success spans must leave `StatusDescription` as `null`.
- Cancellation spans must leave `StatusDescription` as `null`.
- Failure spans must populate `StatusDescription` with the thrown exception message.

### Exception recording rules

- Failure spans must record one exception event with OpenTelemetry-standard exception tags.
- Success and cancellation spans must not record exception events.
- Repo-specific error tags do not replace the exception event; they complement it.

### Repo-specific tag policy

Repo-specific tags are required only when a span surface already defines a stable,
surface-owned tag contract.

- `SharedKernel.Mediator` runtime pipeline spans require:
    - `sharedkernel.mediator.outcome`
    - `error.type` on failures only
- `SharedKernel.Mediator` generated spans require:
    - `mediator.outcome`
    - `error.type` on failures only
- `ViajantesTurismo.MigrationService.SeederWorker` spans require:
    - `operation.type=database_seeding`
    - `worker.type=migration`
- `ViajantesTurismo.Catalog` spans and metrics use Catalog-owned operation and outcome tags.
- `SharedKernel.EventSourcing.PostgreSQL` spans and metrics use provider-owned operation, stream,
  checkpoint, and outcome tags.

Surfaces that do not define a stable repository tag contract should still follow the
status and exception-event rules above without inventing extra tags.

### Current repository examples

- Mediator request, notification, and stream spans use `AddException(ex)` plus
  `SetStatus(ActivityStatusCode.Error, ...)` on failures, leave cancellation status unset,
  and emit outcome tags.
- Migration service seeding spans use the same failure-status and exception-event pattern,
  while keeping seeding-specific operation tags on all paths.
- Catalog and PostgreSQL event-sourcing surfaces are registered through service defaults and should
  keep their tag sets low-cardinality because they are used by traces and metrics.

### Helper abstraction decision

The repository currently keeps this contract documentation-first. No shared helper is added
in this issue because the active span producers do not share the same repo-specific tag set,
and the direct tests already provide the drift protection needed for the current surfaces.

## Local verification (Aspire)

1. Start the full application stack:

   ```bash
   dotnet tool run aspire run
   ```

2. Open the Aspire dashboard URL shown in terminal output.

3. Verify traces:
    - Find mediator spans:
        - `mediator.send`
        - `mediator.stream`
        - `mediator.publish`
        - `mediator.notification.handle`
    - Find migration service span:
        - `DatabaseSeeding`
    - Find Catalog spans:
        - `catalog.integration_event.handle`
        - `catalog.tour.stream_update`
        - `catalog.projection.process`
    - Find PostgreSQL event-sourcing spans:
        - `eventsourcing.postgresql.append`
        - `eventsourcing.postgresql.load`
        - `eventsourcing.postgresql.checkpoint`

4. Verify metrics:
    - Meter: `SharedKernel.Mediator`
    - Expected custom metric names:
        - `mediator.requests`
        - `mediator.request.duration`
        - `mediator.notifications`
        - `mediator.notification.duration`
        - `mediator.streams`
    - Additional custom meters:
        - `ViajantesTurismo.Catalog`
        - `SharedKernel.EventSourcing.PostgreSQL`

5. Optional OTLP path check:
   - Set `OTEL_EXPORTER_OTLP_ENDPOINT` to your collector endpoint before startup.
   - Re-run `dotnet tool run aspire run` and confirm traces/metrics arrive in the configured backend.

## Quick code map

- Shared telemetry names: `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs`
- Shared telemetry runtime instrumentation: `src/SharedKernel/SharedKernel.Mediator/AppMediatorInstrumentation.cs`
- Shared service registration: `src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs`
- Shared OTel pipeline setup: `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
- Catalog telemetry names and instrumentation helpers: `src/ViajantesTurismo.Catalog.Application/CatalogTelemetry.cs`
- PostgreSQL event-sourcing telemetry names and instrumentation helpers:
  `src/SharedKernel/SharedKernel.EventSourcing.PostgreSQL/PostgreSqlEventSourcingTelemetry.cs`
- Migration custom source + span emission: `src/ViajantesTurismo.MigrationService/SeederWorker.cs`
- Migration custom source registration: `src/ViajantesTurismo.MigrationService/Program.cs`

## Related package grouping guidance

For the repository-wide review of what observability and runtime code looks reusable enough for
future `SharedKernel.*` extraction, see
`docs/SHAREDKERNEL_OBSERVABILITY_RUNTIME_GROUPING.md`.
