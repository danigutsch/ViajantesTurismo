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

- **MD013**: Line length limited to **120 characters** (excludes tables, code blocks, and headings)
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

### Available Scripts

**Check all markdown files:**

```powershell
npm run lint:md
```

**Auto-fix markdown issues:**

```powershell
npm run lint:md:fix
```

Markdown files are automatically fixed and re-staged by the pre-commit hook.

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

### Available Scripts

**Lint shell scripts:**

```powershell
npm run lint:sh
```

**Format shell scripts:**

```powershell
npm run format:sh
```

**Manual linting (alternative):**

```powershell
npx shellcheck setup-dev.sh scripts/*.sh
```

**Manual formatting (alternative):**

```powershell
npx shfmt -w -i 2 setup-dev.sh scripts/*.sh
```

## PowerShell Linting

**PSScriptAnalyzer** is a PowerShell module that analyzes PowerShell scripts for best practices, security issues, and
code quality.

**Installation** (optional):

```powershell
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
```

The pre-commit hook will use PSScriptAnalyzer if available but will skip it if not installed.

---

## Gherkin/Feature File Linting

### Configuration

Gherkin linting rules are defined in `.gherkin-lintrc` at the solution root.

**Key Rules:**

- **Mandatory tags**: `@BC:<BoundedContext>` and `@Agg:<Aggregate>` required on all features
- **Tag validation**: Enforces project-specific tag patterns (bounded contexts, aggregates, invariants)
- **Indentation**: Consistent 2-space indentation (Feature: 0, Background/Rule/Scenario: 2, Steps: 4)
- **BDD anti-patterns**: Prevents conjunction steps, unnamed features/scenarios
- **Formatting**: No trailing spaces, newline at EOF, no duplicate scenario names

### Available Scripts

**Check all feature files:**

```powershell
npm run lint:gherkin
```

**Note:** gherkin-lint validates but does not support auto-fix. Issues must be corrected manually.

### Pre-commit Integration

Gherkin files are validated during pre-commit and will block commits if validation errors are found.

See `tests/BDD_GUIDE.md` for comprehensive Gherkin linting documentation.

---

## JSON File Linting

### Configuration

JSON linting rules are defined in `eslint.config.mjs` at the solution root using ESLint with the JSONC plugin.

**Key Rules:**

- **Indentation**: 2 spaces
- **Spacing**: Proper key-value spacing, object curly spacing
- **Quotes**: Double quotes for all keys and values
- **Comments**: Allowed in JSON files (e.g., tsconfig.json, package.json)
- **Comma**: No trailing commas

### Available Scripts

**Check all JSON files:**

```powershell
npm run lint:json
```

**Auto-fix JSON formatting:**

```powershell
npm run lint:json:fix
```

### Pre-commit Integration

JSON files are automatically formatted and re-staged during pre-commit.

---

## PowerShell Linting (Optional)

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

**General Settings (C#):**

- `end_of_line = crlf` - Windows-style line endings for C# files
- `insert_final_newline = true` - Ensures files end with a newline
- `indent_size = 4` - 4-space indentation
- `tab_width = 4` - Tab width of 4 spaces

**Naming Conventions (severity: warning):**

- Interfaces must start with `I` (e.g., `IRepository`)
- Types use PascalCase (classes, structs, enums)
- Methods, properties, events use PascalCase

**Code Style (severity: warning):**

- Prefer null coalescing (`??`) and null propagation (`?.`)
- Use object and collection initializers
- Use explicit tuple names
- Prefer compound assignments (`+=`, `-=`)
- Prefer simplified boolean expressions and interpolation

**C# Specific (severity: warning):**

- Using directives outside namespace
- Prefer simple using statements
- Always use braces for control structures
- File-scoped namespaces
- Prefer primary constructors
- Prefer pattern matching over `is` with cast

**Other File Types:**

- Shell scripts (`.sh`): LF line endings, 4-space indent
- PowerShell (`.ps1`): CRLF line endings, 4-space indent
- JSON (`.json`): 2-space indent
- YAML (`.yml`, `.yaml`): 2-space indent
- Markdown (`.md`): Trim trailing whitespace, final newline

#### Build Configuration

**`Directory.Build.props`** enforces code analysis at build time:

- `TreatWarningsAsErrors = true` - All warnings become errors
- `CodeAnalysisTreatWarningsAsErrors = true` - Analysis warnings become errors
- `EnforceCodeStyleInBuild = true` - Style rules checked during build
- `AnalysisLevel = latest` - Use latest .NET analyzers
- `AnalysisMode = All` - Enable all analyzer categories

**Built-in Analyzers:**

- **Microsoft.CodeAnalysis.NetAnalyzers** - Included with .NET SDK 5.0+
- Automatically enabled for projects targeting .NET 5 or later
- No separate NuGet package needed

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

Or run the automated setup script:

```powershell
.\setup-dev.ps1
```

### NPM Commands

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
npm run lint:all         # Run all linters
npm run lint:all:fix     # Auto-fix (markdown, shell, JSON)
```

**All Tools:**

```powershell
npm run tools:restore    # Install all npm and .NET tools
```

### VS Code Integration

**Markdown:**

The [markdownlint extension](https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint)
provides:

- **Real-time linting** as you type (primary quality gate)
- **Inline error messages** with rule explanations
- **Quick fixes** (Ctrl+. or Cmd+.)
**Shell scripts (`.sh`, `.bash`, `scripts/pre-commit`):**

- Lints with ShellCheck using `.shellcheckrc` configuration
- **Blocks commit on errors** (critical issues like SC2086, SC2115, SC2154, SC2155)
- Shows warnings for style issues (non-blocking)
- Auto-formats with shfmt if available (2-space indent, indent switch cases, binary ops at line start,
  space after redirects)
- Re-stages formatted files

**PowerShell scripts (`.ps1`, `.psm1`, `.psd1`):**

- Lints with PSScriptAnalyzer if available
- Shows warnings but never blocks commits

```powershell
# Windows (PowerShell)
Copy-Item scripts/pre-commit .git/hooks/pre-commit

# Unix/Linux/macOS (Bash)
cp scripts/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

### What the Hook Does

The pre-commit hook automatically processes staged files:

**Markdown files (`.md`):**

- Auto-fixes formatting with `markdownlint-cli --fix`
- Re-stages fixed files
- Never blocks commits

**Gherkin/Feature files (`.feature`):**

- Validates with `gherkin-lint`
- **Blocks commits** if validation errors found (e.g., invalid tags, incorrect indentation)
- Manual fixes required (no auto-fix support)

**JSON files (`.json`):**

- Auto-fixes formatting with `eslint --fix`
- Re-stages fixed files
- Never blocks commits

**Shell scripts (`.sh`, `.bash`, `scripts/pre-commit`):**

- Lints with ShellCheck (warnings only, non-blocking)
- Auto-formats with shfmt (4-space indent)
- Re-stages formatted files
- Never blocks commits

**PowerShell scripts (`.ps1`, `.psm1`, `.psd1`):**

- Lints with PSScriptAnalyzer if available
- Shows warnings but never blocks commits

**.NET C# files (`.cs`):**

- Auto-fixes whitespace with `dotnet format whitespace`
- Re-stages formatted files
- Blocks commit if formatting fails (rare)

### Bypass Hook

If needed, bypass the hook with:

```powershell
git commit --no-verify
```

**Note**: Git hooks are local to your repository and not tracked in version control. Each developer chooses whether to
install them.

## Common Issues

### Generic Type Parameters

C# generic types like `Result<T>` must be escaped to avoid triggering MD033 (no-inline-html):

```markdown
❌ Wrong: Result<T>
✅ Correct: Result\<T\>
```

### Tag Placeholders

Documentation placeholders must also be escaped:

```markdown
❌ Wrong: @ADR:<number>
✅ Correct: @ADR:\<number\>
### Line Length

**.NET C# files (`.cs`):**

- Auto-fixes whitespace with `dotnet format whitespace` (line endings, indentation, trailing spaces)
- Re-stages formatted files
- **Blocks commit if formatting fails** (rare)
- EditorConfig rules enforced with warning-as-error severity

## Pre-Commit Hooks

A universal pre-commit hook (using bash/sh) is provided in the `scripts/` directory. It automatically lints and formats
code before each commit. The hook works on Windows (via Git Bash), Linux, and macOS.

### Installation

**Automated Installation (Recommended):**

```powershell
# Windows (PowerShell)
.\setup-dev.ps1
# Or manually:
.\scripts\install-git-hooks.ps1

# Unix/Linux/macOS (Bash)
bash scripts/install-git-hooks.sh
```

**Manual Installation:**

```powershell
# Windows (PowerShell)
Copy-Item scripts/pre-commit .git/hooks/pre-commit

# Unix/Linux/macOS (Bash)
cp scripts/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

### What the Hook Does

The pre-commit hook automatically processes staged files:

**Markdown files (`.md`):**

- Auto-fixes formatting with `markdownlint-cli --fix` using `.markdownlint.json` configuration
- Re-stages fixed files
- Never blocks commits

**Shell scripts (`.sh`, `.bash`, `scripts/pre-commit`):**

- Lints with ShellCheck using `.shellcheckrc` configuration
- **Blocks commit on critical errors** (SC2086, SC2115, SC2154, SC2155)
- Auto-formats with shfmt if available (4-space indent, indent switch cases, binary ops at line start,
  space after redirects)
- Re-stages formatted files

**PowerShell scripts (`.ps1`, `.psm1`, `.psd1`):** be broken across multiple lines for readability.
If absolutely necessary, you can disable the rule for that line:

```markdown
<!-- markdownlint-disable-next-line MD013 -->
This is an exceptionally long line that cannot be broken...
```

**Note:** With the stricter 120-character limit, most prose and documentation should fit comfortably.
URL-only lines in ADR context sections are acceptable exceptions.- markdownlint-disable-next-line MD013 -->
This is an exceptionally long line that cannot be broken...

```html
<!-- markdownlint-disable-next-line MD013 -->
```

## Benefits

1. **Consistency** - All markdown files follow the same formatting standards
2. **Quality** - Catches common markdown mistakes before they reach production
3. **Readability** - Enforces best practices for readable documentation
4. **Automation** - Integrated into editor and CI/CD pipelines
5. **Team Alignment** - Everyone works with the same rules

## Documentation Best Practices

**⚠️ Important**: When documenting configuration files, code samples, or other artifacts that change over time:

- **Reference** the actual file location rather than duplicating its contents
- **Describe** what the file does and key concepts, not the exact syntax
- **Avoid** code snippets that will become outdated as the codebase evolves

**Why?** Duplicated configuration snippets in documentation inevitably drift from reality, causing confusion and
maintenance burden. The source code is the single source of truth.

**Example**: Instead of showing the full `.markdownlint.json` content, we reference its location and describe its key
rules. Developers can always view the actual file for current settings.

## Related Documentation

- [Markdown Guide](https://www.markdownguide.org/basic-syntax/) - Official markdown syntax reference
- [markdownlint Rules](https://github.com/DavidAnson/markdownlint/blob/main/doc/Rules.md) - Complete rule documentation
- [Coding Guidelines](CODING_GUIDELINES.md) - Project coding standards
