# SharedKernel.Mediator

Runtime support surface for `SharedKernel.Mediator`.

## Purpose

This project is the runtime-side companion to the abstractions package. It is the place for
concrete mediator services and lightweight runtime helpers that should remain available even when
source generation owns the main dispatch path.

## Current State

The project now contains the `AppMediator` runtime shell used by generated DI registration.
Generated dispatch and notification publication are still implemented in later slices.

## Dependencies

- [SharedKernel.Mediator.Abstractions](../SharedKernel.Mediator.Abstractions/README.md)
- `Microsoft.Extensions.DependencyInjection.Abstractions`

## See Also

- [SharedKernel.Mediator.SourceGenerator](../SharedKernel.Mediator.SourceGenerator/README.md)
- [tmp/mediator.md](../../../tmp/mediator.md)
