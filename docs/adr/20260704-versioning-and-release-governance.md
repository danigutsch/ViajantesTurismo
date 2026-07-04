# ADR-032: Versioning and Release Governance

**Status**: Accepted — 2026-07-04

## Context

The repository is adding reusable `SharedKernel.*` packages, app release workflows, and contract-owned
API surfaces. These outputs need consistent version signals before automated release workflows can
publish local packages, prereleases, or stable artifacts.

Version decisions must cover NuGet packages, deployed apps, assemblies, HTTP APIs, Aspire resources,
and security-support expectations without requiring every package to become stable at the same time.

## Decision

Use Semantic Versioning for package and API compatibility decisions, with Conventional Commits as the
release-impact signal.

### Versioned outputs

- `SharedKernel.*` NuGet packages are versioned independently by package impact.
- Deployed applications use release tags and build metadata for traceability; they are not treated as
  reusable API packages unless a public compatibility contract is defined.
- Assemblies use the package or app version produced by the release flow. File and informational
  versions may include commit metadata for diagnostics.
- HTTP API compatibility is owned by the contract project and endpoint surface. Breaking request,
  response, status-code, route, or validation-problem changes require a major-version decision or a
  documented compatibility path.
- Aspire AppHost resource names and references are deployment composition signals, not public API
  versions. AppHost changes follow the app release channel that consumes them.

### SharedKernel package principles

`SharedKernel.*` is a proactive reusable package ecosystem for focused best-practice patterns likely
useful across similar .NET projects. Create or keep a SharedKernel package when it has a clear
capability boundary, dependency discipline, independent tests, and a plausible cross-project reuse path.

Do not use SharedKernel as a dumping ground for one bounded context's schema, migrations, read models,
UI behavior, or composition glue. Provider-specific modules must still follow ADR-027.

### Conventional Commit release impact

Map commits to SemVer impact as follows:

| Commit signal | Release impact |
| --- | --- |
| `feat` | Minor version bump |
| `fix` | Patch version bump |
| `perf` | Patch version bump |
| `refactor` | Patch only when observable behavior, public API, or package output changes; otherwise no release |
| `docs`, `test`, `ci`, `build`, `style`, `chore` | No release unless they change shipped artifacts |
| `!` marker or `BREAKING CHANGE:` footer | Major version bump |

When multiple commits affect one output, choose the highest impact. For `0.y.z` packages, breaking
changes may still produce a minor bump while the package is explicitly unstable, but the release notes
must call out the breaking change.

### Prerelease channels

New SharedKernel packages start at `0.1.0-alpha.*` until the owning contracts and tests have survived
real consumption in this repository.

- `alpha` means exploratory but intentionally reusable. Breaking changes are allowed with release-note
  callouts.
- `beta` means the API shape is expected to be stable for current consumers and only compatibility
  fixes are expected before stable.
- Stable `1.0.0` requires documented API ownership, package metadata, CI validation, release notes,
  and a clear support policy.

### Release channels

- Local packages are developer-only outputs from a local feed or artifacts folder. They may include
  commit metadata and must not be promoted as stable releases.
- Prerelease packages use SemVer prerelease identifiers such as `0.1.0-alpha.1` or
  `0.1.0-beta.1`. They may be published for integration testing and early consumers.
- Stable packages use normal SemVer without prerelease identifiers. Promotion requires green CI,
  release notes, tag provenance, package metadata review, and compatibility review for public APIs.

Stable releases are promoted from the same source revision that passed the required prerelease or CI
gate. Do not rebuild different source and call it the same release.

## Consequences

- The repository can implement versioning automation without reopening package-policy decisions.
- SharedKernel packages have an explicit reason to exist while still avoiding catch-all reuse.
- Conventional Commits become a release signal, not only a commit-message style rule.
- Security policy remains current while only `main` is supported. Update `SECURITY.md` when release
  branches or versioned support windows exist.

## Alternatives Considered

1. **One repository-wide version**
   Rejected because reusable packages and deployed apps have different compatibility surfaces.

2. **Publish every SharedKernel package as stable immediately**
   Rejected because early reusable packages need room for API correction before a support promise.

3. **Use manual release impact only**
   Rejected because Conventional Commits already provide a reviewable impact signal.

## Links

- [ADR Index](../ARCHITECTURE_DECISIONS.md)
- [ADR-027: Provider-Specific SharedKernel Infrastructure Modules](20260624-provider-specific-sharedkernel-infrastructure-modules.md)
- [ADR-031: Rich Domain Behavior and State Exposure](20260703-rich-domain-behavior-and-state-exposure.md)
