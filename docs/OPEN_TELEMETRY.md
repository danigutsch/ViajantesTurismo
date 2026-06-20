# OpenTelemetry Custom Telemetry Surfaces

This document lists repository-defined custom telemetry (ActivitySource and Meter), where each
surface is registered, and how to verify the emitted signals locally.

## Custom telemetry surfaces

| Surface type | Name | Defined in | Notes |
| --- | --- | --- | --- |
| ActivitySource | `SharedKernel.Mediator` | `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs` | Used by generated and runtime mediator spans. |
| Meter | `SharedKernel.Mediator` | `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs` | Emits mediator request/notification/stream metrics. |
| ActivitySource | `ViajantesTurismo.MigrationService.SeederWorker` | `src/ViajantesTurismo.MigrationService/SeederWorker.cs` | Emits database seeding span (`DatabaseSeeding`). |

## Registration points

### Shared defaults registration

`ViajantesTurismo.ServiceDefaults` centralizes shared registration used by services calling
`builder.AddServiceDefaults()`.

- Metrics registration:
    - `src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs`
    - `AddSharedKernelMediatorMetrics()` -> `metrics.AddMeter(SharedKernel.Mediator.MediatorTelemetry.Name)`
- Tracing registration:
    - `src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs`
    - `AddSharedKernelMediatorTracing()` -> `tracing.AddSource(SharedKernel.Mediator.MediatorTelemetry.Name)`
- Applied in pipeline:
    - `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
    - `ConfigureOpenTelemetry()` calls both shared registration helpers.

### Migration service registration

The migration service adds its custom source explicitly:

- `src/ViajantesTurismo.MigrationService/Program.cs`
- `.WithTracing(tracingBuilder => tracingBuilder.AddSource(SeederWorker.ActivitySourceName))`

## Exporter configuration

Exporter wiring remains service-default driven and exporter-neutral:

- `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
- OTLP exporter is enabled only when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.

## Failure semantics

Custom repository spans should follow one consistent failure contract:

- set `ActivityStatusCode.Error` when the operation fails
- record an exception event on the activity
- keep any repo-specific outcome or error-type tags alongside the exception event when they are part
  of the surface contract

Current repository examples:

- mediator request, notification, and stream spans use `AddException(ex)` plus
  `SetStatus(ActivityStatusCode.Error, ...)`
- migration service seeding span uses the same pattern for failed seeding runs

## Local verification (Aspire)

1. Start the full application stack:

   ```bash
   dotnet tool run aspire run
   ```

2. Open the Aspire dashboard URL shown in terminal output.

3. Verify traces:
   - Find spans named:
     - `mediator.send`
     - `mediator.stream`
     - `mediator.publish`
     - `mediator.notification.handle`
   - Find migration service span:
     - `DatabaseSeeding`

4. Verify metrics:
   - Meter: `SharedKernel.Mediator`
   - Expected custom metric names:
     - `mediator.requests`
     - `mediator.request.duration`
     - `mediator.notifications`
     - `mediator.notification.duration`
     - `mediator.streams`

5. Optional OTLP path check:
   - Set `OTEL_EXPORTER_OTLP_ENDPOINT` to your collector endpoint before startup.
   - Re-run `dotnet tool run aspire run` and confirm traces/metrics arrive in the configured backend.

## Quick code map

- Shared telemetry names: `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs`
- Shared telemetry runtime instrumentation: `src/SharedKernel/SharedKernel.Mediator/AppMediatorInstrumentation.cs`
- Shared service registration: `src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs`
- Shared OTel pipeline setup: `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
- Migration custom source + span emission: `src/ViajantesTurismo.MigrationService/SeederWorker.cs`
- Migration custom source registration: `src/ViajantesTurismo.MigrationService/Program.cs`

## Related package grouping guidance

For the repository-wide review of what observability and runtime code looks reusable enough for
future `SharedKernel.*` extraction, see
`docs/SHAREDKERNEL_OBSERVABILITY_RUNTIME_GROUPING.md`.
