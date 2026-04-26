# SharedKernel.Mediator.CodeFixes

Code-fix project for `SharedKernel.Mediator`.

## Purpose

This Roslyn component is reserved for focused fixes that pair with the analyzer diagnostics in the
mediator toolchain.

## Current State

The project is scaffolded and packable, but code fixes have not been implemented yet.

## Planned Scope

- Add or repair missing mediator declarations
- Correct invalid registration shapes where a safe fix is possible
- Keep analyzer and code-fix rollout aligned by diagnostic family

## Dependencies

- `Microsoft.CodeAnalysis.Analyzers`
- `Microsoft.CodeAnalysis.CSharp`
- `Microsoft.CodeAnalysis.CSharp.Workspaces`

## See Also

- [SharedKernel.Mediator.Analyzers](../SharedKernel.Mediator.Analyzers/README.md)
- [tmp/dispatching-tooling-expansion-plan.md](../../../tmp/dispatching-tooling-expansion-plan.md)
