# Project test coverage

The [solution](../../ViajantesTurismo.slnx) contains 29 production projects under `src/`.
The inventory names the acceptance layers that own each project's behavior and gives a representative
test or suite. Project names in the owner column omit the common `ViajantesTurismo.` prefix.
The [coverage ownership policy](../COVERAGE_OWNERSHIP.md) defines what counts as direct coverage.

Units and isolated application, component, and HTTP tests verify behavior without an AppHost resource
stack. Contract tests own published shapes and client compatibility. PostgreSQL, external providers,
and browser journeys belong in hosted suites when those boundaries are part of the behavior.
Architecture tests verify project, registration, and topology rules through the solution.

| Production project | Expected layers | Test owners and evidence | Gap disposition |
| --- | --- | --- | --- |
| `ViajantesTurismo.Admin.Api` | Isolated HTTP, contract, hosted integration | `Admin.ApiServiceTests` owns endpoint and security behavior, `Admin.ContractTests` owns generated OpenAPI compatibility, and `HostedTests` owns real identity and database flows. | Keep API policy and wire contracts in their own suites and reserve hosted tests for real dependencies. |
| `ViajantesTurismo.Admin.Application` | Unit, behavior, provider integration | `Admin.UnitTests` owns command and query decisions, `Admin.BehaviorTests` owns use-case scenarios, and `HostedTests` verifies real persistence and transactions. | Extend the existing use-case or provider suite for a new acceptance rule. |
| `ViajantesTurismo.Admin.Contracts.Application` | Contract | `Admin.ContractTests` exercises application DTOs through its bookings, document, and customer client tests. | Preserve compatibility in the existing contract suite. |
| `ViajantesTurismo.Admin.Contracts.Http` | Contract | `Admin.ContractTests` owns generated artifact and typed API client compatibility. | Keep published HTTP shape checks in the existing contract suite. |
| `ViajantesTurismo.Admin.Contracts.IntegrationEvents` | Contract, hosted consumer compatibility | `Admin.ContractTests` covers event names, versions, source-generated JSON, and AsyncAPI artifacts. `HostedTests` proves real delivery and projection. | Maintain producer shape and cross-context delivery checks at their respective boundaries. |
| `ViajantesTurismo.Admin.Domain` | Unit, behavior | `Admin.UnitTests/Domain` covers aggregate rules. `Admin.BehaviorTests` exercises the rules through the application. | The existing project-reference exception records transitive access from the owning test suites. The domain behavior has focused tests. |
| `ViajantesTurismo.Admin.Infrastructure` | Unit, PostgreSQL integration | `Admin.UnitTests` exercises mappings, event dispatch, and composition. `HostedTests` verifies transaction, concurrency, audit, and migration behavior against PostgreSQL. | Keep provider behavior in the hosted integration suite and reuse its fixtures. |
| `ViajantesTurismo.Admin.IntegrationEventWorker` | Unit composition, hosted integration | `Admin.UnitTests` covers worker registration. `HostedTests` verifies Branding projection and replay against PostgreSQL. | For runtime registration exclusivity, see the transport audit below. |
| `ViajantesTurismo.AppHost` | Architecture, hosted smoke and system | `ArchitectureTests` checks application topology, run and publish composition, and test profile wiring. `HostedTests` starts the real resource profiles. | Keep declarative wiring in architecture tests and use the existing hosted profiles for runtime checks. |
| `ViajantesTurismo.Architecture.Analyzers` | Analyzer tests | `Architecture.Analyzers.Tests` owns naming and production HTTP diagnostics. | Add rule cases to the existing analyzer suite when a diagnostic changes. |
| `ViajantesTurismo.Branding.AspNetCore` | Isolated HTTP, real HTTP wire, contract | `Catalog.ApiServiceTests` owns authorization, endpoint behavior, and Kestrel upload checks. `Branding.ContractTests` owns OpenAPI drift. | The existing project-reference exception records coverage through the Catalog host, where Branding routes are registered. |
| `ViajantesTurismo.Branding.Contracts.Http` | Contract | `Branding.ContractTests` covers configured clients, theme values, and generated OpenAPI compatibility. | Keep wire compatibility in the existing contract suite. |
| `ViajantesTurismo.Branding.Contracts.IntegrationEvents` | Contract, hosted consumer compatibility | `Branding.ContractTests` covers state transfer. `Admin.UnitTests` covers generated JSON and projection rules, and `HostedTests` verifies persisted projections. | Maintain independent contract and provider checks for event revisions. |
| `ViajantesTurismo.Branding.Infrastructure` | Isolated storage and validation, PostgreSQL integration | `Branding.InfrastructureTests` owns logo intake, paths, publication, and EF store behavior. `HostedTests` covers PostgreSQL migration and deployment schema. | Keep provider-specific regressions in the hosted integration suite. |
| `ViajantesTurismo.Catalog.Api` | Isolated HTTP, contract, hosted system | `Catalog.ApiServiceTests` owns auth, endpoint behavior, and upload wire tests. `Catalog.ContractTests` owns OpenAPI and clients, and `HostedTests` verifies cross-host reads. | Keep isolated Catalog API tests in Fast Validation. |
| `ViajantesTurismo.Catalog.Application` | Unit, behavior, hosted acceptance | `Catalog.UnitTests` covers use cases and projection logic, `Catalog.BehaviorTests` owns isolated Catalog scenarios, and `HostedTests` covers the real projection. | Preserve focused behavior coverage alongside system delivery. |
| `ViajantesTurismo.Catalog.Contracts.Application` | Contract | `Catalog.ContractTests` exercises request and result DTOs through tours and public content clients. | Extend the same contract suite when the application DTO shape changes. |
| `ViajantesTurismo.Catalog.Contracts.Http` | Contract | `Catalog.ContractTests` covers media, upload, and tour OpenAPI shapes plus typed client behavior. | Keep published HTTP shape checks in the existing contract suite. |
| `ViajantesTurismo.Catalog.Contracts.IntegrationEvents` | Contract, isolated consumer, hosted projection | `Admin.ContractTests` owns AsyncAPI coverage across the two contexts. `Catalog.UnitTests` exercises serialized Catalog events and handlers. `HostedTests` covers the real Admin-to-Catalog projection. | Maintain serialized compatibility and projection behavior separately. |
| `ViajantesTurismo.Catalog.Domain` | Unit, behavior | `Catalog.UnitTests` owns tour state, presentation and projection rules, and `Catalog.BehaviorTests` exercises resulting use-case behavior. | Keep pure rules in the fast unit suite. |
| `ViajantesTurismo.Catalog.Infrastructure` | Isolated adapters, provider integration | `Catalog.UnitTests` covers store adapters, outbox and inbox registration, and telemetry. `HostedTests` owns PostgreSQL locks and storage provider integration. | Keep real provider tests with the existing hosted fixtures. |
| `ViajantesTurismo.Catalog.IntegrationEventWorker` | Unit composition, hosted integration | `Catalog.UnitTests` covers worker startup and consumer registration. `HostedTests` covers transport delivery and replay through the system profile. | For runtime registration exclusivity, see the transport audit below. |
| `ViajantesTurismo.DatabaseObservability` | Component registration, architecture | `DatabaseObservability.Tests` verifies configuration and hosted service registration. `ArchitectureTests` verifies dedicated connection strings and migration ordering. | SharedKernel owns the index-health implementation. Real-server acceptance belongs to the [PostgreSQL observability work](../architecture/postgresql-observability.md). |
| `ViajantesTurismo.Management.Security` | Unit composition, PostgreSQL integration | `Admin.UnitTests` covers context factory and persistence wiring. `HostedTests` covers real session locks and persistence. | Extend the existing provider tests for persistence guarantees. |
| `ViajantesTurismo.Management.Web` | Component, HTTP integration, browser system | `Management.WebTests` owns page behavior and BFF integration. `HostedTests` covers browser flows and real Admin interactions. | Keep component, HTTP, and browser cases in their existing suite owners. |
| `ViajantesTurismo.MigrationService` | Unit orchestration, PostgreSQL integration, hosted startup | `Admin.UnitTests` covers one-shot initialization and process results. `HostedTests` exercises real migrations and starts dependents after migration completion. | Forward and recovery rehearsal belongs to the database migration CI scope. |
| `ViajantesTurismo.Public.Web` | Component, HTTP integration, browser system | `Public.WebTests` owns components, endpoint, and client pipeline behavior. `HostedTests` verifies real Catalog-backed public listing and details. | Preserve the client-fake seam for component tests and reserve AppHost for the cross-host path. |
| `ViajantesTurismo.RepositoryDocumentation` | Architecture, contract | `ArchitectureTests` checks the architecture catalog and data ownership diagram. `Admin.ContractTests` checks generated AsyncAPI against the integration event contracts. | Keep repository-specific documentation inputs here and common renderers in SharedKernel. |
| `ViajantesTurismo.Resources` | Architecture, contract | `ArchitectureTests` exercises resource identifiers through AppHost topology. `Admin.ContractTests` verifies shared hosted profile arguments. | Cover shared identifiers at their consuming boundaries since the project carries configuration vocabulary. |
| `ViajantesTurismo.ServiceDefaults` | Component host, architecture | `ServiceDefaults.Tests` verifies host options, telemetry and privacy. `ArchitectureTests` verifies configured consumers and Collector boundaries. | Keep app-owned registration coverage here and reusable defaults coverage with SharedKernel. |

The architecture suite checks the inventory against all production project names and requires one row
per project. Its existing direct-owner matrix checks project references, allowed test categories,
and named exceptions. These checks use the solution and inventory as their inputs. A Roslyn analyzer
would run at the individual project boundary, where those inputs and other projects' references would
need separate distribution.
[Coverage matrix tests](../../tests/ViajantesTurismo.ArchitectureTests/Conventions/TestCoverageMatrixTests.cs)

## Non-production solution projects

- `Admin.Testing`, `Catalog.Testing`, and `UploadWire.Testing` are test fixture libraries.
  Exercising their fixtures belongs to the consuming test suites.
- `FileScanning.Benchmarks` and `FileUpload.BenchmarkHost` are measurement tooling.
  The application scanning and upload assertions remain with their regular test owners.
- The application consumes SharedKernel through [pinned packages](../../.config/sharedkernel-candidate.json).
  SharedKernel CI owns the [implementation tests](https://github.com/danigutsch/PandaBear.SharedKernel/tree/d1a0676a24a8c0fbe876cf0986bf9db378c6377d/tests).
  The rows above record how the application tests its composed package boundaries.

## Cross-context integration event coverage

The four management-to-public system tests use the System AppHost profile and cover Admin API
creation, observable Catalog projection,
public listing and detail rendering after publication, and replay of a processed transport row.
Replay checks both the Catalog event stream count and one public-facing API result for the tour.
[System tests](../../tests/ViajantesTurismo.HostedTests/Admin/System/PostTransportValidation/PostTransportValidationTests.cs)

| Boundary from the original audit | Verified ownership and disposition |
| --- | --- |
| Admin domain to publication and Catalog application | `TourCreatedDomainEventHandlerTests` proves outbox mapping and `AdminTourCreatedIntegrationHandlerTests` exercises Catalog projection and idempotency. The system journey joins these through production transport. Maintain the fast tests for targeted failures. |
| PostgreSQL concurrent claims, consumer and event uniqueness, retry | The pinned SharedKernel EF Core suite exercises disjoint concurrent claims, duplicate rejection, and retry. VT's system replay scenario supplies consumer-side evidence against the real schema. Keep transport mechanics with [SharedKernel's PostgreSQL tests](https://github.com/danigutsch/PandaBear.SharedKernel/blob/d1a0676a24a8c0fbe876cf0986bf9db378c6377d/tests/SharedKernel.EntityFrameworkCore.Tests/PostgreSqlIntegrationEventTransportTests.cs). |
| Producer and consumer JSON metadata | `AdminTourCreatedIntegrationEventContractTests` proves producer JSON shape through source-generated metadata. `GeneratedIntegrationEventSerializerTests` proves registration for writing the payload and the system journey exercises consumption. Keep a fast consumer-side serialized fixture as a follow-up when testing independent schema evolution becomes necessary. |
| Worker registration and topology | `CatalogIntegrationEventTransportRegistrationTests` proves normal API composition omits and standalone worker composition includes the transport consumer, plus the explicit API-hosted mode. `WorkerOwnershipTests` proves project isolation. The system journey verifies delivery with both hosts running. Runtime attribution to exactly one consumer process remains an integration coverage gap. An assertion against the existing hosted worker/API setup would own that guarantee. |
| Catalog API and Public Web | The hosted system tests query the real Catalog API and use Playwright against Public Web. Isolated API and component tests still own their narrower behaviors. Keep the existing cross-context journey. |

## CI timing

The following successful GitHub Actions jobs are wall time from job start to completion. Their run
links preserve the exact revision and job evidence.

| CI run | Fast Validation 1 | Fast Validation 2 | Hosted Integration and System Tests |
| --- | --- | --- | --- |
| [Taxonomy](https://github.com/danigutsch/ViajantesTurismo/actions/runs/44268061231) | 4m 56s | 3m 42s | 7m 18s |
| [Request limits](https://github.com/danigutsch/ViajantesTurismo/actions/runs/44266492985) | 3m 50s | 4m 36s | 9m 32s |
| [Capacity transactions](https://github.com/danigutsch/ViajantesTurismo/actions/runs/44187075953) | 3m 39s | 4m 06s | 7m 42s |

The taxonomy run's `phase-timings.tsv` artifacts report test phases of 45.7s and 49.7s for the two
fast jobs and 266.4s for the hosted job. Chromium setup in that run took 13.0s. The fast logs report
Catalog unit, API, and behavior project executions of 14.4s, 31.8s, and 3.9s respectively.
The hosted job ran all 294 tests successfully. The phase measurements include each test invocation's
runner overhead and the projects ran within their existing jobs.
[Taxonomy CI run artifacts](https://github.com/danigutsch/ViajantesTurismo/actions/runs/44268061231#artifacts)

The measured Catalog suites fit the current Fast Validation jobs. Catalog's PostgreSQL, storage
provider, and real browser tests already run with the shared hosted setup. The audit retains those
boundaries. [CI slice configuration](../../.config/sharedkernel-ci.json)
