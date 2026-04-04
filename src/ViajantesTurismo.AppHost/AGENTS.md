# AGENTS.md

Instructions for files under `src/ViajantesTurismo.AppHost/`.

## Scope and precedence

- Applies to all files under `src/ViajantesTurismo.AppHost/`.
- If instructions conflict with a higher-level `AGENTS.md`, follow this file for AppHost orchestration work.

## Resource naming

- All resource names must come from `ResourceNames` in `src/ViajantesTurismo.Resources/`.
- Never hardcode resource name strings inline in AppHost orchestration code.

## Resource declaration order

Declare resources in dependency order:

1. Infrastructure resources (database, cache)
2. Migration/setup services
3. Backend API services
4. Frontend/web services

## WaitFor and WaitForCompletion

- Use `.WaitFor(resource)` when a service needs the dependency to be running before start.
- Use `.WaitForCompletion(resource)` when a service must wait for a dependency to finish cleanly (for example, migration services).
- Services that depend on fully migrated state should wait for migration completion.

## Health checks and endpoints

- Register `.WithHttpHealthCheck("/health")` for every HTTP project resource.
- Only frontend web projects should expose external endpoints with `.WithExternalHttpEndpoints()`.
- Backend and infrastructure services should stay internal and use service discovery.

## Service references and dev tooling

- Use `.WithReference(resource)` for connection strings and service discovery injection.
- Attach dev tooling with companion methods where applicable (`.WithPgWeb()`, `.WithRedisInsight()`).

## New integrations

Before adding a new integration:

1. List available integrations.
2. Read integration docs.
3. Prefer versions compatible with the repo Aspire AppHost SDK.
4. Add required package references to the AppHost project.

## References

- `AGENTS.md` (repository root)
- `docs/CODING_GUIDELINES.md`
- `docs/ARCHITECTURE_DECISIONS.md`
