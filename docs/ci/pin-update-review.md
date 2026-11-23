# Action and container pin update review

Owner: repository maintainers. Status: active manual review workflow. Reviewed October 20, 2026.
Keep immutable Action SHAs and image tag-plus-digest pairs. This workflow adds no updater or
write-capable service. Update this inventory when a new pin location is introduced.

## Contents

- [Coverage inventory](#coverage-inventory)
- [Current action provenance](#current-action-provenance)
- [Action update evidence](#action-update-evidence)
- [Image update evidence](#image-update-evidence)
- [SharedKernel pin bump](#sharedkernel-pin-bump)
- [Validation and recovery](#validation-and-recovery)
- [Research sources](#research-sources)

## Coverage inventory

The source pins, not copied version tables, are authoritative. Inventory reviewed against
[Dependabot configuration](../../.github/dependabot.yml), workflows, scripts, Dev Container
configuration, and the AppHost/resource extensions.

| Location | Existing update coverage | Manual review gap and owner |
| --- | --- | --- |
| `.github/workflows/*.yml` external `uses:` | Dependabot scans workflow paths. Manifest actions are ignored and updated through the SharedKernel pin process | CI maintainers verify upstream SHA, release diff, permissions, and transitive action behavior; major updates need explicit migration review |
| `.github/actions/*/action.yml` external `uses:` | `build-sharedkernel-tool` uses `actions/checkout`, SHA-pinned, and SharedKernel's `setup-dotnet-tool` by SHA. The same manifest exclusions apply to these paths | Review composite shell and tool changes as source, and move a composite SHA with the matching workflow SHA |
| `.config/sharedkernel-candidate.json` and `danigutsch/PandaBear.SharedKernel` action refs | Dependabot ignores SharedKernel actions and `dotnet-sdk`; `SharedKernelConsumerWorkflowTests` requires the scanner action ref to equal `libraryRevision` and all shared workflow refs to use one SHA | Maintainers follow the [SharedKernel pin bump](#sharedkernel-pin-bump) steps |
| `.devcontainer/devcontainer.json` base image | Tag-plus-digest pin; no Docker ecosystem entry is configured | Development-environment maintainers resolve and verify the base-image digest manually; do not assume feature-update coverage includes the base image |
| `.devcontainer/devcontainer.json` Features | Dependabot `devcontainers` entry, quarterly; current feature references use digest-only keys | Development-environment maintainers verify whether each digest-only key is actually recognized and updated; treat coverage as unverified until a concrete proposal proves it |
| `src/ViajantesTurismo.AppHost/AppHostResourceExtensions.cs` | Explicit PostgreSQL, PgWeb, Redis, RedisInsight, and Keycloak tag/digest constants | AppHost maintainers update manually; Dependabot does not parse these C# resource declarations |
| Consumed ClamAV, SeaweedFS, and Grafana hosting resource extensions | Image pins currently live in the existing SharedKernel hosting packages; AppHost README inventories the observability/security pins | Package owners review upstream package/image updates; ViajantesTurismo maintainers verify compatibility and runtime behavior when consuming them. No library changes are authorized by this documentation workflow |
| `opencode.json` lint, lint-fix, and formatter commands; `.config/sharedkernel-k6.json` | The markdownlint-cli2, shfmt, and k6 images are pinned by tag plus digest; no configured updater covers these strings | CI maintainers resolve and verify the new digest manually when they move a tag |
| `src/ViajantesTurismo.AppHost/DevelopmentProjectResourceExtensions.cs` | Application publish tags/registry come from deployment configuration rather than third-party dependency pins | Release maintainers review build provenance and deployed digest; do not confuse generated application tags with dependency image updates |

No Dockerfile, Compose, or Kubernetes image update target is configured. Observability configuration
under `observability/` is mounted by resource extensions; review those configs with their images.
NuGet/SDK/local-tool lock refresh is a different workflow in
[Contributing](../../CONTRIBUTING.md#dependency-graph-and-lock-file-maintenance).

Unverified digest-only Feature coverage is a recorded gap, not an exception granting new mutable
pins. A bot configuration entry alone is not proof every
reference in its ecosystem is updated.

## Current action provenance

The November 21, 2026 review used application revision
`6c0b3ad36ea3b36cbb44d9a39987f44550918afd` and SharedKernel revision
`d1a0676a24a8c0fbe876cf0986bf9db378c6377d`. The scanner, shared workflows, build composite
and caller-supplied tool source refs all resolve to that SharedKernel commit. Its
[action pin manifest](https://github.com/danigutsch/PandaBear.SharedKernel/blob/d1a0676a24a8c0fbe876cf0986bf9db378c6377d/.github/action-pins.json)
holds the exact third-party SHAs and release labels.

The application's nine workflows and two composites contain 40 third-party action declarations.
The six consumed SharedKernel files add ten. Nine third-party repositories appear across both
sets. Their upstream release tags resolved to the approved SHAs, including the annotated
CodeQL and Markdownlint tags. All 22 checkout declarations disable persisted credentials.

| Application caller | SharedKernel entry point | Inherited actions and acquisition |
| --- | --- | --- |
| `actionlint.yml` | `actionlint-reusable.yml` | Checkout. Actionlint 1.7.9 uses the release's upstream checksum file. ShellCheck is installed with `apt-get` at the distribution version. |
| `dependency-review.yml` snapshot job | `dependency-snapshot-reusable.yml` | Checkout of the dependency input and tool source, followed by SDK setup and artifact upload. |
| `dependency-review.yml` review job | `dependency-review-reusable.yml` | Conditional checkout and SDK setup for license verification, followed by Dependency Review when requested. |
| `pr-metadata.yml` | `pr-metadata-reusable.yml` | Checkout of the tool source, followed by SDK setup. |
| `build-sharedkernel-tool` composite | `setup-dotnet-tool/action.yml` | SDK setup through `actions/setup-dotnet` in the cached or uncached branch, then locked tool restore and Release build. |
| `secret-scan.yml` scan job | `setup-security-scanner/action.yml` | Gitleaks 8.30.1 archive verified against the committed checksum. |

The metadata, snapshot and review workflows resolve `./tool-source/.github/actions/setup-dotnet-tool`
from the checkout selected by the caller's `source-repository` and `source-ref`. Include that checkout
in a pin review. The direct application-only actions cover artifact download, SARIF upload, changed
files, Markdownlint and GitHub App token creation. Both the inherited Actionlint workflow and the
application Lint job install ShellCheck from the runner distribution. Those packages are an
acquisition gap under the [binary acquisition baseline](security-hardening.md#downloaded-binary-expectations).

The snapshot caller permits `contents: write` on pushes, manual runs and same-repository
non-Dependabot PRs that require dependency review. The reusable workflow consumes that scope for
submission. Dependency review, PR metadata, Actionlint and the scanner start with read-only
contents. The SARIF upload requests `security-events: write` for available reports on pushes,
scheduled or manual runs, and same-repository PRs. Label sync has `issues: write` on pushes to main
and manual runs. Manual Roadmap Maintenance requests a separate, short-lived GitHub App token
after plan verification for its create-only PR step. The App token requests `contents: write`,
`pull-requests: write`, and `issues: read`. The job's `GITHUB_TOKEN` has read permissions.
[Trust boundary map](trust-boundaries.md) ·
[GitHub reusable workflow permissions](https://docs.github.com/en/actions/reference/workflows-and-actions/reusing-workflow-configurations#access-and-permissions-for-nested-workflows)

The repository settings returned `allowed_actions: all`, `sha_pinning_required: false`, a read-only
workflow token default and disabled PR approval by the workflow token. The `main` rules require
code-owner review, and the Lint job runs the checked-in pin validation. The review covers the
workflow inputs and declared scopes. The binary acquisition policy applies when reviewing
runner packages and bundled action code.

### Reproduce the provenance review

From the application root, set `sharedkernel_root` to the absolute path of the checkout at the
reviewed shared-workflow SHA. It matches `libraryRevision` in this review. Use the SDK pinned in
both repositories.

```bash
app_root="$PWD"
kernel_revision=$(jq -r .libraryRevision "$app_root/.config/sharedkernel-candidate.json")
test "$(git -C "$sharedkernel_root" rev-parse HEAD)" = "$kernel_revision"
tool="$sharedkernel_root/tools/SharedKernel.RepoConfig.Tool"
manifest="$sharedkernel_root/.github/action-pins.json"
dotnet restore "$tool/SharedKernel.RepoConfig.Tool.csproj" --locked-mode
dotnet build "$tool/SharedKernel.RepoConfig.Tool.csproj" -c Release --no-restore
dotnet run --project "$tool" -c Release --no-build --no-restore -- \
  verify-action-pins --root "$app_root" --manifest "$manifest"
dotnet run --project "$tool" -c Release --no-build --no-restore -- \
  verify-action-pins --root "$sharedkernel_root" --manifest "$manifest"
```

The verifier checks declarations within each supplied checkout. Review the call paths and
caller inputs above to resolve the inherited references. Use `git ls-remote` on each upstream
repository to compare the manifest SHA with its release tag. For annotated tags use
the peeled ref ending in `^{}`. Record changed permissions and acquisition paths alongside
the pin review.

## Action update evidence

For every proposed external Action SHA change, the reviewer records in the PR:

1. Owning upstream repository, old/new full 40-character commit SHAs, release tag, and release notes.
   Resolve tags to their actual commit, including annotated tags; do not trust the inline comment
   or a SHA copied from an unrelated fork. Keep the readable release comment aligned.
2. Compare link and review of `action.yml`, bundled executable code, composite steps, downloaded
   tools, and nested Actions. Identify new dependencies and transitive supply-chain risk.
3. Runtime/runner compatibility, major-version migration requirements, and changes to network,
   secrets, token scopes, checkout credentials, or artifact handling.
4. Results from Actionlint and affected required checks on the exact proposed head, including fork
   behavior when relevant. Never expose secrets to untrusted code to validate an update.

Keep default permissions at read-only and grant only required job scopes. An update PR does not
justify new write permissions. Dependabot proposals still require human review and normal gates.
SHA pinning fixes the selected revision; it does not prove that revision or its downloaded payloads
are trustworthy, nor guarantee advisory coverage for every SHA-pinned Action.

## Image update evidence

Before changing a tag/digest pair, record the fully qualified trusted registry/repository, old/new
tag, registry manifest digest, and whether that digest names a multi-platform index or a specific
platform manifest. Verify the selected architecture exists for CI and supported developer hosts.
Resolve the proposed tag from the trusted registry at review time; never invent a digest or use
one for a different repository/platform. Use existing trusted registry tooling; do not install a
new updater solely for this check.

Review release/security notes, supported configuration, entrypoint/user/ports, volume format,
health checks, and required migrations. Include vulnerability/provenance evidence where available
and explicitly label unavailable evidence. Update code constants and any existing image inventory
in the same PR. Preserve both the readable tag and immutable digest; do not replace a digest with
`latest` or a moving tag to get a container running.

For Aspire `WithImageSHA256`, supply the verified 64 hexadecimal characters without the `sha256:`
prefix, as described in the [AppHost guide](../../src/ViajantesTurismo.AppHost/README.md).
Validate affected resources with the repository-pinned Aspire CLI after reading their current
state. Check health/logs and representative database, auth, storage, scanner, or telemetry paths
for the resource changed. Dev Container image/Feature changes require its smoke validation.

## SharedKernel pin bump

`.config/sharedkernel-candidate.json` is the one SharedKernel pin. A bump changes these together:

1. Copy the new release `candidate.json` into the pin, then update the `PandaBear.*` versions in
   `Directory.Packages.props` and the SharedKernel tool versions in `.config/dotnet-tools.json`.
2. Keep the SDK in lockstep: `global.json` must match the SharedKernel `global.json` at the new
   `libraryRevision` in `sdk.version` and `sdk.rollForward`; only this repository's `errorMessage`
   differs. The SharedKernel tool build and `stage-candidate` fail otherwise. Dependabot does not
   propose SDK updates.
3. Set every `danigutsch/PandaBear.SharedKernel/...@<sha>` reference to the new revision: `setup-security-scanner`
   in `secret-scan.yml` and the shared references listed in
   [SharedKernel consumption](../sharedkernel/development.md#shared-ci-references-and-the-pin-bump)
   (`pr-metadata-reusable`, `actionlint-reusable`, `dependency-snapshot-reusable`, `dependency-review-reusable`,
   `setup-dotnet-tool`), including their `source-ref` and `revision` inputs. Then compare SharedKernel's
   `.github/action-pins.json` with the third-party pins.
4. Stage and refresh locks: `bash setup-dev.sh`, then
   `dotnet restore ViajantesTurismo.slnx --force-evaluate` and
   `dotnet restore ViajantesTurismo.slnx --locked-mode`.
5. Refresh the roadmap schema: run `dotnet repo init --root <empty directory>`,
   copy any changed `roadmap/schema/*.json` into this repository with a final newline, then run
   `dotnet repo verify`. `RoadmapSchemaContractTests` compares the committed
   schema with the pinned tool's output.
6. Respond to NuGet audit failures: restore audits every package (`NuGetAuditMode` `all`, level `high`,
   warnings as errors), and the SharedKernel tool build audits its own lock file. Adopt a SharedKernel
   candidate or package version that fixes the advisory. Do not add `NuGetAuditSuppress`, lower the
   audit level, or disable audit to finish a bump; without a fixed version, stop and record the
   advisory for a maintainer decision.
7. Renew the preview license approval in `.config/sharedkernel-repo.json` as described in
   [dependency license evidence](dependency-license-evidence.md).
8. Build, run the full test suite (including `SharedKernelToolCallerTests`), Actionlint, and the
   Lint job checks in [artifacts and local reproduction](artifacts-and-local-reproduction.md#lint-job).
9. Run `pwsh ./setup-dev.ps1` by hand on Windows. No CI job runs on Windows, so this manual run is the
   only Windows check of the setup script and the pinned SharedKernel tools.

## Validation and recovery

Run Actionlint for Action changes and the relevant smoke/integration checks for container changes,
then required repository checks. Keep evidence sanitized and tied to the PR head. Do not accept a
new pin solely because a registry pull succeeds.

Restore the previous reviewed SHA or tag/digest pair if an update fails before rollout. After a
stateful service has changed persistent data, an image revert may be unsafe: follow its reviewed
backup/forward-recovery plan and call out possible data loss before any reset or restore. Never
remove volumes as routine update validation.

Any future write-capable updater requires a separate threat-model and maintenance review covering
credentials, repository permissions, trusted input, branch/PR confinement, reviewer ownership,
maintenance cost, and rollback. Compare it against continuing manual review before adoption.
No automatic commits, pushes, merges, or workflow permission changes are introduced here.

## Research sources

Official documentation checked October 20, 2026; the inventory above describes repository state.

- [GitHub: secure use of Actions](https://docs.github.com/en/actions/reference/security/secure-use)
- [GitHub: Dependabot ecosystem options](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference)
- [Repository security baseline](security-hardening.md)
- [Repository governance](governance.md)
