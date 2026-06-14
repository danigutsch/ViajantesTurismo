# ViajantesTurismo.Admin.ContractTests

Contract tests for Admin-facing public or external boundaries that matter independently of broader runtime behavior.

Use this project when the thing being protected is the published contract itself.

## What belongs here

- generated OpenAPI compatibility checks
- canonical-versus-generated artifact drift guards
- serialized request or response payload shape checks
- schema or consumer-provider compatibility slices for one Admin boundary
- consumer/provider compatibility validation rooted in the canonical committed contract artifact

## What does not belong here

- database-backed API behavior coverage
- request lifecycle or persistence assertions that belong in `ViajantesTurismo.Admin.IntegrationTests`
- browser or hosted UI flows that belong in `ViajantesTurismo.Admin.SystemTests` or `ViajantesTurismo.Admin.UiIntegrationTests`
- broad end-to-end coverage of every Admin endpoint family in one test

## Admin seam and entrypoints

The allowed SUT seam in this project is a published Admin API contract artifact or another explicit external boundary.

For the first real slice, that seam is:

- the canonical `tours.openapi.json` artifact under `src/ViajantesTurismo.Admin.Contracts/OpenApi/`
- consumed as the published contract artifact, not through direct endpoint method calls
- validated through a consumer-owned contract slice that reads only the fields this consumer depends on

Generated Admin boundary artifacts under `src/ViajantesTurismo.Admin.Contracts/OpenApi/.generated/` are allowed as
comparison inputs for drift guards, but the committed canonical artifacts remain the published source of truth.

Not allowed for this project:

- direct calls to business endpoints such as `/tours`, `/bookings`, or `/customers` to prove behavior
- database seeding or reset flows for contract-only tests
- generic DI reach-through from tests

## First slice

The first implemented contract boundary is the `Tours` OpenAPI document.

Why this slice:

- it is public and consumer-visible
- it is narrower and more durable than a full API behavior suite
- it protects the consumer-owned OpenAPI shape without duplicating integration behavior tests
- it stays aligned with the committed canonical artifact already packed from `ViajantesTurismo.Admin.Contracts`
