# ViajantesTurismo.AppHost

.NET Aspire orchestration host for the local ViajantesTurismo stack.

## Purpose

`ViajantesTurismo.AppHost` is the repository's code-first Aspire model. It declares local
infrastructure, service relationships, health checks, startup order, and opt-in developer tooling.

Keep application behavior out of this project. Business rules belong in the domain/application
projects, and reusable service defaults belong in `ViajantesTurismo.ServiceDefaults`.

## Services Orchestrated

### Infrastructure

- **PostgreSQL**: database server with PgWeb admin interface
- **Redis**: cache server with RedisInsight admin interface

### Application Services

- **MigrationService**: applies database migrations and seed data, then exits
- **Admin.ApiService**: Admin REST API; waits for the database and migration completion
- **Catalog.ApiService**: localized public content API
- **Management.Web**: Blazor management UI; waits for Redis, the Admin API, and the Catalog API
- **Public.Web**: public-facing Blazor UI; waits for the Catalog API and exposes an external HTTP endpoint

### Optional Developer Tooling

- **admin-performance-smoke**: opt-in k6 smoke scenario resource; enabled only when
  `VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1` is set before AppHost starts

## Service Dependencies

```text
PostgreSQL → Database → MigrationService
                     ↓
                  Admin.ApiService → Management.Web ← Redis
                         ↓
               admin-performance-smoke (opt-in)

Catalog.ApiService → Management.Web
                 ↓
             Public.Web
```

## Resource Names

All resource names come from `ResourceNames` in `src/ViajantesTurismo.Resources`. Do not hardcode
resource name strings in AppHost orchestration code.

## Code Organization

- `AppHost.cs`: primary orchestration map, kept short and dependency ordered
- `PerformanceTestingResourceExtensions.cs`: opt-in performance-testing executable resource wiring

Optional resources should stay in focused extension files when their setup would otherwise clutter the
main orchestration map.

## Features

- Aspire service discovery
- `/health` monitoring for HTTP project resources
- dependency orchestration with `WaitFor()` and `WaitForCompletion()`
- local admin tools through `.WithPgWeb()` and `.WithRedisInsight()`
- opt-in performance smoke execution through `admin-performance-smoke`

## Running

```powershell
# Preferred when using the repo-pinned local .NET tool manifest
dotnet tool run aspire run

# Alternative using only the .NET SDK
dotnet run --project src/ViajantesTurismo.AppHost

# If you installed Aspire CLI globally or via the install script
aspire run
```

This repository pins `aspire.cli` in `.config/dotnet-tools.json`, so `dotnet tool run aspire run` is
the reproducible command for contributors and CI. A global/script installation exposes `aspire`
directly on `PATH`.

The Aspire dashboard URL is printed when the AppHost starts. Use it to inspect services, logs,
traces, metrics, endpoints, and health status.

## Performance Smoke Resource

The AppHost can run the Admin k6 smoke scenario after the Admin API starts:

```bash
VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1 dotnet tool run aspire run
```

The resource is intentionally disabled by default so regular AppHost runs do not execute load tooling.
For profiles, thresholds, wrapper behavior, and result output, see
`tests/performance/README.md` and `tests/performance/k6/README.md`.

## Coverage

The AppHost project is local orchestration code and is excluded from MTP coverage collection in
`coverage.settings.xml`. Sonar coverage exclusions mirror that boundary with
`src/ViajantesTurismo.AppHost/**`.

## Dependencies

- **.NET Aspire**: orchestration framework
- **ViajantesTurismo.Resources**: resource name constants
