# AGENTS.md

Instructions for files under `tests/ViajantesTurismo.Admin.SystemTests/`.

This file narrows the broader `tests/AGENTS.md` guidance for Playwright-based system-test work.

## Scope and precedence

- Applies to all files under `tests/ViajantesTurismo.Admin.SystemTests/`.
- If instructions conflict with `tests/AGENTS.md`, follow this file for system-test-specific work.

## System-test design rules

- Prefer owned-data tests created through `ApiTestExtensions`.
- Default to direct route navigation by known IDs when the grid or list is not the behavior under test.
- Avoid first-row assumptions, seeded-name assumptions, and page-1 assumptions.
- Do not introduce helpers that scan paginator pages until a matching row is found.
- If a list/grid assertion is required, use deterministic row targeting.
- Prefer semantic browser assertions on visible business state over brittle CSS-only checks.

## Specialized helper classes

Use the existing specialized helpers before introducing new plumbing:

- `*Extensions` for API-assisted setup and server-side state changes
- `*Workflow` helper classes for repeated multi-step UI flows
- `*Page` helper classes for deterministic interaction with specific screens or paginated grids
- `*Helpers` for shared semantic locator and interaction helpers

When adding a new helper:

- prefer a dedicated helper class if the logic can be reused across tests
- keep helper methods close to this system-test project
- do not append the `Async` suffix to helper method names
- keep scenario-defining assertions in the test body

## Refactor heuristics

- If a comment describes a multi-step block of UI plumbing, that block is a likely helper candidate.
- If the helper would only hide unstable lookup or pagination traversal, redesign the test instead.
- If direct navigation by known ID is possible, prefer it over rediscovering the entity from a list.

## Validation guidance

- After changing system-test helpers or tests, run the narrowest relevant system test or test class first.
- Prefer `--filter-method` or `--filter-class` when validating changes.
- Watch for stale `.NET Host` file locks after Playwright/Aspire runs; clear them before rerunning if needed.

## References

- `tests/AGENTS.md`
- `tests/ViajantesTurismo.Admin.SystemTests/README.md`
- `docs/TEST_GUIDELINES.md`
