# Hosted integration and system tests

This project combines real infrastructure, HTTP and browser tests in one native xUnit v3 assembly.
Run it with `dotnet test --project tests/ViajantesTurismo.HostedTests` or run the complete solution.

The assembly fixture owns six infrastructure containers: PostgreSQL, Redis, Keycloak, ClamAV,
SeaweedFS and the OpenTelemetry Collector. Garage evaluation tests have been removed.
Application stacks start on demand and share those services, while keeping independent databases,
Keycloak realms and media buckets. Each test scenario owns its data. The assembly stops application
instances before disposing database leases and infrastructure. No custom runner is required.

Tests retain their original namespaces, class names and traits. Use `--filter-namespace`,
`--filter-class` or `--filter-trait` to select a seam. Infrastructure setup still runs for filtered tests.
Playwright browser installation is required only when browser tests run.

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
