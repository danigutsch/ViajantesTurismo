# SonarCloud

Operational details for the repository's hosted SonarCloud analysis path.

## Analysis path

Hosted code quality analysis is executed from the main CI workflow rather than from a
second workflow. The repository keeps Sonar operational configuration in
version-controlled workflow and script files so that analysis behavior remains reproducible
without depending on SonarCloud UI features that may not be available on the current plan.

`scripts/run-sonar-analysis.sh` wraps the `begin` / `build` / `coverage collection` /
`coverage conversion` / `end` pattern for .NET projects. The hosted path reuses the
repo's canonical coverage collection helper (`scripts/collect-test-coverage.sh`) to
generate per-project Cobertura files, then converts that coverage into both HTML and
SonarQube XML formats with the repo-pinned `reportgenerator` tool before publishing
`TestResults/sonar-coverage.xml` to the scanner. The CI workflow wraps that script with
`tee` so the raw scanner output is preserved in `TestResults/sonar-analysis.log`.

The script also sets `sonar.projectBaseDir` explicitly to the repository root. This keeps
Scanner for .NET v8 aligned with the repo's expected analysis scope and avoids warnings
caused by the newer automatic base-directory detection behavior.

The CI workflow then uses GitHub Actions job summaries to publish a short validation
overview directly on the workflow run summary page. This follows GitHub's documented
`GITHUB_STEP_SUMMARY` mechanism so readers can see the quality gate result, SonarCloud
details link, and parse warnings without opening the full step log.

Before the expensive validation path starts, the CI workflow runs
`scripts/validate-sonar-analysis-config.sh` as a dedicated preflight. That step checks
for the required GitHub secret and repository variables, emits a focused annotation, and
writes an actionable summary when configuration is missing.

Local compile-time analysis is complementary rather than separate. The repository
references `SonarAnalyzer.CSharp` as a centrally managed Roslyn analyzer package for
all C# projects, including tests. Test projects set `SonarQubeTestProject=true`
in `tests/Directory.Build.props` so the hosted SonarCloud scanner still treats them as test
scope. Low-signal rules for BDD step-definition patterns are narrowly scoped via `.editorconfig`
to avoid noisy failures without disabling broader hosted test classification. That means:

- ordinary local `dotnet build` runs surface a focused subset of Sonar diagnostics early across
  production, shared, and test code
- ordinary local test-project builds participate in the same package-level Sonar analyzer layer
  while remaining classified as tests in the hosted scanner
- VS Code connected mode stays aligned with the hosted project configuration
- SonarCloud in CI remains the authoritative hosted quality gate and coverage owner

SonarCloud `Automatic Analysis` must stay disabled for this project. The repository
already runs hosted analysis through GitHub Actions, and enabling both modes causes
duplicate-analysis errors.

The repository already relies on an 80% coverage threshold configured in SonarCloud.
Coverage threshold enforcement is therefore part of the hosted Sonar quality gate rather
than a missing future CI feature in this repository.

After the hosted scanner finishes, `scripts/run-sonar-analysis.sh` also queries the
SonarCloud Web API `api/issues/search` endpoint for pull request analysis results. The
repository-owned CI policy fails the SonarCloud job when pull request new code has either
of these issue categories:

- any unresolved issue whose impacted software quality is `SECURITY`
- any unresolved issue whose Web API impact severity is `MEDIUM`, `HIGH`, or `BLOCKER`

The script uses the pull request key and project key instead of scraping SonarCloud UI pages.
GitHub summaries include `SONAR POLICY` lines so the failing issue category is visible in CI.
CI uploads the raw `sonar-policy-responses` artifact when those Web API responses are available.

## Required repository settings

The integrated SonarCloud analysis path requires these GitHub repository settings:

- Actions secret `SONAR_TOKEN`
- Repository variable `SONAR_ORGANIZATION`
- Repository variable `SONAR_PROJECT_KEY`
- SonarCloud project setting: `Automatic Analysis` disabled

Operationally, the SonarCloud project configuration is also the current source of truth
for the existing 80% coverage threshold.

If any of those settings are missing, CI now fails before restore/build work begins so
the workflow error points directly at the missing configuration instead of an indirect
Sonar script exit.

## Local execution and secrets

For local runs of `scripts/run-sonar-analysis.sh`, keep real credentials out of source control.
The repository documents the required variable names in `.env.example`, but contributors should not
commit a real `.env` file.

Recommended local pattern:

1. copy `.env.example` to `.env.local` and replace the placeholder values,
2. run `bash scripts/run-sonar-analysis.sh`.

The Sonar helper scripts now auto-load `.env.local` first and fall back to `.env` when present,
so no manual `source` step is required.

Example:

```bash
cp .env.example .env.local
# edit .env.local with real values
bash scripts/run-sonar-analysis.sh
```

This keeps the committed repository limited to placeholders while still making the expected local
configuration discoverable.

Run normal local checks first (`dotnet build ViajantesTurismo.slnx` and the relevant test slice).
Run full local Sonar before pushing changes that could affect hosted quality results, such as
security-sensitive code, broad refactors, coverage-sensitive changes, or CI/Sonar script changes.

To exercise the pull request issue policy against an already analyzed SonarCloud pull request,
provide the Sonar pull request key without committing it:

```bash
SONAR_PULL_REQUEST_KEY=123 bash scripts/run-sonar-analysis.sh
```

That variable controls the repository-owned Web API policy query. It does not by itself turn a
local scanner run into pull request analysis; CI relies on GitHub Actions pull request metadata for
the hosted scanner analysis.

Troubleshooting notes:

- missing token or configuration: ensure `.env.local` defines `SONAR_TOKEN`, `SONAR_ORGANIZATION`,
  and `SONAR_PROJECT_KEY`
- missing Playwright browsers: rerun the script after `dotnet build` succeeds, or run the relevant
  test slice that installs Playwright in CI
- coverage generation issues: inspect `TestResults/sonar-coverage-collection.log` and
  `TestResults/sonar-reportgenerator.log`
- issue-policy failures: inspect the `SONAR POLICY FAILURE` lines and the SonarCloud pull request
  details for the matching security or medium-or-higher issues

## Analysis exclusions

The scanner `begin` command in `scripts/run-sonar-analysis.sh` sets `sonar.exclusions`,
`sonar.coverage.exclusions`, and `sonar.cpd.exclusions`. The repository uses the narrowest
exclusion type that fits each case:

- `sonar.exclusions` only for files that should not be analyzed at all
- `sonar.coverage.exclusions` for files that should still be analyzed, but should not drive
  coverage targets
- `sonar.cpd.exclusions` for files that should still be analyzed, but contain intentional or
  template-heavy repetition that would distort duplication metrics

| Pattern | Reason |
| --- | --- |
| `**/Migrations/**` | EF Core migration files are scaffolded by `dotnet ef` and should not be manually edited |
| `.devcontainer/**` | Dev Container configuration uses JSONC comments that Sonar's JSON parser cannot read, causing noisy CI warnings |
| `.vscode/**` | VS Code workspace settings and launch configuration use JSONC comments that Sonar's JSON parser cannot read, causing noisy CI warnings |

Coverage exclusions skip:

- `benchmarks/**` because benchmark harnesses are measurement scaffolding
- `samples/**` because sample projects are demonstrative consumer code
- `scripts/**` because repository maintenance and CI helper scripts are linted directly, not exercised via
  the .NET test coverage pipeline
- `tests/performance/**` because the current performance/load-testing assets are JavaScript-based tool inputs,
  not .NET code exercised by the repository's MTP coverage pipeline
- `src/ViajantesTurismo.AppHost/**` because the AppHost is local orchestration code and is already
  excluded from MTP collection in `coverage.settings.xml`
- `src/SharedKernel/SharedKernel.Mediator.SourceGenerator/IsExternalInit.cs` because it is a
  compatibility shim

Duplication exclusions currently cover:

- `benchmarks/**` because benchmark source factories intentionally repeat controlled variants
- mediator analyzer and code-fix files that intentionally repeat handler-shape and template logic

To add further exclusions, append additional comma-separated glob patterns to the matching
`sonar.exclusions`, `sonar.coverage.exclusions`, or `sonar.cpd.exclusions` property in the script.
