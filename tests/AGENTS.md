# AGENTS.md

Instructions for files under `tests/`.

This file overrides root guidance where test-specific behavior is needed.

## Scope and precedence

- Applies to all files under `tests/`.
- If instructions conflict with root `AGENTS.md`, follow this file for test work.

## Test conventions

- Follow `docs/TEST_GUIDELINES.md`.
- Use xUnit v3 with Microsoft.Testing.Platform (MTP).
- Prefer test filters: `--filter-class`, `--filter-method`, `--filter-namespace`, `--filter-trait`.
- Do not use legacy VSTest filter syntax `--filter "FullyQualifiedName~..."`.
- Use Arrange/Act/Assert structure in test methods.
- Unit/integration/web test method names should follow
  `Method_Name_Context_Description_Expected_Behavior`.
- Feature files should follow `<aggregate>-<capability>.feature`.
- Step definition methods should use descriptive Given/When/Then-style names with underscores.

## Test independence

- Tests should be independent and not rely on pre-seeded data where possible.
- Prefer creating data inside each test and asserting only on that test-owned data.
- Avoid cross-test dependencies and shared mutable state.
- Keep seed-dependent tests only when intentional, and document the reason.

## Running tests

- Run all tests: `dotnet test --solution ViajantesTurismo.slnx`.
- Run one test project: `dotnet test --project <path-to-csproj>`.
- Pass test-host args after `--` when required by command shape.

## References

- `docs/TEST_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `tests/BDD_GUIDE.md`
