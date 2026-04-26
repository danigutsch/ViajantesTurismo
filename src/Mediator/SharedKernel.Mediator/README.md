# SharedKernel.Mediator

Runtime support surface for `SharedKernel.Mediator`.

## Purpose

This project is the runtime-side companion to the abstractions package. It is the place for
concrete mediator services and lightweight runtime helpers that should remain available even when
source generation owns the main dispatch path.

## Current State

The project currently references `SharedKernel.Mediator.Abstractions` and acts as the runtime
package placeholder while the stack is being built from scratch.

## Dependencies

- [SharedKernel.Mediator.Abstractions](../SharedKernel.Mediator.Abstractions/README.md)

## See Also

- [SharedKernel.Mediator.SourceGenerator](../SharedKernel.Mediator.SourceGenerator/README.md)
- [tmp/mediator.md](../../../tmp/mediator.md)
