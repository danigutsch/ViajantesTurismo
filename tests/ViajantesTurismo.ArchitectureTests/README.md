# ViajantesTurismo.ArchitectureTests

Architecture regression tests for the Viajantes Turismo solution. These tests protect Clean Architecture boundaries, DDD
conventions, naming rules, and static architectural conventions in the domain layer.

## What we verify

- `LayerDependencyTests` — Ensures Domain never depends on Application/Infrastructure/API,
  Application stays adapter-free, and Infrastructure is not coupled to the transport layer.
- `DddConventionsTests` — Guards aggregate patterns: entities stay sealed and live under the
  domain namespaces; value objects remain immutable with no public setters.
- `NamingConventionTests` — Enforces consistent naming (interfaces start with `I`, contracts end with `Dto`,
  architecture test classes end with `Tests`).
- `ErrorClassTests` — Ensures domain `*Errors` helper classes follow the required static-class convention.

## Running the tests

```powershell
# From the repository root
dotnet test --project tests/ViajantesTurismo.ArchitectureTests
```

> These tests execute quickly (pure reflection / IL inspection) and can run on every build or CI pipeline without
> additional infrastructure.

## Maintenance tips

- **New layers or projects** &rarr; update `Infrastructure/ArchitectureProvider.cs` so assemblies are loaded and
  namespaces stay accurate.
- **Exceptions to the rules** &rarr; prefer redesigning, but if a violation is intentional, document it and adjust the
  corresponding test with a targeted exclusion.
- **Performance** &rarr; ArchUnitNET caches metadata inside the process; avoid loading unnecessary assemblies to keep
  the suite fast.
