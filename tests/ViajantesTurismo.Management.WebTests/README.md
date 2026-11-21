# ViajantesTurismo.Management.WebTests

[bUnit](https://bunit.dev/) component tests for the Blazor management web UI.

## Scope

In-memory rendering tests for Razor components — fast, no browser required.

- Component rendering with different parameters
- CSS classes and visual indicators per state
- User interactions (clicks, form inputs)
- Conditional rendering and edge cases (null, empty, all enum values)

Business logic belongs in `Admin.UnitTests`; full-stack flows in `Admin.SystemTests`.

## Reproduce the Management document download

From the repository root, use the pinned SDK and [SharedKernel packages](../../docs/sharedkernel/development.md).
The fixture creates a fresh in-memory endpoint host, synthetic role, and downstream client for each attempt.

```sh
dotnet test --project tests/ViajantesTurismo.Management.WebTests/ViajantesTurismo.Management.WebTests.csproj \
  -c Release -- --filter-class '*DocumentProxyReproductionTests'
```

Each outcome runs twice. The test Admin gets the synthetic HTML attachment with private delivery headers.
The signed-in test Operator gets `403` before any downstream download. Requests use the application route
and authorization policy through TestServer with generated document IDs and an in-memory downstream response.

Set `VIAJANTESTURISMO_REPRODUCTION_MANIFESTS=1` for the same command to keep four validated manifests under
`TestResults/reproduction/` for `management.document-download`. The approved outcomes are
`attachment-delivered` and `forbidden`. The manifests retain the observed server trace ID with the approved
outcome and attempt metadata. Request and response headers, filenames, document IDs, and bodies stay out of
the manifests. [Reproduction artifact contract](../../docs/testing/reproduction-contract.md)

## Project-Specific Notes

### QuickGrid JSInterop

Components using `QuickGrid` require loose JSInterop mode:

```csharp
public class BookingsListTests : BunitContext
{
    public BookingsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
```

**Affected components**: `BookingsList`, `Tours/Index`, `Customers/Index`, `Bookings/Index`.

### TaskCompletionSource Dialogs

For components using `TaskCompletionSource` (e.g., confirmation dialogs), start the task, wait for the
DOM, interact, then await:

```csharp
var resultTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("Confirm?"));
cut.WaitForState(() => cut.FindAll(".modal").Count > 0);
await cut.Find(".modal-footer .btn-primary").ClickAsync();
var result = await resultTask;
```

Don't `await ShowAsync(...)` directly — it blocks waiting for user interaction that never comes.

### Common Pitfalls

- **"Cannot find element"** → Component may not have rendered yet. Use `WaitForState()`.
- **"Dispatcher" error** → Wrap calls in `cut.InvokeAsync(() => ...)`.
- **Tests hang** → You're awaiting a `TaskCompletionSource` before triggering the completion action.

## See Also

- [tests/README.md](../README.md) — Running tests, coverage, conventions
- [bUnit Documentation](https://bunit.dev/)
