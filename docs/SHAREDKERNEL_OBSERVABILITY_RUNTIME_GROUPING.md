# SharedKernel Observability And Runtime Grouping

Repository inventory for issue [#119](https://github.com/danigutsch/ViajantesTurismo/issues/119).

This document reviews the current repository for reusable observability, logging, runtime,
and startup configuration code that could live in logical `SharedKernel.*` packages.

This is a documentation-only grouping pass. It does **not** propose immediate refactoring of
all listed code. The goal is to separate:

- code that is safe to centralize in a dependency-light core runtime package
- code that belongs in feature-focused bundles with explicit technology dependencies
- code that should remain application-specific

## Current reusable surfaces

### Already centralized

| Surface | Current location | Notes |
| --- | --- | --- |
| OpenTelemetry resource/service identity setup | `src/SharedKernel/SharedKernel.Observability/ObservabilityBuilderExtensions.cs` | Cross-cutting and already package-shaped. |
| Explicit `service.name` detector | `src/SharedKernel/SharedKernel.Observability/ExplicitServiceNameDetector.cs` | Cross-cutting and already package-shaped. |
| Mediator telemetry names | `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs` | Feature-specific to mediator, but reusable across mediator consumers. |
| Mediator runtime instrumentation | `src/SharedKernel/SharedKernel.Mediator/` | Feature-specific runtime helper layer, not general observability infrastructure. |

### Reusable but not yet in the final package boundary

| Surface | Current location | Why it matters |
| --- | --- | --- |
| Shared OTel registration for ASP.NET Core/HTTP/gRPC/EF Core | `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs` | Centralized today, but mixed with app-hosting concerns and technology-specific dependencies. |
| Shared mediator meter/source registration | `src/ViajantesTurismo.ServiceDefaults/OpenTelemetryBuilderExtensions.cs` | Reusable pattern, but feature-coupled to mediator telemetry. |
| Health endpoint defaults | `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs` | Reusable startup behavior, but ASP.NET Core specific. |
| Service discovery + HTTP resilience defaults | `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs` | Reusable startup behavior, but Aspire and HTTP specific. |
| Migration seeding telemetry/logging pattern | `src/ViajantesTurismo.MigrationService/SeederWorker.cs` | Useful pattern reference, but still application-specific rather than package-ready. |

## Recommended grouping

### 1. Core runtime package

Recommended boundary: `SharedKernel.Observability`

This package should stay safe for broad reuse in any service or worker that wants a small,
dependency-conscious observability baseline.

Belongs here:

- service identity/resource configuration helpers
- logging configuration helpers that are not web-stack specific
- runtime metric/tracing baseline wiring that is host-agnostic
- lightweight detector abstractions and implementations
- shared observability naming conventions that are not feature-specific

Current code that fits:

- `src/SharedKernel/SharedKernel.Observability/ExplicitServiceNameDetector.cs`
- `src/SharedKernel/SharedKernel.Observability/ObservabilityBuilderExtensions.cs`

Keep out of this package:

- ASP.NET Core request instrumentation specifics
- gRPC client instrumentation specifics
- Entity Framework Core instrumentation specifics
- exporter policy tied to application startup conventions
- mediator-specific telemetry names or middleware-like behaviors

### 2. Feature-focused observability bundles

These packages are reusable, but only inside a more specific technical or feature boundary.

### `SharedKernel.Mediator` / `SharedKernel.Mediator.Abstractions`

Belongs here:

- mediator telemetry names and tags
- mediator activity/meter usage
- mediator runtime instrumentation helpers

Current code that fits:

- `src/SharedKernel/SharedKernel.Mediator.Abstractions/MediatorTelemetry.cs`
- `src/SharedKernel/SharedKernel.Mediator/SharedKernelMediatorActivitySource.cs`
- `src/SharedKernel/SharedKernel.Mediator/AppMediatorInstrumentation.cs`
- `src/SharedKernel/SharedKernel.Mediator/ActivityBehavior.cs`

Why not move to `SharedKernel.Observability`:

- the dependency is not just "observability"
- the concepts, names, and tags are mediator-domain specific

### Potential `SharedKernel.AspNetCore.Observability`

This package does not exist yet, but the current code suggests a clean future boundary.

Would belong here:

- ASP.NET Core request tracing defaults
- health endpoint trace filtering
- web-host endpoint mapping helpers that are intentionally HTTP-specific

Current candidate code:

- ASP.NET Core instrumentation bits in `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
- `MapDefaultEndpoints` from `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`

### Potential `SharedKernel.Hosting.ServiceDefaults`

This package also does not exist yet, but current startup defaults cluster around one theme:
host-level composition for app services.

Would belong here:

- service discovery defaults
- HTTP resilience defaults
- OTLP exporter activation policy
- shared startup composition for service apps

Current candidate code:

- `AddServiceDefaults` in `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`
- `AddOpenTelemetryExporters` in `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs`

Why separate from `SharedKernel.Observability`:

- these helpers are not dependency-light
- they combine hosting, HTTP, discovery, resilience, and observability concerns
- consumers that only want observability should not have to pull full service-default behavior

### 3. Application-specific code that should stay local

Keep local to the application/service unless a second concrete consumer appears.

Current examples:

- `src/ViajantesTurismo.MigrationService/SeederWorker.cs`
- migration-worker logging messages and seeding span names
- app-specific startup composition in `Program.cs` files

Reason:

- the behavior is tied to one executable's lifecycle and responsibilities
- no evidence yet that a reusable package would reduce duplication across multiple consumers

## Packaging rules of thumb

Use these rules when deciding whether code should move into a shared package later.

Move into a core runtime package only when the code:

- is useful across multiple service types
- does not depend on one bounded context or feature workflow
- does not force consumers to take unnecessary web, database, or transport dependencies
- exposes stable concepts that are unlikely to churn with one application

Move into a feature-focused bundle when the code:

- is reusable, but only within one vertical concern such as mediator, HTTP, ASP.NET Core, gRPC, or EF Core
- requires technology-specific dependencies that would bloat a core runtime package
- has naming/tagging/behavior tied to one technical subsystem

Keep local when the code:

- is only used by one service today
- encodes one executable's operational workflow
- would create a shared package before there is proven reuse

## Concrete follow-up backlog candidates

This issue is documentation-only, but the current code map suggests these future implementation
directions:

1. Keep extending `SharedKernel.Observability` only with host-agnostic service identity and
   baseline logging/metrics/tracing helpers.
2. Consider extracting the ASP.NET Core and host-composition pieces from
   `ViajantesTurismo.ServiceDefaults` into a separate hosting-focused package if a second
   consumer appears.
3. Keep mediator telemetry inside `SharedKernel.Mediator` and
   `SharedKernel.Mediator.Abstractions` rather than moving it into the generic
   observability package.
4. Treat migration-worker telemetry as an application-local pattern until another background
   worker needs the same abstraction.

## Related references

- `docs/OPEN_TELEMETRY.md`
- `src/SharedKernel/SharedKernel.Observability/README.md`
- `src/ViajantesTurismo.ServiceDefaults/README.md`
- `src/SharedKernel/SharedKernel.Mediator/README.md`
