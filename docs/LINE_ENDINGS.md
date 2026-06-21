# Line Endings

The repository uses committed Git attributes as the source of truth for line-ending
normalization. This avoids noisy diffs caused by local `core.autocrlf` settings or editor
defaults.

## Policy

- Git stores text files normalized as LF in the index.
- All text files check out as LF, including C#, MSBuild, JSON, Markdown, YAML, shell,
  feature, XML, and lock files.
- Binary assets are marked as binary and are never normalized.
- `.editorconfig` mirrors the same editing policy for IDEs and editors.

## Contributor Workflow

Do not rely on personal Git settings to define repository behavior. The committed
`.gitattributes` file overrides local `core.autocrlf` for paths covered by this repo.

Recommended local Git settings remain platform-friendly:

```bash
git config --global core.autocrlf input
```

Windows users may keep `core.autocrlf=true`, but the repository attributes still define LF
checkout behavior for explicitly configured paths.

## Renormalizing Files

When `.gitattributes` changes, refresh tracked line endings from a clean worktree:

```bash
git add --renormalize .
git status --short
```

Review the resulting diff carefully. Line-ending normalization changes should be isolated in a
dedicated pull request whenever they touch many files.

## Validation

Run the repository lint entrypoint:

```bash
bash scripts/lint-all.sh
```

The lint entrypoint runs `scripts/check-line-endings.sh`, which uses `git ls-files --eol` to
verify that checked-out text files match the committed `.gitattributes` policy.
