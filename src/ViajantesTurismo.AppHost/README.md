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

## Container Images

Infrastructure and companion tooling images are pinned by digest in `AppHostResourceExtensions.cs`
to keep local runs reproducible. There is no production image policy yet. When one exists, update
these digest pins to match the production-approved images.

| Resource | Source tag used for pin | Digest |
| --- | --- | --- |
| PostgreSQL | `docker.io/library/postgres:17.6` | `sha256:00bc86618629af00d2937fdc5a5d63db3ff8450acf52f0636ec813c7f4902929` |
| PgWeb | `docker.io/sosedoff/pgweb:0.17.0` | `sha256:a5256d416e2e8b92d69a4459058e3eca33a9f075d8325491644411d0bc3bd70b` |
| Redis | `docker.io/library/redis:8.6` | `sha256:4483474d5e78c444ce180037def4430ec0d02553663ded2a6d7a1c922da00ecf` |
| RedisInsight | `docker.io/redis/redisinsight:3.0` | `sha256:4455c3304eafe1311d0a367022bad41520e307138b7272e1c0c308ce781f7162` |

## Code Organization

- `AppHost.cs`: primary orchestration map, kept short and dependency ordered
- `AppHostResourceExtensions.cs`: infrastructure and service resource wiring, including image pins
- `DevelopmentProjectResourceExtensions.cs`: development endpoint and environment defaults
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
