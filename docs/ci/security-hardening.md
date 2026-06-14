# CI security hardening baseline

This document records the repository's GitHub Actions supply-chain baseline for CI and the
 concrete controls currently implemented in workflow and helper-script paths.

## Current posture

- External GitHub Actions are SHA-pinned in `.github/workflows/**`.
- Workflow jobs use least-privilege `permissions`, with elevated scopes isolated to the
  dedicated SARIF upload job in `.github/workflows/secret-scan.yml`.
- The shared CI setup path restores NuGet packages in locked mode and restores repo-pinned
  `.NET` local tools through `.config/dotnet-tools.json`.
- `actionlint` and `gitleaks` are downloaded as pinned release artifacts and verified with
  upstream SHA-256 checksum files before installation.
- The shared devcontainer smoke path now uses a repo-owned installer that pins both Node.js
  and `@devcontainers/cli`, verifies the Node archive against the published `SHASUMS256.txt`
  manifest, and verifies the CLI tarball against npm registry integrity metadata before use.

## Allowed dependency acquisition patterns

The CI baseline allows only these acquisition paths:

- SHA-pinned GitHub Actions from trusted upstream repositories.
- Locked NuGet restore from committed `packages.lock.json` files.
- Repo-pinned `.NET` local tools restored through `dotnet tool restore`.
- Downloaded release binaries only when the workflow or helper script pins the exact version
  and verifies integrity before extraction or execution.
- Playwright browser installation only from the generated `playwright.ps1` script emitted by
  the already-built test project.

Avoid these patterns in CI unless a follow-up issue explicitly approves them:

- `curl | sh` or similar remote-script execution.
- Ad hoc `npm`, `npx`, `pnpm`, or `yarn` installs in workflow `run` steps.
- Unpinned package-manager installs for tools that affect required checks.

## SHA pinning policy

- Keep every external `uses:` reference pinned to a full commit SHA.
- Keep the upstream release comment beside the SHA so Dependabot PRs stay reviewable.
- Treat moving from one pinned SHA to another as a supply-chain review event, not a routine
  formatting change.

## Downloaded binary expectations

When CI downloads a binary directly, the baseline is:

- Pin the tool version explicitly in the workflow or script.
- Download over HTTPS with strict failure flags.
- Verify the downloaded archive against a trusted checksum or integrity value before
  extraction or execution.
- Prefer repo-owned install logic over upstream remote install scripts.
- Record any remaining provenance gap in docs if the upstream does not provide stronger
  attestation than checksums or signed package metadata.

Current state:

- `.github/workflows/actionlint.yml` meets the checksum baseline but does not yet verify
  publisher provenance beyond upstream checksums.
- `.github/workflows/secret-scan.yml` meets the checksum baseline but does not yet verify
  publisher provenance beyond upstream checksums.
- `scripts/run-devcontainer-smoke.sh` no longer pipes a remote installer script directly into
  `sh`; it now owns a version-pinned verified install path.

## npm and tool execution policy

- Prefer repo-pinned `.NET` local tools or SHA-pinned GitHub Actions for required CI checks.
- Treat Node-based tool execution as acceptable only when it comes from a pinned GitHub
  Action or from a repository-owned install path that verifies the exact downloaded payload.
- Do not add direct `npm install`, `npx`, or equivalent package execution to required CI
  paths without a separate threat and maintenance review.
- Keep workflow `run` steps thin and push reusable logic into repository scripts or local
  composite actions.

## Trust boundaries and follow-up

- Pull request code is untrusted input. Keep `pull_request_target` out of build, test, and
  analysis paths for this repository.
- Fork pull requests must continue to run with read-only assumptions and without write-scoped
  code-scanning upload paths in the primary scan job.
- See [Trust boundaries](trust-boundaries.md) for the event and permission model that layers on
  top of this supply-chain baseline.
