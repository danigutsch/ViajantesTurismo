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
The repository may document required variable names in `.env.example`, but contributors should not
commit a real `.env` file.

Recommended local pattern:

1. copy `.env.example` to an ignored local file such as `.env.local` or load the values from your shell,
2. export `SONAR_TOKEN`, `SONAR_ORGANIZATION`, and `SONAR_PROJECT_KEY` into the current shell,
3. run `bash scripts/run-sonar-analysis.sh`.

Example:

```bash
set -a
source .env.local
set +a
bash scripts/run-sonar-analysis.sh
```

This keeps the committed repository limited to placeholders while still making the expected local
configuration discoverable.

## Analysis exclusions

The scanner `begin` command in `scripts/run-sonar-analysis.sh` sets `sonar.exclusions` to
skip auto-generated code that should not be analyzed.

| Pattern | Reason |
| --- | --- |
| `**/Migrations/**` | EF Core migration files are scaffolded by `dotnet ef` and should not be manually edited |
| `.devcontainer/**` | Dev Container configuration uses JSONC comments that Sonar's JSON parser cannot read, causing noisy CI warnings |
| `.vscode/**` | VS Code workspace settings and launch configuration use JSONC comments that Sonar's JSON parser cannot read, causing noisy CI warnings |

To add further exclusions, append additional comma-separated glob patterns to the existing
`sonar.exclusions` property in the script.
