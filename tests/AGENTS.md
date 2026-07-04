# AGENTS.md

Instructions for files under `tests/`.

This file overrides root guidance where test-specific behavior is needed.

## Scope and precedence

- Applies to all files under `tests/`.
- If instructions conflict with root `AGENTS.md`, follow this file for test work.

## Test conventions

- Follow `docs/TEST_GUIDELINES.md`.
- Use xUnit v3 with Microsoft.Testing.Platform (MTP).
- Do not append the `Async` suffix to test or helper method names; use
  descriptive names without the suffix, even when the implementation is
  asynchronous.
- Prefer test filters: `--filter-class`, `--filter-method`, `--filter-namespace`, `--filter-trait`.
- Do not use legacy VSTest filter syntax `--filter "FullyQualifiedName~..."`.
- Use Arrange/Act/Assert structure in test methods.
- Before implementing multi-step logic that is **not** the core behavior under test,
  always look for an existing helper method or helper class first.
- If no suitable helper exists and the logic is repeated or hurts readability,
  prefer creating a helper method/class and then using that helper instead of inlining the plumbing.
- Do not add helper methods, local helper functions, or nested helper types to xUnit test classes.
- Keep truly local test logic visible in the test body, or move reusable plumbing to a dedicated
  helper type near the consuming test project.
- Before adding new test plumbing, check whether the repository already has a builder,
  fixture, page object, or helper for the same concern, and extend it when appropriate
  instead of creating a parallel pattern.
- Keep the behavior under test and assertions visible in the test;
  move only non-test-critical setup/navigation/mechanical steps into helpers.
- Keep reusable test-only helpers close to the consuming test project, and do not move
  domain or application behavior into test helpers.
- Unit/integration/web test method names should be descriptive, natural-language
  phrases with underscores between words. Do not append fixed suffixes like
  `Expected_Behavior`.
- Example: `Creates_a_tour_when_the_request_is_valid`.
- Feature files should follow `<aggregate>-<capability>.feature`.
- Step definition methods should use descriptive Given/When/Then-style names with underscores.
- Prefer precise assertions on business-visible outcomes over incidental implementation details.
- New or touched maintained tests must use the repository assertion package:
  `tests/SharedKernel.Testing.Assertions` / `SharedKernel.Testing.Assertions`.
  If a test project does not already reference it, add the project reference and a global
  `<Using Include="SharedKernel.Testing.Assertions" />` (or a file `using`) instead of using
  `Xunit.Assert` directly.
- Prefer extension assertions from `SharedKernel.Testing.Assertions`, for example
  `actual.ShouldBe(expected)`, `actual.ShouldNotBeNull()`, `items.ShouldContain(expected)`,
  `text.ShouldContain(expected, StringComparison.Ordinal)`, and
  `action.ShouldThrow<InvalidOperationException>()`.
- Use `TestAssert` only as the low-level repository assertion surface when no extension wrapper exists
  yet and there is a specific reason not to add one.
- Direct `Xunit.Assert` is allowed only inside `tests/SharedKernel.Testing.Assertions` wrapper
  implementation, generated/sample/non-maintained test assets, or a documented temporary migration
  exception. Do not add new direct `Assert.*` calls to maintained tests.
- Prefer assigning computed values to locals before asserting on them; avoid embedding method calls
  directly inside assertion arguments when that makes debugging harder.
- Do not use the null-forgiving operator (`!`) in tests to dereference values or feed assertions;
  assert non-null explicitly before dereferencing. A narrow exception is allowed when intentionally
  passing `null` into a null-guard test, where `null!` keeps the guard behavior visible without
  weakening nullable flow elsewhere. The operator is also acceptable inside generated-code/source-
  template strings and for test-framework or fixture-injected fields that are initialized outside
  the constructor; prefer constructor initialization whenever practical.

## Test independence

- Tests should be independent and not rely on pre-seeded data where possible.
- Prefer creating data inside each test and asserting only on that test-owned data.
- Avoid cross-test dependencies and shared mutable state.
- Keep seed-dependent tests only when intentional, and document the reason.
- Prefer deterministic tests; avoid fixed delays and unnecessary timing assumptions.
- Await observable outcomes instead of using `Task.Delay` unless the delay itself is the behavior under test.

## Running tests

- Run all tests: `dotnet test --solution ViajantesTurismo.slnx`.
- Run one test project: `dotnet test --project <path-to-csproj>`.
- Pass test-host args after `--` when required by command shape.

## Test boundaries

- Keep unit tests isolated from real external dependencies; use mocks, fakes, or test doubles where appropriate.
- Reserve real browser, network, database, or container interactions for integration, behavior, or system tests.
- Do not silently broaden a test from unit scope to integration scope
  unless that wider interaction is the behavior being verified.

## References

- `docs/TEST_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `tests/BDD_GUIDE.md`
