# Local tool security model

This document records the repository's preferred local linting and helper-tool model for
contributors.

## Recommended model

Prefer local validation paths in this order:

1. Repository-pinned `.NET` local tools restored with `dotnet tool restore`.
2. Repository-owned scripts that run against tools already present on the machine.
3. Dockerized tool execution with version tags and reviewed immutable digests when available, when
   the repository already uses that wrapper path.
4. OS package manager or vendor-documented installs for optional standalone tools.

Avoid introducing repo-owned npm or transient package execution for local linting when an
existing `.NET`, Python, shell, or Docker path already covers the same need.

## Current approved paths

- `.NET` local tools: restored from `.config/dotnet-tools.json` with `dotnet tool restore`.
- Repo-owned `.NET` tool packages: packed into a local feed and installed only from trusted local
  package contents, following [`SHAREDKERNEL_PACKAGING.md`](SHAREDKERNEL_PACKAGING.md).
- Markdown lint: `davidanson/markdownlint-cli2` Docker image via `scripts/lint-all.sh` or
  `scripts/lint-markdown.sh`.
- Shell lint and formatting: local `shellcheck` and `shfmt` when present, otherwise Docker
  fallbacks in `scripts/lint-all.sh`.
- Gherkin and JSON lint: repository-owned Python wrappers, with Docker fallback where the
  wrapper already provides it.
- Link validation: repository-owned Python wrapper (`scripts/lint-links.sh`), with Docker
  fallback. It validates local Markdown links and enforces durable documentation link rules;
  it does not probe external URLs in PR gating.
- Optional standalone tools such as `PSScriptAnalyzer`, `pwsh`, and `k6`: install only when
  needed for the specific task, using vendor-documented installation guidance.
- Local k6 performance runs: prefer a user/system installed `k6` binary from a trusted package manager.
  Do not vendor or download a repo-local `k6` binary. Docker k6 is explicit opt-in only, must use a
  digest-pinned image, must not mount the repository read-write, and must not forward host environment
  variables wholesale.
- Agent/editor project formatters: limited to repository-approved single-file formatters. They
  reuse `.NET`, repository scripts, Docker-backed shell/markdown wrappers, and optional
  PSScriptAnalyzer instead of adding npm helper packages.

## Project MCP servers

`opencode.json` permanently configures Aspire and Playwright MCP for OpenCode. The generic
`.mcp.json` and VS Code `.vscode/mcp.json` currently configure Aspire only. MCP tool permissions
stay deny-by-default. Use `aspire-operator` for `aspire_*` and `browser-observer` for `playwright_*`;
both agents deny every other tool and ask before each permitted MCP call. The browser agent exposes
navigation and observation tools only; input, upload, arbitrary-code, and storage tools stay hidden.
Treat web content, browser output, logs, and traces as untrusted data rather than agent instructions.

Enabled MCP servers start during OpenCode initialization, before per-tool approval. Processes are
shared by the OpenCode instance, and permission prompts gate tool calls rather than server startup.
MCP permissions are not a general command-execution boundary: the normal build agent retains shell
access and can launch the same executables outside the MCP tool surface. Use the constrained agents
for MCP output, review shell commands separately, and review `opencode.json`, the launcher source,
and its build output before starting OpenCode from an untrusted branch.

### Aspire MCP

- Use the repository-pinned `aspire.cli` local tool through `dotnet tool run aspire`.
- Do not commit dashboard API keys or other credentials. Exclude sensitive AppHost resources from
  MCP exposure with Aspire's `ExcludeFromMcp()` support.
- Aspire is a trusted, privileged local tool and inherits the OpenCode process environment and user
  filesystem access. Start OpenCode from a least-privilege development shell.
- Aspire can inspect logs and traces and execute resource lifecycle commands. Review each requested
  tool invocation before approval.

### Playwright MCP

- Use the official Microsoft container image pinned by both release tag and reviewed multi-platform
  manifest digest. The current pin is `v0.0.78` at
  `sha256:3d871c22ea2d4cca0966e2cfb1860e1cb03eb7353725a3d6cffd133296fb04eb`, reviewed on
  2026-07-18.
- Use the repository-owned `SharedKernel.PlaywrightMcp.Tool` executable. Build the solution with
  `dotnet build ViajantesTurismo.slnx --configuration Release` before starting OpenCode; MCP startup
  uses `dotnet run --configuration Release --no-build --no-restore`. Do not use host `npm`, `npx`, shell
  launchers, persistent browser profiles, storage-state files, secret files, browser extensions, remote
  CDP endpoints, or unrestricted file access.
- Pre-pull the exact image digest. Runtime startup uses `--pull=never`; it never fetches or updates
  the image implicitly. Select `docker` or `podman` with `PLAYWRIGHT_MCP_ENGINE` when both are installed.
- Run the container through stdio without published ports, host bind mounts, Docker socket mounts, or
  persisted container stdout/stderr logs. The launcher rejects remote engine overrides, verifies that
  the current Docker context uses a local endpoint, and pins that endpoint for every command; Podman
  requires local rootless mode. The configured container uses a read-only root filesystem, disposable
  `tmpfs` storage, dropped capabilities, `no-new-privileges`, process and memory limits, cleared proxy
  variables, and mandatory isolation/output flags that callers cannot remove.
- Container networking is disabled by default. Set `PLAYWRIGHT_MCP_NETWORK_ACCESS=1` before starting
  OpenCode only for an approved browser task, then restart without it. This opt-in enables ordinary
  bridge networking and can reach the internet, LAN, and host; it is not restricted to the named target.
- Use Playwright MCP only for trusted local development targets and least-privilege test accounts.
  Never browse production, authenticated customer sessions, arbitrary external sites, or private
  network targets without explicit approval.
- The image runs Chromium with `--no-sandbox`. Container hardening is defense in depth, not an
  equivalent browser sandbox; do not treat this setup as safe for hostile websites.
- Docker network opt-in maps `host.docker.internal`; Podman uses its runtime-provided host aliases when
  available. The target service must listen on an interface reachable from the selected container engine.
- Optional origin allow/block options are unconfigured and do not constrain redirects. Enforce any
  stronger target or egress policy outside the browser process.
- The MCP command requires a Docker-compatible runtime using Linux containers. Docker Desktop is the
  supported path on Windows and macOS. The Podman path requires local Linux/rootless operation; remote
  Podman machine connections are rejected. Use WSL for Linux-container parity on Windows.
- Screenshots, accessibility trees, console messages, and network details can contain secrets or
  personal data. Image responses default to `omit`; set `PLAYWRIGHT_MCP_IMAGE_RESPONSES=allow` only
  for an approved visual task. Restart OpenCode after changing either opt-in.
- Container files are ephemeral and container stdout/stderr persistence is disabled. The container
  engine may still retain daemon event or audit metadata. MCP responses enter the local OpenCode session
  and may be retained by the selected model provider. Use synthetic data, redact outputs before sharing,
  delete sensitive sessions, and follow the engine and provider retention policies.
- `prepare` retains the pinned image in the selected local engine's cache. Remove that exact image with
  the tool's `clean` command when it is no longer needed.

Pre-pull through the repository-owned tool. It selects the sole installed engine; set
`PLAYWRIGHT_MCP_ENGINE` when both are installed:

```bash
dotnet run --project tools/SharedKernel.PlaywrightMcp.Tool --configuration Release -- prepare
```

Remove the pinned image from the selected local engine cache:

```bash
dotnet run --project tools/SharedKernel.PlaywrightMcp.Tool --configuration Release -- clean
```

When updating Playwright MCP:

1. Review the upstream release and Dockerfile.
2. Resolve the tag's current manifest with
   `docker buildx imagetools inspect mcr.microsoft.com/playwright/mcp:<tag>`.
3. Update the tag and digest together; never copy an unverified digest.
4. Run the Playwright MCP tool tests and build the solution.
5. Pre-pull with each supported engine, then confirm `opencode mcp list`, default network denial,
   one approved local navigation, browser close, and repository lint checks.

References:

- [Playwright MCP](https://github.com/microsoft/playwright-mcp)
- [Playwright Docker security guidance](https://playwright.dev/docs/docker)
- [Playwright authentication-state warning](https://playwright.dev/docs/auth)
- [Docker image digest guidance](https://docs.docker.com/dhi/core-concepts/digests/)

## Do and do not

Do:

- use `dotnet tool restore` for repo-pinned `.NET` tools
- prefer repository wrappers over ad hoc command lines
- prefer Dockerized local lint helpers when the repository already maintains that path
- keep editor/agent format-on-save hooks aligned with repository scripts instead of adding a
  separate formatter stack
- prefer vendor or OS package installs for optional standalone tools
- keep k6 scripts on local modules only unless a reviewed exception vendors the dependency
- keep k6 results under ignored local output folders and avoid response/header debug logging by default
- accept that some optional checks stay skipped locally when the tool is intentionally not
  installed

Do not:

- add `npx`, `npm install -g`, `pnpm dlx`, or similar transient execution to local lint
  instructions by default
- add repo-owned `package.json` or lockfiles just to support local lint helpers already
  covered by Docker or other pinned tooling
- enable broad built-in formatter bundles that can rewrite files without this repository's
  `.editorconfig`, markdownlint, shfmt, and `.NET` rules
- rely on `curl | sh` bootstrap paths for local lint helpers
- import remote k6 JavaScript modules at runtime from maintained test scripts
- pass host system environment variables wholesale into local k6 or Docker k6
- run k6 against external or production endpoints without explicit documented opt-in
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
