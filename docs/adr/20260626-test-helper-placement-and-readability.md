# ADR-029: Test Helper Placement and Readability

## Context

Tests should make behavior easy to read. The arrange, act, and assert flow should show the scenario,
inputs, action, and business-visible outcome without making readers inspect hidden class members.

Helper methods and nested helper types inside an xUnit test class can hide complexity behind the test
class itself. Private helpers are especially easy to add because they feel local, but they make the
test class carry two responsibilities: specifying behavior and hosting reusable plumbing. Internal
static helpers on a test class have the same problem when multiple tests reuse them: the behavior is
spread across the class instead of being explicit in each test or isolated in a named helper type.

Nested helper types, including internal nested types, also add hidden structure to a test class and
make navigation harder. They keep complexity inside the test class instead of moving it to a named
test helper file where the abstraction can be reviewed independently.

## Decision

xUnit test classes must not declare helper members directly when those members hide reusable or
non-test behavior.

Apply these rules:

- Keep scenario-defining setup and assertions visible in the test body.
- Use local functions only for truly local logic used by one test.
- Move reusable setup, assertions, fakes, builders, and test doubles to dedicated helper types in
  their own files near the consuming test project.
- Flag private helper methods declared directly on xUnit test classes.
- Flag reused internal static helper methods declared directly on xUnit test classes.
- Flag non-public nested helper types declared directly on xUnit test classes, including internal
  nested types.

`SKTEST004` enforces this rule so contributors get fast feedback before review.

## Consequences

- Test classes stay focused on behavior, not helper hosting.
- Reusable plumbing gets a clear name and file location.
- Hidden complexity moves out of test classes and becomes easier to review.
- Small, one-off helper logic can still stay local as a local function.
- Some compact nested helper patterns are intentionally rejected to preserve readability and one
  top-level type per file.

## Alternatives

- Allow internal nested helper types in test classes. Rejected because they still hide complexity in
  the test class and weaken the readability rule.
- Allow private helper methods for one-off test setup. Rejected because local functions provide the
  same locality without expanding the test class surface.
- Suppress analyzer findings for existing tests. Rejected because this would make the rule optional
  and reduce consistency.

## Status

Accepted.

## Links

- [ADR Index](../ARCHITECTURE_DECISIONS.md)
- [Test Guidelines](../TEST_GUIDELINES.md)
