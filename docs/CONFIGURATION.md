# Configuration Standards

This document defines how application and service configuration values are classified, named,
validated, and documented.

## Classification

Use the smallest durable mechanism for each value:

| Kind | Use when | Example |
| --- | --- | --- |
| Resource name | Aspire or service-discovery identity shared by projects | `ResourceNames.Cache` |
| Configuration | Operators may change the value per environment | timeouts, retention windows, feature flags |
| Secret | Value grants access or identifies a credential | connection strings, API keys |
| Constant | Value is a protocol, route, or internal invariant | health endpoint path |
| Domain invariant | Business rule protected by domain code | booking status transitions |
| Test/tooling value | Local-only harness or developer workflow value | smoke-test arguments |

Do not move constants into configuration just because they are strings. Do move production values
when environments need different values or operators need documented control.

## Naming

- Use PascalCase section names matching the owning feature or component.
- Use descriptive key names with units in the name when the type is not self-evident.
- Use .NET `TimeSpan` options for durations and document values in round-trip `c` format.
- Keep resource names in `src/ViajantesTurismo.Resources/ResourceNames.cs` when multiple projects
  need the same Aspire/service-discovery identity.

## Binding and validation

Configuration surfaces should use strongly typed options when they have more than one setting, need
validation, or are consumed by more than one method.

Required options rules:

- bind from one named section
- validate required values and safe ranges at startup
- use `IValidateOptions<TOptions>` for runtime options validation instead of inline validation lambdas
- use `ValidateOnStart` for production services where invalid configuration should fail fast
- keep defaults close to the options type or the registration site
- let tests override options through normal configuration or dependency injection

## Documentation template

Each production configuration surface should document:

- section and key names
- default value
- unit and allowed range
- whether the key is safe to log
- matching environment variable name when useful
- which project consumes the value
- test override guidance

## Current classification notes

| Value | Classification | Action |
| --- | --- | --- |
| Redis resource name `cache` | Resource name | Use `ResourceNames.Cache`, not inline strings. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Configuration | Already documented in OpenTelemetry guidance. |
| Catalog idempotency lock duration | Configuration | `CatalogIntegrationEvents:IdempotencyLockDuration`, default `00:05:00`, use .NET `TimeSpan` round-trip `c` format, must be greater than zero. |
| PostgreSQL event-sourcing schema | Provider option | Keep the current provider option and identifier validation; add DI binding only when an app needs per-environment schema selection. |
| Catalog projection batch size | Configuration candidate | Defer until projection runtime ownership and operational tuning needs are clear. |
| Standard HTTP resilience defaults | Library policy | Defer to a resilience policy review; do not replace framework defaults speculatively. |
| `AllowedHosts` | Hosting configuration | Review separately before changing production defaults. |
| CDN asset URLs | External asset policy | Decide asset strategy before configuring broadly. |
| Endpoint paths, route templates, and contract limits | Constant or domain/API invariant | Keep centralized constants and validation rules unless the public contract changes. |
| Analyzer `.editorconfig` options | Test/tooling value | Keep in analyzer configuration; not production runtime configuration. |
| AppHost performance-test environment variables | Test/tooling value | Keep as local opt-in tooling documented by AppHost; not production runtime configuration. |
| Launch profile localhost ports | Test/tooling value | Keep in `launchSettings.json`; not production runtime configuration. |

## Catalog integration events

| Key | Default | Unit/range | Safe to log | Consumed by | Test override |
| --- | --- | --- | --- | --- | --- |
| `CatalogIntegrationEvents:IdempotencyLockDuration` | `00:05:00` | .NET `TimeSpan` round-trip `c` format; greater than zero | Yes | `ViajantesTurismo.Catalog.Application` integration event consumers | Override the options value through dependency injection or configuration. |

Environment variable form: `CatalogIntegrationEvents__IdempotencyLockDuration`.
