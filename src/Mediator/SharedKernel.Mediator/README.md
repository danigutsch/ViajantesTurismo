# SharedKernel.Mediator

Runtime support surface for `SharedKernel.Mediator`.

## Purpose

This project is the runtime-side companion to the abstractions package. It is the place for
concrete mediator services and lightweight runtime helpers that should remain available even when
source generation owns the main dispatch path.

## Current State

The consumer-side source generator now emits the `AppMediator` shell used by generated DI
registration.
This runtime package stays focused on shared abstractions-facing runtime support that can remain
available while source generation owns mediator composition and request dispatch.

## Dependencies

- [SharedKernel.Mediator.Abstractions](../SharedKernel.Mediator.Abstractions/README.md)
- `Microsoft.Extensions.DependencyInjection.Abstractions`

## See Also

- [SharedKernel.Mediator.SourceGenerator](../SharedKernel.Mediator.SourceGenerator/README.md)
- [tmp/mediator.md](../../../tmp/mediator.md)
