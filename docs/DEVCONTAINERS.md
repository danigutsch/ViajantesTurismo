# Dev Containers

Contributor-facing guidance for the repository's containerized development path.

This repository includes a `.devcontainer/devcontainer.json` configuration for local
VS Code Dev Containers.

## Support stance

| Path | Status | Notes |
| --- | --- | --- |
| VS Code Dev Containers | Supported | Primary documented containerized workflow for contributors using the local repository checkout. |

## What the container configuration provides

The current `.devcontainer/devcontainer.json` defines a shared environment with:

- Ubuntu 26.04 LTS via the digest-pinned `mcr.microsoft.com/devcontainers/dotnet:dev-11.0-resolute` image,
  referenced by digest alone because the Dev Containers CLI cannot parse a tag together with a digest
- an image digest that is refreshed by hand: Dependabot's `devcontainers` ecosystem updates Features only, so a new
  digest comes from resolving the tag at `mcr.microsoft.com` and is validated with a `validation_mode=full` smoke run
- the exact .NET SDK from `global.json`, installed by `.devcontainer/on-create.sh`
- Git and Docker-in-Docker Dev Container Features
- repository-specific VS Code extensions and default settings
- lifecycle commands for one-time setup, dependency restore, and build verification
- named volume mounts for NuGet packages, ASP.NET state, and `.build` output reuse
- requested host resources of 8 CPUs, 32 GB RAM, and 64 GB storage

The current lifecycle flow is:

1. `onCreateCommand` runs `.devcontainer/on-create.sh`, which installs the pinned SDK and runs
   `setup-dev.sh` to stage the pinned SharedKernel candidate
2. `postCreateCommand` runs `.devcontainer/post-create.sh`
3. `postStartCommand` runs `.devcontainer/post-start.sh`, which trusts the development certificate

`Directory.Build.props` moves `bin` and `obj` to `.build` when `REMOTE_CONTAINERS` is `true`. Builds that run with a
minimal environment, such as OpenAPI generation through `sharedkernel-ci`, drop `REMOTE_CONTAINERS` unless the target
lists it in `preserveEnvironment`, so every `openApi` target of `.config/sharedkernel-ci.json` lists it and keeps its
paths equal to the restore.

By default, the post-create script restores NuGet packages and builds `ViajantesTurismo.slnx` unless
`DEVCONTAINER_VERIFY_BUILD=0` is set.

## Tooling taxonomy alignment

- The devcontainer covers the main required contributor path by providing `.NET 10`,
  Git, Docker access, and the repo-pinned `.NET` local tools after
  `dotnet tool restore`.
- Optional host tools from the README taxonomy, such as `pwsh`,
  `PSScriptAnalyzer`, and `k6`, do not all need to be installed on the host
  when you use the devcontainer workflow.
- CI-owned validation such as GitHub Actions policy checks, secret scanning, and
  workflow enforcement remains owned by CI even when you develop inside the
  container.

## Local VS Code Dev Container workflow

### Prerequisites

Use this documented path when working locally with Visual Studio Code.

- Visual Studio Code
- Dev Containers extension
- A Docker-compatible container runtime available to the Dev Containers extension
- Network access to `mcr.microsoft.com`, `ghcr.io`, `dot.net`, `builds.dotnet.microsoft.com`,
  `github.com` and `release-assets.githubusercontent.com` (the pinned SharedKernel source and
  candidate packages), `api.nuget.org`, and the Ubuntu and Microsoft package feeds used by Dev
  Container Features; the smoke validation below also downloads its Dev Container CLI from
  `nodejs.org` and `registry.npmjs.org`
- Enough local resources to satisfy the requested devcontainer host requirements

### Open the repository in the container

1. Open the repository or `ViajantesTurismo.code-workspace` in VS Code.
2. Open the Command Palette.
3. Run `Dev Containers: Reopen in Container`.
4. Wait for the container build and lifecycle commands to complete.

If you change `.devcontainer/**` later, rebuild the environment with
`Dev Containers: Rebuild Container`.

### Minimum validation checklist

After the container opens successfully, verify the minimum supported contributor path:

```bash
dotnet --version
git --version
docker version --format "{{.Server.Version}}"
dotnet tool run aspire run
```

Expected outcomes:

- `dotnet --version` reports the repo-pinned .NET 10 SDK line
- `docker version` succeeds so the in-container Docker daemon can orchestrate Aspire's dependent services
- `dotnet tool run aspire run` starts the AppHost and shows the Aspire dashboard URL

If you intentionally skipped build verification by setting `DEVCONTAINER_VERIFY_BUILD=0`,
run this once before relying on the environment:

```bash
dotnet build ViajantesTurismo.slnx --no-restore
```

### Run the shared smoke validation locally

To run the same non-interactive devcontainer smoke path used by CI, run the pinned
`sharedkernel-repo` from the repository root after `dotnet tool restore`:

```bash
dotnet tool run sharedkernel-repo -- verify-devcontainer --exec 'dotnet --version && git --version && docker version --format "{{.Server.Version}}"' --root .
```

`verify-devcontainer`:

- downloads the pinned Dev Container CLI and Node.js into `TestResults/devcontainer-tools`,
  checking both against hashes pinned in the tool rather than running a remote installer pipe
- builds and starts the repository devcontainer with `devcontainer up`, which runs the
  configured lifecycle hooks
- runs each `--exec` command inside the container with `bash -lc`
- writes logs to `TestResults/devcontainer-smoke`
- removes the temporary container when it finishes, unless `--keep-container` is given

The CLI download supports Linux `x86_64` hosts only; on other hosts pass
`--devcontainer-cli <path>` to a compatible preinstalled Dev Container CLI.

The `Devcontainer Smoke` workflow runs the same command:

- weekly schedule and pushes run the toolchain check above
- the monthly schedule runs full validation: the same check, then Chromium's runtime libraries and
  every CI slice of `.config/sharedkernel-ci.json` one after another with `sharedkernel-ci run`
- manual dispatch lets you choose either mode

The full-mode commands are the `INSTALL_PLAYWRIGHT_DEPENDENCIES` and `RUN_TEST_SLICES` values in
`.github/workflows/devcontainer-smoke.yml`; pass them as further `--exec` arguments to reproduce a full
run. Running one slice at a time avoids concurrent all-solution test execution flakes.

Recommended follow-up validation while iterating:

```bash
dotnet test --solution ViajantesTurismo.slnx
```

## Relationship to CI

The repository also has a supplemental `Devcontainer Smoke` workflow in
`.github/workflows/devcontainer-smoke.yml`. It runs weekly, monthly in full mode, on manual dispatch,
and on pushes to `main` that touch `.devcontainer/**`, `global.json`, `setup-dev.sh`, or the workflow.

That workflow runs `sharedkernel-repo verify-devcontainer`, so local and CI smoke validation share
the same bootstrap path.

The command validates the non-interactive baseline by:

- building the devcontainer with the Dev Container CLI
- letting lifecycle commands run
- checking `dotnet`, `git`, and Docker access inside the container

It is intentionally supplemental. The repository's primary PR gate still runs on standard
GitHub-hosted runners.

## Troubleshooting

### Container creation fails locally

- Rebuild without cache from VS Code.
- Check that your container runtime is running.
- Check available CPU, memory, and storage against the requested host requirements.

### Aspire does not start in the container

- Verify `docker version` works inside the container.
- Rebuild the dev container after changes to `.devcontainer/devcontainer.json` so the Docker-in-Docker feature is reprovisioned.
- Confirm the container runtime still has enough resources allocated.

## Related documentation

- [README](../README.md) — primary contributor entry point
- [.devcontainer README](../.devcontainer/README.md) — configuration-specific notes
- [CI overview](ci/overview.md) — maintainer-facing CI and governance docs
