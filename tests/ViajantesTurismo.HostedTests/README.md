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
`--filter-class` or `--filter-trait` to select a seam. A filtered run starts only the infrastructure
its tests need. Playwright browser installation is required only when browser tests run.

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

xUnit creates an assembly fixture whenever one selected test exists, so the fixture has to stay cheap:
`HostedTestFixture.InitializeAsync` starts nothing. The first test that needs infrastructure makes the
fixture compose one Aspire application that holds all six containers as explicit-start resources
(nothing runs yet; this takes about five seconds). Each container is then started once, when a test
first asks for it, and the containers one request needs start concurrently.

| What the selected tests need | Containers started |
| --- | --- |
| Only pure helpers, such as `PostgreSqlTestCleanupTests` | None, and no Aspire application |
| Only leased databases, such as the PostgreSQL persistence seams | PostgreSQL |
| `Catalog/Media` | ClamAV and SeaweedFS |
| An `Admin/Api` stack | PostgreSQL, Keycloak, ClamAV, SeaweedFS and the collector, but not Redis |
| An `Admin/System` stack | All six |
| The whole project | All six |

The full suite still starts all six containers, so its cost does not change; the saving is for focused runs.

What to know when you write or debug a test:

- Each container has exactly one start path, guarded by a gate. Starting a container twice recreates it and
  loses its data. Never start a container outside `HostedTestFixture`, and read an endpoint or connection
  string only through the asynchronous accessors (`GetPostgreSqlConnectionString`,
  `GetIdentityProviderEndpoint`, `GetClamAvEndpoint`, `GetSeaweedFsEndpoint`, `GetApplication`) or lease
  databases through `Databases`. Before its container starts, an endpoint is a valid-looking address with
  nothing behind it.
- Each container gets its own three-minute start budget. The former fixture had one budget for all six. The
  infrastructure application and every application stack have their own three-minute budget as well.
- A start that fails or times out is remembered. Every test that needs that container fails quickly with the
  same error, which names the container, and nothing retries per test. Tests that do not need it are not
  affected, and the failure does not fail the whole assembly. The same holds for the infrastructure application
  and for an application stack: when its budget expires, the remembered error is a `TimeoutException` that names
  it (if stopping the half-started application fails too, the error lists both failures).
- The PostgreSQL start is complete when the `fixture` database is healthy, not only the server. Aspire creates
  the database after the server is ready, and the connection string that `GetPostgreSqlConnectionString` returns
  points at it. This adds about a second to a run that starts PostgreSQL.
- Aspire picks each container's host port when the infrastructure application starts and reserves it only inside
  that application, and a container binds its port when it starts. A container that starts minutes into the run
  can therefore find its port taken by another process on the same host, such as a parallel test run. That start
  fails like any other failed start and is remembered; run the tests again.
- The start runs on a token the fixture owns. Cancelling or timing out the test that triggered a start
  abandons only that test's wait; the start continues for the tests that need it next.
- A stack waits for every container it needs before it starts, so the first test of a stack pays for them
  and later tests reuse them.
- `HostedTestFixture.ConformanceUserId(stack)` is valid only after the identity provider has been demanded,
  for example through `GetApplication` or `GetIdentityProviderEndpoint`. Before that it throws an
  `InvalidOperationException` that says so.
- The gates are SharedKernel's `OnDemandResource<T>`, and `Databases` is a `PostgreSqlTestDatabasePool` built with
  its resolver constructor, `new PostgreSqlTestDatabasePool(ct => postgres.Get(ct))`. Both come from the pinned
  package, so this repository keeps no copy of either. The pool contacts no server until a test creates its first
  database.
