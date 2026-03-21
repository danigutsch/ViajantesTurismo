# CI Governance and Rollout

Operational details, artifact guidance, failure reproduction steps, and action trust policy for
the GitHub Actions CI workflow (`.github/workflows/ci.yml`).

## Workflow Jobs

The CI workflow runs on every pull request targeting `master`, every push to `master`, and on
`workflow_dispatch`. It defines two parallel jobs:

### Build and Test

| Attribute | Value |
| --- | --- |
| Job key | `build-and-test` |
| Job name | `Build and Test` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Checkout repository (`actions/checkout`)
2. Set up .NET SDK from `global.json` (`actions/setup-dotnet`)
3. Set up Node.js from `.nvmrc` (`actions/setup-node`)
4. `dotnet restore ViajantesTurismo.slnx`
5. `dotnet build ViajantesTurismo.slnx --no-restore`
6. Trust HTTPS developer certificate (`dotnet dev-certs https --trust`, `continue-on-error: true`)
7. `dotnet test --solution ViajantesTurismo.slnx --no-build`
8. Upload test result artifacts (`actions/upload-artifact`, runs on `always()`)

> **Note:** Step 6 uses `continue-on-error: true` due to a [known SDK bug](https://github.com/dotnet/aspnetcore/issues/65391)
> where `dotnet dev-certs https --trust` exits with code 4 on `ubuntu-latest` in SDK 10.0.103+.

### Lint

| Attribute | Value |
| --- | --- |
| Job key | `lint` |
| Job name | `Lint` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Checkout repository (`actions/checkout`)
2. Set up Node.js from `.nvmrc` with npm cache (`actions/setup-node`)
3. `npm ci`
4. `npm run lint:all`

## Artifacts

Test result artifacts are uploaded by the `build-and-test` job unconditionally (`if: always()`).

| Artifact name | Contents | Retention |
| --- | --- | --- |
| `test-results` | `**/TestResults/**` from all test projects | 7 days |

The artifact includes per-project `TestResults` folders, which contain `.trx` result files and
`coverage.cobertura.xml` when coverage collection is enabled.

Artifact scope is kept narrow — only test outputs that materially help diagnose failures are
included. Do not broaden the upload glob without a clear reason.

## Reproducing Failures Locally

All CI commands map directly to local developer commands.

### Build and Test job

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet build ViajantesTurismo.slnx --no-restore
dotnet dev-certs https --trust
dotnet test --solution ViajantesTurismo.slnx --no-build
```

### Lint job

```bash
# From repository root
npm ci
npm run lint:all
```

If `npm run lint:all` fails, run individual linters to isolate the failure:

```bash
npm run lint:md        # Markdown
npm run lint:sh        # Shell scripts
npm run lint:json      # JSON files
npm run lint:gherkin   # Gherkin/feature files
```

Auto-fix what can be auto-fixed:

```bash
npm run lint:all:fix
```

## Required Status Checks

Once branch protection is configured for `master`, require both of these exact job names:

- `Build and Test`
- `Lint`

These names match the `name:` fields in `.github/workflows/ci.yml`. Any rename of the jobs must
be reflected in branch protection settings.

## Action Versioning Policy

All GitHub Actions used in this workflow are pinned to **major version tags**
(for example `actions/checkout@v4`). This is the initial baseline policy.

### Rationale

Major version tags offer a practical balance between stability and security for the early
lifecycle of this workflow:

- The risk surface from a compromised major-version tag is accepted as reasonable given
  that these are all official GitHub-maintained actions.
- Major version tags receive patch and minor fixes automatically, including security patches.
- Pinning to immutable SHAs provides stronger supply-chain guarantees but increases
  maintenance burden; that trade-off is deferred until the workflow has proven stable.

### Trusted actions

The workflow uses only official GitHub-maintained actions:

| Action | Purpose |
| --- | --- |
| `actions/checkout@v4` | Repository checkout |
| `actions/setup-dotnet@v4` | .NET SDK provisioning |
| `actions/setup-node@v4` | Node.js provisioning |
| `actions/upload-artifact@v4` | Test result artifact upload |

Third-party actions are not used. Before adding any third-party action, document the trust
decision and update this table.

### Update process

- GitHub Dependabot is the recommended mechanism for keeping action references current.
  See [Dependabot configuration](https://docs.github.com/en/code-security/dependabot/working-with-dependabot/keeping-your-actions-up-to-date-with-dependabot)
  for setup details.
- Until Dependabot is configured, review action release notes manually when bumping versions.
- When upgrading a major version (for example `@v4` to `@v5`), review the migration guide and
  verify the workflow still passes before merging.

### Future migration to SHA pinning

If the project adopts a stricter supply-chain posture, migrate to immutable SHAs and introduce
a documented process for keeping those SHAs current (for example, using a tool such as
[`pin-github-action`](https://github.com/mheap/pin-github-action) or Dependabot SHA pinning).

## Workflow Ownership (CODEOWNERS)

The `CODEOWNERS` file at the repository root requires review for all changes to workflow files.

Any pull request that modifies `.github/workflows/**` will request review from the designated
code owners. See `CODEOWNERS` for the current ownership mapping.

## Deferred Work

The following items are intentionally out of scope for the baseline workflow and are captured as
follow-up work:

- SHA pinning for actions (see [Action Versioning Policy](#action-versioning-policy) above)
- Dependabot configuration for GitHub Actions version updates
- Branch protection rule configuration with required status checks
- Coverage report generation and upload as a separate artifact
- Path-based workflow optimization (skip jobs on doc-only changes)
- Scheduled devcontainer smoke validation
- Multi-OS matrix (not required until a concrete cross-platform requirement appears)
- Coverage thresholds (not enforced until baseline coverage trends are established)
- Dependency review for pull requests that change manifests or lock files

## Related Documentation

- [README — Continuous Integration](../README.md#continuous-integration) - Contributor-facing CI summary
- [Code Quality Tools](CODE_QUALITY.md) - Local linting and formatting tools
- [PBI-2026-03-15-02](backlog/PBI-2026-03-15-02-github-actions-ci-workflow.md) - Original delivery plan
