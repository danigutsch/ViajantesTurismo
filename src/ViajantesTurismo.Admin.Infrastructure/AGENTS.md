# AGENTS.md

Instructions for files under `src/ViajantesTurismo.Admin.Infrastructure/`.

## Scope and precedence

- Applies to all files under `src/ViajantesTurismo.Admin.Infrastructure/`.
- If instructions conflict with higher-level `AGENTS.md` files, follow this file for Infrastructure-layer work.

## DbContext model

- Use separate write and read DbContext responsibilities.
- Write context is authoritative for aggregate persistence and unit-of-work commit.
- Read context remains no-tracking for query-side DTO projections.

## Store implementations

- Keep one internal sealed store per aggregate abstraction.
- Include the required aggregate graph for domain operations.
- Use split queries where multiple collection navigations are loaded.
- Keep add/delete operations simple and synchronous where EF does not require async.

## EF Core configuration

- Keep one `IEntityTypeConfiguration<T>` class per entity/value object mapping concern.
- Use `ContractConstants` for lengths and constraints.
- Configure owned/value objects explicitly.
- Configure key generation and delete behavior intentionally.

## Query services

- Query services return DTOs only, never domain entities.
- Keep projection mapping explicit and deterministic.

## Migrations and DI

- Keep migrations in the Infrastructure project.
- Register contexts, stores, query service, and unit-of-work in Infrastructure dependency injection.

## References

- `AGENTS.md` (repository root)
- `docs/CODING_GUIDELINES.md`
- `docs/ARCHITECTURE_DECISIONS.md`
