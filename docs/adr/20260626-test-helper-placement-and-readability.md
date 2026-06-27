# ADR-029: Test Helper Placement and Readability

## Context

Tests should make behavior easy to read. The arrange, act, and assert flow should show the scenario,
inputs, action, and business-visible outcome without making readers inspect hidden class members.

Helper methods, local helper functions, and nested helper types inside xUnit tests can hide
complexity behind another layer. Private and internal helpers are common forms of this pattern, but
accessibility is not the core issue. The issue is that the test carries two responsibilities:
specifying behavior and hosting reusable plumbing.

Nested helper types also add hidden structure to a test class and make navigation harder. They keep
complexity inside the test class instead of moving it to a named test helper file where the
abstraction can be reviewed independently.

## Decision

xUnit test classes must not declare helper members directly.

Apply these rules:

- Keep scenario-defining setup and assertions visible in the test body.
- Keep one-off behavior and assertions inline in the test body instead of hiding them in local
  functions.
- Move reusable setup, assertions, fakes, builders, and test doubles to dedicated helper types in
  their own files near the consuming test project.
- Flag helper methods declared directly on xUnit test classes, regardless of accessibility.
- Flag nested helper types declared directly on xUnit test classes, regardless of accessibility.
- Flag local helper functions declared inside xUnit test methods.

`SKTEST004` enforces this rule so contributors get fast feedback before review.

## Consequences

- Test classes stay focused on behavior, not helper hosting.
- Reusable plumbing gets a clear name and file location.
- Hidden complexity moves out of test classes and becomes easier to review.
- Small, one-off logic stays visible in the test body instead of moving behind a local helper
  function.
- Some compact nested helper patterns are intentionally rejected to preserve readability and one
  top-level type per file.

## Alternatives

- Allow internal nested helper types in test classes. Rejected because they still hide complexity in
  the test class and weaken the readability rule.
- Allow public or internal helper methods in test classes. Rejected because accessibility does not
  change whether the method hides test plumbing in the test class.
- Allow private helper methods or local functions for one-off test setup. Rejected because they still
  hide behavior behind another layer inside the test.
- Suppress analyzer findings for existing tests. Rejected because this would make the rule optional
  and reduce consistency.

## Status

Accepted.

## Links

- [ADR Index](../ARCHITECTURE_DECISIONS.md)
- [Test Guidelines](../TEST_GUIDELINES.md)
