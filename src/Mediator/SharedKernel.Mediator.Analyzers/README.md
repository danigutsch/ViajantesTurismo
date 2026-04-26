# SharedKernel.Mediator.Analyzers

Analyzer project for `SharedKernel.Mediator`.

## Purpose

This Roslyn component is reserved for declaration, usage, and packaging diagnostics that guide
consumers toward the supported mediator patterns.

## Current State

The project is scaffolded and packable, but analyzer rules have not been implemented yet.

## Planned Scope

- Missing or duplicate handler diagnostics
- Invalid registration or generation-boundary diagnostics
- Unsupported usage diagnostics for generated mediator flows

## Dependencies

- `Microsoft.CodeAnalysis.Analyzers`
- `Microsoft.CodeAnalysis.CSharp`

## See Also

- [SharedKernel.Mediator.CodeFixes](../SharedKernel.Mediator.CodeFixes/README.md)
- [tmp/dispatching-tooling-expansion-plan.md](../../../tmp/dispatching-tooling-expansion-plan.md)
