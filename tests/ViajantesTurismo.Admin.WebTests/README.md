# ViajantesTurismo.Admin.WebTests

Unit tests for the Blazor Web components in the ViajantesTurismo Admin application.

## Overview

This project contains bUnit-based unit tests for the Razor components in `ViajantesTurismo.Admin.Web`.
These tests focus on component rendering, user interactions, and UI state management without requiring
a browser or full integration setup.

## Technology Stack

- **Testing Framework**: xUnit v3
- **Component Testing**: [bUnit](https://bunit.dev/) - Blazor component testing library
- **SDK**: Microsoft.NET.Sdk.Razor (enables `.razor` file support)
- **Target Framework**: .NET 10.0

## Test Philosophy

Following the [Microsoft Blazor testing guidelines](https://learn.microsoft.com/en-us/aspnet/core/blazor/test), we use:

- **Unit Testing** for components without complex JS interop
- **Fast, in-memory rendering** (milliseconds execution time)
- **Mocked dependencies** (API clients, services, IJSRuntime)
- **Semantic HTML comparison** using bUnit's `MarkupMatches`

## Test Structure

```text
tests/ViajantesTurismo.Admin.WebTests/
├── Components/
│   ├── Pages/           # Page component tests
│   └── Shared/          # Shared component tests
│       └── BookingStatusBadgeTests.cs
├── _Imports.razor       # Common using statements for .razor test files
├── ViajantesTurismo.Admin.WebTests.csproj
└── README.md
```

## Writing Tests

### Best Practices

Following bUnit best practices from [bUnit documentation](https://bunit.dev/docs/getting-started/writing-tests.html):

1. **Inherit from `BunitContext`** - Provides `Render<T>()` and other testing methods
2. **Use semantic assertions** - `MarkupMatches()` for HTML comparison
3. **Follow AAA pattern** - Arrange-Act-Assert (though `Arrange` often optional with bUnit)
4. **Test public behavior** - Focus on rendered output and user interactions, not implementation
5. **Use CSS selectors** - `cut.Find("span.badge")` for element queries

### Test Example

```csharp
public sealed class BookingStatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData(BookingStatusDto.Pending, "bg-warning")]
    [InlineData(BookingStatusDto.Confirmed, "bg-success")]
    public void Booking_Status_Badge_Should_Apply_Correct_Css_Class_For_Each_Status(
        BookingStatusDto status,
        string expectedCssClass)
    {
        // Act - Render component with parameter
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert - Verify CSS class is applied
        var badge = cut.Find("span.badge");
        Assert.Contains(expectedCssClass, badge.ClassList);
    }
}
```

### Naming Convention

Follows project standard: `Method_Context_Expected_Behavior` with underscores

```csharp
public void Booking_Status_Badge_Should_Apply_Correct_Css_Class_For_Each_Status()
public void Customer_Form_Should_Validate_Required_Fields()
public void Tours_List_Should_Display_All_Tours_When_Loaded()
```

### Common Testing Patterns

#### 1. **Rendering with Parameters**

```csharp
var cut = Render<MyComponent>(parameters => parameters
    .Add(p => p.Title, "Test Title")
    .Add(p => p.Count, 5));
```

#### 2. **Finding Elements**

```csharp
var button = cut.Find("button.btn-primary");
var list = cut.FindAll("li");
var icon = cut.Find("i.bi-check");
```

#### 3. **Triggering Events**

```csharp
var button = cut.Find("button");
button.Click();  // Synchronous click
await button.ClickAsync();  // Async click - waits for event handler but NOT for re-render

// Note: ClickAsync() awaits the event handler callback, not the render cycle.
// Use WaitForState() after clicking if you need to wait for DOM changes.
```

#### 4. **Mocking Services**

```csharp
// Inject mock service
Services.AddSingleton<ICustomersApiClient>(mockClient);

// Render component (will receive mocked service)
var cut = Render<CustomersList>();
```

#### 5. **Testing Component Output**

```csharp
// Check rendered markup
cut.MarkupMatches("<h1>Expected Title</h1>");

// Check text content
Assert.Equal("Welcome", cut.Find("h1").TextContent);

// Check class list
Assert.Contains("active", cut.Find("button").ClassList);
```

#### 6. **Testing All Enum Values**

```csharp
var allStatuses = Enum.GetValues<BookingStatusDto>();

foreach (var status in allStatuses)
{
    var cut = Render<StatusBadge>(p => p.Add(x => x.Status, status));
    Assert.NotNull(cut.Find(".badge"));
}
```

#### 7. **Testing Async Components with TaskCompletionSource**

When testing components that use `TaskCompletionSource` (like confirmation dialogs),
follow the pattern from
[bUnit's async state documentation](https://bunit.dev/docs/interaction/awaiting-async-state.html):

```csharp
[Fact]
public async Task Dialog_Should_Return_True_When_Confirmed()
{
    // Arrange - Start ShowAsync (returns Task<bool>) but don't await yet
    var cut = Render<ConfirmDialog>();
    var resultTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("Confirm action?"));

    // Wait for modal to appear in DOM
    cut.WaitForState(() => cut.FindAll(".modal").Count > 0);

    // Act - Click button (completes the TaskCompletionSource)
    var confirmButton = cut.Find(".modal-footer .btn-primary");
    await confirmButton.ClickAsync();

    // Assert - Now await the result
    var result = await resultTask;
    Assert.True(result);
}
```

**Key Points:**

- Use `InvokeAsync()` to call methods that trigger `StateHasChanged()`
- Call `WaitForState()` to wait for DOM changes after async operations
- For TaskCompletionSource patterns: start task → wait for render → trigger completion → await result

#### 8. **Waiting for Async State Changes**

Use `WaitForState()` when component state changes asynchronously:

```csharp
// Wait for element to appear
cut.WaitForState(() => cut.Find(".loading-spinner") != null);

// Wait for element to disappear
cut.WaitForState(() => cut.FindAll(".modal").Count == 0);

// Wait with custom timeout (default is 1 second)
cut.WaitForState(() => cut.Find(".result") != null, TimeSpan.FromSeconds(5));
```

**When to use:**

- After calling async component methods (`ShowAsync()`, `LoadDataAsync()`, etc.)
- When testing visibility changes triggered by button clicks
- Any time the component re-renders based on async operations

## Running Tests

```powershell
# Run all Web tests
dotnet test tests/ViajantesTurismo.Admin.WebTests

# Run specific test class (MTP filter syntax)
dotnet test --filter-method "*BookingStatusBadgeTests*"

# Run with coverage (MTP code coverage extension)
dotnet test tests/ViajantesTurismo.Admin.WebTests -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml

# Watch mode (re-run on file changes)
dotnet watch test --project tests/ViajantesTurismo.Admin.WebTests
```

## What to Test

### ✅ Test These Scenarios

- Component renders correctly with different parameter combinations
- CSS classes applied based on component state/props
- Icons, badges, and visual indicators display correctly
- Text content matches expected values
- User interactions (button clicks, form inputs) trigger correct behavior
- Conditional rendering (show/hide based on state)
- All enum values handled without errors
- Edge cases (null, empty, invalid values)

### ❌ Don't Test These

- Business logic (belongs in `Admin.UnitTests` for domain/application layer)
- API integration (belongs in `Admin.IntegrationTests`)
- Complex JS interop (consider E2E tests with Playwright if needed)
- CSS styling specifics (only test class presence, not appearance)
- Third-party component internals

## Coverage Goals

- **Target**: 80%+ code coverage for component logic
- **Focus**: Happy paths, error states, and boundary conditions
- **Skip**: Simple pass-through components with no logic

## Dependencies

Components may depend on:

- **API Clients** (`CustomersApiClient`, `ToursApiClient`, `BookingsApiClient`) - Mock in tests
- **Services** (DI services) - Register mocks with `Services.AddSingleton<T>()`
- **JSInterop** - Mock with bUnit's `FakeJSRuntime` or custom `IJSRuntime` mock
- **Navigation** - Use bUnit's `FakeNavigationManager`

## Related Documentation

- [bUnit Documentation](https://bunit.dev/)
- [Microsoft Blazor Testing Guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/test)
- [Project Test Guidelines](../../docs/TEST_GUIDELINES.md)
- [BDD Guide](../BDD_GUIDE.md)
- [Code Quality](../../docs/CODE_QUALITY.md)

## Examples

### Simple Component Test

```csharp
public void Home_Page_Should_Display_Welcome_Message()
{
    // Act
    var cut = Render<Home>();

    // Assert
    cut.MarkupMatches(@"
        <h1>
            <i class=""bi bi-bicycle""></i> ViajantesTurismo Admin Dashboard
        </h1>
    ");
}
```

### Component with Dependencies

```csharp
public void Customers_List_Should_Display_Customers_From_Api()
{
    // Arrange
    var mockApiClient = new Mock<ICustomersApiClient>();
    mockApiClient.Setup(x => x.GetCustomersAsync())
        .ReturnsAsync(new[] { new CustomerDto { Id = 1, Name = "John" } });

    Services.AddSingleton(mockApiClient.Object);

    // Act
    var cut = Render<CustomersList>();

    // Assert
    Assert.Contains("John", cut.Find("table").TextContent);
}
```

### Testing User Interactions

```csharp
public void Delete_Button_Should_Trigger_Confirmation_Dialog()
{
    // Arrange
    var dialogShown = false;
    var cut = Render<CustomerCard>(parameters => parameters
        .Add(p => p.OnDelete, () => dialogShown = true));

    // Act
    var deleteButton = cut.Find("button.btn-danger");
    deleteButton.Click();

    // Assert
    Assert.True(dialogShown);
}
```

### Testing Modal Dialogs with TaskCompletionSource

```csharp
public async Task Confirmation_Dialog_Should_Return_True_When_User_Confirms()
{
    // Arrange
    var cut = Render<ConfirmDialog>();

    // Start the async operation (don't await yet - it waits for user interaction)
    var resultTask = cut.InvokeAsync(() => cut.Instance.ShowAsync(
        "Are you sure you want to delete this customer?",
        title: "Confirm Delete",
        confirmText: "Yes, Delete",
        cancelText: "Cancel"
    ));

    // Wait for modal to render
    cut.WaitForState(() => cut.FindAll(".modal").Count > 0);

    // Act - User confirms
    var confirmButton = cut.Find(".modal-footer .btn-primary");
    await confirmButton.ClickAsync();

    // Assert - Check the result
    var result = await resultTask;
    Assert.True(result);

    // Verify modal is hidden
    cut.WaitForState(() => cut.FindAll(".modal").Count == 0);
}
```

## Troubleshooting

### Common Issues

1. **"Cannot find element"** - Check CSS selector, component may not have rendered the
   element yet. Use `WaitForState()` to wait for async rendering.
2. **"Ambiguous match"** - Use more specific selector (e.g., `button.btn-primary` instead of `button`)
3. **"Service not registered"** - Add mock service with `Services.AddSingleton<T>()`
4. **"Parameter required"** - All required `[Parameter]` properties must be set with `.Add()`
5. **"The current thread is not associated with the Dispatcher"** - Wrap component method
   calls in `InvokeAsync()`:

   ```csharp
   // ❌ Wrong - calls StateHasChanged() outside dispatcher
   cut.Instance.ShowAsync("Message");

   // ✅ Correct - executes on component's dispatcher
   cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
   ```

6. **Tests hang indefinitely** - Don't await `ShowAsync()` or similar methods that use
   `TaskCompletionSource` until AFTER triggering the completion action:

   ```csharp
   // ❌ Wrong - hangs waiting for user interaction that never comes
   await cut.Instance.ShowAsync("Confirm?");

   // ✅ Correct - start task, interact with UI, then await
   var resultTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("Confirm?"));
   cut.WaitForState(() => cut.Find(".modal") != null);
   await cut.Find("button.confirm").ClickAsync();
   var result = await resultTask;  // Now it completes
   ```

### JSInterop Configuration for QuickGrid

Components using `QuickGrid` (BookingsList, Tours/Index, Customers/Index) require JSInterop configuration:

```csharp
public class BookingsListTests : BunitContext
{
    public BookingsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
```

**Why?** QuickGrid imports a JavaScript module
(`./_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js`).
Loose mode returns default values for unconfigured JS calls, allowing tests to focus on HTML output without JS setup.

**Components using QuickGrid:**

- `BookingsList.razor` (shared)
- `Tours/Index.razor`
- `Customers/Index.razor`
- `Bookings/Index.razor` (uses BookingsList)

**Components using regular `<table>`:**

- `PaymentsList.razor` (no JSInterop needed)

### Tips

- Use `cut.Markup` to inspect rendered HTML during debugging
- Use `cut.SaveSnapshot()` to save HTML to file for inspection
- Check bUnit's [troubleshooting guide](https://bunit.dev/docs/misc-test-tips.html)
- **Debugger attached?** bUnit automatically disables timeouts when `Debugger.IsAttached` is true

## Contributing

When adding new component tests:

1. Create test file in matching folder structure (`Components/Pages/` or `Components/Shared/`)
2. Inherit from `BunitContext`
3. Follow naming conventions (`ComponentName_Context_Expected_Behavior`)
4. Use Theory tests for multiple similar scenarios
5. Test all enum values when components accept enum parameters
6. Test error/edge cases (null, invalid, empty)
7. Keep tests fast and focused on one behavior each

---

**For more information, see the main [TEST_GUIDELINES.md](../../docs/TEST_GUIDELINES.md).**
