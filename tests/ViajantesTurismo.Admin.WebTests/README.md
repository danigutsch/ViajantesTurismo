# ViajantesTurismo.Admin.WebTests

[bUnit](https://bunit.dev/) component tests for the Blazor Admin web UI.

## Scope

In-memory rendering tests for Razor components — fast, no browser required.

- Component rendering with different parameters
- CSS classes and visual indicators per state
- User interactions (clicks, form inputs)
- Conditional rendering and edge cases (null, empty, all enum values)

Business logic belongs in `Admin.UnitTests`; full-stack flows in `Admin.E2ETests`.

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
