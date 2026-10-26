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

Tests retain their original namespaces, class names and traits. Use `--filter-namespace`,
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
