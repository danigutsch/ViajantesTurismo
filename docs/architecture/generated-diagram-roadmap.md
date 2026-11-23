# Generated diagram roadmap

This roadmap defines which architecture diagrams are generated, curated, or deferred. The goal is to
keep generated outputs tied to stable repository sources and avoid generated diagrams that imply
metadata the code does not expose.

| Area | Source of truth | Output | Refresh and check path | Status |
| --- | --- | --- | --- | --- |
| System overview | Curated block in `docs/architecture/generated-diagrams.json`. | `system-overview.md` | `dotnet docs` through the existing diagram refresh path. | Generated from curated architecture data. |
| Project dependencies | `*.csproj` project references. | `boundaries-and-dependencies.md` | Same generator config. | Generated. |
| AppHost runtime wiring | `src/ViajantesTurismo.AppHost/AppHostComposition.cs`. | `runtime-wiring-and-deployment.md` | Same generator config. | Generated. |
| CI main workflow | `.github/workflows/ci.yml` jobs and `needs`. | `ci-validation-flows.md` | Same generator config. | Generated. |
| Supplemental workflows | `.github/workflows/*.yml` workflow names. | `ci-validation-flows.md` | Same generator config. | Generated. |
| Minimal API endpoints | `MapGet`, `MapPost`, `MapPut`, `MapPatch`, and `MapDelete` declarations. | `generated-endpoint-route-map.md` | Same generator config; stale output fails the existing lint path. | Generated. |
| Events and messages | Integration-event contracts, event creation sites, consumer registrations, and handlers. | `generated-event-message-flow-map.md` | Same generator config; missing metadata is shown as missing. | Generated. |
| Data store ownership | [Architecture metadata](architecture-metadata.json). | `data-ownership.md` | Repository architecture CLI and architecture tests. | Generated. |
| Machine-readable architecture facts | Stable fact markers plus configured C# switches, registrations, and required invocations. | `FLOWS.md` and the documentation index. | `dotnet docs check` in the CI Lint job. | Source-conformance checked. |
| Mediator workflows | Versioned topology from the SharedKernel mediator generator. | Mediator workflow diagram after adoption. | Generate and verify with the matching SharedKernel documentation tool. | Awaiting a reviewed SharedKernel candidate. |
| Code-generation strategy | Repository .NET tool model and local tool security policy. | This roadmap and `diagram-guidance.md`. | No new scripts, Python, npm, or transient tooling. | Use existing .NET generator. |

## Validation integration

The existing docs/scripts/spec lint path already checks generated diagram drift. Contributors can run
the underlying .NET tool directly when they want the exact stale file list:

```text
dotnet docs verify --config docs/architecture/generated-diagrams.json
```

Refresh with `dotnet docs generate --config docs/architecture/generated-diagrams.json`.

## Mediator diagrams

SharedKernel owns the versioned mediator topology and its documentation renderer. Adopt the
application generator and documentation tool together through the
[reviewed candidate workflow](../sharedkernel/development.md).
When a reviewed candidate carries the versioned topology contract, generate from the real Admin
and Catalog compiler output, verify the diagram with the same candidate, and record the source
inputs and refresh command in the resulting document.

## Code-generation research result

Use the existing .NET local documentation tool for generated Markdown. Do not add Python, npm, MSBuild
targets, or new scripts for this roadmap. Roslyn or mediator-generator changes are out of scope unless the
runtime metadata can produce source-backed diagrams without guessing.
