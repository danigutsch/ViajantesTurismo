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
including on fork pull requests. Failed selection or tests fail the aggregate first. A locked
restore and Release build of the full solution also cover samples and benchmarks outside test
dependency graphs. Each collected project writes into its own temporary report directory.

## Changed-code policy

Pull requests and merge groups require at least **80% combined coverage of changed executable
lines and conditions**. This is not a whole-repository 80% threshold. Full runs publish coverage
trends; selected slices do not represent repository-wide trends.

The C# `verify-coverage` command consumes merged Cobertura and a zero-context Git diff. It handles
duplicate line and branch observations and rejects malformed reports, malformed diffs, and missing
changed-source evidence. A file without a coverage entry is exempt only when Release portable
PDBs positively identify the current source checksum and contain no executable sequence points
for it. Missing, stale, conflicting, or executable symbol evidence still fails. This supports
interface-only files without hiding uninstrumented application code. Its JSON output defaults
to `TestResults/coverage-gate.json`.

```bash
# Restore and build before collection
dotnet restore ViajantesTurismo.slnx --locked-mode
dotnet build ViajantesTurismo.slnx --no-restore
dotnet tool restore
bash scripts/run-tests-with-coverage.sh

# Build current Release symbols for non-executable source evidence
dotnet build ViajantesTurismo.slnx -c Release --no-restore

# Replace origin/main with the actual comparison base when reproducing a CI run
git -c core.quotePath=false diff --no-ext-diff --unified=0 --find-renames \
  origin/main...HEAD -- '*.cs' '*.razor' > TestResults/coverage-changes.patch
dotnet run --project tools/ViajantesTurismo.RepoConfig.Tool -c Release --no-restore -- \
  verify-coverage --report TestResults/CoverageReport/Cobertura.xml \
  --diff TestResults/coverage-changes.patch
```

Use `--output <path>` to select another JSON evidence path. Inspect
`TestResults/CoverageReport/index.html`, the JSON result, and the diff when the check fails.
Never reuse stale reports after source, test, collector, or configuration changes.

## Branch protection

The required workflow check is `Quality Gate`. After its concrete check passed, the required
`SonarCloud` status was replaced in branch protection and ruleset `14460324`, allowing the
replacement workflow to merge. Both settings were read back and verified on October 20, 2026.
`Build and Test`, `Lint`, `Dependency Review`, and `Secret Scan` remain required. Strict up-to-date checks, GitHub Actions check attribution
(app `15368`), signed commits, additional review requirements, and the no-bypass policy remain
in force.

Workflow changes do not update remote protection automatically. For any future replacement:

1. Confirm the replacement check passes on the exact pull-request head before changing protection.
2. Read both branch protection and every active ruleset targeting `main`.
3. Replace only the retired status, preserving check attribution and all other protection settings.
4. Read back both settings and confirm the pull request still requires the intended checks.

SonarCloud account state and removal of obsolete remote secrets or variables have not been
verified. Those are separate administrative cleanup tasks; current CI does not require them.
