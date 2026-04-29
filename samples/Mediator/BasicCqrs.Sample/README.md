# BasicCqrs.Sample

Single console sample for `SharedKernel.Mediator`.

## Purpose

This sample demonstrates the smallest end-to-end CQRS flow currently supported by
the package set:

- a query and handler
- a command with a response and handler
- generated `AppMediator` dispatch
- generated `AddSharedKernelMediator()` DI registration

## Run

```bash
dotnet run --project samples/Mediator/BasicCqrs.Sample/BasicCqrs.Sample.csproj
```

## Notes

The sample intentionally stays on request/response dispatch only. Notification
publish and richer sample slices remain future work in `tmp/mediator.md`.
