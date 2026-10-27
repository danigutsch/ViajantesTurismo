# API compatibility gates

This repository protects Admin, Catalog, and Branding HTTP contracts through canonical OpenAPI
artifacts and contract tests. SharedKernel package APIs, public API baselines, package validation,
and release policy belong to the [standalone library](https://github.com/danigutsch/PandaBear.SharedKernel).
See its [release policy](https://github.com/danigutsch/PandaBear.SharedKernel/blob/f5975b60601e245bda6bb8e45559d8ce3a24a8e0/docs/release-policy.md)
for library checks; do not run source-level library validation from this application checkout.

## Release phase policy

| Phase | HTTP compatibility expectation |
| --- | --- |
| Alpha / `0.y.z` / `-alpha` | Evolve current contracts directly. Do not retain legacy implementations or add parallel endpoints solely for backward compatibility. Document significant consumer-visible breaks. |
| Beta / `-beta` | Incompatible changes require a new API version and deprecation of the previous route. |
| Release candidate / `-rc` | Treat the API as final except for critical fixes; block incompatible changes. |
| Stable / `>=1.0.0` | Use a new HTTP API version for incompatible changes. Deprecate first when practical. |

## Local checks

Follow the [SharedKernel consumption workflow](sharedkernel/development.md#adopt-a-library-change)
to bootstrap immutable pinned artifacts, validate application behavior, and record compatibility
against both committed repository revisions. Library CI owns package API validation; application CI
owns consumer builds, tests, and HTTP contract checks.

## Breaking-change marker

Document significant consumer-visible breaks, caller impact, and any required data migration in the
same change. A Conventional Commit breaking marker does not waive HTTP compatibility from beta onward.
Library package breaking markers and release exceptions are governed in the standalone library.

## OpenAPI snapshots

The Admin, Catalog, and Branding contract tests read the committed OpenAPI artifacts and compare them
with generated documents. Regenerate the committed artifacts intentionally when HTTP contracts change:

After a locked restore, `sharedkernel-ci openapi` builds the `admin`, `branding`, and `catalog` targets of
`.config/sharedkernel-ci.json` in order with a minimal child environment. Trusted generation omits JWT/OIDC
registration while retaining authorization metadata for the generated Bearer security documents. Normal
application runs remain fail-closed when authentication configuration is missing.

```bash
dotnet tool run sharedkernel-ci -- openapi refresh
```

ADR-040 keeps the `branding` target and the Branding-owned canonical artifact paths stable while
Catalog.ApiService hosts the module. Generation is Catalog-hosted and Branding-only. Preserve `branding`, `public-branding`, and Branding-only `v1` artifacts;
do not replace them with Catalog-owned or mixed-route snapshots.

The Management Branding optimistic-concurrency change keeps the route and full-state PUT but adds
`SourceEpoch`, `SourceRevision`, strong `ETag`/`If-Match`, 400 malformed syntax, 412 false
precondition, and 428 missing precondition metadata. OpenAPI descriptions preserve RFC 9110 list,
weak-tag strong-comparison, and existing-singleton `*` semantics. Refresh canonical Branding artifacts
and typed clients together. Management `GET` owns the response `ETag`; normalized successful `PUT`
omits it and clients refresh through `GET`, replacing document, epoch, revision, and validator as one
editor state. Clients expose precondition failure as an outcome and never auto-retry it; an interleaving
writer between `PUT` and `GET` must be surfaced through the later representation.

Then run:

```bash
dotnet test --project tests/ViajantesTurismo.Admin.ContractTests/ViajantesTurismo.Admin.ContractTests.csproj --filter-class "*OpenApi*"
dotnet test --project tests/ViajantesTurismo.Catalog.ContractTests/ViajantesTurismo.Catalog.ContractTests.csproj --filter-class "*OpenApi*"
dotnet test --project tests/ViajantesTurismo.Branding.ContractTests/ViajantesTurismo.Branding.ContractTests.csproj --filter-class "*OpenApi*"
```
