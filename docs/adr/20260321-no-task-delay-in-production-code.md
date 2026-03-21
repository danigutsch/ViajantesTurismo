# ADR-019: No Direct Task.Delay in Production Code

**Status**: Accepted — 2026-03-21

## Context

The repository currently contains direct `Task.Delay` usage in production code under `src/`, including interactive UI
components. Although some of these uses are small and user-facing, they encode business or UI flow with arbitrary time
delays instead of explicit state transitions, callbacks, owned timers, or testable abstractions.

In this codebase, direct timing waits in production paths create several problems:

- they hide lifecycle and coordination problems behind elapsed time
- they make behavior timing-sensitive and harder to reason about
- they encourage copy-paste workarounds in nearby code
- they reduce determinism in tests and troubleshooting
- they couple UX behavior to arbitrary waits instead of state changes

The issue is architectural, not only stylistic. The repository needs a clear rule so reviewers and contributors do not
have to rediscover this standard case by case.

## Decision

We do not allow direct `Task.Delay` usage in production code under `src/` as a normal implementation pattern.

Production code should instead prefer one of the following:

- explicit state transitions driven by user actions or application events
- component or service abstractions that model time intentionally and can be tested deterministically
- owned timers or background work with explicit lifecycle management where time-based behavior is truly required
- framework-native mechanisms that express UI state changes without arbitrary sleeps

Tests under `tests/` are not covered by this ADR. Test code may still use delay-based constructs when there is no
better deterministic mechanism, although arbitrary waits remain discouraged there as well.

If a production scenario temporarily requires direct `Task.Delay`, it must be treated as an explicit exception with:

- a documented rationale
- a linked backlog item or remediation plan
- a narrow scope
- a removal condition

## Consequences

### Pros

- Pushes the codebase toward explicit, event-driven, and testable control flow.
- Reduces timing-based flakiness and hidden coordination bugs.
- Gives reviewers a clear standard for rejecting delay-based production fixes.
- Establishes a policy foundation for automated repository checks.

### Cons

- Some UI flows may require refactoring rather than a quick timing workaround.
- A few existing usages will need follow-up work or temporary exceptions before the policy is fully enforced.
- Enforcing the rule may require a repository-owned guard, analyzer, or architecture test.

## Alternatives Considered

1. **Allow `Task.Delay` when a developer judges it harmless**
   Rejected: too subjective, inconsistent in reviews, and likely to spread.

2. **Rely on Sonar or ad hoc review comments only**
   Rejected: the repository needs a project-specific rule that expresses intent clearly and can be automated.

3. **Ban all timer-related constructs everywhere, including tests**
   Rejected: too broad for the immediate problem and likely to create unnecessary friction.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [PBI-2026-03-21-01 — Add a repository guard against Task.Delay in production code](../backlog/PBI-2026-03-21-01-task-delay-production-code-guard.md)
