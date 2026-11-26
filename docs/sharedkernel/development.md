# SharedKernel consumption

SharedKernel implementations, reusable test helpers, analyzers, generators, samples, benchmarks,
and library tooling belong to the separate SharedKernel repository. ViajantesTurismo consumes
`PandaBear.SharedKernel.*` packages. C# namespace, assembly and project names use `SharedKernel.*`.
Generic documentation and repository-configuration tooling belongs to SharedKernel. Application
documentation configuration, CI and coverage policy, setup scripts, and compatibility orchestration stay here.

The app pins [`0.1.0-alpha.963.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.963.1)
from clean library revision `9d5a1975e147989630f75e5345117462575ca3be`. The candidate includes the
Linux resource guard for local consumer validation. The app uses its durable PostgreSQL scheduler
and observed batch drains for document maintenance.
[Merged artifact validation](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/45770249644) ·
[Published candidate validation](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/45772572655)

## Compatible dependency updates

The Npgsql driver and EF provider use 10.0.4. EF Core, EF-backed Data Protection and the
design-time CLI stay on 10.0.15. The published SharedKernel provider uses EF 10 index
metadata APIs. Adoption of EF 11 requires provider compatibility with its index metadata
and preservation of the application's inclusive idempotency cutoff.

The application keeps a central OpenAPI.NET 3.11.1 version for the extension preservation
fix. Recheck that entry when adopting a candidate with the same upstream dependency floor.
[OpenAPI.NET release](https://www.nuget.org/packages/Microsoft.OpenApi/3.11.1)

AWS SDK for S3 4.1.2.1 fixes intermittent rejection of seven-day presigned URL expiry.
The SeaweedFS tests cover storage compatibility with TLS and workload identity.
The analyzer, browser test runner and coverage tool updates are recorded in the dependency
review and CI configuration. EF and gRPC OpenTelemetry instrumentation retain their existing
preview versions while their package lines have only preview releases.
[S3 package](https://www.nuget.org/packages/AWSSDK.S3/4.1.2.1) ·
[AWS release notes](https://github.com/aws/aws-sdk-net/releases/tag/4.1.10.0) ·
[Dependency review](../ci/dependency-license-evidence.md)

## Runtime identifier upgrade

The SharedKernel runtime identifier rename changes the Management Web token-session claims.
An existing Management Web session signs out at its first authenticated request after adoption.
Complete the rollout before asking operators to sign in again. Old and new Management Web
instances recognize different claim names, so a mixed rollout can prompt another sign-in.
After rollback, wait until all Management Web instances run the restored version before signing in again.
Older server-side tickets still decrypt with the same Data Protection purpose and use
`IssuedUtc` in place of the old session-start item. Cookie sign-out removes a rejected ticket.
Remaining cached tokens expire under the established session policy.
[SharedKernel runtime identifier change](https://github.com/danigutsch/PandaBear.SharedKernel/commit/aa5bbfd128b895f32d7af29ac4907d91bca5b13d)

The EF package now records unique-rule names as `SharedKernel:UniqueRule`. The rename
produces zero relational migration operations for Admin, Catalog, and Branding. Each context's
next model-driven migration records the canonical key in its generated snapshot while preserving
its committed migration history.

The named HTTP clients now use `SharedKernel.AspNetCore.Authentication.Bff.IdentityProvider`,
`SharedKernel.AspNetCore.WebAnalytics.Umami`, and `SharedKernel.Aspire.Hosting.Umami` in their
log and HTTP client identifiers.

## Earlier candidates

Candidate [`0.1.0-alpha.774.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.774.1)
updated the host, observability and mediator registration names and introduced the short .NET tool commands.
[Published artifact validation](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/45174602104)

Candidate [`0.1.0-alpha.727.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.727.1)
let Admin commands declare transaction locks without an Entity Framework dependency. The consumer verifier
also began validating committed lock exemptions for the selected solution.
[Published artifact validation](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/45034665982)

Candidate [`0.1.0-alpha.719.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.719.1)
introduced the optional BFF cache concurrency contract. Management Web uses it for conditional replacement and deletion
of protected PostgreSQL token entries. The [published validation](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/45029597240)
verified Linux and Windows consumption and NativeAOT.

Candidate [`0.1.0-alpha.716.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.716.1)
introduced shared sitemap and theme CSS endpoints, filesystem object storage and media helpers, upload deadlines,
documentation generators, test fixtures, and migration artifact verification. Its Linux and Windows package consumption
and NativeAOT evidence is recorded in
[SharedKernel validation](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/45005831777).

Candidate [`0.1.0-alpha.653.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.653.1) was the first
.NET 11-only candidate adopted here.
On top of `0.1.0-alpha.649.1` it targets .NET 11 only, so this adoption moves the application to
`net11.0` and SDK 11.0.100, the SDK `stage-candidate` requires to match SharedKernel's. ASP.NET Core and the
`Microsoft.Extensions.*` packages the candidate requires are on the 11.0.0 line, `Microsoft.OpenApi` is 3.11.0 (required
by `Microsoft.AspNetCore.OpenApi` 11) and the analyzers build against Roslyn 5.12. The Entity Framework family stays on
10.x (EF Core 10.0.15, Npgsql's EF provider 10.0.3, the EF Core health check, data protection and Sqlite packages) until
Npgsql's EF provider 11 is stable; `dotnet-ef` stays on the EF Core 10 line. `PandaBear.IO.ReadOnlyMemoryStream` is
removed in favor of `System.IO.ReadOnlyMemoryStream`. .NET 11 also reports new analyzer diagnostics and changes some runtime
behavior; SharedKernel's `docs/dotnet-multi-targeting.md` lists them. The history below applies to the earlier candidates.
Version `0.1.0-alpha.649.1`, on top of `0.1.0-alpha.557.1`, moved to the .NET 10.0.15 security release and SDK 10.0.404 and raised OpenTelemetry to
1.21, Microsoft.OpenApi to 2.13, Playwright to 1.64.1, xUnit v3 to 4.0.3 and Magick.NET to 15.1.0. That adoption raised
the matching central versions. The tool engines moved from `tools/Shared` into `src` libraries, so the setup scripts and
`build-sharedkernel-tool` check out the `src` and `tools` cone. It added `InvariantStringWriter` and SKTEST022 for captured test
output, page-view targets in `AspNetCore.WebAnalytics` with an Umami website resource for Aspire, `Money.CreateFromInput`,
CPF input that ignores separators anywhere, Pix email keys checked without a regular expression, accessible form fields and
focus-keeping confirmations in `Blazor`, and a theme preview whose sample headings are no longer heading elements. The CI tool
also blocks restores whose vulnerability audit could not run (NU1900 and NU1905). Version `0.1.0-alpha.557.1` made SKTEST002
require the canonical casing of listed acronyms and type names in test method names (`json` becomes `JSON`, `id` becomes
`ID`, `uri` becomes `URI`). A repository can add its own segments with `sharedkernel_testing_additional_allowed_segments` in `.editorconfig`;
SKTEST021 reports a malformed entry. It also made the CI tool report a parse error for a non-numeric `--timeout-minutes`.
Version `0.1.0-alpha.547.1` added shared test doubles: `Testing.ObjectStorage` (`InMemoryObjectStore`),
`Testing.MalwareScanning` (`TestMalwareScanner`), recording, routing and gated HTTP handlers in `Testing.Http`,
`StreamFormFile` in `Testing.AspNetCore`, and cancellation and disposal streams in `Testing`; the application's local
copies of these fakes are removed. It also adds the `prune-prereleases` command of the SharedKernel CI tool and shortens
the default local candidate version. Version `0.1.0-alpha.541.1` added the public-site building blocks (`Blazor.Site`, `AspNetCore.Localization`,
`ServiceDefaults`, `Aspire.Hosting`, `EntityFrameworkCore.Npgsql`), BFF token management in
`AspNetCore.Authentication.Bff` that replaces Duende.AccessTokenManagement, and system-test infrastructure. It is breaking:
the service defaults own the `HttpClient` pipeline, `AddHttpClientDefaults` is removed and `ImageSanitizing` is renamed
`UploadSanitizing`. It also adds image format and animation probing with PNG encoding, and raises the ASP.NET Core floors to
10.0.14. Version `0.1.0-alpha.530.1` added `EditSession` to `PandaBear.SharedKernel.Blazor` (its `Sync` loads with a
`ValueTask`), made the unhandled-exception handler answer a client that accepts only HTML with the problem body, and moved
Aspire to 13.6.5 for CVE-2026-120006 and CVE-2026-118740. Version `0.1.0-alpha.521.1` made the coverage gate ignore compiler-generated
methods, added ordered document blocks (`DocumentBlock` and `DocumentSection.Blocks`), documented how a host keeps its own
client-error body through `ProblemDetailsOptions`, and added the sandbox settings and packages updates commands of the
SharedKernel tools. Version `0.1.0-alpha.511.1` had split `PandaBear.SharedKernel.AspNetCore` into a core plus `AspNetCore.Authentication`,
`AspNetCore.RateLimiting`, `AspNetCore.Seo` and `Idempotency.AspNetCore` (new namespaces `PandaBear.AspNetCore.Authentication`,
`.RateLimiting`, `.Seo` and `PandaBear.Idempotency.AspNetCore`; `AddSecurityEventTelemetry` became
`AddAuthenticationEventTelemetry` and `AddRateLimitEventTelemetry`). It kept `lockExemptProjects`.
The packages, symbol archives and manifest are published in GitHub prerelease
[`v0.1.0-alpha.653.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.653.1).
The pin records Linux and Windows package consumption and NativeAOT for `net11.0` as `passed`, merged with
`sharedkernel-version merge-evidence` from SharedKernel's published-candidate run
[44146765771](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/44146765771), and stable promotion as `pending`.

`SKTEST018` requires execution traits in test projects. `.editorconfig` sets it to `warning`, which the repository treats
as an error during builds.

The library repository keeps only its newest prereleases and the versions that consumers pin; a pruned release keeps its
tag but cannot be staged again. The previews this repository pinned before, from `0.1.0-alpha.649.1` at revision
`db8fd3fcec1c362e8741c59ef441596e9499a7c3` back to `0.1.0-alpha.521.1`, are pruned once this adoption lands; their
application validation is historical evidence.

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
`.local/sharedkernel-source/SharedKernel.CI.Tool` as a blobless, cone sparse checkout of `src` and
`tools`, which hold every library the tool can reference, fetches the pinned `libraryRevision`, and checks
it out detached. It reuses that directory only when it is this sparse clone, or one made for the earlier
`tools/SharedKernel.CI.Tool` and `tools/Shared` cone, which it moves to `src` and `tools`; otherwise it
stops and leaves the directory in place. `--library-root` names a clean checkout of the exact revision instead.

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
dotnet ci openapi generate
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
those changes, and use a published candidate with recipe support to record compatibility.
On a clean machine, run the [SDK and candidate setup](#stage-the-candidate) first.

```text
dotnet release verify-consumer --candidate .local/sharedkernel-feed/candidate.json --recipe .config/consumer-verification.json
```

The [committed recipe](../../.config/consumer-verification.json) runs the same ordered .NET commands
on Linux and Windows where the test prerequisites are installed. It restores tools and audited
locked dependencies, verifies lock coverage, builds Release, generates OpenAPI contracts, and tests
the solution. OpenAPI generation runs in the verification clone.

When adopting a candidate that adds or updates the isolated Podman runner, run the full solution
and focused Hosted storage checks in two sequential private engines from the same verified clone.
The Linux recipe binds the caller's image store, runtime parent, and separate retained output
directories. [Isolated consumer validation](../../tests/ViajantesTurismo.HostedTests/README.md#verify-a-consumer-candidate-with-podman)

Compatibility records both committed repository revisions and package hashes in
`TestResults/consumer-compatibility.json` as `passed`, `failed`, or `pending` (uncommitted changes run
nothing). Validation runs in a temporary local clone of the recorded application commit with an isolated
package cache, then removes both. The working checkout's restore assets and binaries remain unchanged. A renamed package
identity does not establish compatibility with previously published `SharedKernel.*` packages.
Rollback may use only an approved, non-retired compatible candidate after validation. Update the
application dependency configuration, candidate manifest, and lock files together, then stage
that candidate's exact artifacts. Retired releases must remain deleted and must not be consumed again.

### Guard local consumer validation on Linux

After staging a published candidate that includes `dotnet repo run-with-budget`, run the opt-in
guard from the application root. Set `VALIDATION_SCRATCH` to an existing absolute directory on
disk and `VALIDATION_REPORT` to an absolute path for a new JSON report. Set the two `_MIB` floors
to the available host RAM and scratch space you want to reserve. Set `VALIDATION_SAMPLE_SECONDS`
to the interval between resource checks. The guard uses the scratch directory for `TMPDIR` and
leaves the report at the caller's path after the verifier cleans up.
Use paths outside the working tree or ignored by Git to keep the application checkout clean
for the committed-revision check.

On a host with a delegated cgroup v2 memory controller, set `VALIDATION_MEMORY_HIGH` and
`VALIDATION_MEMORY_MAX` for the transient user scope. The scope applies limits to processes
that stay in it. Run `dotnet repo run-with-budget` directly on hosts without user scopes.
[Transient scopes](https://www.freedesktop.org/software/systemd/man/latest/systemd-run.html) ·
[Memory controls](https://www.freedesktop.org/software/systemd/man/latest/systemd.resource-control.html)

```sh
systemd-run --user --scope \
  -p "MemoryHigh=$VALIDATION_MEMORY_HIGH" -p "MemoryMax=$VALIDATION_MEMORY_MAX" -p MemorySwapMax=0 \
  dotnet repo run-with-budget --working-directory "$PWD" --scratch "$VALIDATION_SCRATCH" \
  --report "$VALIDATION_REPORT" --minimum-host-memory-mib "$VALIDATION_HOST_FLOOR_MIB" \
  --minimum-scratch-mib "$VALIDATION_SCRATCH_FLOOR_MIB" --reject-memory-filesystem \
  --sample-seconds "$VALIDATION_SAMPLE_SECONDS" \
  -- dotnet release verify-consumer --candidate .local/sharedkernel-feed/candidate.json \
     --recipe .config/consumer-verification.json
```

For the Linux Podman recipe, pass the recipe path and the same caller inputs shown in the
[isolated validation instructions](../../tests/ViajantesTurismo.HostedTests/README.md#verify-a-consumer-candidate-with-podman).
The guard stops its observed owned child process tree when a limit is crossed. Podman may put
containers in separate scopes. Keep the host OOM, swap and shared-memory monitor active and
check each private engine and its cleanup through the isolated runner.

Retain the guard report together with the consumer compatibility record and pinned artifact
hashes. For a safe preflight control, repeat with a new report path and set
`VALIDATION_HOST_FLOOR_MIB` above the host's `MemTotal` converted to MiB. Expect exit 125,
`reason` set to `HostMemoryFloor`, and `childStarted` set to `false` in the report.

Generic outbox and idempotency implementation tests live in the library persistence test project.
The transfer preserves 37 guards in library test commit `3752abb54681e28f4d16abd29966bab3819dd0ea`;
application tests continue to verify domain dispatch, saved envelopes, registration, and schema policy
through public package interfaces. This test-only commit does not change the pinned runtime artifacts.

## Shared CI references and the pin bump

Generic CI pieces come from SharedKernel's
[reusable workflows and actions](
https://github.com/danigutsch/PandaBear.SharedKernel/blob/691b489eb98b4114ff5bc6e76c8899f10cca9222/docs/ci/reusable-workflows.md),
referenced by full commit SHA. The workflow references currently use
`691b489eb98b4114ff5bc6e76c8899f10cca9222`, which equals the `libraryRevision` in
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
sparse checkout of `src` and `tools` without persisted credentials, builds the tool
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

Generic commands use `dotnet repo <command>` and documentation commands use
`dotnet docs <command>`. Endpoint metadata methods and audience classification
are consumer settings in `docs/architecture/generated-diagrams.json`. Shared telemetry privacy processors
always enforce the baseline, with additional business-sensitive attribute policy supplied by ServiceDefaults.

## Shared security tooling

Security baseline checking and deterministic scanner orchestration are provided by the pinned
`dotnet repo security` commands. Application document contracts, endpoint policies, CI
permissions, and runtime security tests remain here. SharedKernel also uses the same implementation
for its own baseline. The Linux scanner installer pins and verifies Gitleaks separately from NuGet
packages; see [the application baseline](../security/security-baseline.md).
