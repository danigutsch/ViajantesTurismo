# FOSS compliance

This repository is licensed under the [MIT license](../LICENSE.txt).

## Dependency license policy

Prefer permissive dependencies with clear provenance and SPDX license evidence.

Allowed for routine dependency updates:

- `MIT`
- `Apache-2.0`
- `BSD-2-Clause`
- `BSD-3-Clause`
- `0BSD`
- `ISC`
- `PostgreSQL`
- `Zlib`
- `Unlicense`
- `CC0-1.0`

Denied for routine dependency updates:

- `AGPL-*`
- `GPL-*`
- `LGPL-*`
- `SSPL-1.0`

Escalate before merging when a dependency has an unknown, custom, mixed, reciprocal, source-available,
copyleft, commercial, or weak-provenance license. Scanner output is evidence, not final legal advice.

The `Dependency Review` workflow enforces the automatable portion for pull requests by failing on
moderate-or-higher advisories and by allowing the routine-license set above for changed dependencies,
subject to the exact preview metadata corrections below. Update this document and
`.github/workflows/dependency-review.yml` together when the routine license set changes.

## Exact preview license evidence

On October 21, 2026, the repository owner approved using the consumed packages' declared licenses
for StreamJsonRpc `2.25.29` (MIT) and Magick.NET-Q8-AnyCPU `15.0.0` (Apache-2.0) while consuming
SharedKernel candidate `1.0.0-alpha.20261021.2` at clean revision
`f5975b60601e245bda6bb8e45559d8ce3a24a8e0`.

The pinned review Action ignores versions in package license exemptions. Before emitting either
package-specific exemption, the workflow therefore checks that exact candidate version, revision,
and clean marker, and validates every occurrence of each package across the reviewed head's tracked
lock files. A changed candidate, stable version, missing approved package, or unexpected dependency
version fails visibly and requires removing or explicitly re-reviewing the exemption.

The [dependency license evidence record](ci/dependency-license-evidence.md) preserves nuspec hashes,
upstream notices, owner approval, and the review deadline. No general MS-PL, NOASSERTION, or LGPL
allowance is granted. Complete snapshots address false dependency additions independently of these
metadata corrections. Magick.NET native notices and inclusion evidence still require a separate
distribution review; the preview correction does not resolve those obligations.

## Vulnerability severity gates

Dependency Review fails pull requests on `moderate` or higher advisories because it evaluates newly
introduced dependency changes. NuGet audit remains at `high` in `Directory.Build.props` because it runs
for all projects during restore/build and may include advisory noise unrelated to the current change.
The preview license corrections do not disable vulnerability checks.

Treat the thresholds as intentionally different until the repository has reviewed full-solution NuGet
audit volume. Lowering NuGet audit to `moderate` is a separate policy change because it can affect every
developer build.

## Third-party notices and provenance

Do not vendor third-party source, generated files, templates, images, fonts, or binary assets without
recording provenance and license evidence in the same pull request. Preserve upstream notices and
review the actual distribution, including native components, rather than relying only on a package's
declared license. Unknown or incomplete license evidence requires explicit review before release.

SharedKernel package metadata, packaging, API compatibility, SBOM generation, and attribution tooling
belong to the [standalone library repository](https://github.com/danigutsch/PandaBear.SharedKernel).
This application consumes its pinned packages; it does not maintain copies of that release tooling.
The committed candidate manifest and package hashes support application consumption provenance.
Follow the [consumer development workflow](sharedkernel/development.md) for adoption and verification.

## Application release evidence

The application [Release Prep workflow](ci/supplemental-workflows.md#release-prep-workflow) calculates
a version and exports a nonempty Aspire resource model at
`artifacts/aspire/release-prep/aspire-manifest.json`. It does not generate application packages,
SBOMs, attribution inventories, library release metadata, or deployment-ready images, and it does
not deploy, tag, or publish. The resource model is review evidence, not a complete distribution
inventory or proof of license compliance.

A future application distribution workflow must define its own component inventory, notices,
provenance, and applicable native-source or relinking obligations before claiming those artifacts.
Public artifact attestations remain deferred; no application attestation guarantee is made here.

## Community and support docs

Support routing is documented in [Support](../SUPPORT.md). Security disclosure stays in
[Security](../SECURITY.md).

`CODE_OF_CONDUCT.md` is intentionally deferred. Maintainers need to choose a public community conduct
policy and enforcement contact before adding one.
