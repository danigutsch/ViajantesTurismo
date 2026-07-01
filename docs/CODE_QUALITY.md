# Code Quality Tools

This project uses automated tools to enforce consistent formatting and style across both documentation and code files.

## Tools Overview

- **[markdownlint](https://github.com/DavidAnson/markdownlint)** - Markdown documentation formatting
  (`DavidAnson/markdownlint-cli2-action` in CI and Dockerized CLI locally, no npm required)
- **[ShellCheck](https://www.shellcheck.net/)** - Bash/shell script linting (direct CLI or Docker fallback)
- **[shfmt](https://github.com/mvdan/sh)** - Bash/shell script formatting (direct CLI or Docker fallback)
- **Repository Gherkin linter** (`scripts/lint-gherkin.py`) - BDD/Gherkin feature-file linting
  (runs via local Python or Docker fallback)
- **Python JSON validator** (`scripts/lint-json.py`) - JSON and JSONC validation with repo-specific exclusions
  (runs via local Python or Docker fallback)
- **Repository link validator** (`scripts/lint-links.py`) - Local Markdown/text link, anchor, and
  durable docs-link validation (runs via local Python or Docker fallback)
- **[PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer)** - PowerShell script linting
  (PowerShell module)
- **[dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)** - .NET code
  formatting (.NET SDK tool)

## Local Tool Security Model

The repository's local lint and helper-tool posture is intentionally npm-minimized.

- Prefer repo-pinned `.NET` local tools, repository-owned scripts, and Dockerized wrappers.
- Prefer vendor or OS package installs for optional standalone tools.
- Avoid adding `npx`, global npm installs, or other transient package execution to supported
  local lint flows unless a dedicated review approves that exception.

See [Local tool security model](local-tool-security.md) for the explicit allowed/blocked
patterns and follow-up checklist.

## Why Automated Quality Checks?

Following industry best practices from projects
like [.NET Docs](https://github.com/dotnet/docs), [GitHub Docs](https://github.com/github/docs),
and [Electron](https://github.com/electron/electron), we use a **multi-layer approach**:

1. **Editor Integration** - Real-time feedback as you type (primary quality gate)
2. **Pre-commit Hooks** - Optional safety net before committing
3. **CI/CD Pipeline** - Enforces standards for all team members

**Note**: We deliberately **do not** run these checks during .NET builds. Documentation and code style quality checks
are separate from code compilation, preventing unnecessary build failures and keeping builds fast.

## Markdown Linting

### Configuration

Markdown linting rules are defined in `.markdownlint.json` at the solution root.

**Strictness Level:** High - Enhanced for better documentation quality

**Key Rules:**

- **MD013**: Line length limited to **160 characters** (excludes tables, code blocks, and headings)
- **MD007**: List indentation uses **4 spaces** (matches official Markdown Guide)
- **MD003**: Enforce ATX-style headings (`# Heading` not `Heading\n=======`)
- **MD004**: Enforce dash-style lists (`-` not `*` or `+`)
- **MD024**: Duplicate headings allowed in different sections (`siblings_only: true`)
- **MD033**: Limited HTML elements allowed (details, summary, br, img, kbd, sub, sup)
- **MD035**: Horizontal rules use `---` style
- **MD046**: Fenced code blocks required (not indented code blocks)
- **MD048**: Backtick-style fenced code blocks (not tildes)
- **All other rules**: Enabled at maximum strictness

See the actual `.markdownlint.json` file for the current configuration rather than a potentially outdated snapshot in
documentation.

### VS Code Integration

The [markdownlint extension](https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint) is
**already installed** and provides:

- **Real-time linting** as you type (primary quality gate)
- **Auto-fix on save** (can be enabled in VS Code settings)
- **Inline error messages** with rule explanations
- **Quick fixes** available via Ctrl+. or Cmd+.

## Shell Script Linting

### Tools

- **ShellCheck** - Lints bash/shell scripts for common issues, best practices, and security vulnerabilities
- **shfmt** - Formats shell scripts with consistent indentation and style

### Configuration

**ShellCheck** configuration is defined in `.shellcheckrc` at the solution root:

- **Strictness**: All optional checks enabled (`enable=all`)
- **Severity**: Reports all issues down to style level
- **Error-level checks**: SC2086, SC2115, SC2154, SC2155 (fail build)
- **Shell dialect**: Bash

**shfmt** formatting rules:

- 4-space indentation (`-i 4`)
- Indent case statements (`-ci`)
- Binary operators at beginning of line (`-bn`)
- Space after redirect operators (`-sr`)

See [Available Scripts](#available-scripts) section below.

## PowerShell Linting

**PSScriptAnalyzer** is a PowerShell module that analyzes PowerShell scripts for best practices, security issues, and
code quality.

**Installation** (optional):

```powershell
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
```

PowerShell script analysis remains optional for local contributors.

## Gherkin/Feature File Linting

### Configuration

Gherkin linting rules are defined in `.gherkin-lintrc` at the solution root.

**Key Rules:**

- **Mandatory tags**: `@BC:<BoundedContext>` and `@Agg:<Aggregate>` required on all features
- **Tag validation**: Enforces project-specific tag patterns (bounded contexts, aggregates, invariants)
- **Indentation**: Consistent feature-file indentation (Feature: 0, Background/Rule/Scenario: 4, Steps: 8)
- **BDD anti-patterns**: Prevents conjunction steps, unnamed features/scenarios
- **Formatting**: No trailing spaces, newline at EOF, no duplicate scenario names

See [Available Scripts](#available-scripts) for validation commands.

### CI Integration

Gherkin files are validated in CI and can also be checked manually with the repository script.

See `tests/BDD_GUIDE.md` for comprehensive Gherkin linting documentation.

---

## JSON File Linting

### Configuration

JSON validation is performed by `scripts/lint-json.py`, which accepts repository JSON and JSONC-style comments.

**Key Rules:**

The validator focuses on parse correctness for repository JSON files without a repo-tracked npm toolchain.

### Tool

- **PSScriptAnalyzer** - Lints PowerShell scripts for best practices, security issues, and code quality

## Link Validation

The repository validates maintained Markdown and selected text links with `scripts/lint-links.sh`. The chosen
approach is a repository-owned Python script with the same local Python or Docker fallback pattern
as the JSON and Gherkin linters. This avoids transient npm execution and avoids adding a new package
ecosystem for documentation checks.

The validator checks stable PR-gating rules:

- local relative Markdown and selected text links resolve to files inside the repository
- Markdown heading anchors resolve when the target is a Markdown file
- maintained docs do not link directly to GitHub issue or pull request URLs
- maintained docs do not use repo-relative issue or pull request links such as `issues/123` or
  `pull/123`

External URLs are intentionally not probed by the required lint path because network checks are
slow and flaky. If external URL checking is added later, prefer a scheduled or opt-in workflow with
a documented allowlist for confirmed false positives.

When the durable GitHub issue or pull request rule fails, replace the link with durable context in
the document, link to an ADR or maintained doc, or move short-lived tracking details to an excluded
template, changelog, or test fixture when that file type is the correct home.

Current exclusions are narrow: generated Markdown, repository templates, and issue-specific test
planning notes are not part of the maintained-docs link policy.

### How to Install

```powershell
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
```

This is automatically installed by `setup-dev.ps1`.

### Commands

**Lint PowerShell script:**

```powershell
Invoke-ScriptAnalyzer -Path script.ps1 -Settings PSGallery
```

## .NET Code Formatting and Analysis

### Configuration Files

Code formatting and style rules are defined in multiple files:

- **`.editorconfig`** - Code formatting, naming conventions, and style preferences
- **`Directory.Build.props`** - Build-time analysis and warnings as errors

#### EditorConfig Settings

This file follows [EditorConfig](https://editorconfig.org/) standards and is automatically recognized
by Visual Studio, Rider, and VS Code.

See `.editorconfig` for complete formatting rules, naming conventions, and code style preferences.

#### Build Configuration

**`Directory.Build.props`** enforces strict code analysis at build time (warnings as errors, all analyzer categories
enabled) and follows the latest built-in SDK analyzer wave with `AnalysisLevel=latest`.

See `Directory.Build.props` for complete build configuration.

### Guard-Clause Analyzer Enforcement

Enable guard checks for production code.

#### CA1062: Validate arguments of public methods

Use `CA1062` for required null validation on public methods and constructors.

- **Production projects:** enable it as `error`
- **Test projects:** do not use a broad project-wide suppression; add real guards in reusable test
  support code and keep any remaining exceptions file-scoped to framework-owned binding surfaces
- **Generated code and migrations:** exclude

Preferred patterns:

- `ArgumentNullException.ThrowIfNull(value);`
- `ArgumentException.ThrowIfNullOrWhiteSpace(value);`
- `ArgumentOutOfRangeException.ThrowIfNegative(value);`

Keep suppressions narrow and do not use guard clauses instead of domain validation.

### Sonar analysis layers

The repository uses a three-layer Sonar model:

- **Build-time analyzer package**: `SonarAnalyzer.CSharp` is referenced centrally through
  `Directory.Packages.props` and `Directory.Build.props` so C# projects across the repository,
  including tests, get local Roslyn diagnostics during ordinary builds. Test projects still set
  `SonarQubeTestProject=true` in `tests/Directory.Build.props` so hosted SonarCloud classifies
  them as test scope. Low-signal rules for BDD step-definition patterns remain narrowly scoped via
  `.editorconfig`.
- **IDE connected mode**: VS Code uses the Sonar connected-mode settings in `.vscode/settings.json`
  to align local issue visibility with the hosted project configuration.
- **Hosted quality gate**: CI still runs the repo-pinned `dotnet-sonarscanner` path described in
  `docs/ci/sonarcloud.md`.

### NuGet audit and restore policy

Package restore and vulnerability scanning are intentionally explicit instead of relying on SDK
defaults that may drift between releases.

- `NuGet.Config` owns the package source and audit source configuration and points both to the
  repository-approved `nuget.org` V3 feed.
- `Directory.Build.props` sets `NuGetAudit=true`, `NuGetAuditMode=all`, and
  `NuGetAuditLevel=high` so transitive dependency auditing stays on, but restore failures focus on
  high and critical advisories instead of every low-severity bulletin.
- Transitive security pins belong in a distinctive `Transitive security pins` block in
  `Directory.Packages.props` near the parent package. Each pin must name the parent package, the
  vulnerable transitive package/version, the advisory, and the removal condition.
- Local restores stay unlocked by default to keep everyday dependency maintenance practical.
- CI restores run in locked mode and now also set `ContinuousIntegrationBuild=true` explicitly so
  deterministic build behavior is owned by repository policy rather than ambient environment luck.

### Production Code Timing Policy

Direct `Task.Delay(...)` usage in production code under `src/` is discouraged and should be treated as an architectural
exception, not a normal implementation tool.

Prefer one of the following instead:

- explicit state transitions based on application or UI events
- owned timers with clear lifecycle management when time-based behavior is truly required
- abstractions that make time-dependent behavior deterministic and testable
- framework-native mechanisms that express deferred UI behavior without arbitrary waits

This policy is defined in [ADR-019: No Direct Task.Delay in Production Code](adr/20260321-no-task-delay-in-production-code.md).

Scope notes:

- applies to production code under `src/`
- does not automatically apply to test projects under `tests/`
- any temporary production exception should be documented with rationale and a removal plan

### Commands

**Format all .NET code:**

```powershell
dotnet format
```

**Format only whitespace (line endings, indentation, trailing spaces):**

```powershell
dotnet format whitespace
```

**Check formatting without making changes:**

```powershell
dotnet format --verify-no-changes
```

**Single-file formatters:**

Agent/editor format-on-save integrations should run only repository-approved single-file
formatters after edits:

- C# (`.cs`): resolve the repository root, then run `dotnet format ViajantesTurismo.slnx --include <file> --no-restore`
- Markdown (`.md`): `bash scripts/lint-markdown.sh --fix <file>`
- Shell (`.sh`, `.bash`): `shfmt -w -i 4 -ci -bn -sr <file>` with the same Docker fallback model as `scripts/lint-all.sh`
- PowerShell (`.ps1`, `.psm1`, `.psd1`): `pwsh -NoProfile -File scripts/format-powershell-file.ps1 -Path <file>` using PSScriptAnalyzer `Invoke-Formatter`

Broad formatter bundles such as Prettier, Biome, or default shfmt should stay disabled unless
they are explicitly adopted by the repository so format-on-edit stays aligned with this
repository's `.editorconfig`, markdownlint, `.NET`, and shell policies.
JSON, JSONC, YAML, and Gherkin stay validation-only until the repository adopts an approved
single-file formatter for those formats.

### Prerequisites

Install .NET local tools (dotnet-ef, reportgenerator, aspire):

```powershell
dotnet tool restore
```

Install PowerShell dependencies:

```powershell
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
```

**Or run the automated setup script** (recommended):

```powershell
.\setup-dev.ps1
```

### Available Scripts

**Markdown:**

```powershell
bash scripts/lint-markdown.sh                    # CI-owned markdown lint wrapper
bash scripts/lint-markdown.sh --fix docs/example.md
```

**Shell Scripts:**

```powershell
shellcheck **/*.sh       # Lint shell scripts with ShellCheck
shfmt -w -i 4 -ci -bn -sr **/*.sh    # Format shell scripts with shfmt
```

**PowerShell Scripts:**

```powershell
pwsh -NoProfile -File scripts/format-powershell-file.ps1 -Path ./setup-dev.ps1
Invoke-ScriptAnalyzer -Path ./setup-dev.ps1
```

**Gherkin/Feature Files:**

```powershell
bash scripts/lint-gherkin.sh tests/**/*.feature     # Repository Gherkin lint wrapper (Python-based)
```

**JSON Files:**

```powershell
bash scripts/lint-json.sh **/*.json        # Check all JSON files
```

**Links:**

```powershell
bash scripts/lint-links.sh                 # Check maintained Markdown links and durable link policy
bash scripts/lint-links.sh docs README.md  # Check selected Markdown paths
```

**All Linters:**

```powershell
bash scripts/lint-all.sh                 # Run all linters in check mode
bash scripts/lint-all.sh --fix           # Apply available autofixes (shfmt + markdownlint), then lint
bash scripts/lint-all.sh --skip-markdown # Run non-markdown linters only (CI helper)
```

**All Tools:**

```powershell
dotnet tool restore      # Install all pinned .NET tools
```

## Local Validation

The repository no longer installs git hooks by default.

- Lint is CI-owned and runs through `bash scripts/lint-all.sh` and `DavidAnson/markdownlint-cli2-action` in workflows.
- `bash scripts/lint-all.sh --fix` is available locally to apply supported autofixes before strict checks.
- Markdown lint runs via the official `DavidAnson/markdownlint-cli2-action` GitHub Action,
  and locally through the Docker image `davidanson/markdownlint-cli2`.
- Gherkin lint runs via the repository-owned `scripts/lint-gherkin.py` (Python-based, no npm).
- Link validation runs via the repository-owned `scripts/lint-links.py` (Python-based, no npm) and
  is included in `bash scripts/lint-all.sh`.
- Running `bash scripts/lint-all.sh` locally does not require manual linter installs when Docker is
  available; wrappers fall back to pinned Docker images for shellcheck, shfmt, markdown lint, and
  Python-based linters.
- If Docker is unavailable, wrappers use local tools when present and fail with an explicit message
  when neither local tool nor Docker is available.
- Commit message validation remains available locally through
  `bash scripts/validate-commit-message.sh <path-to-commit-message-file>`.
- If you previously installed repository hooks from an older revision, remove or replace the copied
  files under `git rev-parse --git-path hooks` so commits do not keep calling deleted npm-based
  hook scripts.
- For the repository's explicit local helper-tool acquisition rules, see
  [Local tool security model](local-tool-security.md).

### Local validation runtime

Use the repository benchmark script when a change may affect local validation time:

```bash
bash scripts/benchmark-local-validation.sh
```

The script writes tab-separated phase timings to `TestResults/local-validation-benchmark-timings.tsv`
and logs beside that file. It uses Bash, the .NET SDK, and standard POSIX utilities available on
Unix-like development environments.

Useful focused runs:

```bash
# Restore and build only. This is the safe default.
bash scripts/benchmark-local-validation.sh

# One CI test slice only, after a successful build
bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice fast-validation

# Full measured path with a custom output file
bash scripts/benchmark-local-validation.sh --all-slices --output TestResults/local-validation-$(date +%Y%m%d-%H%M%S).tsv

# Solution-level all-tests baseline. This is a benchmark, not the CI lane policy.
bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --solution-tests
```

Available slice names match `scripts/ci-test-slices/*.txt`:

- `fast-validation`
- `admin-integration`
- `mediator-heavy`
- `admin-system`

Dependency-heavy slices may require a running container runtime, trusted development certificates,
or Playwright browsers. If a local machine cannot run those prerequisites, capture the restore,
build, and feasible slice timings, then state which slice was skipped and why in the pull request.

Measured on this worktree on 2026-06-29:

| Phase | Command | Duration | Status |
| --- | --- | ---: | --- |
| Restore | `bash scripts/benchmark-local-validation.sh --skip-build --skip-tests --output TestResults/issue-426-restore-benchmark.tsv` | 2s | success |
| Build, warm repeat | `bash scripts/benchmark-local-validation.sh --skip-restore --skip-tests --output TestResults/issue-426-build-benchmark.tsv` | 26s | success |
| Fast validation slice | `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice fast-validation --output TestResults/issue-426-fast-validation-benchmark.tsv` | 167s | success |
| Admin integration slice | `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice admin-integration --output TestResults/issue-426-admin-integration-benchmark.tsv` | 100s | success |
| Mediator heavy slice | `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice mediator-heavy --output TestResults/issue-426-mediator-heavy-benchmark.tsv` | 65s | success |
| Admin system slice | `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice admin-system --output TestResults/issue-426-admin-system-benchmark.tsv` | 71s | success |
| Solution all-tests baseline | `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --solution-tests --output TestResults/issue-438-solution-tests-baseline.tsv` | 131s | failure; hosted/system concurrency conflict |
| Admin integration plus PostgreSQL event sourcing | `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice admin-integration --output TestResults/issue-438-admin-integration-with-postgres.tsv` | 212s | success |

Low-risk improvement implemented here: local timing can run test slices without coverage collection
while CI keeps its existing coverage collection and required checks intact.

Recommended fast local path by change type:

| Change type | Fast local path | Full confidence before PR when risk is real |
| --- | --- | --- |
| Package-only change | `dotnet restore ViajantesTurismo.slnx --force-evaluate`, then `dotnet restore ViajantesTurismo.slnx --locked-mode` | `dotnet build ViajantesTurismo.slnx --no-restore` plus impacted test slice |
| AppHost-only change | `dotnet build src/ViajantesTurismo.AppHost/ViajantesTurismo.AppHost.csproj --no-restore` | `dotnet tool run aspire run` and `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice admin-system` |
| Test-only change | `dotnet test --project <changed-test-project>.csproj` | Matching CI slice through `bash scripts/benchmark-local-validation.sh --skip-restore --skip-build --slice <name>` |

Concrete improvement options:

1. Keep using `scripts/ci-test-slices/*.txt` as the single slice membership source for CI and local
   benchmarks so project lists do not drift.
2. Compare local no-coverage slice timings with CI coverage slice timings before changing any CI
   coverage behavior.
3. Split hosted browser/system tests further by trait only after measured timings show one stable
   hotspot; keep the current required checks intact until then.
4. Consider a documented local prerequisite check for dependency-heavy slices if failures are mostly
   missing container runtime, certificates, or Playwright browsers rather than test regressions.

See [ADR-030: CI Test Lane Selection](adr/20260629-ci-test-lane-selection.md) for the lane decision
rules and decision diagram.

## Commit Message Conventions

This repository uses [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) and enforces them with `scripts/validate-commit-message.sh`.

**Required format:**

```text
<type>[optional scope]: <description>
```

**Allowed types:**

- `feat`
- `fix`
- `docs`
- `ci`
- `build`
- `test`
- `refactor`
- `perf`
- `style`
- `chore`
- `revert`

**Examples:**

```text
ci: add dependency review workflow
docs(ci): document branch protection requirements
fix(web): prevent empty booking date submission
```

**Manual validation:**

```powershell
printf "%s\n" "ci: add dependency review workflow" > /tmp/commit-msg.txt
bash scripts/validate-commit-message.sh /tmp/commit-msg.txt
```

### VS Code Integration

All quality tools have VS Code extensions installed for real-time feedback:

- **markdownlint** - Real-time linting, auto-fix on save, quick fixes
- **ShellCheck** - Shell script analysis
- **ESLint** - JSON/JavaScript linting
- **EditorConfig** - Automatic formatting per file type

## Test Coverage Tools

### Microsoft Code Coverage Configuration

Test coverage is collected using **Microsoft Testing Platform (MTP)** with the
[`Microsoft.Testing.Extensions.CodeCoverage`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-extensions-code-coverage)
extension (referenced in all test projects). Settings are defined in `coverage.settings.xml` at the solution root.

See [xUnit v3 Code Coverage with MTP](https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp) for the
official guide.

**Key Configuration:**

- **Format**: `cobertura` (XML format for CI/CD integration)
- **Output**: Each test project generates a `coverage.cobertura.xml` in its `TestResults` folder
- **Include**: All `ViajantesTurismo.*` assemblies
- **Exclude**:
    - Generated files
    - Test projects (`*Tests.dll`)
    - Migrations
    - AppHost project

### Running Tests with Coverage

**Available coverage switches** (passed after `--` to the test host):

| Switch | Purpose |
| --- | --- |
| `--coverage` | Enable code coverage collection (required) |
| `--coverage-output-format` | Output format: `coverage`, `xml`, or `cobertura` |
| `--coverage-output` | Output filename |
| `--coverage-settings` | Path to XML settings file (e.g. `coverage.settings.xml`) |

**Collect coverage:**

```powershell
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml

# With explicit settings file
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings ../../coverage.settings.xml
```

**Generate HTML report:**

```powershell
# Run tests with coverage
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml

# Generate report (requires reportgenerator tool)
dotnet tool restore
dotnet reportgenerator -reports:**/TestResults/**/coverage.cobertura.xml -targetdir:TestResults/CoverageReport -reporttypes:Html

# Open report
start TestResults/CoverageReport/index.html
```

**Or use the VS Code task:**

Run the "test with coverage" task from the Command Palette (Ctrl+Shift+P > Tasks: Run Task).

### Coverage Reports

**ReportGenerator** is included as a .NET local tool and generates:

- **HTML reports**: Interactive browsable coverage
- **Badge SVGs**: For README.md
- **Cobertura XML**: For CI/CD integration

**Install (if not already):**

```powershell
dotnet tool restore
```

### Coverage Goals

See [Test Guidelines](TEST_GUIDELINES.md#bdd-coverage--ci) for coverage goals and testing strategy.

## Related Documentation

- [Analyzer Hardening Roadmap](ANALYZER_HARDENING_ROADMAP.md) - Phased analyzer adoption,
  SharedKernel analyzer family ownership, and rollout guidance
- [CI Overview](ci/overview.md) - GitHub Actions workflow structure, required checks, and
  links to maintainer-facing CI operations docs
- [Test Guidelines](TEST_GUIDELINES.md) - Testing strategy and coverage goals
- [BDD Guide](../tests/BDD_GUIDE.md) - Behavior-driven development patterns
- [Coding Guidelines](CODING_GUIDELINES.md) - .NET coding standards
