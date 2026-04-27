# SharedKernel.Mediator.CodeFixes

Code-fix project for `SharedKernel.Mediator`.

## Purpose

This Roslyn component is reserved for focused fixes that pair with the analyzer diagnostics in the
mediator toolchain.

## Current State

The project now provides safe fixes for the currently implemented generator diagnostics:

- `SKMED001` can generate a missing handler file when no handler candidate exists.
- `SKMED003` can repair the explicit-interface-only handler shape by adding a public forwarding
  `Handle(...)` method.

Unsafe or cross-assembly diagnostics remain intentionally unfixed until a safe local repair exists.

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
