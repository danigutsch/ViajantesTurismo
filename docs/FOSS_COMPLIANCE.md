# FOSS compliance

This repository is licensed under the [MIT license](../LICENSE.txt). Package metadata uses the SPDX
license expression `MIT` unless a project explicitly documents a different license.

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
moderate-or-higher advisories and by allowing only the routine-license set above for changed
dependencies. Update this document and `.github/workflows/dependency-review.yml` together when the
routine license set changes.

## Third-party notices

Do not vendor third-party source, generated files, templates, images, fonts, or binary assets without
recording provenance and license evidence in the same pull request.

For NuGet dependencies, the current attribution inventory source is the resolved `packages.lock.json`
files plus package license metadata surfaced by NuGet and GitHub dependency review. If a dependency
requires a NOTICE file, attribution text, or bundled license text, add the required notice artifact in
the same change that introduces the dependency.

## SBOM and release artifacts

The release-prep workflow currently generates packages, release notes, a changelog, API compatibility
reports, Aspire deployment artifacts, and a release manifest with SHA-256 package hashes. A formal SBOM
is not generated yet because no repository-approved SBOM tool is pinned in local or CI tooling.

When adding SBOM generation, use the repository local tool security model:

1. prefer a repository-pinned `.NET` local tool or repository-owned script;
2. avoid `npx`, transient package execution, or unpinned install scripts;
3. generate from resolved dependency/package outputs, not project manifests alone;
4. publish the SBOM with release-prep artifacts and document the refresh command here.

Until then, release reviewers should use `artifacts/release-prep/release-manifest.json`, package lock
files, and dependency-review results as the repository's baseline provenance and dependency evidence.

## Package metadata

Packable projects should inherit repository metadata from `Directory.Build.props` unless they have a
documented exception:

- `Authors`: `ViajantesTurismo contributors`
- `Company`: `ViajantesTurismo`
- `PackageLicenseExpression`: `MIT`
- `RepositoryUrl`: `https://github.com/danigutsch/ViajantesTurismo`
- `PublishRepositoryUrl`: `true`

Do not replace SPDX package license metadata with embedded license files unless a package has a specific
non-standard licensing need.
