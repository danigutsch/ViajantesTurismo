# Quality gates

The repository owns build analysis and coverage checks in source control. No hosted analysis
account, scanner, connected-mode configuration, or analysis token is required.

## Build analysis

The pinned .NET SDK 10.0.402 supplies analyzers with `AnalysisLevel=latest`,
`AnalysisMode=All`, code-style enforcement, and warnings treated as errors.
SharedKernel analyzers, architecture tests, test checks, dependency review, and secret
scanning remain independent gates. `.editorconfig` owns scoped severity policy.

Cyclomatic complexity is limited by `CA1502`: `CodeMetricsConfig.txt` sets the threshold (50) and
`.editorconfig` enables the rule as a warning, so an over-limit method fails the build. A method above the
limit needs a path-scoped `.editorconfig` exception that names its owner and the issue that removes it;
`BookingFinancialHistory.Persistence.cs` is the only one today. Lower the threshold as exceptions are
refactored; never raise it.

Cognitive complexity and duplicate-code detection are intentionally not enforced: no .NET analyzer provides
them, and an external tool would add a supply-chain dependency for a signal that review and the complexity
limit already cover. Review those concerns explicitly rather than reading a passing build as proof of them.

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

The `Quality Gate` CI job downloads selected test-slice artifacts and runs without secrets,
including on fork pull requests. Failed selection or tests fail the aggregate first. A locked
restore and Release build of the full solution also cover samples and benchmarks outside test
dependency graphs. Each test project writes its Cobertura report into its own `results/<Project>/`
directory. ReportGenerator 5.5.11 merges the reports into HTML for local inspection.

## Changed-code policy

Pull requests and merge groups require at least **80% combined coverage of changed executable
lines and conditions**. This is not a whole-repository 80% threshold. Full runs publish coverage
trends; selected slices do not represent repository-wide trends.

`sharedkernel-ci coverage verify` reads `.config/sharedkernel-ci.json`: `coverage.thresholdPercent` (80),
the `coverage.include` patterns (`src/*.cs`, `src/*.razor`), and the `coverage.exclude` patterns
(migrations, `obj`, generated files, and AppHost). It runs its own zero-context `git diff <base>...HEAD`
with repository Git configuration and attributes neutralized, reads every `*.cobertura*.xml` under each
`--report`, and refuses a report with a document type declaration. A line is covered when any report hits
it. A file without a coverage entry is exempt only when the Release portable PDBs that
`coverage record-symbols` recorded name its current content hash and map no executable code to it.
Missing, stale, or executable symbol evidence still fails. This supports interface-only files without
hiding uninstrumented application code. Its JSON output defaults to `TestResults/coverage-gate.json`.

```bash
# Collect coverage for every test project through the CI engine
dotnet tool restore
dotnet tool run sharedkernel-ci -- select
dotnet tool run sharedkernel-ci -- run --coverage

# Build current Release symbols for non-executable source evidence
dotnet restore ViajantesTurismo.slnx --locked-mode
dotnet build ViajantesTurismo.slnx -c Release --no-restore
dotnet tool run sharedkernel-ci -- coverage record-symbols

# Replace the base with the actual comparison base (full object ID) when reproducing a CI run
dotnet tool run sharedkernel-ci -- coverage verify --report TestResults/selection \
  --base "$(git rev-parse origin/main)" --symbols TestResults/symbol-evidence.json
```

Use `--output <path>` to select another JSON evidence path. Inspect the JSON result when the check
fails, and merge the reports with ReportGenerator for an HTML view. Never reuse stale reports after
source, test, collector, or configuration changes.

## Branch protection

The required workflow check is `Quality Gate`, with `Build and Test`, `Lint`, `Dependency Review`, and
`Secret Scan`, plus `PR Metadata`. `Dependency Review` and `PR Metadata` are reported as
`Dependency Review / Review` and `PR Metadata / Validate squash header` after the shared-workflow adoption; the
ruleset update is a manual step to do before that change merges. Strict up-to-date checks, GitHub Actions check attribution (app `15368`), signed commits,
additional review requirements, and the no-bypass policy remain in force. Branch protection and ruleset
`14460324` were read back and verified on October 20, 2026.

Workflow changes do not update remote protection automatically. For any future replacement:

1. Confirm the replacement check passes on the exact pull-request head before changing protection.
2. Read both branch protection and every active ruleset targeting `main`.
3. Replace only the retired status, preserving check attribution and all other protection settings.
4. Read back both settings and confirm the pull request still requires the intended checks.
