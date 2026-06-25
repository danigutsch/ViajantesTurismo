# ADR-028: API Entry Point Test Markers

## Context

ASP.NET Core `WebApplicationFactory<TEntryPoint>` needs a concrete type from the target web
application assembly so tests can locate and boot the application entry assembly.

For minimal APIs with top-level statements, the compiler-generated `Program` type is not a normal
public type. Exposing it directly for tests can require a partial `Program` shim. That shim adds a
production type solely for tests, can conflict with one-type-per-file guidance, and may trigger
namespace or coverage analyzer concerns.

This repository already uses `*Marker` classes as assembly markers for architecture tests and
composition roots. Those marker classes should stay static metadata holders and should not become
instantiable just to satisfy test infrastructure.

## Decision

API projects that need `WebApplicationFactory<TEntryPoint>` coverage may add a dedicated internal
entry-point marker type in the API project.

Use this shape:

- Keep the existing public `*Marker` class static when it is only an assembly metadata holder.
- Add a separate top-level internal `*EntryPoint` type in its own file for web-host tests.
- Expose that internal type to the corresponding test assembly with `InternalsVisibleTo`.
- Do not add partial `Program` shims, coverage exclusions, or analyzer suppressions only to support
  `WebApplicationFactory<TEntryPoint>`.
- Keep the entry-point marker free of business behavior; it exists only to identify the entry
  assembly for test hosting.

Current example:

- `CatalogApiMarker` remains the static Catalog API assembly marker.
- `CatalogApiEntryPoint` is the internal concrete type used by Catalog API startup tests.
- `ViajantesTurismo.Catalog.ApiService` grants internals visibility to
  `ViajantesTurismo.Catalog.ApiServiceTests`.

## Consequences

- `Program.cs` stays focused on startup code and remains covered by host tests.
- Test-only hosting needs do not expand the public production API surface.
- Static `*Marker` semantics stay consistent across the repository.
- Each concrete top-level type has its own file, avoiding analyzer suppressions for generated
  top-level `Program` namespace behavior.
- API test projects get a stable `WebApplicationFactory<TEntryPoint>` target without relying on
  compiler-generated entry-point type details.

## Alternatives

- Add an `internal partial Program` shim. Rejected because it adds a test-enabling production type
  around compiler-generated top-level statements and can require analyzer or coverage handling.
- Make the existing public `*Marker` type instantiable. Rejected because it changes marker semantics
  and expands production surface for a test-host concern.
- Exclude `Program.cs` from coverage. Rejected because startup and default endpoint behavior is
  observable and should be covered by focused host tests.

## Status

Accepted.

## Links

- [ADR Index](../ARCHITECTURE_DECISIONS.md)
- [SonarCloud CI Policy](../ci/sonarcloud.md)
