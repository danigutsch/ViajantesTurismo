# SharedKernel integration testing helpers

Shared source helpers for integration tests that need real hosted resources.

## Scope

- Aspire application startup and cleanup.
- Resource health waits with explicit timeouts and cancellation.
- PostgreSQL public-schema resets for known-baseline tests.

## Boundaries

- Keep app-specific resource names, data builders, and workflows in consuming test projects.
- Do not expose raw `IServiceProvider` or generic scope plumbing through these helpers.
- Do not add Testcontainers, Respawn, or broader lifecycle frameworks until at least two real callers need them.

## Usage

- Link the needed source files into a test project with `Compile Include`.
- Start hosted apps through `AspireTestApplication.Start<TAppHost>(...)`.
- Dispose the returned application in the fixture `DisposeAsync` path.
- Reset PostgreSQL with `PostgreSqlPublicSchemaReset.Reset(connection, ct)` before serial tests that need a known database baseline.
