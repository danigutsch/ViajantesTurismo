# Code Quality Tools

This project uses automated tools to enforce consistent formatting and style across both documentation and code files.

## Tools Overview

- **[markdownlint](https://github.com/DavidAnson/markdownlint)** - Markdown documentation formatting (npm)
- **[ShellCheck](https://www.shellcheck.net/)** - Bash/shell script linting (npm)
- **[shfmt](https://github.com/mvdan/sh)** - Bash/shell script formatting (npm)
- **[gherkin-lint](https://github.com/vsiakka/gherkin-lint)** - BDD/Gherkin feature file linting (npm)
- **[ESLint](https://eslint.org/)** with **[eslint-plugin-jsonc](https://www.npmjs.com/package/eslint-plugin-jsonc)** -
  JSON file linting (npm)
- **[PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer)** - PowerShell script linting
  (PowerShell module)
- **[dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)** - .NET code
  formatting (.NET SDK tool)

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

The pre-commit hook will use PSScriptAnalyzer if available but will skip it if not installed.

## Gherkin/Feature File Linting

### Configuration

Gherkin linting rules are defined in `.gherkin-lintrc` at the solution root.

**Key Rules:**

- **Mandatory tags**: `@BC:<BoundedContext>` and `@Agg:<Aggregate>` required on all features
- **Tag validation**: Enforces project-specific tag patterns (bounded contexts, aggregates, invariants)
- **Indentation**: Consistent 2-space indentation (Feature: 0, Background/Rule/Scenario: 2, Steps: 4)
- **BDD anti-patterns**: Prevents conjunction steps, unnamed features/scenarios
- **Formatting**: No trailing spaces, newline at EOF, no duplicate scenario names

See [Available Scripts](#available-scripts) for validation commands.

### Pre-commit Integration

Gherkin files are validated during pre-commit and will block commits if validation errors are found.

See `tests/BDD_GUIDE.md` for comprehensive Gherkin linting documentation.

---

## JSON File Linting

### Configuration

JSON linting rules are defined in `eslint.config.mjs` at the solution root using ESLint with the JSONC plugin.

**Key Rules:**

See configuration in `eslint.config.mjs` for complete linting rules.

### Tool

- **PSScriptAnalyzer** - Lints PowerShell scripts for best practices, security issues, and code quality

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
enabled).

See `Directory.Build.props` for complete build configuration.

### Guard-Clause Analyzer Enforcement

Enable guard checks for production code.

#### CA1062: Validate arguments of public methods

Use `CA1062` for required null validation on public methods and constructors.

- **Production projects:** enable it as `error`
- **Test projects:** keep it suppressed
- **Generated code and migrations:** exclude

Preferred patterns:

- `ArgumentNullException.ThrowIfNull(value);`
- `ArgumentException.ThrowIfNullOrWhiteSpace(value);`
- `ArgumentOutOfRangeException.ThrowIfNegative(value);`

Keep suppressions narrow and do not use guard clauses instead of domain validation.

### Sonar analysis layers

The repository uses a three-layer Sonar model:

- **Build-time analyzer package**: `SonarAnalyzer.CSharp` is referenced centrally through
  `Directory.Packages.props` and `Directory.Build.props` so all C# projects — including test
  projects — get local Roslyn diagnostics during ordinary builds. Test projects set
  `SonarQubeTestProject=true` in `tests/Directory.Build.props` so the hosted SonarCloud scanner
  treats them accordingly, but the build-time Roslyn analyzer still runs. Low-signal rules for
  BDD step-definition patterns are narrowly scoped via `.editorconfig` to avoid noisy failures
  without disabling broader test analysis.
- **IDE connected mode**: VS Code uses the Sonar connected-mode settings in `.vscode/settings.json`
  to align local issue visibility with the hosted project configuration.
- **Hosted quality gate**: CI still runs the repo-pinned `dotnet-sonarscanner` path described in
  `docs/ci/sonarcloud.md`.

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

### Prerequisites

Install Node.js dependencies (includes markdownlint, shellcheck, shfmt, gherkin-lint, and ESLint):

```powershell
npm install
```

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
npm run lint:md          # Check all markdown files
npm run lint:md:fix      # Auto-fix markdown issues
```

**Shell Scripts:**

```powershell
npm run lint:sh          # Lint shell scripts with ShellCheck
npm run format:sh        # Format shell scripts with shfmt
```

**Gherkin/Feature Files:**

```powershell
npm run lint:gherkin     # Validate all feature files
```

**JSON Files:**

```powershell
npm run lint:json        # Check all JSON files
npm run lint:json:fix    # Auto-fix JSON formatting
```

**All Linters:**

```powershell
npm run lint:all         # Run all linters (markdown, shell, JSON, Gherkin)
npm run lint:all:fix     # Auto-fix markdown, shell, and JSON (Gherkin manual)
```

**All Tools:**

```powershell
npm run tools:restore    # Install all npm and .NET tools
```

## Pre-Commit Hooks

Git hooks are provided in `scripts/pre-commit` and `scripts/commit-msg`. They lint staged
files before each commit and validate commit messages against Conventional Commits.

**Installation:**

```powershell
# Windows (PowerShell)
.\setup-dev.ps1
# Or manually: .\scripts\install-git-hooks.ps1

# Unix/Linux/macOS (Bash)
bash scripts/install-git-hooks.sh
```

**What it does:**

- **Markdown**: Auto-fixes with markdownlint (never blocks)
- **Gherkin**: Validates with gherkin-lint (blocks on errors)
- **JSON**: Auto-fixes with ESLint (never blocks)
- **Shell**: Lints with ShellCheck, formats with shfmt (never blocks)
- **PowerShell**: Lints with PSScriptAnalyzer if available (never blocks)
- **.NET C#**: Auto-fixes whitespace with `dotnet format` (blocks on failure)
- **Commit messages**: Validates `<type>[optional scope]: <description>` with `commitlint` (blocks on errors)

Bypass if needed: `git commit --no-verify`

## Commit Message Conventions

This repository uses [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) and enforces them with [`commitlint`](https://commitlint.js.org/).

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
"ci: add dependency review workflow" | npx commitlint
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

- [CI Overview](ci/overview.md) - GitHub Actions workflow structure, required checks, and
  links to maintainer-facing CI operations docs
- [Test Guidelines](TEST_GUIDELINES.md) - Testing strategy and coverage goals
- [BDD Guide](../tests/BDD_GUIDE.md) - Behavior-driven development patterns
- [Coding Guidelines](CODING_GUIDELINES.md) - .NET coding standards
