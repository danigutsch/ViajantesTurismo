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
- Use repository-owned extension assertions from `SharedKernel.Testing.Assertions` across tests,
  for example `actual.ShouldBe(expected)`, `actual.ShouldNotBeNull()`, `items.ShouldContain(expected)`,
  and `action.ShouldThrow<InvalidOperationException>()`.
- Use `TestAssert` only when no extension wrapper exists yet and there is a specific reason not to add one;
  direct `Xunit.Assert` is legacy-compatible only while the wrapper migration is incomplete.
- Prefer assigning computed values to locals before asserting on them; avoid embedding method calls
  directly inside assertion arguments when that makes debugging harder.
- Do not use the null-forgiving operator (`!`) in tests; assert non-null explicitly before dereferencing.

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
