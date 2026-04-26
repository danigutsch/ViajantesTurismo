# SharedKernel.Mediator.Abstractions

Core contracts for the `SharedKernel.Mediator` stack.

## Purpose

This project defines the public request, handler, notification, stream, pipeline, and mediator
abstractions that the runtime, generator, analyzers, and tests build on.

## Current Contents

- Request contracts: `IRequest<TResponse>`, `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`
- Handler contracts: `IRequestHandler<,>`, command/query handler variants
- Notification and stream contracts
- Pipeline contracts and ordering metadata
- Mediator-facing contracts: `ISender`, `IPublisher`, `IMediator`
- `Unit` and `MediatorModuleAttribute`

## Dependencies

None.

## See Also

- [SharedKernel.Mediator](../SharedKernel.Mediator/README.md)
- [SharedKernel.Mediator.SourceGenerator](../SharedKernel.Mediator.SourceGenerator/README.md)
