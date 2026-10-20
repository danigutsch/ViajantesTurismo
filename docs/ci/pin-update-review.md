# Action and container pin update review

Owner: repository maintainers. Status: active manual review workflow. Reviewed October 20, 2026.
Keep immutable Action SHAs and image tag-plus-digest pairs. This workflow adds no updater or
write-capable service. Update this inventory when a new pin location is introduced.

## Contents

- [Coverage inventory](#coverage-inventory)
- [Action update evidence](#action-update-evidence)
- [Image update evidence](#image-update-evidence)
- [Validation and recovery](#validation-and-recovery)
- [Research sources](#research-sources)

## Coverage inventory

The source pins, not copied version tables, are authoritative. Inventory reviewed against
[Dependabot configuration](../../.github/dependabot.yml), workflows, scripts, Dev Container
configuration, and the AppHost/resource extensions.

| Location | Existing update coverage | Manual review gap and owner |
| --- | --- | --- |
| `.github/workflows/*.yml` external `uses:` | Dependabot `github-actions`, monthly grouped minor/patch proposals and security updates; full SHA plus release comment is current policy | CI maintainers verify upstream SHA, release diff, permissions, and transitive action behavior; major updates need explicit migration review |
| `.github/actions/setup-ci-prerequisites/action.yml` | Repository-local composite action is reviewed as source; current composite has no external `uses:` action dependency | CI maintainers review shell/download/tool changes; inventory any future nested external reference and verify actual bot coverage |
| `.devcontainer/devcontainer.json` base image | Tag-plus-digest pin; no Docker ecosystem entry is configured | Development-environment maintainers resolve and verify the base-image digest manually; do not assume feature-update coverage includes the base image |
| `.devcontainer/devcontainer.json` Features | Dependabot `devcontainers` entry, quarterly; current feature references use digest-only keys | Development-environment maintainers verify whether each digest-only key is actually recognized and updated; treat coverage as unverified until a concrete proposal proves it |
| `src/ViajantesTurismo.AppHost/AppHostResourceExtensions.cs` | Explicit PostgreSQL, PgWeb, Redis, RedisInsight, and Keycloak tag/digest constants | AppHost maintainers update manually; Dependabot does not parse these C# resource declarations |
| Consumed ClamAV, SeaweedFS, and Grafana hosting resource extensions | Image pins currently live in the existing SharedKernel hosting packages; AppHost README inventories the observability/security pins | Package owners review upstream package/image updates; ViajantesTurismo maintainers verify compatibility and runtime behavior when consuming them. No library changes are authorized by this documentation workflow |
| `scripts/lint-all.sh`, `lint-markdown.sh`, `lint-{json,links,gherkin}.sh` | Docker fallback tools currently use version tags, including markdownlint, Python, ShellCheck, and shfmt; no configured updater covers shell image strings | CI maintainers own this immutable-pin gap; inventory and review digests in a separate hardening change. Do not describe tag-only references as already immutable |
| `src/ViajantesTurismo.AppHost/DevelopmentProjectResourceExtensions.cs` | Application publish tags/registry come from deployment configuration rather than third-party dependency pins | Release maintainers review build provenance and deployed digest; do not confuse generated application tags with dependency image updates |

No Dockerfile, Compose, or Kubernetes image update target is configured. Observability configuration
under `observability/` is mounted by resource extensions; review those configs with their images.
NuGet/SDK/local-tool lock refresh is a different workflow in
[Contributing](../../CONTRIBUTING.md#dependency-graph-and-lock-file-maintenance).

The missing shell-image digests and unverified digest-only Feature coverage are recorded gaps,
not exceptions granting new mutable pins. A bot configuration entry alone is not proof every
reference in its ecosystem is updated.

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
