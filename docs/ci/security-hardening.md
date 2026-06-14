# CI security hardening baseline

This document records the repository's GitHub Actions supply-chain baseline for CI and the
concrete controls currently implemented in workflow and helper-script paths.

It records which controls the repository adopts now, which ones it explicitly avoids, and
which ones remain deferred for a later implementation or tooling decision.

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

## Control decisions

### Adopt now

- Keep external GitHub Actions pinned to full commit SHAs.
- Keep workflow permissions read-only by default and isolate any required write scope to a
  separate follow-up job.
- Keep required validation paths on repository-owned scripts, locked NuGet restore, and
  repo-pinned local tools where practical.
- Keep direct binary downloads limited to pinned versions with checksum or registry-integrity
  verification before execution.
- Keep `npm` lifecycle scripts and ad hoc package-manager installs out of required CI paths.
- Keep contributor-facing reusable logic in repository scripts or composite actions rather than
  long workflow `run` blocks.

### Reject for this repository

- Do not introduce `pull_request_target` into build, test, lint, or analysis jobs that execute
  pull request code.
- Do not add `curl | sh` bootstrap patterns to required CI jobs.
- Do not allow direct `npm install`, `npx`, `pnpm dlx`, or equivalent ephemeral package
  execution in required CI checks.
- Do not expand write-scoped automation into the primary scan or test jobs when a split
  artifact-producing and artifact-publishing model is sufficient.

### Defer for later review

- Publisher provenance stronger than upstream checksums for downloaded release binaries remains
  desirable, but current upstreams do not offer a simple repository-wide replacement path.
- Action-source governance beyond SHA pinning and CODEOWNERS review remains deferred until the
  repository has a concrete need for stronger organization-level policy enforcement.
- Additional automation for action-pin freshness or download provenance auditing remains deferred
  until maintenance cost is justified by real drift or incident pressure.

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

## Follow-up tasks after this baseline

- Continue the trust-boundary track by narrowing where secret-dependent CI paths may run and
  how fork pull requests are skipped.
- Decide whether local lint and helper-tool execution must match the same acquisition and trust
  restrictions documented here for hosted CI.
- Treat any future provenance upgrade for downloaded binaries as a dedicated follow-up change so
  the repository can compare security gain against workflow complexity.

## Trust boundaries and follow-up

- Pull request code is untrusted input. Keep `pull_request_target` out of build, test, and
  analysis paths for this repository.
- Fork pull requests must continue to run with read-only assumptions and without write-scoped
  code-scanning upload paths in the primary scan job.
- See [Trust boundaries](trust-boundaries.md) for the event and permission model that layers on
  top of this supply-chain baseline.
