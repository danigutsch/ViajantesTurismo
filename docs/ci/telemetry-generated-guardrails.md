# Telemetry and generated-code guardrails

ViajantesTurismo validates application behavior through pinned SharedKernel packages. Reusable
telemetry implementation, mediator generators, analyzers, code fixes, and package-consumption tests
belong to the [standalone library](https://github.com/danigutsch/PandaBear.SharedKernel).
Library CI owns generated-output, package-consumption, and promised NativeAOT validation; application
CI does not maintain a separate library test lane.

## Application guardrails

- `ServiceDefaultsTelemetryTests` verifies application registration exports mediator, Catalog, and
  provider tracing and metrics through the public package interfaces.
- `ServiceDefaultsPrivacyTelemetryTests` verifies exported traces and logs remove sensitive values
  while preserving operational outcomes.
- `TelemetryPrivacyConfigurationTests` guards Npgsql tracing privacy registration.
- Application domain-dispatch tests, HTTP/OpenAPI contract tests, and integration tests verify consumer
  behavior and composition against the pinned candidate.

These projects remain in the application test slices declared in `.config/sharedkernel-ci.json`.
`sharedkernel-ci select` requires every application test project to belong to exactly one slice.

## Library changes and adoption

Change reusable generators and telemetry implementations in SharedKernel, where their focused tests
and packed-consumer checks run. Adopt new immutable artifacts through the
[application compatibility workflow](../sharedkernel/development.md#adopt-a-library-change), including
application builds, tests, and the committed candidate revision and package hashes. Library validation
does not replace application contract or privacy checks, and application tests do not replace library
source-generator or NativeAOT evidence.
