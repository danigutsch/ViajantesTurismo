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
- **Catalog.ApiService**: localized public content and public theme API
- **Management.Web**: Blazor management UI; waits for Redis, the Admin API, and the Catalog API
- **Public.Web**: public-facing Blazor UI; waits for the Catalog API and exposes an external HTTP endpoint

### Optional Developer Tooling

- **admin-performance-smoke**: opt-in k6 smoke scenario resource; enabled only when
  `VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1` is set before AppHost starts
- **Grafana LGTM observability stack**: opt-in local telemetry backend; enabled only when
  `VT_ASPIRE_ENABLE_OBSERVABILITY_STACK=1` is set before AppHost starts

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

Infrastructure and companion tooling images are pinned by tag and digest in
`AppHostResourceExtensions.cs` to keep local runs reproducible. There is no production image policy
yet. When one exists, update each tag and digest pair to match the production-approved image.

Do not commit placeholder digests. `WithImageSHA256(...)` must contain the verified 64-character
digest without the `sha256:` prefix. The `SKASPIRE001` code fix may temporarily insert uncompilable
placeholders to make the missing value obvious; replace them with verified registry values before the
code builds or before committing.

| Resource | Source tag used for pin | Digest |
| --- | --- | --- |
| PostgreSQL | `docker.io/library/postgres:18.4` | `sha256:4aabea78cf39b90e834caf3af7d602a18565f6fe2508705c8d01aa63245c2e20` |
| PgWeb | `docker.io/sosedoff/pgweb:0.17.0` | `sha256:a5256d416e2e8b92d69a4459058e3eca33a9f075d8325491644411d0bc3bd70b` |
| Redis | `docker.io/library/redis:8.8` | `sha256:2838d5524559494f6f1cd66e97e76b200d64a633a8614200620755ed395daf32` |
| RedisInsight | `docker.io/redis/redisinsight:3.6` | `sha256:aa21bbd198455b4ad964f76782db951155aa0d712321f599972d1525f031f0e6` |
| OpenTelemetry Collector | `docker.io/otel/opentelemetry-collector-contrib:0.130.1` | `sha256:9c247564e65ca19f97d891cca19a1a8d291ce631b890885b44e3503c5fdb3895` |
| Grafana | `docker.io/grafana/grafana:12.0.2` | `sha256:b5b59bfc7561634c2d7b136c4543d702ebcc94a3da477f21ff26f89ffd4214fa` |
| Loki | `docker.io/grafana/loki:3.5.1` | `sha256:a74594532eec4cc313401beedc4dd2708c43674c032084b1aeb87c14a5be1745` |
| Tempo | `docker.io/grafana/tempo:2.8.1` | `sha256:bc9245fe3da4e63dc4c6862d9c2dad9bcd8be13d0ba4f7705fa6acda4c904d0e` |
| Prometheus | `docker.io/prom/prometheus:v3.5.0` | `sha256:63805ebb8d2b3920190daf1cb14a60871b16fd38bed42b857a3182bc621f4996` |

## Code Organization

- `AppHost.cs`: primary orchestration map, kept short and dependency ordered
- `AppHostResourceExtensions.cs`: infrastructure and service resource wiring, including tag and digest pins
- `DevelopmentProjectResourceExtensions.cs`: development endpoint and environment defaults
- `PerformanceTestingResourceExtensions.cs`: opt-in performance-testing executable resource wiring
- `ObservabilityStackResourceExtensions.cs`: opt-in Grafana LGTM stack and OpenTelemetry Collector wiring

Optional resources should stay in focused extension files when their setup would otherwise clutter the
main orchestration map.

## Features

- Aspire service discovery
- `/health` monitoring for HTTP project resources
- dependency orchestration with `WaitFor()` and `WaitForCompletion()`
- local admin tools through `.WithPgWeb()` and `.WithRedisInsight()`
- opt-in performance smoke execution through `admin-performance-smoke`
- opt-in local Grafana LGTM stack through `VT_ASPIRE_ENABLE_OBSERVABILITY_STACK=1`

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

## Observability Stack Resource

The AppHost can run a local Grafana LGTM stack and route application telemetry through an
OpenTelemetry Collector:

```bash
VT_ASPIRE_ENABLE_OBSERVABILITY_STACK=1 dotnet tool run aspire run
```

The optional stack includes:

- `opentelemetry-collector`: receives OTLP telemetry from AppHost resources and routes signals
- `grafana`: local dashboard UI with provisioned datasources and dashboards
- `loki`: log backend
- `tempo`: trace backend
- `prometheus`: metric backend scraping the collector's Prometheus exporter

The Aspire dashboard remains available for local inspection. Grafana is added for source-controlled
datasource, provisioning, and dashboard validation work. Provisioning files live under
`observability/` at the repository root.

## Coverage

The AppHost project is local orchestration code and is excluded from MTP coverage collection in
`coverage.settings.xml`. Sonar coverage exclusions mirror that boundary with
`src/ViajantesTurismo.AppHost/**`.

## Dependencies

- **.NET Aspire**: orchestration framework
- **ViajantesTurismo.Resources**: resource name constants
