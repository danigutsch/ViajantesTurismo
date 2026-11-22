# Hosted integration and system tests

This project combines real infrastructure, HTTP and browser tests in one native xUnit v3 assembly.
Run it with `dotnet test --project tests/ViajantesTurismo.HostedTests` or run the complete solution.

The assembly fixture owns six infrastructure containers: PostgreSQL, Redis, Keycloak, ClamAV,
SeaweedFS and the OpenTelemetry Collector. It starts none of them up front: a container starts the
first time a selected test needs it (see [Demand-driven infrastructure](#demand-driven-infrastructure)).
Garage evaluation tests have been removed.
Application stacks start on demand and share those services, while keeping independent databases,
Keycloak realms and media buckets. Each test scenario owns its data. The assembly stops application
instances before disposing database leases and infrastructure. No custom runner is required.

Namespaces follow the folder layout under `ViajantesTurismo.HostedTests`; class names and traits are unchanged. Use `--filter-namespace`,
`--filter-class` or `--filter-trait` to select a seam. Playwright browser installation is required only when browser
tests run.

| Folder | Entry point and responsibility |
| --- | --- |
| Admin/Infrastructure | Direct persistence, migrations, PostgreSQL locking and constraints |
| Admin/Api | HTTP contracts and API persistence interactions |
| Admin/System | Browser workflows and semantic UI assertions |
| Catalog/Infrastructure | Persistence and storage adapters |
| Catalog/Media | Real malware scanning and SeaweedFS protocol behavior |
| Management/Security | Real security persistence, protected tickets and token storage |

Compiler isolation between these test groups is replaced by explicit namespaces and architecture
checks. Keep test bodies at their intended entry points; database setup and fault injection remain
in typed scenario helpers. Retain repository parallelism and create owned data instead of global resets.
SharedKernel owns PostgreSQL database leasing and bounded cleanup; application-specific fixtures,
configuration, authentication and business assertions stay in this project.

Use `--filter-trait Scope=system` to run the cross-deployable workflows. Unit tests of the
hosted test helpers live next to those helpers and use `Scope=unit`. All System cases use
the `system` application stack owned by the assembly fixture.

Local and CI runs use the same assembly-owned lifecycle. Every application stack leases fresh
Admin, Catalog, and security databases and receives its own Keycloak realm and media bucket.
The scanner, telemetry collector, and cache are owned by the test run. The concurrent AppHost
test creates a customer through one Admin API and verifies that the sibling API cannot read it.

## Demand-driven infrastructure

`HostedTestFixture.InitializeAsync` starts nothing, because xUnit creates an assembly fixture whenever one test is selected.
The first test that needs infrastructure makes the fixture compose one Aspire application with all six containers as
explicit-start resources. Each container then starts once, when a test first asks for it.

| What the selected tests need | Containers started |
| --- | --- |
| Only pure helpers, such as `PostgreSqlTestCleanupTests` | None, and no Aspire application |
| Only leased databases, such as the PostgreSQL persistence seams | PostgreSQL |
| `Catalog/Media` | ClamAV and SeaweedFS |
| An `Admin/Api` stack | PostgreSQL, Keycloak, ClamAV, SeaweedFS and the collector, but not Redis |
| An `Admin/System` stack or the whole project | All six |

- Start a container only through `HostedTestFixture`: starting one twice recreates it and loses its data. Read endpoints and
  connection strings through its asynchronous accessors (`GetPostgreSqlConnectionString`, `GetIdentityProviderEndpoint`,
  `GetClamAvEndpoint`, `GetSeaweedFsEndpoint`, `GetApplication`) and lease databases through `Databases`.
- Each container, the infrastructure application and each application stack has its own three-minute start budget. A start
  that fails or times out is remembered: the tests that need it fail with an error that names it, and the others are
  unaffected.
- The start runs on a token the fixture owns, so cancelling the test that triggered it abandons only that test's wait.
- The PostgreSQL start completes when the `fixture` database is healthy, which Aspire creates after the server.
- `ConformanceUserId(stack)` is valid only after the identity provider has been demanded.
- Aspire reserves each host port only inside the infrastructure application, so a container that starts late can find its port
  taken by another process, such as a parallel run. That start fails like any other; run the tests again.
- ClamAV runs with the definitions of its pinned image (`WithFreshClam(false)`), so a run downloads none. `clamd` answers a
  `PING` after about eight seconds either way.
- No hosted container mounts a named volume, so a run leaves none behind. Earlier runs left `clamav-hosted-<id>-definitions` and
  `seaweedfs-hosted-<id>-data`; remove only exact names you recognize with `podman volume rm <name>`.
- The gates are SharedKernel's `OnDemandResource<T>`, and `Databases` is built with
  `new PostgreSqlTestDatabasePool(ct => postgres.Get(ct))`, which contacts no server until the first database is created.

## PostgreSQL fixture ownership and cleanup

Use `HostedTestFixture.Databases.Create(ct)` for a test-owned database and dispose the lease with
`await using`. Catalog gallery placement and Management security persistence use those leases.
The Admin document stores use `PostgreSqlTestServerFixture`, which also leases through the shared
pool and borrows the same assembly-owned server. The full Admin API and browser cases use their
existing application stacks and the same assembly owner.

SharedKernel's lease releases the database even if the test token is cancelled. Startup and
cleanup preserve the identity of the owned resource. The first orphan-container report lacked
an owner identity and an Aspire trace, so the validation below measures the current lifecycle.
[SharedKernel fixture guide](https://github.com/danigutsch/PandaBear.SharedKernel/blob/main/src/SharedKernel/SharedKernel.IntegrationTesting/README.md).

- The default-parallel focused run passed 44 tests covering database cancellation and isolation,
  the Admin document store, Catalog gallery placement, and Management security persistence.
- Thirty fresh browser test hosts also completed against the shared fixture without manual cleanup.
- A controlled Linux interruption sent `SIGINT` while a test held an active PostgreSQL lease.
  The probe identified the container by its mapped port and Aspire creator and resource labels.
  The runner recorded cancellation, and that exact container was gone after 15.5 seconds.
  Forceful process termination relies on the container runtime and can leave a resource behind.

For a cleanup failure, inspect the runner's Aspire logs and the container with the matching creator
and resource labels. Compare the owner process and resource ID before acting on a container.
Run the four PostgreSQL coverage groups with the hosted test runner.

```bash
dotnet test --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj \
  --filter-class '*PostgreSqlTestServerFixtureTests' \
  --filter-class '*DocumentStorePostgreSqlTests' \
  --filter-class '*GalleryPlacementPostgreSqlTests' \
  --filter-class '*ManagementSecurityPersistencePostgreSqlTests'
```
