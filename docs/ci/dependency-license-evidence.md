# Dependency snapshot and license evidence

Owner: repository maintainers. Reviewed October 24, 2026. Re-review on package/SDK/Aspire changes
or by January 20, 2027, whichever comes first. This record does not grant general license approval.

## Contents

- [Snapshot failure and correction](#snapshot-failure-and-correction)
- [StreamJsonRpc metadata review](#streamjsonrpc-metadata-review)
- [Magick native audit](#magick-native-audit)
- [Security candidate renewal](#security-candidate-renewal)
- [Payments rebase candidate](#payments-rebase-candidate)
- [Rate-limit and reconciliation candidate renewal](#rate-limit-and-reconciliation-candidate-renewal)
- [DDD standards candidate](#ddd-standards-candidate)
- [ShouldThrow message candidate](#shouldthrow-message-candidate)
- [Remaining decision](#remaining-decision)

## Snapshot failure and correction

The inspected GitHub automatic NuGet submission restores the first 20 project paths emitted by
`find`, without sorting, and does not discover the `.slnx` solution. Base and head restored different
subsets despite identical dependency inputs. The dependency comparison reported 1,134 additions and
287 removals on a documentation-only change. Rerunning the base submission did not resolve it.

The repository Dependency Review workflow submits the complete committed lock graph for the exact
PR base/head SHAs with the same stable `viajantes-solution-nuget` correlator. Each target is checked
out into an isolated `dependency-input` directory. The shared `dependency snapshot` engine verifies
the exact clean target revision, discovers the committed solution's projects, and requires their
lock files. It emits the locked package versions, framework-specific nodes, direct/transitive
classification, and dependency edges; malformed, missing, or unresolved graph input fails visibly.
Snapshot JSON is retained as CI evidence. This describes the committed restore graph, not a claim
that all build-time dependency inputs have been dynamically evaluated.

Both targets use one reviewed generator implementation, independently pinned by
`SNAPSHOT_GENERATOR_REVISION` to SharedKernel revision
[`8895a57934be23177f9979a23620774e5085eb3d`](https://github.com/danigutsch/PandaBear.SharedKernel/commit/8895a57934be23177f9979a23620774e5085eb3d).
The workflow verifies the exact clean source checkout, selects its `global.json` SDK, and builds
`SharedKernel.RepoConfig.Tool`. Only the generator's own prerequisites are restored. The runtime
candidate `0.1.0-alpha.70.1` predates this command; its manifest does not select the generator.
No application solution is restored, and neither target's historical candidate packages, release
assets, or local feed are needed. There is no fallback to a mutable branch, retired preview, or old
detector binary.
Retiring a prior release therefore cannot break a base snapshot merely because that base once
consumed its packages.

The write-capable snapshot job runs only for same-repository PRs from non-Dependabot actors. It does
not use `pull_request_target`, repository secrets, or persisted checkout credentials. The scoped
`contents: write` token exists only in the native PowerShell submission step. That step posts the
validated JSON to GitHub's snapshot endpoint and fails on HTTP errors. A failed/canceled generation
or submission job fails the required review rather than allowing a successful aggregate. Fork and
Dependabot PRs retain the read-only review against available GitHub graph data; this does not claim
complete dynamic snapshot coverage for them. Merge-group events retain their existing review path.

This source-built engine replaces the component-detection submission Action. The previously pinned
Action downloaded the latest detector binary, and that detector required restore assets/binlogs
rather than accepting `packages.lock.json` directly. Merely removing restore from that path would
lose graph coverage. The replacement introduces no new external package or scanner dependency and
never downloads retired candidate artifacts. GitHub manual submissions take precedence over
automatic submissions per [its documented deduplication](https://docs.github.com/en/rest/dependency-graph/dependency-submission).
The same repository-owned submission runs on `main` pushes and manual dispatches. GitHub's managed
NuGet auto-submission still cannot stage the pinned local candidate feed; rerunning it does not fix
that missing prerequisite. Use this committed-lock submission to retain dependency graph data,
complete PR snapshots, and the required Dependency Review without historical restore coupling.

## StreamJsonRpc metadata review

Reviewed package: `pkg:nuget/StreamJsonRpc@2.25.29`. On October 21, 2026, the repository owner
explicitly approved accepting this package while consuming the current SharedKernel preview.
The owner also approved using the consumed packages' declared licenses for this preview:
StreamJsonRpc `2.25.29` declares MIT; Magick.NET-Q8-AnyCPU `15.0.0` declares Apache-2.0.
Both nuspec declarations were rechecked against the installed exact versions on October 21, 2026.
This approval is limited to candidate `1.0.0-alpha.20261021.2`, clean library revision
`f5975b60601e245bda6bb8e45559d8ce3a24a8e0`, and these two exact dependency versions.
Vulnerability checks and the moderate severity gate remain active.

The pinned review Action's `purlsMatch` implementation ignores package versions when matching
`allow-dependencies-licenses`. Supplying a version in that option would exempt future versions too,
contrary to the intended scope. The workflow therefore validates the exact candidate version,
revision, clean marker, and every approved package entry in every tracked lock file on the reviewed
head commit before emitting the package-specific exception. A changed candidate (including a
stable version), missing package, or unexpected dependency version fails visibly and requires
removing or explicitly re-reviewing the exception. The purl alone is not the boundary. See the
[exact pinned source](https://github.com/actions/dependency-review-action/blob/2031cfc080254a8a887f58cffee85186f0e49e48/src/purl.ts).
Complete base/head snapshots still resolve false additions. No general MS-PL or NOASSERTION
allowance is granted. Only the two named package identities receive a license metadata exception.

The installed NuGet package declares MIT and repository commit
`332cc170a82fa424e0b4f902ab9f9a876e827f38`. Aspire.Hosting 13.6.1 still references it directly;
AppHost, PostgreSQL, and Redis hosting packages resolve it transitively. It is not an obsolete direct
application dependency to delete from lock files.

The packaged NOTICE also lists build/test dependencies including `Xunit.Combinatorial` and
`Xunit.StaFact` with MS-PL notices. The nupkg assembly inventory contains StreamJsonRpc libraries,
resources, and its analyzers/code fixes, not those xUnit assemblies; its nuspec does not declare
those test dependencies. GitHub's repository-level mixed classification must not replace inspection
of the actual package's declared license and distribution. Preserve the upstream NOTICE when
redistributing the package; this correction does not delete attribution obligations.

Local exact-package evidence (SHA-256):

| File | Digest |
| --- | --- |
| `streamjsonrpc.nuspec` | `ded76e786e0f2f58f33c9a56e06cf1d0f564a674f01947714fffaed23b0098f7` |
| `NOTICE` | `398758d9e312d3070dcb15d19fdfdc9c699635dceeedf8037c1c703405bbe19e` |

Sources: the package's versioned NuGet archive and
[Aspire 13.6.1 source](https://github.com/microsoft/aspire/blob/v13.6.1/src/Aspire.Hosting/Aspire.Hosting.csproj).
Re-review a new version's archive and notices rather than assuming the same conclusion. This
record preserves provenance. The preview approval does not cover future versions or remove notice
and redistribution obligations.

## Magick native audit

Exact package: `Magick.NET-Q8-AnyCPU@15.0.0`, repository commit
`79bb8700d7fe3a564f62926ab299fc2520cb2269`. Its nuspec declares Apache-2.0, but its `Notice.txt`
also covers native dependencies with LGPL notices, including GLib, libheif, liblqr, libraw, Pango,
and librsvg. The managed license alone is insufficient evidence for the complete distribution.
The owner's preview approval corrects the review classification using this exact package's
Apache-2.0 declaration. It does not resolve the native components' distribution obligations or
grant an exception to other Magick.NET package identities or future versions.

Audited the installed nupkg extracted by NuGet without executing native code. A byte-string probe
looked for `heif_context_alloc`, `g_malloc`, `rsvg_handle_new`, `libraw_init`, and `lqr_carver_new`.
Linux ELF inspection additionally checked defined dynamic symbols and `DT_NEEDED` entries.

| Runtime artifact under `runtimes/` | SHA-256 | Evidence |
| --- | --- | --- |
| `linux-x64/native/Magick.Native-Q8-x64.dll.so` | `604a949384414fece8ded77d6d9f93aadec0b79cc665d5bb7086dcc6674e59ff` | All five symbols are defined inside the ELF library; runtime-needed libraries are system libc/math/thread/resolver/loader, not separate HEIF/GLib/etc. libraries |
| `linux-arm64/native/Magick.Native-Q8-arm64.dll.so` | `42a69c012f2164fec2ce1539ccc6e63b2e1064bfe516b6d9169f85ab7fca712a` | All five sampled symbol strings present; linkage not separately verified |
| `linux-musl-x64/native/Magick.Native-Q8-x64.dll.so` | `0c854669df8dc30975c2e13435dbe17f010beaaf71cb23ed328c944be448c626` | All five sampled symbol strings present; linkage not separately verified |
| `osx-arm64/native/Magick.Native-Q8-arm64.dll.dylib` | `6a6d52a85784ca710fdc473adadd5cd87d0c6664ffcdf4ad840233ded45f4295` | All five sampled symbol strings present; linkage not separately verified |
| `win-x64/native/Magick.Native-Q8-x64.dll` | `8e317b3eca8bb01119b7436908da9de5bc9d8466dba866e54db646fb525d45e7` | Sampled strings absent; this is not evidence those dependencies or obligations are absent |
| `win-arm64/native/Magick.Native-Q8-arm64.dll` | `f1cb00830672bb4755e5b1147f1a93962d1c6f31acf6e78640a4592a09f589b8` | Same limitation as Windows x64 |
| `win-x86/native/Magick.Native-Q8-x86.dll` | `aca548de9a54645577043c9f3e66153c21b5a6043c5202c16b2e90482cf3da19` | Same limitation as Windows x64 |

Evidence hashes: nuspec `63e1eb3796c8cfe8e068d05976343466f4fd106826a76b47e3aa8a1eb71c7919`;
Notice.txt `a08ce0f18e36f83c971ab35a7504523fa25a797e99eec57e67c96ab4648b1409`.
The binary probes are technical inclusion evidence, not a legal compliance determination or an
exhaustive dependency inventory. Windows and non-x64 linkage need platform-appropriate inspection.

## Security candidate renewal

Status: **owner-approved October 23, 2026** for candidate `0.1.0-alpha.70.1`, clean library
revision `ef0dd0a67b8982dfdf0cb8f5e23224c397971a5b`. The owner explicitly renewed the same two
metadata exceptions for StreamJsonRpc `2.25.29` and Magick.NET-Q8-AnyCPU `15.0.0`, preserving all
existing gates. Earlier approvals for `1.0.0-alpha.20261021.2` and retired
`1.2.0-alpha.20261023.1` remain historical evidence; this renewal applies only to the newly named
fresh candidate. Retired release artifacts must not be consumed again.

Rechecked the installed exact archives, nuspec declarations, and notices against the recorded audit.
StreamJsonRpc `2.25.29` still declares MIT; Magick.NET-Q8-AnyCPU `15.0.0` still declares Apache-2.0.
All 8 StreamJsonRpc and 14 Magick package entries across the tracked lock graph retain the exact
version, content hash, and dependency edges from application base `7a6be4ec`. The four previously
recorded nuspec/notice SHA-256 values above match. Archive SHA-256 values are:

| Package archive | SHA-256 |
| --- | --- |
| `StreamJsonRpc.2.25.29.nupkg` | `d0f7be6b1b623df638a35371eb297aa0f7df18ff50555fc9448573e7a5c1111b` |
| `Magick.NET-Q8-AnyCPU.15.0.0.nupkg` | `7ed7338e11721c0a11ab9480c5e00bb119b5b0ac59c994fa2cc553df21662cf5` |

The renewal changes only the exact candidate version/revision boundary. It retains the
same two package identities and exact versions, every-lock-entry verification, missing-package
failure, moderate vulnerability threshold, and both license/vulnerability checks. It grants no
NOASSERTION, MS-PL, LGPL, other package, future version, stable release, or native redistribution
approval. The Magick native inclusion evidence and unresolved distribution obligations above remain.

NuGet distinguishes the signed package archive hash from its unsigned content hash. The lock-file
content hash is compared with the same previous locked graph, rather than incorrectly comparing it
with the whole signed ZIP SHA-512; see
[NuGet's content-hash implementation](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Packaging/PackageArchiveReader.cs).

## Payments rebase candidate

Prepared candidate `0.1.0-alpha.94.1` uses clean library revision
`d08008cd3e8901c304377edcc4683de7c3a85ecb`. It combines the accepted security changes with merged
request-binding defaults, assertion helpers and the shared snapshot generator. Linux/Windows package
consumption and NativeAOT passed in the exact-source CI run. All 74 archive hashes were verified.

Comparison of the complete external package version/content-hash sets against application base
`f460444b` found no additions or removals. The prepared workflow still permits only the same exact
StreamJsonRpc `2.25.29` and Magick.NET-Q8-AnyCPU `15.0.0` metadata exceptions; no license category,
vulnerability threshold or native distribution gate was relaxed. On October 23, 2026, the owner
authorized GitHub prerelease distribution and renewed these unchanged exact-package exceptions
for this candidate. Publication remains conditional on successful application validation; registry
publication and the native distribution obligations remain outside this approval.

## Rate-limit and reconciliation candidate renewal

On October 24, 2026, the owner approved unchanged GitHub prerelease
`0.1.0-alpha.112.1`, clean library revision
`e4d13f63ff872a86478a16e5a3c3ce1ecba0fcb1`, and renewed the same exact
StreamJsonRpc `2.25.29` and Magick.NET-Q8-AnyCPU `15.0.0` metadata exceptions
for this candidate. The 8 StreamJsonRpc and 14 Magick lock entries retain their
versions, content hashes, and dependency edges compared with application main
`f2337a26`. Both archive SHA-256 hashes match the recorded evidence above.

Producer run `40039601794` passed API validation and Linux/Windows packed
consumption, including NativeAOT. The application Release build passed without
warnings or errors. The owner requested no application test rerun after the merge;
this build evidence does not claim a new full isolated compatibility run.

The workflow remains bounded to this exact clean candidate and the same two
package versions. Vulnerability and forbidden-license gates remain enabled.
NuGet publication, future candidates, stable promotion, and native distribution
approval remain excluded; the native obligations described above are unchanged.

## DDD standards candidate

Candidate `0.1.0-alpha.133.1` uses clean library revision
`078adf2b2b158f38a211f48274af548d12a85752`. It adds the domain encapsulation analyzers and code fix,
the opt-in assertion `userMessage` rule, packaged repository tooling and the other library changes
merged since `0.1.0-alpha.112.1`. Linux/Windows package consumption and NativeAOT passed in the
exact-source CI run. All 77 archive hashes were verified.

Comparison of the complete external package version/content-hash sets across all 61 lock files
against application main `a92f03fb` (candidate `0.1.0-alpha.112.1`) found no additions or removals;
both sets hold the same 283 entries. The workflow still permits only the same exact StreamJsonRpc `2.25.29` and
Magick.NET-Q8-AnyCPU `15.0.0` metadata exceptions; no license category, vulnerability threshold or
native distribution gate was relaxed. On October 24, 2026, the owner authorized GitHub prerelease
distribution and renewed these unchanged exact-package exceptions for this candidate. Publication
remains conditional on successful application validation; registry publication and the native
distribution obligations remain outside this approval.

## ShouldThrow message candidate

Candidate `0.1.0-alpha.144.1` uses clean library revision
`ee4126348011b2ddf60d3e580428867689c412dd`. It gives `ShouldThrow<T>` a `userMessage` overload,
removes its `paramName` overloads, moves candidate staging to `sharedkernel-ci stage-candidate`, and
includes the other library changes merged since `0.1.0-alpha.133.1`. Linux/Windows package
consumption and NativeAOT passed in the exact-source CI run. All 77 archive hashes were verified.

Comparison of the complete external package version/content-hash sets across all 61 lock files
against application main `c93781a4` (candidate `0.1.0-alpha.133.1`) found no additions or removals;
both sets hold the same 283 entries. The workflow still permits only the same exact StreamJsonRpc
`2.25.29` and Magick.NET-Q8-AnyCPU `15.0.0` metadata exceptions; no license category, vulnerability
threshold or native distribution gate was relaxed. On October 24, 2026, the owner authorized GitHub
prerelease distribution and renewed these unchanged exact-package exceptions for this candidate.
Registry publication and the native distribution obligations remain outside this approval.

## Remaining decision

Keep current versions and library behavior unchanged while resolving distribution obligations.
The recommended next decision is an explicit native-component review for the actual deployment
RIDs and distribution model: required notices, corresponding source/relinking obligations where
applicable, and how releases provide them. Do not interpret LGPL as automatically prohibiting use
or the Apache package declaration as automatically satisfying every native obligation.

Retain required notices/evidence in release artifacts. The preview metadata exception does not
replace that distribution review. If the distribution cannot satisfy the selected
policy, evaluate a reduced native build or a different image provider with the owning library
repository separately; this application PR does not change SharedKernel implementations.

A zero dependency delta after full snapshots means this docs PR introduces no package change. It
does not approve existing Magick.NET distribution or remove the need for the native review. Keep
license and vulnerability findings visible; never add global NOASSERTION or LGPL allowances to
turn the check green.
