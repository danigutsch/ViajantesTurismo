---
description: "Use when creating, editing, or debugging unit, integration, behavior, or web component tests in this repository. Enforces xUnit v3 + MTP execution/filtering, test naming, and AAA structure."
name: "ViajantesTurismo Test Guidelines"
applyTo: "tests/**/*.cs, tests/**/*.feature"
---

# Test Implementation Guidelines

- Follow `docs/TEST_GUIDELINES.md` for all test work.
- Use xUnit v3 with Microsoft.Testing.Platform (MTP) conventions in this repo.

## Naming and Structure

- Unit/integration/web test method names: `Method_Name_Context_Description_Expected_Behavior`.
- Feature files (`.feature`) should follow: `<aggregate>-<capability>.feature`.
- Step definition methods should use descriptive Given/When/Then-style names with underscores.
- Prefer Arrange/Act/Assert structure with explicit section comments.
- Keep tests focused on one behavior and avoid testing implementation details.

## Running and Filtering Tests

- Run all tests with `dotnet test --solution ViajantesTurismo.slnx`.
- Run one project with `dotnet test --project <path-to-csproj>`.
- Use xUnit filter switches:
    - `--filter-class`
    - `--filter-method`
    - `--filter-namespace`
    - `--filter-trait`
- Do **not** use legacy VSTest filters like `--filter "FullyQualifiedName~..."`.

## Coverage and Host Arguments

- When required by command shape, pass test-host arguments after `--`.
- Coverage pattern used in this repo:

  ```text
  dotnet test --solution ViajantesTurismo.slnx -- \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output coverage.cobertura.xml
  ```

## Test Type Conventions

- Unit tests: fast, in-memory, deterministic.
- Integration tests: validate HTTP + persistence behavior and status codes.
- Behavior tests (`.feature`): use domain language and keep scenarios business-focused.
- Web component tests: follow bUnit patterns used in `tests/ViajantesTurismo.Admin.WebTests`.
- Tests should be independent and not depend on pre-seeded data where possible.

## References

- `docs/TEST_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `docs/CODE_QUALITY.md`
- `tests/BDD_GUIDE.md`
