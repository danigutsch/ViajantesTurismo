# PBI-2026-03-29-02 — Consolidated follow-up work items

## Metadata

- **ID:** PBI-2026-03-29-02
- **Title:** Consolidated follow-up work items
- **Priority:** Medium
- **Status:** Planned
- **Type:** Architecture / Refactoring / Developer Experience / Quality

This document consolidates six separately tracked future-work notes into one repository-backed backlog item so the
intent is preserved in a stable, reviewable file.

## Consolidated source notes

The following ideas are intentionally captured here as separate sections instead of individual files:

- `docs/backlog/PBI-2026-03-21-01-task-delay-production-code-guard.md`
- `docs/backlog/PBI-2026-03-21-02-admin-web-page-composition-reuse-strategy.md`
- `docs/backlog/PBI-2026-03-28-01-apphost-backed-api-fixture-for-coverage.md`
- `docs/backlog/PBI-2026-03-28-02-make-devcontainer-testable-locally.md`
- `docs/backlog/PBI-2026-03-28-03-hardcoded-values-options-review.md`
- `docs/backlog/PBI-2026-03-29-01-pragma-to-suppress-message.md`

## Goal

Preserve the intent of the planned work, make the next implementation steps explicit, and provide a single place where
these follow-up items can be reviewed, refined, and split into delivery-ready PBIs later if needed.

## 1. Remove `Task.Delay` from production web app flows

**Source note:** `PBI-2026-03-21-01-task-delay-production-code-guard.md`

### Problem statement

The web app should not rely on arbitrary `Task.Delay` calls in production UI flows when a more deterministic,
event-driven, or state-driven approach is available. Delay-based behavior is brittle, hard to reason about, and often
creates timing bugs that only appear under load or on slower machines.

### Desired direction

Replace `Task.Delay` usage in the web app with more robust patterns such as:

- awaiting real completion signals instead of sleeping
- reacting to component lifecycle or navigation events
- using explicit UI state transitions instead of timing guesses
- using cancellation-aware timers or dedicated abstractions only when actual timed behavior is required
- keeping testability high by avoiding hidden timing dependencies

### Investigation scope

- inventory current `Task.Delay` usage in production web app code
- classify each usage by intent: debounce, UX pause, loading synchronization, toast dismissal, retry, animation timing,
  or workaround
- replace each usage with a stronger pattern where possible
- document any remaining time-based behavior that is truly intentional and justify the abstraction used
- consider adding a repository guard so new production `Task.Delay` usage is harder to introduce accidentally

### Acceptance criteria

- production web app `Task.Delay` usage is removed or reduced to clearly justified, well-encapsulated cases
- replacements are deterministic and cancellation-aware where relevant
- any intentional timed behavior uses an explicit abstraction or documented pattern rather than ad hoc delays
- guardrails are added or documented so the same problem does not reappear silently

## 2. Standardize admin pages around reusable shared components

**Source note:** `PBI-2026-03-21-02-admin-web-page-composition-reuse-strategy.md`

### Problem statement

Admin pages such as index, details, and similar CRUD-oriented views should follow a shared composition model instead of
being repeatedly assembled from bespoke page-level markup. Repetition increases drift, styling inconsistency, and the
cost of applying improvements across the UI.

### Desired direction

Adopt a standard page composition approach where common page types are built from reusable/shared components. The goal
is not forced abstraction for its own sake, but a clean, maintainable UI structure that makes common patterns obvious.

### Investigation scope

- identify current index/details/edit/create pages that already follow a recognizable pattern
- extract shared composition primitives such as:
    - page shells
    - headings and action bars
    - filter/search areas
    - list/table wrappers
    - detail summary panels
    - empty-state and loading-state components
    - standard form sections
- define what the default page structure should look like for recurring admin scenarios
- prefer composition over inheritance-heavy patterns
- ensure extracted components remain domain-agnostic enough to be reused without becoming vague or over-configurable

### Acceptance criteria

- common admin page patterns are documented and implemented through reusable/shared components
- index/details and similar pages can be composed from the shared structure with minimal duplication
- existing pages become more consistent in layout, behavior, and styling
- the resulting component model is easier to extend and test than the current page-by-page duplication

## 3. Move API fixture/testing setup from `WebApplicationFactory` to Aspire-hosted infrastructure

**Source note:** `PBI-2026-03-28-01-apphost-backed-api-fixture-for-coverage.md`

### Problem statement

The current API fixture approach should be reviewed in light of moving from a `WebApplicationFactory`-style setup to
an Aspire-hosted setup so test execution and coverage reflect the real application topology more faithfully.

Reference context:

- `https://github.com/microsoft/aspire/issues/8499#issuecomment-3952474852`

### Desired direction

Use an Aspire-backed test fixture or host orchestration model where appropriate so integration-style validation runs
against the same service graph assumptions as the AppHost-driven application path.

### Investigation scope

- identify where `WebApplicationFactory` is currently providing useful test convenience versus where it hides real host
  behavior
- evaluate how an AppHost-backed fixture would affect:
    - startup reliability
    - test isolation
    - coverage collection
    - performance
    - local developer ergonomics
- determine which test layers should migrate and which should remain lighter-weight
- avoid replacing fast, useful tests with slower end-to-end infrastructure unless the fidelity gain is real
- document the recommended testing split once the approach is clear

### Acceptance criteria

- the preferred fixture strategy is documented and justified
- any selected Aspire-hosted fixture path is practical for local and CI execution
- coverage and service wiring are no less reliable than the current setup
- test responsibilities across unit, integration, and higher-fidelity hosted tests are clearer after the change

## 4. Make the devcontainer testable locally and share logic with the smoke test

**Source note:** `PBI-2026-03-28-02-make-devcontainer-testable-locally.md`

### Problem statement

The devcontainer path should be easy to validate locally, and the CI smoke test should reuse the same setup logic
instead of maintaining a second, drifting implementation.

### Desired direction

Create one consistent setup path for devcontainer validation that works both locally and in CI. The smoke test should
share this logic so there is a single authoritative bootstrap path.

### Investigation scope

- identify the exact setup steps required to prove the devcontainer is healthy locally
- extract shared setup logic into scripts or a documented, reusable command surface
- align the CI smoke workflow with the same logic instead of duplicating container bootstrapping behavior
- make it clear what “devcontainer testable locally” means:
    - build succeeds
    - lifecycle scripts run
    - expected toolchains are present
    - essential commands can execute inside the container
- keep the validation path lightweight enough to be practical for contributors

### Acceptance criteria

- a contributor can run a single, documented local validation flow for the devcontainer
- CI smoke validation shares the same setup logic or script surface
- local and CI behavior are consistent enough that failures are reproducible rather than mysterious
- future devcontainer changes only need to update one setup path

## 5. Review hardcoded values and decide what belongs in config

**Source note:** `PBI-2026-03-28-03-hardcoded-values-options-review.md`

### Problem statement

The codebase should be reviewed for hardcoded values to determine which ones are true constants and which should move
into configuration via `appsettings`, options binding, or environment variables.

### Desired direction

Improve configurability without blindly turning every literal into configuration. The goal is to move the right values,
not all values.

### Investigation scope

Review hardcoded values and classify them into categories such as:

- true domain constants that should remain in code
- environment-specific settings that belong in configuration
- operational settings that should be exposed via options
- secrets or sensitive values that should come from environment variables or secret stores
- UX defaults or thresholds that may deserve configuration only if they vary by environment or deployment context

Pay particular attention to:

- URLs and endpoint references
- retry settings, timeouts, polling intervals, and limits
- file paths and storage-related settings
- feature toggles or externally variable behavior
- infrastructure assumptions that currently live in code

### Acceptance criteria

- a reviewed list of candidate values is produced with a clear rationale for each keep-or-move decision
- values that should be configurable are migrated to the appropriate configuration mechanism
- configuration shape remains coherent and not over-engineered
- sensitive settings are not left hardcoded in production paths

## 6. Review current suppressions and improve them with best practices

**Source note:** `PBI-2026-03-29-01-pragma-to-suppress-message.md`

### Problem statement

The repository should review current suppressions and improve them using best practices instead of carrying broad or
poorly explained suppression patterns forward indefinitely.

### Desired direction

Audit all current suppressions and replace weak suppression patterns with narrower, better-documented, more maintainable
alternatives wherever possible.

### Investigation scope

Review suppressions such as:

- `#pragma warning disable` / `#pragma warning restore`
- `SuppressMessage` attributes
- global suppressions
- analyzer-specific configuration suppressions
- comment-only “ignore this warning” practices that should become explicit or be removed

For each suppression, determine whether it should:

- be removed because the underlying issue can be fixed
- be narrowed to the smallest relevant scope
- be replaced by a more appropriate suppression mechanism
- gain justification so future reviewers know why it exists
- be moved to configuration only if that is truly the best repository-wide policy

### Acceptance criteria

- current suppressions are inventoried and reviewed
- unnecessary suppressions are removed
- remaining suppressions are narrower, justified, and easier to maintain
- suppression practices are aligned with repository quality expectations and analyzer hygiene best practices

## Recommended execution order

A sensible order for these follow-up items is:

1. review suppressions and `Task.Delay` usage, because both reduce code quality signal and can hide deeper issues
2. review hardcoded values, because configuration boundaries affect later refactoring decisions
3. standardize admin page composition, because shared UI primitives benefit from clearer quality and config decisions
4. align devcontainer local validation with CI smoke logic, because one bootstrap path reduces environment drift
5. evaluate the AppHost-backed fixture migration once the developer and CI experience around the host model is clearer

## Suggested next step

If these items are later split back into individual PBIs, each section in this document should become its own delivery
item with concrete impacted files, dependencies, and validation steps.
