# Generated diagram roadmap

This roadmap defines which architecture diagrams are generated, curated, or deferred. The goal is to
keep generated outputs tied to stable repository sources and avoid generated diagrams that imply
metadata the code does not expose.

| Area | Source of truth | Output | Refresh and check path | Status |
| --- | --- | --- | --- | --- |
| System overview | Curated block in `docs/architecture/generated-diagrams.json`. | `system-overview.md` | `tools/SharedKernel.Documentation.Tool` through the existing diagram refresh path. | Generated from curated architecture data. |
| Project dependencies | `*.csproj` project references. | `boundaries-and-dependencies.md` | Same generator config. | Generated. |
| SharedKernel dependencies | `src/SharedKernel/**/*.csproj` project references. | `boundaries-and-dependencies.md` | Same generator config. | Generated. |
| AppHost runtime wiring | `src/ViajantesTurismo.AppHost/AppHostComposition.cs`. | `runtime-wiring-and-deployment.md` | Same generator config. | Generated. |
| CI main workflow | `.github/workflows/ci.yml` jobs and `needs`. | `ci-validation-flows.md` | Same generator config. | Generated. |
| Supplemental workflows | `.github/workflows/*.yml` workflow names. | `ci-validation-flows.md` | Same generator config. | Generated. |
| Minimal API endpoints | `MapGet`, `MapPost`, `MapPut`, `MapPatch`, and `MapDelete` declarations. | `generated-endpoint-route-map.md` | Same generator config; stale output fails the existing lint path. | Generated. |
| Events and messages | Integration-event contracts, event creation sites, consumer registrations, and handlers. | `generated-event-message-flow-map.md` | Same generator config; missing metadata is shown as missing. | Generated. |
| Machine-readable architecture facts | Stable fact markers plus configured C# switches, registrations, and required invocations. | `FLOWS.md` and the documentation index. | `SharedKernel.Documentation.Tool check` through `scripts/lint-all.sh`. | Source-conformance checked. |
| Mediator workflows | Existing mediator generator output and request/handler source. | No generated diagram yet. | Research only until request, handler, pipeline, notification, and stream metadata are explicit. | Deferred. |
| Code-generation strategy | Repository .NET tool model and local tool security policy. | This roadmap and `diagram-guidance.md`. | No new scripts, Python, npm, or transient tooling. | Use existing .NET generator. |

## Validation integration

The existing docs/scripts/spec lint path already checks generated diagram drift. Contributors can run
the underlying .NET tool directly when they want the exact stale file list:

```text
dotnet run --project tools/SharedKernel.Documentation.Tool -- generate --config docs/architecture/generated-diagrams.json --check
```

Refresh with the same command without `--check`, or use the repository's existing diagram refresh
wrapper when running the broader docs lint flow.

## Deferred mediator diagram research

Mediator diagrams stay deferred because the current source generator output does not yet expose a
stable, docs-oriented topology for request/handler, pipeline behavior, notification, and stream paths.
Generating those diagrams from naming conventions alone would make the documentation look more certain
than the source model.

## Code-generation research result

Use the existing .NET local documentation tool for generated Markdown. Do not add Python, npm, MSBuild
targets, or new scripts for this roadmap. Roslyn or mediator-generator changes should wait until the
runtime metadata is stable enough to produce source-backed diagrams without guessing.
