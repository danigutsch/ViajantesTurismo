# Mutation Testing Evaluation

This document records the current decision on mutation testing for ViajantesTurismo,
focusing on `Stryker.NET` compatibility with the repository's test stack.

## Current repository test stack

The repository currently standardizes on:

- xUnit v3
- Microsoft.Testing.Platform (MTP)
- `.NET 10` with `global.json` test-runner configuration:
    - `"test": { "runner": "Microsoft.Testing.Platform" }`
- `xunit.v3.mtp-v2` across test projects via central package management

Relevant local references:

- `global.json`
- `Directory.Packages.props`
- `docs/TEST_GUIDELINES.md`

## Upstream Stryker.NET support status

### Current evidence

- Stryker.NET documentation exposes an `mtp` test-runner option, but marks Microsoft
  Testing Platform support as **preview**.
- `dotnet-stryker` `4.15.0` can run contained xUnit v3 + MTP smoke targets in this repository
  when `test-runner` is set to `mtp` explicitly.
- Upstream compatibility reports indicate that Stryker.NET can still struggle with xUnit v3
  even without MTP enabled, producing unexpected test-case warnings and unusable mutation
  results.
- This repository uses the stricter combination of xUnit v3 **and** MTP across the suite,
  which increases compatibility risk rather than reducing it.

### Practical implication

Even though Stryker.NET has an `mtp` mode that can run contained smoke targets, current upstream
evidence does not justify treating xUnit v3 + MTP mutation runs as broadly trustworthy for this
repository.

## Decision

Mutation testing with `Stryker.NET` should remain **local-only and non-gating** for now.

## Why it is deferred

1. The repository baseline is intentionally xUnit v3 + MTP.
2. Upstream Stryker.NET support for this combination is not mature enough to trust broadly.
3. Broad adoption would likely require additional project-level configuration before the results
   can be trusted across the suite.
4. A mutation score is only useful if the underlying test-discovery and coverage mapping are
   reliable; current upstream signals do not provide that confidence.

## Recommended repository posture

- Keep `Stryker.NET` limited to the documented local smoke targets right now.
- Do not add mutation-testing CI jobs while xUnit v3 + MTP support remains preview.
- Keep the repository standard on xUnit v3 + MTP instead of weakening the test stack to suit a
  mutation-testing tool.

## Limited smoke guidance

The repository contains local smoke targets for contained unit-test and Roslyn analyzer/source-generator projects.

- config pattern: `tests/<project>/stryker*.json`
- runner: `mtp`
- command: `scripts/run-mutation-smoke.sh`

Run all configured smoke targets from the repository root:

```bash
bash scripts/run-mutation-smoke.sh
```

Treat the result as a compatibility smoke signal, not a repository-wide quality gate.

Constraints for any future expansion:

- prefer contained unit-test projects first
- prefer projects that reference fewer other projects before higher-level test projects
- keep tested logic in the lowest layer where the behavior belongs
- do not change repository-wide test runner settings
- treat any result as non-authoritative until upstream xUnit v3 + MTP support is clearly stable

## Mutation result triage workflow

Use this workflow for local smoke findings and for any future stable adoption. Do not add tests for
every survivor mechanically.

1. Confirm the run is trustworthy.
   - Use `test-runner: mtp` for this repository's xUnit v3 + MTP stack.
   - Re-run a failing or surprising target once before changing code.
   - Stop and record tool risk when discovery, timeout, or mapping output is inconsistent.
2. Classify each finding.
   - **Killed**: no action.
   - **Survived**: inspect whether the changed behavior is observable and valuable.
   - **No coverage**: add direct tests only when the target code owns behavior that should be covered.
   - **Timeout**: first check test determinism, infinite loops, and expensive setup before adding ignores.
   - **Equivalent**: document why no externally visible behavior changed.
3. Choose the lowest-value-safe action.
   - Improve an existing direct test when it already owns the behavior.
   - Add a new direct test when there is no focused owner.
   - Delete or rewrite brittle tests when they only mirror implementation details.
   - Ignore only equivalent, generated, tool-bug, or intentionally unobservable mutations.
4. Keep ignores narrow.
   - Prefer a specific mutator, member, or generated-file pattern over a project-wide ignore.
   - Every ignore needs a short reason near the configuration.
   - Revisit ignores when Stryker.NET or the affected production code changes.

High-value tests from mutation findings should assert business-visible outcomes, contract shape,
state transitions, error behavior, or externally observable side effects. Low-value tests usually
mirror private implementation branches, assert incidental ordering, require excessive setup, or
become flaky without improving mutation signal.

## Rollout order and thresholds

Roll out mutation testing from the lowest dependency layer upward:

1. Pure SharedKernel libraries and analyzers/source generators with contained test projects.
2. Domain and application unit tests.
3. Contract tests for published artifacts.
4. Infrastructure and API integration tests only after local targets are stable.
5. Browser/system tests only by explicit exception; mutation testing is usually poor value there.

Threshold policy stays non-gating while Stryker.NET MTP support is preview. When upstream support is
stable, prefer target-specific baselines:

- 100 percent is reasonable for small deterministic value objects, analyzers, parsers, and pure
  mapping logic when survivors represent real missed behavior.
- Lower documented minimums are acceptable for integration adapters, generated glue, defensive
  framework paths, and code where equivalent mutations are common.
- Repository-wide line or mutation thresholds are not a substitute for direct ownership by the
  project that owns the behavior.

## Revisit conditions

Revisit mutation-testing adoption only when at least one of these becomes true:

- upstream `Stryker.NET` explicitly documents stable xUnit v3 + MTP support
- the current xUnit v3/MTP incompatibility issues are resolved with confirmed real-world success
- a contained local spike proves accurate mutation results without requiring repository-standard
  exceptions

## Recommendation summary

- **Adopt now:** No
- **Use for a limited subset now:** Local smoke only
- **Document as deferred:** Broad adoption remains deferred

## References

- `docs/TEST_GUIDELINES.md`
- `global.json`
- `Directory.Packages.props`
- <https://stryker-mutator.io/docs/stryker-net/configuration/>
- upstream `Stryker.NET` xUnit v3/MTP support issue tracker
- <https://xunit.net/docs/getting-started/v3/microsoft-testing-platform>
