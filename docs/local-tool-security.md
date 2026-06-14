# Local tool security model

This document records the repository's preferred local linting and helper-tool model for
contributors.

## Recommended model

Prefer local validation paths in this order:

1. Repository-pinned `.NET` local tools restored with `dotnet tool restore`.
2. Repository-owned scripts that run against tools already present on the machine.
3. Dockerized tool execution with pinned image tags when the repository already uses that
   wrapper path.
4. OS package manager or vendor-documented installs for optional standalone tools.

Avoid introducing repo-owned npm or transient package execution for local linting when an
existing `.NET`, Python, shell, or Docker path already covers the same need.

## Current approved paths

- `.NET` local tools: restored from `.config/dotnet-tools.json` with `dotnet tool restore`.
- Markdown lint: `davidanson/markdownlint-cli2` Docker image via `scripts/lint-all.sh` or
  `scripts/lint-markdown.sh`.
- Shell lint and formatting: local `shellcheck` and `shfmt` when present, otherwise Docker
  fallbacks in `scripts/lint-all.sh`.
- Gherkin and JSON lint: repository-owned Python wrappers, with Docker fallback where the
  wrapper already provides it.
- Optional standalone tools such as `PSScriptAnalyzer`, `pwsh`, and `k6`: install only when
  needed for the specific task, using vendor-documented installation guidance.

## Do and do not

Do:

- use `dotnet tool restore` for repo-pinned `.NET` tools
- prefer repository wrappers over ad hoc command lines
- prefer Dockerized local lint helpers when the repository already maintains that path
- prefer vendor or OS package installs for optional standalone tools
- accept that some optional checks stay skipped locally when the tool is intentionally not
  installed

Do not:

- add `npx`, `npm install -g`, `pnpm dlx`, or similar transient execution to local lint
  instructions by default
- add repo-owned `package.json` or lockfiles just to support local lint helpers already
  covered by Docker or other pinned tooling
- rely on `curl | sh` bootstrap paths for local lint helpers
- require contributors to install optional tooling just to complete ordinary `.NET`
  development and test workflows

## Practical rationale

- Transient npm execution is convenient but expands supply-chain trust at the exact point
  where contributors least review it.
- The repository already has a working npm-minimized local lint posture, so the safer choice
  is to formalize that model rather than add a second package ecosystem for helper tools.
- Dockerized wrappers keep local commands reproducible without forcing every contributor to
  install each linter directly.
- Repo-pinned `.NET` local tools remain the preferred path when the tool naturally belongs in
  the `.NET` tool manifest.

## Follow-up checklist

- Keep new local lint helpers npm-free unless a documented exception is approved.
- When adding a new helper tool, document whether it is repo-pinned, Dockerized, optional,
  or CI-only.
- If a future tool truly requires Node.js, require a dedicated review covering pinning,
  lockfiles, script execution behavior, and contributor UX before adopting it.
- Revisit optional `mdspell` usage before expanding it; today it stays best-effort and is not
  part of the required local or CI path.
- Treat unrelated local automation such as `.vscode/mcp.json` separately from the lint model
  unless that automation becomes part of the supported validation workflow.
