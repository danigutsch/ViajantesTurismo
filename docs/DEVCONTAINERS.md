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

- .NET 10 via `mcr.microsoft.com/devcontainers/dotnet:dev-10.0-noble`
- Node.js 24, Git, and Docker-in-Docker Dev Container Features
- repository-specific VS Code extensions and default settings
- lifecycle commands for one-time setup, dependency restore, and build verification
- named volume mounts for NuGet packages, ASP.NET state, and `.build` output reuse
- requested host resources of 8 CPUs, 32 GB RAM, and 64 GB storage

The current lifecycle flow is:

1. `onCreateCommand` runs `.devcontainer/on-create.sh`
2. `postCreateCommand` runs `.devcontainer/post-create.sh`
3. `postStartCommand` runs `dotnet dev-certs https --trust || true`

By default, the post-create script restores NuGet packages, installs npm packages,
installs git hooks, and builds `ViajantesTurismo.slnx` unless
`DEVCONTAINER_VERIFY_BUILD=0` is set.

## Local VS Code Dev Container workflow

### Prerequisites

Use this documented path when working locally with Visual Studio Code.

- Visual Studio Code
- Dev Containers extension
- A Docker-compatible container runtime available to the Dev Containers extension
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
node --version
git --version
docker version --format "{{.Server.Version}}"
dotnet tool run aspire run
```

Expected outcomes:

- `dotnet --version` reports the repo-pinned .NET 10 SDK line
- `node --version` reports Node 24
- `docker version` succeeds so the in-container Docker daemon can orchestrate Aspire's dependent services
- `dotnet tool run aspire run` starts the AppHost and shows the Aspire dashboard URL

If you intentionally skipped build verification by setting `DEVCONTAINER_VERIFY_BUILD=0`,
run this once before relying on the environment:

```bash
dotnet build ViajantesTurismo.slnx --no-restore
```

### Run the shared smoke validation locally

To run the same non-interactive devcontainer smoke path used by CI, use the shared
script from the repository root:

```bash
bash scripts/run-devcontainer-smoke.sh
```

To run the same bootstrap path and then execute the full test suite inside the temporary
container, pass the optional test flag:

```bash
bash scripts/run-devcontainer-smoke.sh --run-tests
```

The script:

- builds and starts the repository devcontainer with the pinned Dev Container CLI
- lets the configured lifecycle hooks run
- verifies `.NET`, Node.js, Git, and Docker access inside the container
- optionally runs `dotnet test --solution ViajantesTurismo.slnx --no-build` inside the
  container when `--run-tests` is passed
- writes logs to `TestResults/devcontainer-smoke`
- removes the temporary container automatically when it finishes

If you want to keep the container around for inspection after a failure, set
`DEVCONTAINER_SMOKE_KEEP_CONTAINER=1` before running the script.

The GitHub Actions workflow uses the same option surface:

- weekly schedule, pull requests, and pushes run the regular smoke path
- the monthly schedule runs `--run-tests` to validate the full in-container test suite
- manual dispatch lets you choose either mode

Recommended follow-up validation while iterating:

```bash
dotnet test --solution ViajantesTurismo.slnx
npm run lint:all
```

## Relationship to CI

The repository also has a supplemental `Devcontainer Smoke` workflow in
`.github/workflows/devcontainer-smoke.yml`.

That workflow now runs `bash scripts/run-devcontainer-smoke.sh`, so local and CI smoke
validation share the same bootstrap path.

The shared script validates the non-interactive baseline by:

- building the devcontainer with the Dev Container CLI
- letting lifecycle commands run
- checking `dotnet`, `node`, `git`, and Docker access inside the container

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
