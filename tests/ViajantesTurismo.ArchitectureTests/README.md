# ViajantesTurismo.ArchitectureTests

Architecture regression tests for the Viajantes Turismo solution. These tests protect Clean Architecture boundaries, DDD
conventions, naming rules, and static architectural conventions in the domain layer.

This project also hosts lightweight drift guards for repository-level test architecture conventions when those rules are
best enforced as fast structural tests instead of prose-only guidance.

For Admin test taxonomy and seam guidance, treat `tests/README.md` as the canonical quick-reference and
`docs/TEST_GUIDELINES.md` as the deeper rule set.

## What we verify

- `LayerDependencyTests` — Ensures Domain never depends on Application/Infrastructure/API,
  Application stays adapter-free, Infrastructure is not coupled to transport, and SharedKernel module boundaries stay
  explicit. Abstraction projects stay separated from implementation and adapter dependencies. Domain assemblies
  reference no framework adapters such as System.Text.Json, EF Core, or ASP.NET Core.
- `DddConventionsTests` — Guards aggregate patterns: entities stay sealed and live under the
  domain namespaces; value objects remain immutable with no public setters; child entities change only through their
  aggregate root.
- `NamingConventionTests` — Enforces consistent naming (interfaces start with `I`, contracts end with `Dto`,
  architecture test classes end with `Tests`).
- `ErrorClassTests` — Ensures domain `*Errors` helper classes follow the required static-class convention.
- `AdminTestArchitectureGuardTests` — Guards the canonical Admin test-architecture documentation owner plus the
  approved hosted fixture/base-class seams for integration and system tests.
- `HostedTestFixtureOnDemandTests` — Pins the demand-driven start of `HostedTestFixture`: explicit-start containers, one gated
  and bounded start path per container, no start in the constructor or `InitializeAsync`, and no volume.
- `HostedTestFixtureSafetyTests` — Pins the collector's privacy processors, the shared-infrastructure marker of each stack,
  and the deletion of the work directory.
- `AppHostVolumeTests` — Keeps the AppHost's named volumes for the ClamAV definitions and the SeaweedFS objects.
- `SharedKernelToolCallerTests` — Parses every SharedKernel tool command line in workflows, composite actions,
  setup and repository scripts, Dev Container scripts, `opencode.json`, and workspace tasks with the pinned local tool and its
  `[diagram]` directive, which parses without running the command.
- `SharedKernelConsumerWorkflowTests` — Keeps `danigutsch/PandaBear.SharedKernel` action references at the pinned
  `libraryRevision`.
- `RequiredCheckContractTests` — Matches every required check in `docs/ci/governance.md` to a workflow job that keeps
  the `ci:rerun` ignored-label pattern.

## Running the tests

```powershell
# From the repository root
dotnet test --project tests/ViajantesTurismo.ArchitectureTests
```

> These tests execute quickly (reflection, IL inspection, and file reads) and can run on every build or CI pipeline.
> `SharedKernelToolCallerTests` needs the local tools restored with `dotnet tool restore`.

## Maintenance tips

- `AdminTestArchitectureGuardTests` is the canonical owner of the Admin test taxonomy guardrails;
  keep this README aligned with `tests/README.md` when those conventions change.
- **New layers or projects** &rarr; update `Infrastructure/ArchitectureProvider.cs` so assemblies are loaded and
  namespaces stay accurate.
- **New drift guards** &rarr; keep them narrow, tie them to a canonical document or convention, and tag them with the
  assembly-level `Scope=architecture`, `Surface=solution`, and `Area=shared` metadata.
- **Exceptions to the rules** &rarr; prefer redesigning, but if a violation is intentional, document it and adjust the
  corresponding test with a targeted exclusion.
- **Performance** &rarr; ArchUnitNET caches metadata inside the process; avoid loading unnecessary assemblies to keep
  the suite fast.
