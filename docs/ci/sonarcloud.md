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
`TestResults/sonar-coverage.xml` to the scanner.

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

## Analysis exclusions

The scanner `begin` command in `scripts/run-sonar-analysis.sh` sets `sonar.exclusions` to
skip auto-generated code that should not be analyzed.

| Pattern | Reason |
| --- | --- |
| `**/Migrations/**` | EF Core migration files are scaffolded by `dotnet ef` and should not be manually edited |

To add further exclusions, append additional comma-separated glob patterns to the existing
`sonar.exclusions` property in the script.
