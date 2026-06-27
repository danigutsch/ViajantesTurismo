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
- `dotnet-stryker` `4.15.0` can run a contained xUnit v3 + MTP smoke target in this repository
  when `test-runner` is set to `mtp` explicitly.
- The upstream issue `stryker-mutator/stryker-net#3117` reports that Stryker.NET does not
  handle xUnit v3 properly even without MTP enabled, producing unexpected test-case warnings
  and unusable mutation results.
- This repository uses the stricter combination of xUnit v3 **and** MTP across the suite,
  which increases compatibility risk rather than reducing it.

### Practical implication

Even though Stryker.NET has an `mtp` mode that can run a contained smoke target, current upstream
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

- Keep `Stryker.NET` limited to the documented local smoke target right now.
- Do not add mutation-testing CI jobs while xUnit v3 + MTP support remains preview.
- Keep the repository standard on xUnit v3 + MTP instead of weakening the test stack to suit a
  mutation-testing tool.

## Limited smoke guidance

The repository contains one local smoke target:

- config: `tests/SharedKernel.Domain.Tests/stryker-config.json`
- target: `src/SharedKernel/SharedKernel.Domain/Entity.cs`
- runner: `mtp`

Run it from `tests/SharedKernel.Domain.Tests`:

```powershell
dotnet tool run dotnet-stryker
```

Treat the result as a compatibility smoke signal, not a repository-wide quality gate.

Constraints for any future proof-of-concept:

- pick one contained unit-test project only
- do not change repository-wide test runner settings
- do not introduce alternate project configuration that becomes part of the supported baseline
- treat any result as non-authoritative until upstream xUnit v3 + MTP support is clearly stable

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
- <https://github.com/stryker-mutator/stryker-net/issues/3117>
- <https://xunit.net/docs/getting-started/v3/microsoft-testing-platform>
