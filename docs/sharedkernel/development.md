# SharedKernel consumption

SharedKernel implementations, reusable test helpers, analyzers, generators, samples, benchmarks,
and library tooling belong to the separate SharedKernel repository. ViajantesTurismo consumes
`PandaBear.SharedKernel.*` packages. C# namespaces use `PandaBear.*`; assembly and project names remain `SharedKernel.*`.
Generic documentation and repository-configuration tooling belongs to SharedKernel. Application
documentation configuration, CI and coverage policy, setup scripts, and compatibility orchestration stay here.

The published candidate is `0.1.0-alpha.402.1` at clean library revision
[`dca2de14985fc5a18eb9a632ee17705deb50cbe9`](https://github.com/danigutsch/PandaBear.SharedKernel/commit/dca2de14985fc5a18eb9a632ee17705deb50cbe9).
It adds `IResultLike` to `PandaBear.SharedKernel.Results` and fixes the mediator generator's `SKMED010` and
`SKMED012` false positives; the other packages are unchanged in behavior. The packages, symbol archives and manifest are
published in GitHub prerelease
[`v0.1.0-alpha.402.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.402.1), built by
[CI run 41195436680](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/41195436680).
The pin records Linux/Windows package consumption, NativeAOT and stable promotion as `pending`.

`SKTEST018` is enabled at `suggestion` in `.editorconfig` so adoption can be measured without failing the build. Promote it to
`warning` once all test projects are tagged.

The previous previews `0.1.0-alpha.396.1` at revision `079cd9fb28192d027fb14d959da9e29f7ab6d2a4`,
`0.1.0-alpha.389.1` at revision `9ef862a978a3915b0c63944a83fc65f67fa4a8fb`,
`0.1.0-alpha.372.1` at revision `725be335280c3b7bdb659bf3043b2bfe747b9d12`,
`0.1.0-alpha.368.1` at revision `4d54ab830508be0d4d3189d86858e909c6ed8641`,
`0.1.0-alpha.359.1` at revision `7893ca689c8aa10602cb8accb66f83a7ac2f6525`,
`0.1.0-alpha.339.1` at revision `7c62d629ac152de5993653bbcabf7430c40cfd35` and
`0.1.0-alpha.336.1` at revision `529f0cc4e2df034a1718e2ff8e71fc38d59fc587` remain published. The releases of earlier previews were
removed from the library repository while their tags stay, so they cannot be staged again; their application validation is
historical evidence.

The former candidate `1.2.0-alpha.20261023.1` at library revision
`cc826be664bfc2dc087fe5a140f7232b4f65967d` is retired. Its release was deliberately deleted;
do not recreate, download, restore, or execute its artifacts. Earlier compatibility and package
validation results are historical evidence, not approval to consume a retired candidate.

HTTP test factories apply `AddTestRequestBindingDefaults` from `PandaBear.Testing.AspNetCore`.
Direct TestServer and Kestrel test hosts register the same extension. Invalid bodies return HTTP 400
in Development and Production test environments; ordinary application startup keeps framework defaults.

Dependency review retains main's separately reviewed immutable generator source revision. Its shared
`dependency snapshot` command compares committed lockfiles without restoring historical packages.
The application-owned duplicate generator has been removed. Review generator source independently
of the runtime package pin.
The Results runtime includes its generator, so do not add a separate Results generator package.

CSV structure parsing and advisory-lock hashing use the Tabular and Npgsql packages. Application
import interfaces, accepted headers, normalization, business errors, and lock identities remain local.
The CSV parser preserves the existing simple comma-splitting behavior; quoted commas, escaping, and
multiline fields remain unsupported.

## Stage the candidate

Install the SDK pinned by `global.json`; this repository's and SharedKernel's `global.json` files
must agree on `sdk.version` and `sdk.rollForward`. The committed `.config/sharedkernel-candidate.json` pins the
library revision, package version, and SHA-256 hashes. `setup-dev.sh` and `setup-dev.ps1` stage it
before they restore dependencies and local tools:

```sh
bash setup-dev.sh [--install-sdk] [--library-root <path>] [--feed <dir>]
pwsh ./setup-dev.ps1 [-InstallSdk] [-LibraryRoot <path>] [-Feed <dir>]
```

Without a library root, setup clones the public
[PandaBear.SharedKernel source](https://github.com/danigutsch/PandaBear.SharedKernel) into the ignored
`.local/sharedkernel-source/SharedKernel.CI.Tool` as a blobless, cone sparse checkout of
`tools/SharedKernel.CI.Tool` and `tools/Shared`, fetches the pinned `libraryRevision`, and checks it out
detached. It reuses that directory only when it is this sparse clone; otherwise it stops and leaves
the directory in place. `--library-root` names a clean checkout of the exact revision instead.

Setup builds the CI tool inside the checkout with a locked restore, then runs:

```sh
dotnet run --project <checkout>/tools/SharedKernel.CI.Tool -c Release --no-build -- \
  stage-candidate --pin .config/sharedkernel-candidate.json --library-root <checkout> \
  --output .local/sharedkernel-feed --repository danigutsch/PandaBear.SharedKernel
dotnet restore --locked-mode
dotnet tool restore
```

`--feed <dir>` replaces `--repository` with a directory holding the unchanged pinned packages. With
`--repository`, `stage-candidate` downloads the release `candidate.json` from GitHub prerelease
`v<packageVersion>` and requires its identity to equal the pin, then downloads only missing packages
without a token. Both sources verify the clean library revision and every package hash before
packages enter the ignored `.local/sharedkernel-feed`; different existing packages are refused, and
the pin is written there as `candidate.json` last. Repeating setup with an identical candidate
changes nothing. Package source mapping sends `PandaBear.SharedKernel.*` exclusively to that local
feed, preventing public-feed fallback. Third-party packages and vulnerability auditing continue to
use nuget.org. The devcontainer runs `setup-dev.sh` from `.devcontainer/on-create.sh`.

Then validate the application:

```sh
dotnet build ViajantesTurismo.slnx -c Release --no-restore
dotnet tool run sharedkernel-ci -- openapi generate
dotnet test --solution ViajantesTurismo.slnx -c Release --no-build --no-restore
```

Do not substitute another preview's package files or use cached retired artifacts to bypass missing
releases.

## Adopt a library change

Commit and validate library changes first. Use its C# `validate-candidate` command to create a new,
unique prerelease candidate. Never overwrite an existing candidate version. Run the library's full
suite, API checks, package-consumption checks, and promised NativeAOT checks before adoption.

Update the committed candidate manifest, central package versions, local tool versions, and lock files
in an application worktree. Stage the exact artifacts, run the application checks above, commit
those changes, and record compatibility using:

```sh
dotnet tool run sharedkernel-version -- verify-consumer --candidate .local/sharedkernel-feed/candidate.json -- \
  dotnet test --solution ViajantesTurismo.slnx -c Release
```

Compatibility records both committed repository revisions and package hashes in
`TestResults/consumer-compatibility.json` as `passed`, `failed`, or `pending` (uncommitted changes run
nothing). Validation runs in a temporary local clone of the recorded application commit with an isolated
package cache, then removes both. The working checkout's restore assets and binaries remain unchanged. A renamed package
identity does not establish compatibility with previously published `SharedKernel.*` packages.
Rollback may use only an approved, non-retired compatible candidate after validation. Update the
application dependency configuration, candidate manifest, and lock files together, then stage
that candidate's exact artifacts. Retired releases must remain deleted and must not be consumed again.

Generic outbox and idempotency implementation tests live in the library persistence test project.
The transfer preserves 37 guards in library test commit `3752abb54681e28f4d16abd29966bab3819dd0ea`;
application tests continue to verify domain dispatch, saved envelopes, registration, and schema policy
through public package interfaces. This test-only commit does not change the pinned runtime artifacts.

## Shared CI references and the pin bump

Generic CI pieces come from SharedKernel's
[reusable workflows and actions](
https://github.com/danigutsch/PandaBear.SharedKernel/blob/dca2de14985fc5a18eb9a632ee17705deb50cbe9/docs/ci/reusable-workflows.md),
referenced by full commit SHA. The workflow references currently use
`dca2de14985fc5a18eb9a632ee17705deb50cbe9`, which equals the `libraryRevision` in
`.config/sharedkernel-candidate.json`. Every pin bump must change all of these in the same commit, and
`SharedKernelConsumerWorkflowTests` fails when they disagree:

| Reference | File |
| --- | --- |
| `.github/workflows/pr-metadata-reusable.yml@<sha>` and `source-ref` | `.github/workflows/pr-metadata.yml` |
| `.github/workflows/actionlint-reusable.yml@<sha>` | `.github/workflows/actionlint.yml` |
| `.github/workflows/dependency-snapshot-reusable.yml@<sha>` and `source-ref` | `.github/workflows/dependency-review.yml` |
| `.github/workflows/dependency-review-reusable.yml@<sha>` and `source-ref` | `.github/workflows/dependency-review.yml` |
| `.github/actions/setup-dotnet-tool@<sha>` | `.github/actions/build-sharedkernel-tool/action.yml` |
| `revision:` of the action pin check | `.github/workflows/ci.yml` (Lint job) |
| `.github/actions/setup-security-scanner@<libraryRevision>` | `.github/workflows/secret-scan.yml` |
| `libraryRevision`, package version and hashes | `.config/sharedkernel-candidate.json` |

`secret-scan.yml` stays a local workflow: SharedKernel offers only the scanner setup action, which it already uses,
and the SARIF upload permission belongs to the caller. Its reference must equal `libraryRevision`. After the bump,
also compare SharedKernel's `.github/action-pins.json` with the third-party pins and update the SHA and version
comment of each differing reference in the same change.

Callers keep the triggers, concurrency groups, ignored-label rules, change planning, license values and permissions.
Because GitHub reports a reusable call as `<caller job> / <called job>`, the required checks change names:

| Previous check | Check after adopting the shared workflow |
| --- | --- |
| `PR Metadata` | `PR Metadata / Validate squash header` |
| `Dependency Review` | `Dependency Review / Review` |
| `Dependency snapshot (base)` | `Dependency snapshot (base) / Snapshot` |
| `Dependency snapshot (head)` | `Dependency snapshot (head) / Snapshot` |
| `Actionlint` (not required) | `Actionlint / Actionlint` |

Update the `main` ruleset to require the two new names; `Build and Test`, `Lint`, `Quality Gate` and `Secret Scan` are
unchanged. Do not merge before the ruleset accepts the new names, or the old checks stay pending forever.

## CI and platform evidence

Hosted Linux and Windows jobs stage the candidate through `.github/actions/setup-ci-prerequisites`.
It runs `.github/actions/build-sharedkernel-tool`, which reads the pinned `libraryRevision`, checks out
`danigutsch/PandaBear.SharedKernel` at that revision into `.local/sharedkernel-source/<tool>` as a cone
sparse checkout of `tools/<tool>` and `tools/Shared` without persisted credentials, builds the tool
there with a locked restore, and exports `SHAREDKERNEL_SOURCE`. The setup action then runs
`stage-candidate --repository danigutsch/PandaBear.SharedKernel`, which downloads the pinned assets
from GitHub prerelease `v<packageVersion>` without a token. Missing assets, revision mismatches, and
hash mismatches fail visibly; missing retired releases must not be recreated to unblock checks. The
secret-scan workflow also pins the shared scanner action to an immutable library revision.

`build-sharedkernel-tool` delegates the SDK, the lock-file NuGet cache and the locked restore and Release build to
SharedKernel's `.github/actions/setup-dotnet-tool`, referenced by full SHA. It keeps the sparse checkout of the
pinned `libraryRevision`, the library-root override and `SHAREDKERNEL_SOURCE`; `setup-ci-prerequisites` keeps
`NUGET_PACKAGES`, candidate staging, `dotnet tool restore` and dev certificate trust. A stale lock file still fails
the restore.

## Action pins

Every third-party action in `.github/workflows` and `.github/actions` pins the SHA and trailing version comment of
SharedKernel's `.github/action-pins.json`. The CI Lint job builds the RepoConfig tool from the shared revision
(`revision:` and `sparse-extra: .github` inputs of `build-sharedkernel-tool`) and runs `verify-action-pins`
against that manifest. To bump an action, or to use a new one, change the SharedKernel manifest first, then every
reference here in the same change.

Optional repository variables `SHAREDKERNEL_LIBRARY_ROOT` and `SHAREDKERNEL_CANDIDATE_FEED` reach the
setup action as explicit inputs. A library-root override skips the hosted checkout, requires a clean
Git top level at the pinned revision, builds the tool there, and stages from
`<library-root>/artifacts/api-compat/packages/<packageVersion>` unless a feed override is also
provided. A feed-only override stages from that feed with the automatically checked-out tool source.

Linux and Windows must consume the same hashed packages. Record platform tests and NativeAOT results
separately; an unavailable platform remains pending. Registry publication remains deferred: this
workflow consumes GitHub release assets and publishes nothing to nuget.org.

Generic commands use `dotnet tool run sharedkernel-repo -- <command>` and documentation commands use
`dotnet tool run sharedkernel-docs -- <command>`. Endpoint metadata methods and audience classification
are consumer settings in `docs/architecture/generated-diagrams.json`. Shared telemetry privacy processors
always enforce the baseline, with additional business-sensitive attribute policy supplied by ServiceDefaults.

## Shared security tooling

Security baseline checking and deterministic scanner orchestration are provided by the pinned
`sharedkernel-repo security` commands. Application document contracts, endpoint policies, CI
permissions, and runtime security tests remain here. SharedKernel also uses the same implementation
for its own baseline. The Linux scanner installer pins and verifies Gitleaks separately from NuGet
packages; see [the application baseline](../security/security-baseline.md).
