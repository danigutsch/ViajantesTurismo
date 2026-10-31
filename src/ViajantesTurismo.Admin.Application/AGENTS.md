# AGENTS.md

Instructions for files under `src/ViajantesTurismo.Admin.Application/`.

## Scope and precedence

- Applies to all files under `src/ViajantesTurismo.Admin.Application/`.
- If instructions conflict with higher-level `AGENTS.md` files, follow this file for Application-layer work.

## Command and handler structure

- Use one command record and one handler class per use case in a feature folder.
- Commands are plain `sealed record` types.
- Handlers are `sealed class` types using primary constructor dependency injection.
- Bookings commands (spike) implement `ICommand<Result>` / `ICommand<Result<T>>` and handlers implement
  `ICommandHandler<TCommand, TResponse>` (`ValueTask<TResponse> Handle`) from `PandaBear.Mediator`; endpoints dispatch through `ISender`.
  Handlers must be public; the assembly carries `[assembly: MediatorModule]` and the host calls `AddSharedKernelMediator()`.
- Pipeline behaviors live in `Bookings/Pipeline` (booking-scoped) or `Pipeline` (all commands), must be open generic over exactly `<TRequest, TResponse>`,
  and declare `[PipelineOrder(PipelineStage.X)]`.
- Do not use MediatR. Other features keep plain handler classes registered with scoped lifetime in Application dependency injection.

## Handle method conventions

Each `Handle` method should:

1. Begin with `ArgumentNullException.ThrowIfNull(command);`
2. Load required aggregates and return typed not-found errors when missing
3. Execute domain operations and check `IsSuccess`
4. Persist through `IUnitOfWork.SaveEntities(ct)` after successful domain work
5. Return `Result` or `Result<T>` for expected failures (no exception-based business flow)

## Error propagation

- Propagate typed failures with `ConvertError<TFrom, TTo>()`.
- Do not rewrap domain errors when conversion utilities already model the mapping.

## Mappers

- Keep static mappers in `Application/Mappings/`.
- Map DTOs to domain value objects inside handlers before domain calls.
- Keep read-side domain-to-DTO mapping in query services.
- Use exhaustive `switch` expressions with explicit default failure paths.

## Interfaces and return types

- Keep use-case orchestration in Application, not domain persistence details.
- Return IDs or status-oriented `Result` values from commands.
- Do not return domain entities across layer boundaries.

## Async and cancellation

- Do not append `Async` suffixes.
- Accept `CancellationToken ct` as the last parameter in async methods.

## References

- `AGENTS.md` (repository root)
- `docs/CODING_GUIDELINES.md`
- [Results package guide](https://github.com/danigutsch/PandaBear.SharedKernel/blob/f5975b60601e245bda6bb8e45559d8ce3a24a8e0/src/SharedKernel/SharedKernel.Results/README.md)
