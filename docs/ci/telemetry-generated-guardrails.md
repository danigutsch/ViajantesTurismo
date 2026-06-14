# Telemetry and generated-code guardrails

This document records the current CI safety model for telemetry-contract and
source-generator-facing regressions, plus the recommended next implementation steps.

## Problem statement

The repository has two related risk areas:

- telemetry contract drift across runtime code, source generator emitters, generated outputs,
  and observability-facing tests
- generated-source regressions that compile in narrower test harnesses but fail later when a
  more realistic sample or package-consumption path exercises the output

The current CI model catches these failures, but not always in the earliest or clearest lane.

## Current guardrails

The current CI baseline already has meaningful coverage:

- `tests/SharedKernel.Observability.Tests` runs in `Fast Validation` and protects the
  repository-owned OpenTelemetry builder surface.
- `tests/SharedKernel.Mediator.GeneratorTests` runs in `Mediator Heavy Tests` and protects
  generator-emitted dispatch, dependency injection, and telemetry span behavior.
- `tests/SharedKernel.Mediator.PackageConsumptionTests` runs in `Mediator Heavy Tests` and
  compiles, runs, publishes, and AOT-publishes fresh consumer projects against the shipped
  packages.
- `tests/SharedKernel.Mediator.CodeFixes.Tests` in the same lane protects analyzer/code-fix
  behavior that can affect generated-consumer correctness.

That means the repository already has the right kinds of tests; the main question is how to
make their safety role clearer and keep them targeted.

## Current gaps

- The guardrail story is implicit. Maintainers have to infer from project names which tests
  protect telemetry contracts and generated outputs.
- Telemetry-facing and generated-sample-facing checks are split across `Fast Validation` and
  `Mediator Heavy Tests`, but the workflow docs do not present them as one coherent safety
  model.
- Package-consumption and generated sample coverage is strong, but the repository does not yet
  expose a dedicated, named CI contract saying "these projects are the generated-output
  guardrail lane."
- Later full validation still provides the clearest failure signal for some regressions because
  the earlier lane documentation and ownership model are not explicit.

## Recommended target design

Prefer the smallest guardrail model that improves clarity without adding another required CI
 lane:

1. Keep `Fast Validation` responsible for cheap runtime and observability-facing checks.
2. Keep `Mediator Heavy Tests` as the dedicated generated-output and package-consumption
   guardrail lane.
3. Make the lane contract explicit in docs and in centralized slice membership data so project
   drift is harder to introduce silently.
4. Avoid adding a brand-new workflow or another required status check unless the existing lane
   proves too slow or too noisy after clearer ownership is in place.

## Recommended concrete changes

Smallest useful implementation sequence:

1. Centralize test-slice project membership so the generated guardrail projects are declared in
   one place.
   Status: addressed by `#175`.
2. Document that `Mediator Heavy Tests` is the repository's generated-output guardrail lane.
3. Document that `SharedKernel.Observability.Tests` protects the runtime observability contract
   while `GeneratorTelemetryBehaviorTests` protects generated telemetry behavior.
4. Only after that, measure whether another narrower preflight check is still needed.

## Trade-offs

### Safety wins

- Clearer ownership reduces accidental removal of guardrail projects from the right lane.
- Generated sample/package-consumption tests stay visible as the early compile/publish safety
  net instead of feeling like incidental heavy tests.
- Telemetry contract drift is easier to reason about because runtime and generated protections
  are named explicitly.

### Cost control

- No new required check is introduced yet.
- No duplicate full-solution compile path is added.
- Existing heavy tests keep doing the expensive work they already own.

### Risks if over-designed too early

- A new dedicated workflow or required check could duplicate runtime cost without materially
  improving detection.
- A compile-only generated-sample lane might still miss the exact package-consumption or AOT
  path that the current heavy lane already covers.

## Implementation recommendation

Recommended near-term implementation:

- treat `Mediator Heavy Tests` as the canonical generated-code guardrail lane
- keep `SharedKernel.Observability.Tests` in `Fast Validation`
- update CI docs so maintainers can see the telemetry/generated split immediately
- revisit dedicated extra guardrails only if failures still escape beyond these lanes after the
  ownership model is explicit
