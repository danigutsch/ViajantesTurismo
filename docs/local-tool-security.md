# Local tool security model

This document records the repository's preferred local linting and helper-tool model for
contributors.

## Recommended model

Prefer local validation paths in this order:

1. Repository-pinned `.NET` local tools restored with `dotnet tool restore`.
2. Stage-0 setup scripts that run against tools already present on the machine.
3. Docker images pinned by reviewed immutable digests.
4. OS package manager or vendor-documented installs for optional standalone tools.

Avoid introducing repo-owned npm or transient package execution for local linting when an
existing `.NET`, shell, or Docker path already covers the same need.

## Current approved paths

- `.NET` local tools: restored from `.config/dotnet-tools.json` with `dotnet tool restore`.
- SharedKernel `.NET` tools (`dotnet ci`, `dotnet repo`, `dotnet release`,
  `dotnet docs`, `dotnet browsermcp`, `dotnet codefix`, `dotnet loadtest`): restored from the pinned
  candidate that `setup-dev.sh` or `setup-dev.ps1` stages in `.local/sharedkernel-feed`. Staging runs
  the CI tool's `stage-candidate` command from a sparse SharedKernel checkout at the pinned
  `libraryRevision`, which verifies each package hash before restore; see
  [SharedKernel consumption](SHAREDKERNEL_CONSUMPTION.md).
- Markdown lint: the digest-pinned `davidanson/markdownlint-cli2` Docker image, as the opencode
  `lint` and `lint-fix` commands and the Markdown formatter in `opencode.json` run it.
- Shell lint and formatting: local `shellcheck`; local `shfmt`, otherwise the digest-pinned
  `mvdan/shfmt` Docker image.
- Gherkin, JSON, line-ending, text-encoding, and roadmap checks: `dotnet repo`
  `verify-gherkin`, `verify-json`, `verify-line-endings`, `verify-text-encoding`, and `verify`.
- Link validation: `dotnet repo verify-links`. It validates local Markdown
  links and enforces durable documentation link rules; it does not probe external URLs in PR gating.
- Optional standalone tools such as `PSScriptAnalyzer`, `pwsh`, and `k6`: install only when
  needed for the specific task, using vendor-documented installation guidance.
- Local k6 performance runs: `dotnet loadtest` with a user/system installed `k6` binary from a trusted
  package manager, or with `--container` and the digest-pinned `grafana/k6` image.
  Do not vendor or download a repo-local `k6` binary. Docker k6 is explicit opt-in only, must use a
  digest-pinned image, must not mount the repository read-write, and must not forward host environment
  variables wholesale.
- Agent/editor project formatters: limited to repository-approved single-file formatters. They
  reuse `.NET`, repository scripts, Docker-backed shell/markdown wrappers, and optional
  PSScriptAnalyzer instead of adding npm helper packages.

## Script retention and migration

Use a pinned .NET tool for reusable commands, a repository-owned .NET tool for application
metadata, and an MSBuild target or task for build-graph work. The retained standalone shell
and PowerShell entry points each have a separate dependency and a condition for revisiting
that choice.

| Entry point | Owner and reason to retain it | Revisit when |
| --- | --- | --- |
| `scripts/ensure-dotnet-sdk.sh`, `scripts/ensure-dotnet-sdk.ps1` | Build tooling. They find or optionally install the exact SDK pinned in `global.json`, even when that SDK is missing. | A cross-platform bootstrap outside .NET can verify the exact SDK archive and install it beside existing SDK versions. |
| `setup-dev.sh`, `setup-dev.ps1` | Build tooling. They select the SDK, bootstrap the pinned SharedKernel CLI from source, and stage the verified package feed before restored .NET tools can run. | The same bootstrap can be invoked on clean Windows and Unix installations through a supported SDK or tool entry point. |
| `.devcontainer/on-create.sh` | Developer environment. The container hook prepares writable directories and SDK state before normal development commands run. | Dev Container features can establish the same ownership, cache directories, and pinned SDK for the non-root user. |
| `.devcontainer/post-create.sh` | Developer environment. The lifecycle hook loads the SDK environment from on-create, runs locked restore, and preserves the optional build check. | The lifecycle configuration can provide the same SDK environment and build option through direct commands. |
| `.devcontainer/post-start.sh` | Developer environment. The lifecycle hook reloads the pinned SDK environment before attempting to trust the development certificate. | The container configuration provides the pinned SDK environment and equivalent trust setup on every start. |
| `observability/tests/VerifyPostgreSqlServerMetrics.sh` | Observability. The fixture exercises the pinned external Collector and synthetic PostgreSQL independently of the application build. | A maintained .NET Collector fixture reproduces the healthy, disabled, denied and unavailable cases and the metadata privacy assertions. |

Both C# tools under `scripts/` stay repository-owned. `ViajantesTurismo.Architecture.Tool`
produces application architecture metadata for the shared documentation generator.
`ViajantesTurismo.AsyncApi.Tool` owns the application event inventory and checks the
[generated AsyncAPI contract](ASYNC_CONTRACTS.md).

### Migrated commands

| Former helpers | Current command or owner |
| --- | --- |
| `commitlint.sh`, `validate-commit-message.sh` | `dotnet ci verify-commit-message` validates local messages and CI event payloads. |
| `update-architecture-diagrams.sh`, `generate-invariant-coverage.ps1` | `dotnet docs generate` owns configured generated diagrams. The standalone invariant report generator was retired. |
| `refresh-dependency-lockfiles.sh` | `dotnet restore --force-evaluate` updates locks. `dotnet restore --locked-mode` verifies the committed locks. |
| `ci-test-slices/`, `run-ci-test-slice.sh`, `benchmark-local-validation.sh`, `collect-ci-build-test-diagnostics.sh`, `write-ci-artifact-summary.sh` | `dotnet ci select` and `run` use `.config/sharedkernel-ci.json` and emit slice timing and diagnostic evidence. |
| `collect-test-coverage.sh`, `run-tests-with-coverage.sh` | `dotnet ci run --coverage`, `coverage record-symbols`, and `coverage verify` collect and gate changed-code coverage. |
| `install-playwright.sh` | `dotnet ci install-playwright` installs browsers for the hosted test project. |
| `lint-links.py`, `lint-links.sh`, `test-lint-links.py`, `lint-gherkin.py`, `lint-gherkin.sh`, `check-line-endings.sh` | `dotnet repo verify-links`, `verify-gherkin`, and `verify-line-endings` own the checks. |
| `lint-all.sh`, `lint-markdown.sh`, `format-powershell-file.ps1` | CI Lint and the commands listed above cover repository validation. Digest-pinned Markdownlint and optional PSScriptAnalyzer cover their respective file types. |
| `run-devcontainer-smoke.sh`, `run-mutation-smoke.sh` | `dotnet repo verify-devcontainer` and `dotnet ci run-mutation-tests` run the targeted checks. |

See [CI validation and local reproduction](ci/artifacts-and-local-reproduction.md) for the
commands used by the current workflow.

## Local tool restore ordering

Keep the RID-specific `aspire.cli` entry last in `.config/dotnet-tools.json`. The pinned SDK can reuse
stale RID asset metadata during a cold `dotnet tool restore`, misidentifying later portable tool
commands as `aspire`; ordinary restore and `--disable-parallel` both reproduced the failure.
The proposed upstream fix was closed without merging. Remove this ordering constraint only after
an upstream fix is available in the pinned SDK and cold restore is verified.

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
for MCP output, review shell commands separately, and review `opencode.json` and the pinned
`PandaBear.SharedKernel.PlaywrightMcp.Tool` version in `.config/dotnet-tools.json` before starting OpenCode from an
untrusted branch.

### Aspire MCP

- Use the repository-pinned `aspire.cli` local tool through `dotnet tool run aspire`.
- Do not commit dashboard API keys or other credentials. Exclude sensitive AppHost resources from
  MCP exposure with Aspire's `ExcludeFromMcp()` support.
- Aspire is a trusted, privileged local tool and inherits the OpenCode process environment and user
  filesystem access. Start OpenCode from a least-privilege development shell.
- Aspire can inspect logs and traces and execute resource lifecycle commands. Review each requested
  tool invocation before approval.

#### Local AppHost discovery

Restore the pinned tools and start the AppHost with `dotnet tool run aspire run`. Start the configured
MCP server from the same development workspace. Use `list_apphosts` to locate the running AppHost and
`select_apphost` with the project path when several are running. Use `list_resources` to check state and
health. Review scoped console logs, structured logs, or traces for the chosen resource when state alone
does not explain the failure. Keep diagnostic output inside the trusted local session and record only
the relevant names, states, and health results when sharing evidence.

| Local resource | Read-only workflow |
| --- | --- |
| PostgreSQL | Check `database-server` and its database resources. Check migration completion before investigating connection or readiness errors. |
| Redis | Check `cache` health and its scoped console logs. |
| ClamAV | Check scan service health and scoped startup logs. Use synthetic files for application-level diagnostics. |
| SeaweedFS | Check the object storage resource state and health. Keep object contents and storage credentials out of the diagnostic record. |
| Observability | Start the AppHost with `ASPIRE_ENABLE_OBSERVABILITY_STACK=1` to discover `grafana`, `loki`, `tempo`, and `prometheus` alongside `opentelemetry-collector`. Check the Collector before inspecting a downstream backend. |

The pinned Aspire CLI `17.0.0` answered the stdio `initialize` and `tools/list` requests with 15 tools.
The diagnostic workflow uses `list_apphosts`, `select_apphost`, `list_resources`, and scoped read tools.
The earlier isolated AppHost check on `13.6.5` found each resource above Running and Healthy with the
optional observability stack enabled.
Use the HTTPS launch profile in an isolated AppHost with synthetic data for this check.
Resource responses also include URLs and environment metadata. Keep only the resource names, states,
and health outcomes in the validation receipt.
Stop the AppHost after collecting the result. The [AppHost runbook](../src/ViajantesTurismo.AppHost/README.md)
describes the resource graph and telemetry configuration.

#### Resource-specific tools

Use the Aspire resource health and scoped diagnostic tools for the current AppHost resources. Add a
service-specific MCP server when it answers a diagnostic question that these tools cannot answer and
the service supplies a least-privilege identity for that question.

| Resource | Tool decision |
| --- | --- |
| PostgreSQL | Use the approved server and index-health dashboards. A SQL MCP server would need a separately provisioned identity and a bounded query contract for the required metrics. [PostgreSQL monitoring](architecture/postgresql-observability.md) |
| Redis | Keep health and startup diagnostics in Aspire. The Redis MCP server includes key access, writes, and management tools. A future trial requires synthetic cache data and a restricted Redis ACL. [Redis MCP capabilities](https://redis.io/docs/latest/integrate/redis-mcp/) |
| ClamAV | Use the scanner resource's health and scoped console logs. Test scanner behavior through the application's existing synthetic-file fixture. [ClamAV daemon commands](https://docs.clamav.net/manual/Usage/Scanning.html#clamd) |
| SeaweedFS | Use the resource health and the storage integration fixture. Keep bucket contents and workload credentials within the S3 client boundary. [SeaweedFS S3 API](https://github.com/seaweedfs/seaweedfs/wiki/Amazon-S3-API) |
| Collector, Prometheus, Loki, and Tempo | Use Aspire resource health and the source-provisioned Grafana dashboards. Review exported snapshots through the existing Collector privacy boundary. [Local observability stack](../observability/README.md) |
| PostgreSQL monitoring project | Use the index-health advisory dashboard and its bounded outcomes. Keep monitoring connection values and database object names in the existing monitoring boundary. |
| Keycloak | Inspect resource health and OIDC discovery for the synthetic development realm. Realm, user, role, and token operations require a separately approved administrator workflow. |
| PgWeb and Redis Insight | Use the existing local browser tools for approved diagnostic work. Their interactive queries and key access stay outside the MCP workflow. |
| k6 performance smoke | Use the pinned opt-in AppHost smoke scenario with its synthetic identity. Read its aggregate test result through the existing performance workflow. |
| Migration, API, frontend, and worker projects | Use the scoped Aspire resource status and redacted runtime diagnostics. Their existing HTTP, storage, and messaging boundaries own application operations. |
| Grafana | Use the optional metadata-only stdio workflow below to locate a provisioned dashboard or inspect a panel definition. |

##### Grafana dashboard metadata

Use a disposable local Grafana instance on an internal container network with anonymous access
disabled. Grant the dedicated service account Viewer permissions for the diagnostic dashboard and
keep its token and instance address in the operator's environment. Pre-pull the reviewed image
digest for Grafana MCP `2.3.0` before starting the client. [Grafana MCP release](https://github.com/grafana/mcp-grafana/releases/tag/v2.3.0) ·
[Service-account authentication](https://grafana.com/docs/grafana/latest/ai-tools/mcp/configure/authentication/)

```bash
podman run --rm --interactive --pull=never --network "$GRAFANA_MCP_NETWORK" \
  --read-only --cap-drop ALL --security-opt no-new-privileges \
  --env GRAFANA_URL --env GRAFANA_SERVICE_ACCOUNT_TOKEN \
  docker.io/grafana/mcp-grafana@sha256:97d82eea2a7d493beafc56eaa4572dcfbe4f7255ed193cfa12da07ba62c8a3fa \
  --transport stdio --enabled-tools dashboard,search --disable-write --disable-query --usage-stats disabled
```

The local proof used `search_dashboards` and `get_dashboard_summary` on the file-provisioned
PostgreSQL dashboard. The server also exposed folder search, dashboard lookup, property and panel
query definition reads, and dashboard version listing. All seven tools advertised read-only
semantics. The Viewer could read the dashboard and received HTTP 403 when attempting to save one.
Anonymous dashboard search returned HTTP 401. Close the stdio session, revoke the token, and stop
the disposable instance to end the workflow.
[Grafana tool controls](https://grafana.com/docs/grafana/latest/ai-tools/mcp/configure/enable-and-disable-tools/)

### Playwright MCP

- Use the official Microsoft container image pinned by both release tag and reviewed multi-platform
  manifest digest. The current pin is `v0.0.78` at
  `sha256:3d871c22ea2d4cca0966e2cfb1860e1cb03eb7353725a3d6cffd133296fb04eb`, reviewed on
  2026-07-18.
- Use the pinned Playwright MCP local tool. Run `dotnet tool restore` before starting
  OpenCode; MCP startup uses `dotnet browsermcp serve`. Do not use host `npm`, `npx`, shell
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

Pre-pull through the pinned tool. It selects the sole installed engine; set
`PLAYWRIGHT_MCP_ENGINE` when both are installed:

```bash
dotnet browsermcp prepare
```

Remove the pinned image from the selected local engine cache:

```bash
dotnet browsermcp clean
```

When updating Playwright MCP:

1. Review the upstream release and Dockerfile.
2. Resolve the tag's current manifest with
   `docker buildx imagetools inspect mcr.microsoft.com/playwright/mcp:<tag>`.
3. Update the tag and digest together in SharedKernel's `SharedKernel.PlaywrightMcp.Tool`; never copy an
   unverified digest.
4. Bump the SharedKernel pin to a candidate that ships the update and run `dotnet tool restore`.
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
- Treat unrelated local automation such as `.vscode/mcp.json` separately from the lint model
  unless that automation becomes part of the supported validation workflow.
