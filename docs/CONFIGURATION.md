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
- Prefer `TimeSpan` options for durations and document accepted formats.
- Keep resource names in `src/ViajantesTurismo.Resources/ResourceNames.cs` when multiple projects
  need the same Aspire/service-discovery identity.

## Binding and validation

Configuration surfaces should use strongly typed options when they have more than one setting, need
validation, or are consumed by more than one method.

Required options rules:

- bind from one named section
- validate required values and safe ranges at startup
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
| Catalog idempotency lock duration | Configuration candidate | Convert to options when tuning is required. |
| `AllowedHosts` | Hosting configuration | Review separately before changing production defaults. |
| CDN asset URLs | External asset policy | Decide asset strategy before configuring broadly. |
