# AGENTS.md

Instructions for files under `src/ViajantesTurismo.Admin.Contracts.Application/`.

## Scope and precedence

- Applies to all files under `src/ViajantesTurismo.Admin.Contracts.Application/`.
- If instructions conflict with higher-level `AGENTS.md` files, follow this file for Contracts-layer work.
- HTTP clients and OpenAPI artifacts belong in `ViajantesTurismo.Admin.Contracts.Http`.
- Integration-event contracts belong in `ViajantesTurismo.Admin.Contracts.IntegrationEvents`.

## DTO naming and shape

- Create request DTOs use `Create*Dto`.
- Update request DTOs use `Update*Dto`.
- Read/response DTOs use `Get*Dto`.
- Nested value DTOs use `*Dto`.
- Prefer `sealed record` DTOs with required `init` properties.

## Validation

- Use built-in `DataAnnotations` attributes directly on DTO properties.
- Use `IValidatableObject` for cross-property rules.
- Keep rule logic in dedicated `*Validation` helper classes when shared.
- Do not create custom validation attribute subclasses.

## Contract constants

- Keep external DTO and request-validation limits in `ContractConstants`.
- Do not reference `ContractConstants` from Domain.
- Domain and Infrastructure use domain-owned limit types for domain/persistence constraints.

## API client interfaces

- Keep `I*ApiClient` interfaces in `ViajantesTurismo.Admin.Contracts.Http` so Web and test projects can depend on shared HTTP abstractions.
- Use `Task`-based methods with `CancellationToken ct` as the last parameter.

## AOT safety

- Maintain AOT-compatible patterns in Contracts.
- Avoid reflection-heavy or dynamic runtime behavior.

## References

- `AGENTS.md` (repository root)
- `docs/CODING_GUIDELINES.md`
