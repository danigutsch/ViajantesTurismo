# SharedKernel consumption

SharedKernel implementations, reusable test helpers, analyzers, generators, samples, benchmarks,
and library tooling belong to the separate SharedKernel repository. ViajantesTurismo consumes
`PandaBear.SharedKernel.*` packages. C# namespaces use `PandaBear.*`; assembly and project names remain `SharedKernel.*`.
Generic documentation and repository-configuration tooling belongs to SharedKernel. Application
documentation configuration, CI and coverage policy, setup scripts, and compatibility orchestration stay here.

The published candidate is `0.1.0-alpha.191.1` at clean library revision
[`96c6684921e227441da56d8883c2a59bba29b09e`](https://github.com/danigutsch/PandaBear.SharedKernel/commit/96c6684921e227441da56d8883c2a59bba29b09e).
It aligns the ClamAV, SeaweedFS and Grafana hosting resources with the Aspire integrations: the resource types
move to `Aspire.Hosting.ApplicationModel`, ClamAV and SeaweedFS mount no volume unless `WithDataVolume` or
`WithDataBindMount` is called, and every resource exposes `PrimaryEndpoint`, `Host` and `Port`. All 80 package hashes
match the unchanged artifacts from
[CI run 40284890189](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/40284890189). The
unchanged 80 packages, 55 symbol archives and manifest are published in GitHub prerelease
[`v0.1.0-alpha.191.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.191.1).
The pin records Linux/Windows package consumption, NativeAOT and stable promotion as `pending`.

The previous previews `0.1.0-alpha.187.1` at revision `12864e0bc2a490d5343f5a11ff7876919ea6c190`,
`0.1.0-alpha.171.1` at revision `753f94c9a8dfd09084cb3b0e6d49739c0a16cac1`,
`0.1.0-alpha.169.1` at revision `72625116fe443f123bf07f64c02aeb20c1456efd`,
`0.1.0-alpha.167.1` at revision `301f7fcce859e4bb5237b2e4a9abaefc71b4b90a`,
`0.1.0-alpha.161.1` at revision `b40822b58c9f6c1797dbea6de1e2a12319906b5b`,
`0.1.0-alpha.159.1` at revision `afaaa2994edd43e82cb4b3ff622cd7ef17fa0f4d`,
`0.1.0-alpha.144.1` at revision `ee4126348011b2ddf60d3e580428867689c412dd`,
`0.1.0-alpha.133.1` at revision `078adf2b2b158f38a211f48274af548d12a85752`,
`0.1.0-alpha.112.1` at revision `e4d13f63ff872a86478a16e5a3c3ce1ecba0fcb1` and `0.1.0-alpha.94.1` at
revision `d08008cd3e8901c304377edcc4683de7c3a85ecb` remain published. Their application validation is
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
