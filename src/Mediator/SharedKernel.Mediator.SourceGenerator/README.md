# SharedKernel.Mediator.SourceGenerator

Incremental source generator for `SharedKernel.Mediator`.

## Purpose

This Roslyn component discovers mediator contracts in the owning project and emits generated source
for the mediator toolchain.

## Current Scope

- Builds the initial discovery model from `IRequest<TResponse>`, `IRequestHandler<,>`, and
  `IPipelineBehavior<,>`
- Emits the first generated discovery report
- Packs as an analyzer-style assembly under `analyzers/dotnet/cs`

## Dependencies

- `Microsoft.CodeAnalysis.Analyzers`
- `Microsoft.CodeAnalysis.CSharp`

## See Also

- [SharedKernel.Mediator.Abstractions](../SharedKernel.Mediator.Abstractions/README.md)
- [SharedKernel.Mediator.GeneratorTests](../../../tests/SharedKernel.Mediator.GeneratorTests/README.md)
