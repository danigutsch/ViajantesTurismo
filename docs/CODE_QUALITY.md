# Code Quality Tools

This project uses automated tools to enforce consistent formatting and style across both documentation and code files.

## Tools Overview

- **[markdownlint](https://github.com/DavidAnson/markdownlint)** - Markdown documentation formatting
- **[ShellCheck](https://www.shellcheck.net/)** - Bash/shell script linting
- **[shfmt](https://github.com/mvdan/sh)** - Bash/shell script formatting
- **[PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer)** - PowerShell script linting
- **[dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)** - .NET code formatting

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

**Key Rules:**

- **MD013**: Line length limited to 300 characters (excludes tables and code blocks)
- **MD007**: List indentation uses 4 spaces (matches official Markdown Guide)
- **MD024**: Duplicate headings allowed in different sections (`siblings_only: true`)
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

- **ShellCheck** - Lints bash/shell scripts for common issues and best practices
- **shfmt** - Formats shell scripts with consistent indentation (4 spaces, indent case statements)

### Available Scripts

**Lint shell scripts:**

```powershell
npx shellcheck scripts/pre-commit
```

**Format shell scripts:**

```powershell
npx shfmt -w -i 4 -ci scripts/pre-commit
```

Shell scripts are automatically linted and formatted by the pre-commit hook.

### VS Code Integration

Install the [ShellCheck extension](https://marketplace.visualstudio.com/items?itemName=timonwong.shellcheck) for
real-time linting.

## PowerShell Script Linting

### Tool

- **PSScriptAnalyzer** - Lints PowerShell scripts for best practices, security issues, and code quality

### Installation

```powershell
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
```

This is automatically installed by `setup-dev.ps1`.

### Available Commands

**Lint PowerShell script:**

```powershell
Invoke-ScriptAnalyzer -Path script.ps1 -Settings PSGallery
```

PowerShell scripts are automatically analyzed (non-blocking warnings) by the pre-commit hook.

### IDE Integration

- **VS Code**: The [PowerShell extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.PowerShell)
  includes integrated PSScriptAnalyzer
- **Visual Studio**: Built-in PowerShell Tools include analysis

## .NET Code Formatting

### Configuration

Code formatting rules are defined in `.editorconfig` at the solution root.
This file follows [EditorConfig](https://editorconfig.org/) standards and is automatically recognized
by Visual Studio, Rider, and VS Code.

**Key Settings:**

- `end_of_line = crlf` - Windows-style line endings for C# files
- `insert_final_newline = true` - Ensures files end with a newline
- `indent_size = 4` - 4-space indentation
- `tab_width = 4` - Tab width of 4 spaces

### Available Commands

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

**Format with detailed output:**

```powershell
dotnet format --verbosity diagnostic
```

.NET code is automatically formatted (whitespace only) and re-staged by the pre-commit hook.

### IDE Integration

- **Visual Studio**: Enable "Format document on save" in Tools → Options → Text Editor → Code Cleanup
- **Rider**: Enable "Reformat code" in Settings → Tools → Actions on Save
- **VS Code**: Install [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
  extension and enable format on save

## Local Development

### Prerequisites

Install Node.js dependencies:

```powershell
npm install
```

This installs:

- `markdownlint-cli` - Markdown linting
- `shellcheck` - Shell script linting
- `shfmt` - Shell script formatting

Install PowerShell dependencies:

```powershell
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
```

Or run the automated setup script:

```powershell
.\setup-dev.ps1
```

### Available Scripts

**Markdown:**

```powershell
npm run lint:md          # Check all markdown files
npm run lint:md:fix      # Auto-fix markdown issues
```

**All checks:**

```powershell
npm run lint:md          # Markdown only (for now)
```

### VS Code Integration

**Markdown:**

The [markdownlint extension](https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint)
provides:

- **Real-time linting** as you type (primary quality gate)
- **Inline error messages** with rule explanations
- **Quick fixes** (Ctrl+. or Cmd+.)
- Automatic use of `.markdownlint.json` configuration

**Shell Scripts:**

Install [ShellCheck extension](https://marketplace.visualstudio.com/items?itemName=timonwong.shellcheck) for real-time
bash linting.

**PowerShell:**

Install [PowerShell extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.PowerShell) for integrated
PSScriptAnalyzer.

## CI/CD Integration

Both markdown linting and code formatting run in your CI/CD pipeline to enforce quality standards across the team.

### GitHub Actions Example

```yaml
- name: Lint Markdown
  run: npm run lint:md

- name: Lint Shell Scripts
  run: npx shellcheck scripts/**/*.sh scripts/pre-commit

- name: Check Code Formatting
  run: dotnet format --verify-no-changes --verbosity diagnostic
```

### Azure Pipelines Example

```yaml
- script: npm run lint:md
  displayName: 'Lint Markdown Files'

- script: npx shellcheck scripts/**/*.sh scripts/pre-commit
  displayName: 'Lint Shell Scripts'

- script: dotnet format --verify-no-changes --verbosity diagnostic
  displayName: 'Check Code Formatting'
```

## Git Hooks

A universal pre-commit hook (using bash/sh) is provided in the `scripts/` directory. It automatically lints and formats
code before each commit. The hook works on Windows (via Git Bash), Linux, and macOS.

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

- Auto-fixes formatting with `markdownlint-cli --fix`
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
```

### Line Length

If a line legitimately exceeds 300 characters (rare), you can disable the rule for that line:

```markdown
<!-- markdownlint-disable-next-line MD013 -->
This is an exceptionally long line that cannot be broken...
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
