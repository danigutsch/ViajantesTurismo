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
- Do not append the `Async` suffix to test or helper method names; keep names descriptive without the suffix, even when methods are asynchronous.
- Feature files (`.feature`) should follow: `<aggregate>-<capability>.feature`.
- Step definition methods should use descriptive Given/When/Then-style names with underscores.
- Prefer Arrange/Act/Assert structure with explicit section comments.
- Keep tests focused on one behavior and avoid testing implementation details.
- Before adding multi-step plumbing that is not the core behavior under test, look for an existing helper,
  builder, fixture, or page object first and extend it when appropriate instead of creating a parallel pattern.
- Prefer dedicated helper classes for reusable test helper methods instead of keeping them as
  private methods on test classes.
- Keep private methods inside a test class only when they are truly local to that class and do not
  apply anywhere else.
- Keep the behavior under test and assertions visible in the test; move only non-critical setup,
  navigation, and mechanical steps into helpers.
- Keep reusable test-only helpers close to the consuming test project, and do not move domain or
  application behavior into test helpers.
- Prefer precise assertions on business-visible outcomes over incidental implementation details.

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
- Unit tests should stay isolated from real external dependencies; prefer mocks, fakes, or test doubles.
- Integration tests: validate HTTP + persistence behavior and status codes.
- Behavior tests (`.feature`): use domain language and keep scenarios business-focused.
- Web component tests: follow bUnit patterns used in `tests/ViajantesTurismo.Admin.WebTests`.
- Reserve real browser, network, database, or container interactions for integration, behavior, or E2E tests.
- Do not silently broaden a unit test into an integration test unless that wider interaction is the behavior
being verified.
- Tests should be independent and not depend on pre-seeded data where possible.
- Prefer deterministic tests; avoid fixed delays and unnecessary timing assumptions.
- Await observable outcomes instead of using `Task.Delay` unless the delay itself is the behavior under test.

## References

- `docs/TEST_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `docs/CODE_QUALITY.md`
- `tests/BDD_GUIDE.md`
