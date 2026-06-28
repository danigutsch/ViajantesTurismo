# Configuration Standards

This document defines how application and service configuration values are classified, named,
validated, and documented.

## Research baseline

This policy follows the official `Microsoft.Extensions.Configuration` and options guidance:

- <https://learn.microsoft.com/dotnet/core/extensions/configuration>
- <https://learn.microsoft.com/dotnet/core/extensions/configuration-providers>
- <https://learn.microsoft.com/dotnet/core/extensions/options>
- <https://learn.microsoft.com/dotnet/core/extensions/options-validation-generator>

The relevant guidance for this repository is:

- use the options pattern for related settings instead of scattering raw `IConfiguration` reads
- keep options classes bindable with public get/set properties
- bind each options type from one named section
- validate options with `IValidateOptions<TOptions>` or source-generated validation
- call `ValidateOnStart` when invalid settings should fail service startup
- prefer the configuration binding generator for AOT-compatible projects
- document environment variable names with `__` for hierarchy delimiters
- keep secrets out of source-controlled settings files
- use normal configuration providers or DI overrides in tests, not parallel production wiring

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

## Hardcoded value policy

Review hardcoded values in production code before adding a new configuration surface.

Move a value to configuration when:

- operators may tune it per environment
- the value affects reliability, availability, throughput, cost, or compliance
- the value is a timeout, lock duration, lease, retry/backoff, polling interval, batch size,
  cache duration, queue size, concurrency limit, threshold, feature switch, or deployment endpoint
- changing the value should not require rebuilding the application

Keep a value as code when:

- it is a domain invariant or public contract limit
- it is a route template, health endpoint path, telemetry semantic name, resource name, or protocol value
- it is test, analyzer, source-generator, benchmark, or local tooling configuration
- the value has no current operational owner or environment-specific need

Prefer named constants for code-owned values that appear in more than one place. Do not add a new
options type for a single caller unless the setting needs validation, operator documentation, or
future environment override now.

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

Use `IOptions<TOptions>` for static startup configuration. Use `IOptionsMonitor<TOptions>` only when
the service must observe changes after startup. Avoid `IOptionsSnapshot<TOptions>` in singleton
services.

Validation failures should include the section/key or setting purpose. Safe range validation should
reject disabled-looking values such as zero or negative durations unless zero is explicitly supported
and documented.

## Analyzer and static-check direction

Analyzer coverage should be conservative:

- flag production `src/` values that look like tunable operational policy
- exclude migrations, generated code, source generators, analyzers, tests, benchmarks, and AppHost
  local-tooling glue by default
- prefer warnings with fix guidance over auto-fixes for ambiguous values
- never flag domain invariants, contract constants, route templates, telemetry names, or resource names
  without a precise rule
- document intentional exceptions near the value or in this file

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

## Current production inventory

| Area | Candidate | Classification | Decision |
| --- | --- | --- | --- |
| Catalog integration events | Idempotency lock duration | Configuration | Bound to `CatalogIntegrationEvents:IdempotencyLockDuration`; default stays in `IntegrationEventOptions`; validated on start. |
| Catalog projections | Projection batch size or replay limits | Configuration candidate | Defer until projection runtime tuning is implemented. |
| SharedKernel PostgreSQL event sourcing | Schema name | Provider option | Keep as provider option and identifier validation; add app binding only when an app needs environment-specific schema selection. |
| AppHost container image digests | Image SHA-256 values | Local orchestration supply-chain pins | Keep as AppHost constants; not runtime configuration. |
| AppHost performance smoke values | k6 profile, VUs, duration, result path | Test/tooling value | Keep as opt-in environment variables for local tooling. |
| Contract validation limits | DTO/domain length, price, count, and range values | Domain/API invariant | Keep in contract/domain constants. |
| Endpoint paths and route templates | `/health`, API routes | Stable contract | Keep in code constants/routes. |

## Catalog integration events

| Key | Default | Unit/range | Safe to log | Consumed by | Test override |
| --- | --- | --- | --- | --- | --- |
| `CatalogIntegrationEvents:IdempotencyLockDuration` | `00:05:00` | .NET `TimeSpan` round-trip `c` format; greater than zero | Yes | `ViajantesTurismo.Catalog.Application` integration event consumers | Override the options value through dependency injection or configuration. |

Environment variable form: `CatalogIntegrationEvents__IdempotencyLockDuration`.
