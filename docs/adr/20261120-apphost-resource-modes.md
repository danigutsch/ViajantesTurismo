# ADR-048: AppHost resource modes

## Status

Proposed for review, 2026-11-20.

## Context

The Full product profile is the AppHost default in both Aspire run and publish. Hosted tests
explicitly request System or Admin and reuse fixture-owned infrastructure. A profile selects
application surfaces. Aspire's execution context supplies the additional run or publish decision
needed by developer tooling and local identity wiring.

## Decision

- Keep product profiles and resource composition in the AppHost. Select developer companions only
  for the Full profile in run mode. Evaluate feature flags inside that selection.
- Run the local Keycloak provider and its credentials only in run mode. Shared test infrastructure
  may configure its existing OIDC authority for that run. Published applications receive authority,
  issuer, client identity and client credentials from deployment configuration.
- Keep the trusted telemetry gateway available to all local profiles, with an explicit external
  OTLP override. Use separate flags for the local observability backends and PostgreSQL metrics.
- Preserve Aspire endpoint, health, service-reference and migration completion annotations as the
  source of readiness and exposure. The fixture supplies the equivalent external infrastructure
  configuration through the same AppHost composition.
- Reuse existing Aspire and SharedKernel resource integrations. ViajantesTurismo owns the resource
  mode policy because it names this application's surfaces and testing profiles.

## Verification

The [resource mode matrix](../../src/ViajantesTurismo.AppHost/README.md#resource-modes) records
resource ownership and exposure. Architecture tests compose the default publish model with local
feature switches enabled and exercise Full, System and Admin run models. Publish retains the
application graph while omitting local identity and developer resources.

## References

- [Aspire execution context](https://aspire.dev/deployment/environments/#use-execution-context-for-run-vs-publish-decisions)
- [Runtime wiring and deployment mapping](../architecture/runtime-wiring-and-deployment.md)
- [OIDC configuration reference](../CONFIGURATION.md)
