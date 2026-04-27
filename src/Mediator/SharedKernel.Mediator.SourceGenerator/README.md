# SharedKernel.Mediator.SourceGenerator

Incremental source generator for `SharedKernel.Mediator`.

## Purpose

This Roslyn component discovers mediator contracts in the owning project and emits generated source
for the mediator toolchain.

## Current Scope

- Builds the discovery model from request, handler, pipeline, notification, stream, and module
  contracts
- Emits the readable discovery report
- Emits generated DI registration for handlers, pipelines, notifications, and stream handlers
- Emits the generated `AppMediator` shell into the consumer compilation
- Reports generated DI diagnostics for inaccessible and duplicate registrations
- Packs as an analyzer-style assembly under `analyzers/dotnet/cs`

## Dependencies

- `Microsoft.CodeAnalysis.Analyzers`
- `Microsoft.CodeAnalysis.CSharp`

## See Also

- [SharedKernel.Mediator.Abstractions](../SharedKernel.Mediator.Abstractions/README.md)
- [SharedKernel.Mediator.GeneratorTests](../../../tests/SharedKernel.Mediator.GeneratorTests/README.md)
