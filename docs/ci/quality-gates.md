# Quality gates

The repository owns build analysis and coverage checks in source control. No SonarCloud
account, scanner, connected-mode configuration, or analysis token is required.

## Build analysis

The pinned .NET SDK 10.0.402 supplies analyzers with `AnalysisLevel=latest`,
`AnalysisMode=All`, code-style enforcement, and warnings treated as errors.
SharedKernel analyzers, architecture tests, test checks, dependency review, and secret
scanning remain independent gates. `.editorconfig` owns scoped severity policy.

This is not exact Sonar rule or finding parity. The removed explicit rules are `S2326`,
`S2094`, `S1186`, `S4136`, `S4144`, and `S4035`. Sonar cognitive-complexity and duplication
metrics and its high-impact issue classification are not reproduced. Review those concerns
explicitly rather than interpreting a passing build as equivalent hosted analysis.

## Coverage collection

`coverlet.MTP` 10.1.0 replaces the Microsoft coverage extension and runs with xUnit v3 on
Microsoft.Testing.Platform. The authoritative configuration is `tests/testconfig.json`.
It excludes test assemblies, AppHost, migrations, generated files, and explicitly generated
or excluded attributes. Compiler-generated async and iterator implementations remain included.

Coverage of separate AppHost child processes is not automatically equivalent to the previous
collector. A running end-to-end scenario does not prove that its child-service code was
instrumented. Missing coverage evidence for changed source fails the gate rather than being
silently discarded. Resolve instrumentation or add appropriate direct tests; do not hide gaps
with broader exclusions.

ReportGenerator 5.5.11 merges Cobertura reports and produces HTML for inspection.
The `Quality Gate` CI job downloads selected test-slice artifacts and runs without secrets,
including on fork pull requests. Failed selection or tests fail the aggregate first.

## Changed-code policy

Pull requests and merge groups require at least **80% combined coverage of changed executable
lines and conditions**. This is not a whole-repository 80% threshold. Full runs publish coverage
trends; selected slices do not represent repository-wide trends.

The C# `verify-coverage` command consumes merged Cobertura and a zero-context Git diff. It handles
duplicate line and branch observations and rejects malformed reports, malformed diffs, and missing
changed-source evidence. Its JSON output defaults to `TestResults/coverage-gate.json`.

```bash
# Restore and build before collection
dotnet restore ViajantesTurismo.slnx --locked-mode
dotnet build ViajantesTurismo.slnx --no-restore
dotnet tool restore
bash scripts/run-tests-with-coverage.sh

# Replace origin/main with the actual comparison base when reproducing a CI run
git -c core.quotePath=false diff --no-ext-diff --unified=0 --find-renames \
  origin/main...HEAD -- '*.cs' '*.razor' > TestResults/coverage-changes.patch
dotnet run --project tools/SharedKernel.RepoConfig.Tool -c Release --no-restore -- \
  verify-coverage --report TestResults/CoverageReport/Cobertura.xml \
  --diff TestResults/coverage-changes.patch
```

Use `--output <path>` to select another JSON evidence path. Inspect
`TestResults/CoverageReport/index.html`, the JSON result, and the diff when the check fails.
Never reuse stale reports after source, test, collector, or configuration changes.

## Branch-protection migration

The workflow check is named `Quality Gate`. Existing remote protection may still require
`SonarCloud`; changing workflow files does not update GitHub protection automatically.
After the concrete replacement check passes, a maintainer must replace that required status
with `Quality Gate`, retaining `Build and Test`, `Lint`, `Dependency Review`, and `Secret Scan`.
Do not remove the old requirement before the replacement has passed. Remove obsolete Sonar
secrets and variables through repository administration after retiring the integration.
