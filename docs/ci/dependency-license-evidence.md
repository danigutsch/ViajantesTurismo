# Dependency snapshot and license evidence

Owner: repository maintainers. Reviewed October 20, 2026. Re-review on package/SDK/Aspire changes
or by January 20, 2027, whichever comes first. This record does not grant general license approval.

## Contents

- [Snapshot failure and correction](#snapshot-failure-and-correction)
- [StreamJsonRpc metadata review](#streamjsonrpc-metadata-review)
- [Magick native audit](#magick-native-audit)
- [Remaining decision](#remaining-decision)

## Snapshot failure and correction

The inspected GitHub automatic NuGet submission restores the first 20 project paths emitted by
`find`, without sorting, and does not discover the `.slnx` solution. Base and head restored different
subsets despite identical dependency inputs. The dependency comparison reported 1,134 additions and
287 removals on a documentation-only change. Rerunning the base submission did not resolve it.

The repository Dependency Review workflow now restores the entire locked solution for the exact
PR base/head SHAs and submits both graphs with the same stable `viajantes-solution-nuget`
correlator. The restore binary log supplies evaluated graph evidence to Component Detection.
A failed/canceled snapshot job fails the required review rather than allowing a successful aggregate.

The write-capable snapshot job runs only for same-repository PRs from non-Dependabot actors. It does
not use `pull_request_target`, repository secrets, or persisted checkout credentials. Fork and
Dependabot PRs retain the read-only review against available GitHub graph data; this does not claim
complete dynamic snapshot coverage for them. Merge-group events retain their existing review path.

The checked-in submission Action uses the same full SHA observed in GitHub's automatic workflow.
It introduces a repository-managed Action dependency and its downloaded/transitive scanning tools,
so maintainers must review that supply-chain surface when updating the pin. No new NuGet dependency
is introduced. Manual submissions take precedence over automatic submissions per
[GitHub's documented deduplication](https://docs.github.com/en/rest/dependency-graph/dependency-submission).
Automatic submission is left enabled as fallback; no remote security setting is changed.

## StreamJsonRpc metadata review

Reviewed package: `pkg:nuget/StreamJsonRpc@2.25.29`. No package license exemption is configured.
Vulnerability checks and the moderate severity gate remain active.

The pinned review Action's `purlsMatch` implementation ignores package versions when matching
`allow-dependencies-licenses`. Supplying a version in that option would exempt future versions too,
contrary to the intended scope. Do not use that option as a version-specific correction. See the
[exact pinned source](https://github.com/actions/dependency-review-action/blob/2031cfc080254a8a887f58cffee85186f0e49e48/src/purl.ts).
Complete base/head snapshots resolve false additions without changing license policy. If a real
package change later needs an exception, use a separately reviewed mechanism that actually checks
its version and preserve this evidence; do not rely on the appearance of a versioned purl.

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
record preserves provenance without disabling license checks for the package or any future version.

## Magick native audit

Exact package: `Magick.NET-Q8-AnyCPU@15.0.0`, repository commit
`79bb8700d7fe3a564f62926ab299fc2520cb2269`. Its nuspec declares Apache-2.0, but its `Notice.txt`
also covers native dependencies with LGPL notices, including GLib, libheif, liblqr, libraw, Pango,
and librsvg. The managed license alone is insufficient evidence for the complete distribution.
No Magick.NET license exception is added.

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

## Remaining decision

Keep current versions and library behavior unchanged while resolving distribution obligations.
The recommended next decision is an explicit native-component review for the actual deployment
RIDs and distribution model: required notices, corresponding source/relinking obligations where
applicable, and how releases provide them. Do not interpret LGPL as automatically prohibiting use
or the Apache package declaration as automatically satisfying every native obligation.

If that review approves the existing distribution, record a version-scoped exception and retain
required notices/evidence in release artifacts. If the distribution cannot satisfy the selected
policy, evaluate a reduced native build or a different image provider with the owning library
repository separately; this application PR does not change SharedKernel implementations.

A zero dependency delta after full snapshots means this docs PR introduces no package change. It
does not approve existing Magick.NET distribution or remove the need for the native review. Keep
license and vulnerability findings visible; never add global NOASSERTION or LGPL allowances to
turn the check green.
