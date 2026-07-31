# Text Encoding and Line Endings

The repository uses committed Git attributes as the source of truth for line-ending
normalization. This avoids noisy diffs caused by local `core.autocrlf` settings or editor
defaults.

## Policy

- Tracked non-binary text files use UTF-8. Editors save new and changed text without a BOM.
- Valid UTF-8 BOMs remain accepted during the current incremental migration.
- Tracked non-binary files containing NUL bytes or invalid UTF-8 are rejected.
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

Run the focused text-encoding check:

```bash
dotnet run --project tools/SharedKernel.RepoConfig.Tool -- text-encoding --root .
```

The .NET command is the authoritative encoding check. It parses NUL-delimited stage records from
`git ls-files --stage -z`, processes only stage-0 regular modes `100644` and `100755`, and skips
symlink and gitlink entries. It queries index attributes with `git check-attr --cached -z --stdin
binary`, skipping content only when `binary` is exactly `set`. Assigned, `unset`, `unspecified`, and
`merge=binary` paths remain scanned.

`git check-attr --cached` still reads repository `info/attributes`, global `core.attributesFile`, and
system attributes; `--source` does not provide index-only isolation. Before checking attributes, the
command therefore clears inherited Git routing, configuration, attribute, object, index, alternate,
and replacement variables. It discovers the worktree-specific index, object directory, shared-index
companion when present, and storage object format through isolated `git rev-parse` calls. It then
creates a private temporary bare Git directory with the matching SHA-1 or SHA-256 format, copies the
exact index snapshot and shared-index companion, and supplies empty private info, global, and core
attribute files. The private Git directory reads blobs from the original physical object directory,
including repository-declared object alternates, without copying object storage or accepting
caller-provided alternate routing. Both `check-attr --cached` and `cat-file --batch` use this same
isolated environment with replacement objects disabled. Isolation setup failures fail closed.

Content is streamed by object ID through `git cat-file --batch`, so working-tree files and symlinks
are never followed. Each text blob is capped at 64 MiB. Valid UTF-8 BOMs remain accepted during the
incremental migration; NUL bytes and strict UTF-8 decoding failures are rejected. Diagnostics escape
control characters and never include blob content or raw Git errors.

The root `.editorconfig` entry is editor guidance for new and changed files. Neither `dotnet format`
nor Git line-ending attributes are the authoritative encoding validator.

Run the complete repository lint entrypoint:

```bash
bash scripts/lint-all.sh
```

The lint entrypoint runs `scripts/check-line-endings.sh`, which uses `git ls-files --eol` to
verify that checked-out text files match the committed `.gitattributes` policy.
