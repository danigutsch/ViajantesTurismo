# Markdown Linting

This project uses [markdownlint](https://github.com/DavidAnson/markdownlint) to enforce consistent markdown formatting
across all documentation files.

## Why Markdown Linting?

Following industry best practices from projects
like [.NET Docs](https://github.com/dotnet/docs), [GitHub Docs](https://github.com/github/docs),
and [Electron](https://github.com/electron/electron), we use a **multi-layer approach**:

1. **Editor Integration** - Real-time feedback as you type (primary quality gate)
2. **Pre-commit Hooks** - Optional safety net before committing
3. **CI/CD Pipeline** - Enforces standards for all team members

**Note**: We deliberately **do not** run Markdown linting during .NET builds. Documentation quality checks are separate
from code compilation, preventing unnecessary build failures and keeping builds fast.

## Configuration

Markdown linting rules are defined in `.markdownlint.json` at the solution root.

**Key Rules:**

- **MD013**: Line length limited to 300 characters (excludes tables and code blocks)
- **MD007**: List indentation uses 4 spaces (matches official Markdown Guide)
- **MD024**: Duplicate headings allowed in different sections (`siblings_only: true`)
- **All other rules**: Enabled at maximum strictness

See the actual `.markdownlint.json` file for the current configuration rather than a potentially outdated snapshot in
documentation.

## Local Development

### Prerequisites

Install Node.js dependencies:

```powershell
npm install
```

This installs `markdownlint-cli` as defined in `package.json`.

### Available Scripts

**Check all markdown files:**

```powershell
npm run lint:md
```

**Auto-fix markdown issues:**

```powershell
npm run lint:md:fix
```

### VS Code Integration

The [markdownlint extension](https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint) is *
*already installed** and provides:

- **Real-time linting** as you type (primary quality gate)
- **Inline error messages** with rule explanations
- **Quick fixes** (Ctrl+. or Cmd+.)
- Automatic use of `.markdownlint.json` configuration

This is your first line of defense - fix issues as you write!

## CI/CD Integration

Markdown linting runs in your CI/CD pipeline to enforce quality standards across the team.

### GitHub Actions Example

```yaml
- name: Lint Markdown
  run: npm run lint:md
```

### Azure Pipelines Example

```yaml
- script: npm run lint:md
  displayName: 'Lint Markdown Files'
```

## Git Hooks

A universal pre-commit hook (using bash/sh) is provided in the `scripts/` directory. It works on Windows (via Git Bash),
Linux, and macOS.

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

The hook will:

- Detect staged `.md` files before each commit
- Run `markdownlint-cli` on only the changed files (fast!)
- Block the commit if linting fails
- Allow bypass with `git commit --no-verify` if needed

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
