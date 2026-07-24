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
- **ClamAV**: private TCP malware scanner for untrusted uploads
- **Keycloak**: local OIDC conformance identity provider; browser-facing because it hosts the
  Management Web authorization endpoint

### Application Services

- **MigrationService**: applies database migrations, atomically initializes synthetic Admin data only
  in Development, then exits
- **DatabaseObservability**: waits for migrations and optionally collects read-only index-health
  evidence for both PostgreSQL databases
- **Admin.ApiService**: Admin REST API; waits for the database and migration completion
- **Catalog.ApiService**: localized public content and public theme API
- **Management.Web**: Blazor management UI; waits for Redis, the Admin API, and the Catalog API
- **Public.Web**: public-facing Blazor UI; waits for the Catalog API and exposes an external HTTP endpoint

### Optional Developer Tooling

- **admin-performance-smoke**: opt-in k6 smoke scenario resource through
  `ViajantesTurismo.Performance.Tool`; enabled only when `VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1` is set
  before AppHost starts
- **Grafana LGTM observability stack**: opt-in local telemetry backend; enabled only when
  `ASPIRE_ENABLE_OBSERVABILITY_STACK=1` is set before AppHost starts

## Service Dependencies

```text
PostgreSQL → Database → MigrationService
                      ↓
              DatabaseObservability (optional collection)
                      ↓
                   Admin.ApiService → Management.Web ← Redis
                         ↓
               admin-performance-smoke (opt-in)

Catalog.ApiService → Management.Web
        ↓
    Public.Web

ClamAV (private TCP) → Admin.ApiService, Catalog.ApiService, Integration Event Worker
```

## Resource Names

Application resource names come from `ResourceNames` in `src/ViajantesTurismo.Resources`. The
optional Grafana LGTM stack uses defaults from `SharedKernel.Aspire.Hosting.Grafana`. Do not
hardcode resource name strings in AppHost orchestration code.

## Container Images

Infrastructure and companion tooling images are pinned by tag and digest to keep local runs
reproducible. Core AppHost resource pins live in `AppHostResourceExtensions.cs`. Optional Grafana
LGTM stack pins live in `SharedKernel.Aspire.Hosting.Grafana` so the local backend wiring can be
reused by AppHosts without putting exporter choices into runtime observability packages.

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
| Keycloak | `quay.io/keycloak/keycloak:26.7.0` | `sha256:2eb3cd316835c990e69e26ade292ffa78f6fb0db7d5fc6377463c162e1979ac0` |
| ClamAV | `docker.io/clamav/clamav:1.5` | `sha256:6f4a9e7d616ffc8d1070200fe35ac860735fdd522161a1043f94856e6ee13c28` |
| OpenTelemetry Collector | `docker.io/otel/opentelemetry-collector-contrib:0.130.1` | `sha256:9c247564e65ca19f97d891cca19a1a8d291ce631b890885b44e3503c5fdb3895` |
| Grafana | `docker.io/grafana/grafana:12.0.2` | `sha256:b5b59bfc7561634c2d7b136c4543d702ebcc94a3da477f21ff26f89ffd4214fa` |
| Loki | `docker.io/grafana/loki:3.5.1` | `sha256:a74594532eec4cc313401beedc4dd2708c43674c032084b1aeb87c14a5be1745` |
| Tempo | `docker.io/grafana/tempo:2.8.1` | `sha256:bc9245fe3da4e63dc4c6862d9c2dad9bcd8be13d0ba4f7705fa6acda4c904d0e` |
| Prometheus | `docker.io/prom/prometheus:v3.5.0` | `sha256:63805ebb8d2b3920190daf1cb14a60871b16fd38bed42b857a3182bc621f4996` |

## Release publish integration points

The AppHost must not calculate application versions. Release workflows calculate versions with
`SharedKernel.Versioning.Tool calculate-release`, then pass the computed values into Aspire publish
steps as configuration, environment variables, or MSBuild properties.
Release workflow context lives in [`docs/ci/supplemental-workflows.md`](../../docs/ci/supplemental-workflows.md).

### Execution mode

Use Aspire's `builder.ExecutionContext.IsRunMode` and `IsPublishMode` to choose resources that
exist only for local orchestration. Do not infer the execution mode from a release workflow
configuration value. `VT_ASPIRE_CONTAINER_IMAGE_TAG` supplies container metadata during publish; it
does not decide whether the AppHost is running locally or publishing a deployment model.

Keycloak and its HTTP development authority are run-mode-only resources. `aspire publish` omits
them entirely. Published Management Web must receive deployment-provided OIDC authority, issuer,
client ID, and client-secret configuration plus
`Authentication:TokenExchange:Enabled=true` and
`Authentication:TokenExchange:Provider=Keycloak`. Its identity provider must expose a
Keycloak-compatible RFC 8693 token endpoint. API bearer validation and authorization remain
provider-neutral.

Use these Aspire 13.4 integration points for release work:

- `PublishAsDockerFile(...)` on project resources that should be built as application containers
  during `aspire publish`. This is enabled only when `VT_ASPIRE_CONTAINER_IMAGE_TAG` is supplied.
- `WithImageTag(...)` on the generated container resource to apply the computed container image tag.
- `WithImageRegistry(...)` when a release workflow supplies the target registry.
- `WithImagePushOptions(...)` when publishing needs explicit registry push behavior.
- `WithManifestPublishingCallback(...)` only for deployment metadata that Aspire does not already
  model through container image annotations or resource environment variables.

Container tags for application resources use the versioning tool's `package_version` output. Release
Prep passes the same computed values as environment variables that MSBuild imports as properties, so
deployed assemblies carry `InformationalVersion`, which the shared diagnostics emit as OpenTelemetry
`service.version` and in startup logs. Source SHA and other traceability values are passed as
deployment metadata environment variables (`VT_DEPLOYMENT_VERSION`, `VT_SOURCE_REVISION`), not in the
container tag. Infrastructure image tags and SHA-256 digests remain pinned independently and must not
be replaced with application release versions.

Release workflow inputs consumed by the AppHost:

| Configuration key | Source | Purpose |
| --- | --- | --- |
| `VT_ASPIRE_CONTAINER_IMAGE_TAG` | `calculate-release` `package_version` | Application container tag |
| `VT_ASPIRE_CONTAINER_REGISTRY` | optional workflow variable | Target registry for image publish |
| `VT_ASPIRE_DEPLOYMENT_VERSION` | `calculate-release` `informational_version` | Deployment metadata environment value |
| `VT_ASPIRE_SOURCE_REVISION` | workflow commit SHA | Deployment traceability metadata |

Reference APIs researched for the pinned `aspire.cli` `13.4.6` toolchain:

- [`ProjectResourceBuilderExtensions.PublishAsDockerFile`](https://github.com/microsoft/aspire/blob/v13.4.6/src/Aspire.Hosting/ProjectResourceBuilderExtensions.cs)
- [`ContainerResourceBuilderExtensions.WithImageTag`](https://github.com/microsoft/aspire/blob/v13.4.6/src/Aspire.Hosting/ContainerResourceBuilderExtensions.cs)
- [`ContainerResourceBuilderExtensions.WithImageRegistry`](https://github.com/microsoft/aspire/blob/v13.4.6/src/Aspire.Hosting/ContainerResourceBuilderExtensions.cs)
- [`ResourceBuilderExtensions.WithImagePushOptions`](https://github.com/microsoft/aspire/blob/v13.4.6/src/Aspire.Hosting/ResourceBuilderExtensions.cs)
- [`ResourceBuilderExtensions.WithManifestPublishingCallback`](https://github.com/microsoft/aspire/blob/v13.4.6/src/Aspire.Hosting/ResourceBuilderExtensions.cs)
- [`ContainerImageAnnotation`](https://github.com/microsoft/aspire/blob/v13.4.6/src/Aspire.Hosting/ApplicationModel/ContainerImageAnnotation.cs)

## Code Organization

- `AppHost.cs`: primary orchestration map, kept short and dependency ordered
- `AppHostResourceExtensions.cs`: infrastructure and service resource wiring, including core tag and digest pins
- `DevelopmentProjectResourceExtensions.cs`: development endpoint and environment defaults
- `PerformanceTestingResourceExtensions.cs`: opt-in performance-testing executable resource wiring
- `ObservabilityStackResourceExtensions.cs`: opt-in gate and AppHost-specific Grafana LGTM stack naming
- `SharedKernel.Aspire.Hosting.Grafana`: reusable Aspire hosting wiring for the Grafana LGTM stack

Optional resources should stay in focused extension files when their setup would otherwise clutter the
main orchestration map.

## Features

- Aspire service discovery
- `/health` monitoring for HTTP project resources
- dependency orchestration with `WaitFor()` and `WaitForCompletion()`
- local admin tools through `.WithPgWeb()` and `.WithRedisInsight()`
- opt-in performance smoke execution through `admin-performance-smoke`
- opt-in local Grafana LGTM stack through `ASPIRE_ENABLE_OBSERVABILITY_STACK=1`
- opt-in read-only PostgreSQL index-health collection through
  `Aspire:Features:DatabaseObservability=true`

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

## Local malware scanning

ClamAV exposes only its private TCP `clamd` endpoint on port `3310`; it has no public endpoint or
HTTP health check. Admin.ApiService, Catalog.ApiService, and Integration Event Worker receive the
private host and port through AppHost configuration and wait for the ClamAV PING/PONG health check.

The scanner persists definitions in `clamav-definitions` at `/var/lib/clamav`. Test runs use the
isolated name `clamav-<suffix>-definitions`. FreshClam is enabled by default. A missing or reset
definitions volume can make the next startup take longer while definitions download and ClamAV loads
them. The AppHost does not configure a ClamAV memory limit; ensure the local container runtime has
enough memory for the daemon and its definitions. Health waits, not fixed delays, decide readiness.

To reset definitions deliberately, stop the AppHost, inspect the exact local volume with
`docker volume ls`, and then remove only that verified volume with
`docker volume rm <verified-volume-name>`. This is destructive: it deletes cached signatures and
forces a new definitions download. Do not use broad cleanup such as `docker volume prune`.

## Local OIDC conformance

Keycloak receives a dynamically allocated `localhost` HTTP endpoint for development and CI
conformance only. It is intentionally browser-facing because it hosts the authorization endpoint
used by Management Web. The AppHost declares it only in `IsRunMode`; production identity providers
remain deployment configuration and are never included in the publish model. Management Web's
enforced audience-token exchange requires a Keycloak-compatible RFC 8693 endpoint in every
deployment.

AppHost stores the following required values as local secrets:

- `management-web-client-secret`: Keycloak receives it as `MANAGEMENT_WEB_CLIENT_SECRET` and
  Management Web receives it as `Authentication__ClientSecret`.
- `identity-provider-conformance-user-password`: Keycloak receives it as
  `LOCAL_CONFORMANCE_PASSWORD` for the `conformance` user.
- `identity-provider-admin-password`: Keycloak receives it as `KC_BOOTSTRAP_ADMIN_PASSWORD` for
  the `admin` bootstrap user.

The imported confidential client ID is `web-app`. Keycloak permits only the local dynamic
`https://localhost:<port>` and `https://127.0.0.1:<port>` host origins; Keycloak does not support
an exact callback path with a dynamic HTTPS port. The configuration is local-conformance only and
has no browser web origins because Management Web is a server-side BFF. No resolved credential
belongs in source control.

`web-app` requests the approved Admin, Catalog, and Branding API scopes during sign-in. The protected
server-side BFF ticket retains the source token, then obtains or reuses a protected cached exchanged
token for the exact backend audience. Each backend receives only its exchanged token; neither token
reaches the browser.
`conformance-test-client` permits password grants solely for owned integration and browser-test
setup; it is a local realm client and is not a production client.

SeaweedFS access and secret keys are persisted Aspire parameters. To override them locally, use the
AppHost user-secrets store and omit resolved values from source control:

```bash
dotnet user-secrets set "Parameters:seaweedfs-access-key" "<local-access-key>" --project src/ViajantesTurismo.AppHost
dotnet user-secrets set "Parameters:seaweedfs-secret-key" "<local-secret-key>" --project src/ViajantesTurismo.AppHost
```

Use deployment-managed secret stores for production. Do not copy generated credentials, connection
strings, endpoint URLs, scanner responses, or uploaded content into documentation or settings files.

## Performance Smoke Resource

The AppHost can run the Admin k6 smoke scenario after the Admin API starts:

```bash
VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1 dotnet tool run aspire run
```

The resource is intentionally disabled by default so regular AppHost runs do not execute load tooling.
It uses the repository-owned .NET performance tool rather than shell wrappers, so local k6 is the
default and Docker mode remains explicit opt-in. For profiles, thresholds, security defaults, and result
output, see `tests/performance/README.md` and `tests/performance/k6/README.md`.

## Trusted Telemetry Gateway and Observability Stack

Every supported AppHost profile starts the pinned OpenTelemetry Collector and uses the repository-owned
forwarding contract to route compatible Aspire-annotated application telemetry through it before
the Aspire dashboard. Add the local Grafana LGTM backends with:

```bash
ASPIRE_ENABLE_OBSERVABILITY_STACK=1 dotnet tool run aspire run
```

The resources are:

- always-on `opentelemetry-collector`: drops all span events, clears trace status descriptions,
  removes explicit sensitive/high-cardinality trace attributes, and routes sanitized signals
- optional `grafana`: local dashboard UI with provisioned datasources and dashboards
- optional `loki`: log backend
- optional `tempo`: sanitized trace backend
- optional `prometheus`: metric backend scraping the Collector's Prometheus exporter

Raw telemetry exists on the trusted application-to-Collector hop. AppHost forwarding covers the
normal AppHost OTLP annotation contract, but a manually constructed exporter or a process outside this
AppHost can send directly to another endpoint and bypass the gateway. Restrict backend network access
accordingly. The Aspire dashboard remains available for local inspection. Grafana is added for
source-controlled datasource, provisioning, and dashboard validation work. Configuration lives under
`observability/` at the repository root; reusable gateway/stack wiring lives in
`SharedKernel.Aspire.Hosting.Grafana`.

The checked-in gateway and backend configuration is local-only: its YAML does not require Collector
receiver TLS or client authentication, although Aspire may inject development-certificate TLS under an
HTTPS launch profile. Tempo transport is insecure, Loki uses HTTP, and Grafana permits anonymous local
Administrator access. Deployments must provide authenticated and encrypted Collector ingress and
downstream transport, restrict direct backend endpoints, use non-anonymous backend access, and keep
credentials out of source-controlled configuration.

## Database Observability Resource

`database-observability` starts after database migrations and applies the reusable
`SharedKernel.Observability.Npgsql` monitor to both `admin-database` and `catalog-database`. It does
not receive application database references and never falls back to application credentials.

The resource is absent unless the AppHost uses the `Full` profile and
`Aspire:Features:DatabaseObservability=true` is configured. When it is enabled, configure these
AppHost parameters through user secrets or deployment configuration:

```text
Parameters:admin-index-health-connection-string=<least-privilege admin monitoring connection>
Parameters:catalog-index-health-connection-string=<least-privilege catalog monitoring connection>
```

Use a monitoring role with only the statistics access documented in
[`docs/architecture/postgresql-observability.md`](../../docs/architecture/postgresql-observability.md).
Do not put those connection strings in source-controlled configuration.

## Coverage

The AppHost project is local orchestration code and is excluded from MTP coverage collection in
`coverage.settings.xml`. Sonar coverage exclusions mirror that boundary with
`src/ViajantesTurismo.AppHost/**`.

## Dependencies

- **.NET Aspire**: orchestration framework
- **ViajantesTurismo.Resources**: resource name constants
