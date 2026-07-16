# Configuration Standards

This document separates production runtime configuration from library options, local AppHost
orchestration values, and test/tooling settings.

## Production runtime configuration

Production operators should start here. This table is the canonical list of app settings that may be
overridden per environment.

| Key | Default | Unit/range | Safe to log | Consumed by | Notes |
| --- | --- | --- | --- | --- | --- |
| `Authentication:Authority` | None; required | Absolute HTTPS OIDC authority URI; HTTP only with the explicit Development override | Yes | `ViajantesTurismo.Management.Web` | Deployment-provided OIDC authority for the Management BFF and required token exchange. |
| `Authentication:Issuer` | None; required | Absolute HTTPS issuer URI; HTTP only with the explicit Development override | Yes | `ViajantesTurismo.Management.Web` | Expected issuer for Management OIDC validation. |
| `Authentication:ClientId` | None; required | Non-empty string | Yes | `ViajantesTurismo.Management.Web` | Confidential OIDC client identifier used for Management sign-in and token exchange. |
| `Authentication:ClientSecret` | None; required secret | Non-empty string | No | `ViajantesTurismo.Management.Web` | Confidential OIDC client secret used for Management sign-in and token exchange. |
| `Authentication:DataProtection:CertificatePath` | None; required outside Development | Path to the PKCS#12 certificate that encrypts the shared Data Protection key ring | No | `ViajantesTurismo.Management.Web` | Deployment-provided certificate path; startup fails outside Development when absent. |
| `Authentication:DataProtection:CertificatePassword` | None; required secret outside Development | Non-empty string | No | `ViajantesTurismo.Management.Web` | Password for the Data Protection key-encryption certificate. |
| `Authentication:TokenExchange:Enabled` | None; required `true` | Boolean; must be `true` | Yes | `ViajantesTurismo.Management.Web` | Enables the required BFF audience-token exchange; it is not an optional feature flag. |
| `Authentication:TokenExchange:Provider` | None; required `Keycloak` | Case-insensitive `Keycloak` | Yes | `ViajantesTurismo.Management.Web` | Selects the current Keycloak RFC 8693 audience-token exchange implementation. |
| `CatalogIntegrationEvents:IdempotencyLockDuration` | `00:05:00` | .NET `TimeSpan` round-trip `c` format; greater than zero | Yes | `ViajantesTurismo.Catalog.Application` integration event handlers | Controls how long an idempotency lock is held while processing one integration event. |
| `PublicWeb:Sitemap:CanonicalOrigin` | None in production | Absolute `http` or `https` origin without path, query, fragment, or userinfo | Yes | `ViajantesTurismo.Public.Web` | Emits canonical absolute URLs in public `/sitemap.xml` and `/robots.txt`. Local AppHost injects its HTTPS endpoint. |

Environment variable form:

| Key | Environment variable |
| --- | --- |
| `Authentication:Authority` | `Authentication__Authority` |
| `Authentication:Issuer` | `Authentication__Issuer` |
| `Authentication:ClientId` | `Authentication__ClientId` |
| `Authentication:ClientSecret` | `Authentication__ClientSecret` |
| `Authentication:DataProtection:CertificatePath` | `Authentication__DataProtection__CertificatePath` |
| `Authentication:DataProtection:CertificatePassword` | `Authentication__DataProtection__CertificatePassword` |
| `Authentication:TokenExchange:Enabled` | `Authentication__TokenExchange__Enabled` |
| `Authentication:TokenExchange:Provider` | `Authentication__TokenExchange__Provider` |
| `CatalogIntegrationEvents:IdempotencyLockDuration` | `CatalogIntegrationEvents__IdempotencyLockDuration` |
| `PublicWeb:Sitemap:CanonicalOrigin` | `PublicWeb__Sitemap__CanonicalOrigin` |

Production configuration rules:

- keep production-overridable settings visible in this section
- document default, unit/range, safety to log, and consuming project
- use `__` for environment variable hierarchy delimiters
- keep secrets out of source-controlled settings files
- validate unsafe values at startup with `ValidateOnStart`

## Feature flag governance

Feature flags are production runtime configuration only when a current feature needs controlled
rollout, a short-lived operational kill switch, a measured experiment, or a permission/entitlement
switch. Do not add speculative flags, shared flag abstractions, or flag packages before a real toggle
point exists.

Current status: this repository has no active feature flags. Add the first flag only with the feature
that consumes it, not as standalone infrastructure.

Allowed flags:

| Flag kind | Allowed when | Expected lifetime |
| --- | --- | --- |
| Release flag | Code is deployed before broad user release to reduce rollout risk. | Remove in the first cleanup PR after full rollout. |
| Ops flag | Operators need to disable optional high-risk behavior without redeploying. | Keep only while the operational risk remains. |
| Experiment flag | Product needs a measured A/B or cohort test. | Remove after the decision is made. |
| Permission flag | Access is tied to entitlement, internal preview, or beta participation. | Prefer authorization or entitlement modeling when it becomes permanent. |

Prohibited flags:

- domain invariants, validation rules, state transitions, or legal/compliance requirements
- authentication or authorization bypasses
- security, privacy, audit, or data-retention controls that must never be optional
- long-lived product variants without a cleanup owner and removal criteria
- build-time constants, route templates, telemetry names, resource names, and protocol values
- flags in `src/ViajantesTurismo.Admin.Domain`; Domain behavior must stay deterministic

Naming, defaults, ownership, and expiry:

- Use section `FeatureFlags` with PascalCase flag keys, for example
  `FeatureFlags:EnableNewBookingReview`.
- Use environment variables with `__`, for example `FeatureFlags__EnableNewBookingReview`.
- Default release, experiment, and permission flags to `false` unless the safe legacy behavior is
  the enabled path and the PR explains why.
- Default ops kill switches to the safest user-facing behavior, documented in the production table.
- Document every production flag in the production runtime configuration table with owner,
  expiry/removal criteria, consuming service, safe-to-log status, and expected default.
- Name flags for the business behavior being exposed, not the implementation detail being replaced.

Testing requirements:

- Test both disabled and enabled paths for every release, ops, and experiment flag before merge.
- Keep disabled-path tests until the flag is removed so the fallback remains valid.
- Add integration or system coverage when the flag changes routing, persistence, messaging, public
  API shape, authorization, or user-visible workflow.
- Avoid exhaustive cross-product testing for unrelated flags; test interactions only when two flags
  touch the same behavior.
- Update contract or snapshot tests for both states when a flagged endpoint changes public output.

Cleanup rules:

- The PR that adds a flag must state the cleanup trigger: release complete, experiment decision,
  incident closed, or entitlement modeled permanently.
- Remove stale flags by deleting both code paths, configuration rows, test overrides, and deployment
  variables in one focused cleanup PR.
- Prefer deleting the flag over keeping a permanently enabled branch. Permanent behavior belongs in
  normal code or authorization policy, not a release toggle.
- Keep the old safe path only while rollback remains realistic and tested.

Implementation rules:

- Prefer native .NET configuration and options for the first simple flag.
- Read flags at application/API/web edges. Pass ordinary values into deeper services when needed.
- Do not add a shared flag service, registry, factory, cache, or wrapper for a single caller.
- Do not use `Microsoft.FeatureManagement` unless the PR needs its built-in gates, filters,
  per-request snapshots, or Azure App Configuration integration now.
- Do not use an external flag service until multiple deployed services need coordinated dynamic
  changes, auditing, or non-developer operations.
- If a library or service is justified, document why native configuration is insufficient and add
  tests proving enabled and disabled behavior through normal configuration providers.

## Aspire deployment and publish mapping

Aspire deployment/publish output should preserve the same boundaries as this document: production
runtime settings are operator-facing application settings, resource references are generated from the
AppHost model, and local AppHost values stay local unless they are explicitly promoted.

| Source | Aspire/deployment mapping | Documentation location |
| --- | --- | --- |
| Production runtime option section | App setting or environment variable on the consuming deployed service | Production runtime configuration table above |
| Secret value | Secret parameter or managed secret store reference | Secret boundary docs; never example secret values |
| Connection string | Resource reference generated from `.WithReference(...)` or deployment secret/reference | Owning resource/deployment docs, not production option table |
| Endpoint/service discovery value | Resource endpoint reference generated from `.WithReference(...)` | AppHost/deployment docs |
| Existing Azure resource name | Aspire parameter used by `AsExisting(...)` or equivalent deployment customization | Deployment docs for the environment |
| Local AppHost setting | `launchSettings.json`, `ASPIRE_*`, or local environment variable | AppHost/local orchestration section |
| Library/SharedKernel option | Bound by the consuming app only if promoted to production | Library section, then production table when promoted |

Deployment rules:

- keep `src/ViajantesTurismo.AppHost` as the deployment model source for resources and references
- pass resource relationships with `.WithReference(...)` instead of copying connection strings or URLs
- use Aspire parameters for deployment-time choices such as existing Azure resource names
- document production app settings in this file before adding deployment environment variables
- document secrets by source and owning boundary; do not put secret values in docs, AppHost code, or
  source-controlled settings files
- customize generated deployment resources with publish/deployment APIs only when the setting belongs
  to infrastructure shape, not application behavior

### Option grouping and variable groups

Do not create a generic “variable group” for all library or SharedKernel options. Group settings by
the configuration section that owns the behavior and by the deployed service that consumes it.

Recommended grouping:

| Grouping level | Use for | Example |
| --- | --- | --- |
| Options section | Related application behavior | `CatalogIntegrationEvents:*` |
| Deployed service | Environment variables applied to one runtime service | Catalog API service app settings |
| Deployment parameter set | Infrastructure choices required by publish/provisioning | Existing ACA environment name/resource group |
| Secret boundary | Credentials and secret references | Database connection secret/reference |

CI/CD platforms may still use their own “variable group” feature, but that is deployment plumbing.
The repository standard remains the options section and production table. If a CI/CD variable group
is used, mirror the section names and service ownership from this document instead of inventing a
separate taxonomy.

### Ensuring every option is configured correctly

Every production-bound option must have:

1. one owning options type and section name
2. a row in the production runtime configuration table
3. a default or documented requirement to supply a value
4. startup validation with `IValidateOptions<TOptions>` or source-generated validation
5. `ValidateOnStart` in the consuming production service registration
6. a test proving the section binds through normal configuration or deployment-equivalent overrides
7. a documented environment variable name using `__`

Library options that are not production-bound need only library-level defaults and validation. Promote
them to the production table when a deployed app binds them from configuration.

## Library and SharedKernel options

Library options are consumed by application code but are not automatically production settings. They
become production settings only when an app binds them from app configuration and documents them in
the production table above.

| Area | Option/value | Classification | Decision |
| --- | --- | --- | --- |
| SharedKernel PostgreSQL event sourcing | Schema name | Provider option | Keep provider-level option and identifier validation; add app binding only when a deployed app needs environment-specific schema selection. |
| Standard HTTP resilience defaults | Resilience policy | Library policy | Defer to a resilience policy review; do not replace framework defaults speculatively. |

Library option rules:

- expose `Add{Feature}` registration methods with clear option ownership
- keep defaults close to the options type or registration site
- prefer `BindConfiguration(sectionName)` when the app owns the section path
- use `Action<TOptions>` overloads for tests or library consumers that need direct overrides
- do not place library-only options in the production table until a production app binds them

## AppHost and local orchestration configuration

AppHost values describe local Aspire orchestration and developer resources. They are not production
runtime settings unless deployment docs explicitly promote them.

| Value | Classification | Decision |
| --- | --- | --- |
| Resource names such as `cache`, `database`, and `catalog-api` | Aspire/service-discovery identity | Use `ResourceNames`; do not inline strings in AppHost orchestration code. |
| Public Web sitemap canonical origin | Local AppHost endpoint reference | AppHost injects `PublicWeb__Sitemap__CanonicalOrigin` from the Public Web HTTPS endpoint so local crawler URLs match the active Aspire endpoint. |
| Container image SHA-256 values | Local orchestration supply-chain pins | Keep as AppHost constants; not runtime configuration. |
| AppHost performance smoke variables | Local opt-in tooling | Keep as environment variables for the local performance harness. |
| Launch profile localhost ports | Developer tooling | Keep in `launchSettings.json`; not production runtime configuration. |

AppHost rules:

- model resources and dependencies in AppHost code
- use `.WithReference(...)` for connection strings and service-discovery injection
- keep resource names centralized in `src/ViajantesTurismo.Resources/ResourceNames.cs`
- document local-only opt-in variables near the AppHost or owning feature docs

## Test and tooling configuration

Test/tooling settings configure validation, analyzers, generators, benchmarks, fixtures, and local
harnesses. They must not be documented as production runtime settings.

| Value | Classification | Decision |
| --- | --- | --- |
| Analyzer `.editorconfig` options | Analyzer/tooling value | Keep in analyzer configuration. |
| Test fixture connection strings | Test value | Use in-memory configuration or test host overrides. |
| Benchmark and k6 values | Test/tooling value | Keep in the benchmark or performance-test docs. |
| Source-generator options | Build/tooling value | Keep in analyzer config or project properties. |

Test/tooling rules:

- override options through normal configuration providers or dependency injection
- do not duplicate production wiring solely for tests
- keep secrets and real production values out of test settings
- keep generated and benchmark-specific values out of production inventories

## Research baseline and presentation guidance

This policy follows official Microsoft and Aspire guidance:

- <https://learn.microsoft.com/aspnet/core/fundamentals/configuration/>
- <https://learn.microsoft.com/azure/azure-app-configuration/concept-feature-management>
- <https://learn.microsoft.com/dotnet/core/extensions/configuration>
- <https://learn.microsoft.com/dotnet/core/extensions/configuration-providers>
- <https://learn.microsoft.com/dotnet/core/extensions/options>
- <https://learn.microsoft.com/dotnet/core/extensions/options-library-authors>
- <https://learn.microsoft.com/dotnet/core/extensions/options-validation-generator>
- <https://learn.microsoft.com/dotnet/architecture/cloud-native/feature-flags>
- <https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview>
- <https://learn.microsoft.com/dotnet/aspire/app-host/configuration>
- <https://learn.microsoft.com/dotnet/aspire/deployment/azure/aca-deployment>
- <https://martinfowler.com/articles/feature-toggles.html>

Documentation structure follows these practices:

- put the operator-facing production table first
- separate app configuration from host, AppHost, library, and test/tooling configuration
- show both hierarchical keys and environment variable names
- include defaults, units, validation range, and logging safety in one scan-friendly table
- explain where non-production values live instead of mixing them with operator settings
- document secrets by storage boundary, not by example secret values
- map deployable settings from the production table to service app settings or Aspire deployment
  parameters

## Classification policy

Use the smallest durable mechanism for each value:

| Kind | Use when | Example |
| --- | --- | --- |
| Production configuration | Operators may change the value per environment | timeouts, retention windows, feature flags |
| Secret | Value grants access or identifies a credential | connection strings, API keys |
| Library option | A reusable package or SharedKernel component exposes behavior to app consumers | provider schema option |
| AppHost value | Aspire local orchestration needs a stable resource or tool value | `ResourceNames.Cache` |
| Constant | Value is a protocol, route, or internal invariant | health endpoint path |
| Domain invariant | Business rule protected by domain code | booking status transitions |
| Test/tooling value | Local-only harness or developer workflow value | smoke-test arguments |

Do not move constants into configuration just because they are strings. Do move production values
when environments need different values or operators need documented control.

## Hardcoded value policy

Review hardcoded values in production code before adding a new configuration surface.

Move a value to production configuration when:

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

## Naming, binding, and validation

- Use PascalCase section names matching the owning feature or component.
- Use descriptive key names with units in the name when the type is not self-evident.
- Use .NET `TimeSpan` options for durations and document values in round-trip `c` format.
- Bind each options type from one named section.
- Use `IValidateOptions<TOptions>` for runtime validation instead of inline validation lambdas.
- Use `ValidateOnStart` for production services where invalid configuration should fail fast.
- Use `IOptions<TOptions>` for static startup configuration.
- Use `IOptionsMonitor<TOptions>` only when the service must observe changes after startup.
- Avoid `IOptionsSnapshot<TOptions>` in singleton services.

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

## Current classification notes

| Value | Classification | Action |
| --- | --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Production configuration | Already documented in OpenTelemetry guidance. |
| Catalog projection batch size | Configuration candidate | Defer until projection runtime ownership and operational tuning needs are clear. |
| `AllowedHosts` | Hosting configuration | Review separately before changing production defaults. |
| CDN asset URLs | External asset policy | Decide asset strategy before configuring broadly. |
| Endpoint paths, route templates, and contract limits | Constant or domain/API invariant | Keep centralized constants and validation rules unless the public contract changes. |
| Feature flags | No active production flags | Add only with a current feature that needs controlled rollout, then document owner and expiry. |
