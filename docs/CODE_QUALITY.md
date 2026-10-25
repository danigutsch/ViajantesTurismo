# Code Quality Tools

This project uses automated tools to enforce consistent formatting and style across both documentation and code files.

## Tools Overview

- **[markdownlint](https://github.com/DavidAnson/markdownlint)** - Markdown documentation formatting
  (`DavidAnson/markdownlint-cli2-action` in CI and Dockerized CLI locally, no npm required)
- **[ShellCheck](https://www.shellcheck.net/)** - Bash/shell script linting (direct CLI or Docker fallback)
- **[shfmt](https://github.com/mvdan/sh)** - Bash/shell script formatting (direct CLI or Docker fallback)
- **Repository Gherkin validator** (`sharedkernel-repo verify-gherkin`) - BDD/Gherkin feature-file
  linting
- **Repository JSON validator** (`sharedkernel-repo verify-json`) - Git-indexed JSON
  validation using the .NET SDK, with comments and trailing commas allowed
- **Repository link validator** (`sharedkernel-repo verify-links`) - Local Markdown/text link, anchor,
  and durable docs-link validation
- **Repository line-ending and encoding validators** (`sharedkernel-repo verify-line-endings` and
  `verify-text-encoding`) - `.gitattributes` line endings and UTF-8 text in the Git index
- **[PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer)** - PowerShell script linting
  (PowerShell module)
- **[dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)** - .NET code
  formatting (.NET SDK tool)

## Local Tool Security Model

The repository's local lint and helper-tool posture is intentionally npm-minimized.

- Prefer repo-pinned `.NET` local tools and digest-pinned containers.
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
2. **Explicit Local Checks** - Run the relevant checks before committing
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
- **Sourced files**: resolved relative to each script (`source-path=SCRIPTDIR`); SC1091 is disabled

**shfmt** formatting rules:

- 4-space indentation (`-i 4`)
- Indent case statements (`-ci`)
- Binary operators at beginning of line (`-bn`)
- Space after redirect operators (`-sr`)

See [Available Commands](#available-commands) section below.

## PowerShell Linting

**PSScriptAnalyzer** is a PowerShell module that analyzes PowerShell scripts for best practices, security issues, and
code quality.

**Installation** (optional):

```powershell
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
```

PowerShell script analysis remains optional for local contributors.

The setup scripts detect whether PSScriptAnalyzer is installed and print the optional install command.

**Lint PowerShell script:**

```powershell
Invoke-ScriptAnalyzer -Path script.ps1 -Settings PSGallery
```

## Gherkin/Feature File Linting

### Configuration

Gherkin rules are the `gherkin` section of `.config/sharedkernel-repo.json`, which
`sharedkernel-repo verify-gherkin` applies to the tracked `.feature` files under `tests/`.

**Key Rules:**

- every tag on a tag line matches an `allowedTagPatterns` entry of the `gherkin` section of
  `.config/sharedkernel-repo.json` as a whole, and forbidden tags such as `@skip` are rejected
- every file has a named `Feature:` and at least one scenario; feature names are unique across the
  checked files, at most 250 characters
- every `Scenario:` and `Scenario Outline:` is named, at most 150 characters, and unique within its file
- every `Scenario Outline:` has `Examples:`
- no trailing whitespace, no tabs outside blank and comment lines, and a newline at end of file

The validator does not check indentation, step style, or the presence of particular tags such as
`@BC:` and `@Agg:`; those stay review conventions.

See [Available Commands](#available-commands) for validation commands.

### CI Integration

Gherkin files are validated by the CI Lint job and can also be checked manually with the same command.

See `tests/BDD_GUIDE.md` for comprehensive Gherkin linting documentation.

---

## JSON File Linting

### Configuration

JSON validation is performed by `sharedkernel-repo verify-json`, which accepts
JSONC-style comments and trailing commas in tracked `*.json` files.

**Key Rules:**

The validator reads the working-tree contents of Git-indexed `*.json` files and requires UTF-8,
with an optional UTF-8 BOM. It reports each invalid file and fails when any cannot be parsed or read.
Untracked package caches, generated artifacts, and nested library checkouts are not lint inputs.

### Tool

- **Repository JSON validator** - Run
  `dotnet tool run sharedkernel-repo -- verify-json --root .`.
  The command validates all indexed JSON files and accepts no directory arguments. Roadmap
  validation uses this same repository-wide policy.

## Link Validation

The repository validates maintained Markdown and selected text links with
`dotnet tool run sharedkernel-repo -- verify-links --root .`, which scans the files that the `links`
section of `.config/sharedkernel-repo.json` selects. The pinned .NET tool avoids transient npm execution
and adds no package ecosystem for documentation checks.

The validator checks stable PR-gating rules:

- local relative Markdown and selected text links resolve to files inside the repository
- Markdown heading anchors resolve when the target is a Markdown file
- maintained docs do not refer to GitHub issues or pull requests by direct URL, repository-relative
  path, or bare numeric shorthand

External URLs are intentionally not probed by the required lint path because network checks are
slow and flaky. If external URL checking is added later, prefer a scheduled or opt-in workflow with
a documented allowlist for confirmed false positives.

When the durable GitHub issue or pull request rule fails, replace the reference with durable context in
the document, link to an ADR or maintained doc, or move short-lived tracking details to an excluded
template, changelog, or test fixture when that file type is the correct home.

Current exclusions are narrow: generated Markdown, repository templates, and issue-specific test
planning notes are not part of the maintained-docs link policy.

## .NET Code Formatting and Analysis

### Configuration Files

Code formatting and style rules are defined in multiple files:

- **`.editorconfig`** - Code formatting, naming conventions, and style preferences
- **`Directory.Build.props`** - Build-time analysis and warnings as errors

#### EditorConfig Settings

This file follows [EditorConfig](https://editorconfig.org/) standards and is automatically recognized
by Visual Studio, Rider, and VS Code.

See `.editorconfig` for complete formatting rules, naming conventions, and code style preferences.

For JetBrains Rider and ReSharper repository hygiene, see `docs/JETBRAINS_RIDER.md`.

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

### Build analysis and coverage gates

The SDK analyzers use `AnalysisLevel=latest`, `AnalysisMode=All`, and warnings as errors.
SharedKernel analyzers and architecture checks remain enabled. SonarAnalyzer and SonarCloud
have been removed; this does not reproduce every Sonar diagnostic, cognitive-complexity metric,
duplication metric, or issue classification. See [quality gates](ci/quality-gates.md) for
removed rules, coverage policy, and the branch-protection migration.

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
- CI restores run in locked mode and set `ContinuousIntegrationBuild=true` explicitly so
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
- Markdown (`.md`): the digest-pinned `davidanson/markdownlint-cli2` container with `--fix <file>`
- Shell (`.sh`, `.bash`): `shfmt -w -i 4 -ci -bn -sr <file>`, or the digest-pinned `mvdan/shfmt` container
  when `shfmt` is not installed

`opencode.json` holds these formatters with the exact image digests. PowerShell files have no
approved single-file formatter.

Broad formatter bundles such as Prettier, Biome, or default shfmt should stay disabled unless
they are explicitly adopted by the repository so format-on-edit stays aligned with this
repository's `.editorconfig`, markdownlint, `.NET`, and shell policies.
JSON, JSONC, YAML, PowerShell, and Gherkin stay validation-only until the repository adopts an approved
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

### Available Commands

**Markdown:**

```bash
docker run --rm --user "$(id -u):$(id -g)" --env HOME=/tmp --volume "$PWD:/workspace" --workdir /workspace \
  docker.io/davidanson/markdownlint-cli2:v0.18.1@sha256:173cb697a255a8a985f2c6a83b4f7a8b3c98f4fb382c71c45f1c52e4d4fed63a \
  --config /workspace/.markdownlint-cli2.jsonc "**/*.md" "#artifacts"
```

Add `--fix` before the globs to apply fixes.

**Shell Scripts:**

```powershell
shellcheck -- .devcontainer/*.sh scripts/*.sh setup-dev.sh            # Lint with .shellcheckrc
shfmt -w -i 4 -ci -bn -sr .devcontainer/*.sh scripts/*.sh setup-dev.sh # Format shell scripts
```

**PowerShell Scripts:**

```powershell
Invoke-ScriptAnalyzer -Path ./setup-dev.ps1
```

**Gherkin/Feature Files:**

```powershell
dotnet tool run sharedkernel-repo -- verify-gherkin --root .
```

**JSON Files:**

```powershell
dotnet tool run sharedkernel-repo -- verify-json --root .
```

**Links:**

```powershell
dotnet tool run sharedkernel-repo -- verify-links --root .                  # Configured files
dotnet tool run sharedkernel-repo -- verify-links docs README.md --root .  # Selected paths
```

**Encoding, line endings, and generated documentation:**

```powershell
dotnet tool run sharedkernel-repo -- verify-text-encoding --root .
dotnet tool run sharedkernel-repo -- verify-line-endings --root .
dotnet tool run sharedkernel-docs -- verify --config docs/architecture/generated-diagrams.json
dotnet tool run sharedkernel-docs -- check --config docs/architecture/documentation-conformance.json
```

The opencode `lint` command runs every check above in order, and `lint-fix` formats the shell scripts
and Markdown and regenerates the architecture diagrams with
`dotnet tool run sharedkernel-docs -- generate --config docs/architecture/generated-diagrams.json`.

**All Tools:**

```powershell
dotnet tool restore      # Install all pinned .NET tools
```

## Local Validation

The repository installs no Git hooks.

- Lint is CI-owned: the CI Lint job runs shellcheck, the `sharedkernel-repo` and `sharedkernel-docs`
  checks, and `DavidAnson/markdownlint-cli2-action`.
- The opencode `lint-fix` command applies the supported autofixes before strict checks.
- Markdown lint runs via the official `DavidAnson/markdownlint-cli2-action` GitHub Action,
  and locally through the digest-pinned Docker image `davidanson/markdownlint-cli2`.
- Gherkin, link, JSON, line-ending, and text-encoding checks run through the pinned
  `sharedkernel-repo` .NET tool; no Python or npm is involved.
- Local shellcheck runs need an installed `shellcheck`; Markdown and shfmt fixes run in Docker.
- Validate a commit message locally with
  `dotnet tool run sharedkernel-ci -- verify-commit-message --file <path-to-commit-message-file>`.
- For the repository's explicit local helper-tool acquisition rules, see
  [Local tool security model](local-tool-security.md).

### Local validation runtime

Measure the solution-wide baseline with a plain timed run after a restore and build:

```bash
time dotnet test --solution ViajantesTurismo.slnx --no-restore --no-build
```

Measure one CI slice through the same engine CI uses. `select` without `--base` selects every test
project; `run` restores, builds, and tests the slice's selected projects and writes per-phase timings to
`TestResults/selection/execution-<slice>/phase-timings.tsv`:

```bash
dotnet tool run sharedkernel-ci -- select
dotnet tool run sharedkernel-ci -- run --slice fast-validation-1
```

The slice names are the `slices[].name` values of `.config/sharedkernel-ci.json`.

Dependency-heavy slices may require a running container runtime, trusted development certificates,
or Playwright browsers. If a local machine cannot run those prerequisites, capture the restore,
build, and feasible slice timings, then state which slice was skipped and why in the pull request.

#### Analyzer and code-fix runner measurements

When measuring analyzer or code-fix runner changes, record the commit SHA, SDK, OS, hardware, power
mode, command, warm/cold state, exit code, validated diff, median/min/max wall time, and peak RSS.
Use disposable fixture copies for code-fix runs; never run a bulk code-fix command against a repository
checkout. The runner opens one workspace and short-circuits project scanning on the first fixable
diagnostic. When no fix is available, it scans candidate projects to determine the terminal state; it
performs an additional complete diagnostic scan only when formatted unsupported locations must be
reported. It intentionally does not enable Fix All: the code-fix providers retain their rule-specific
safety policies.

Use five warm runs per fixture and compare 0, 1, 10, and 100 actionable diagnostics plus a mixed
unsupported case. Any measured batching or file-scoping regression becomes a separate follow-up rather
than a speculative runner change.

Recommended fast local path by change type:

| Change type | Fast local path | Full confidence before PR when risk is real |
| --- | --- | --- |
| Dependency-graph change | `dotnet restore ViajantesTurismo.slnx --force-evaluate`, then `--locked-mode` | `dotnet build ViajantesTurismo.slnx --no-restore` plus impacted test slice |
| AppHost-only change | `dotnet build src/ViajantesTurismo.AppHost/ViajantesTurismo.AppHost.csproj --no-restore` | `dotnet tool run aspire run` and `dotnet tool run sharedkernel-ci -- run --slice hosted-tests --install-playwright` |
| Test-only change | `dotnet test --project <changed-test-project>.csproj` | Matching CI slice through `dotnet tool run sharedkernel-ci -- run --slice <name>` |

Concrete improvement options:

1. Keep `.config/sharedkernel-ci.json` as the single slice membership source for CI and local
   runs so project lists do not drift.
2. Compare local no-coverage slice timings with CI coverage slice timings before changing any CI
   coverage behavior.
3. Split hosted browser/system tests further by trait only after measured timings show one stable
   hotspot; keep the current required checks intact until then.
4. Consider a documented local prerequisite check for dependency-heavy slices if failures are mostly
   missing container runtime, certificates, or Playwright browsers rather than test regressions.

See [ADR-030: CI Test Lane Selection](adr/20260629-ci-test-lane-selection.md) for the lane decision
rules and decision diagram.

## Commit Message Conventions

Use [CONTRIBUTING.md](../CONTRIBUTING.md#commit-messages) as the canonical commit-message policy.
This quality guide only owns the validation command reference:

```powershell
dotnet tool run sharedkernel-ci -- verify-commit-message --file <path-to-commit-message-file>
```

### VS Code Integration

All quality tools have VS Code extensions installed for real-time feedback:

- **markdownlint** - Real-time linting, auto-fix on save, quick fixes
- **ShellCheck** - Shell script analysis
- **ESLint** - JSON/JavaScript linting
- **EditorConfig** - Automatic formatting per file type

## Test Coverage Tools

### Coverlet MTP configuration

`coverlet.MTP` 10.1.0 collects Cobertura through Microsoft.Testing.Platform.
`tests/testconfig.json` is authoritative for collection exclusions. Compiler-generated async
and iterator code stays included. Coverage of separate AppHost child processes is not automatic;
missing evidence for changed source fails the gate. See [quality gates](ci/quality-gates.md).

### Running tests with coverage

Collect coverage through the same engine CI uses, then merge the per-project reports for browsing:

```bash
dotnet tool restore
dotnet tool run sharedkernel-ci -- select
dotnet tool run sharedkernel-ci -- run --slice fast-validation-1 --coverage
dotnet tool run reportgenerator "-reports:TestResults/selection/execution-*/results/**/*.cobertura*.xml" \
    "-targetdir:TestResults/CoverageReport" "-reporttypes:Html;Cobertura"
```

`run --coverage` passes the `--coverlet` test-host switch and writes one Cobertura report per test
project under `TestResults/selection/execution-<slice>/results/<Project>/`. ReportGenerator merges
them into `TestResults/CoverageReport/index.html`. Use `--coverlet` rather than old Microsoft collector
switches such as `--coverage` or `--coverage-settings`.

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

Use the [documentation source-of-truth map](README.md#source-of-truth-map) for related repository guidance.
