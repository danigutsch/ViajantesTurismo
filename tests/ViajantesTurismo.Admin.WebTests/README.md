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
button.Click();  // Triggers onclick handler
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

## Running Tests

```powershell
# Run all Web tests
dotnet test tests/ViajantesTurismo.Admin.WebTests

# Run specific test class
dotnet test --filter "FullyQualifiedName~BookingStatusBadgeTests"

# Run with coverage
dotnet test tests/ViajantesTurismo.Admin.WebTests --collect:"XPlat Code Coverage" --settings coverlet.runsettings

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

## Troubleshooting

### Common Issues

1. **"Cannot find element"** - Check CSS selector, component may not have rendered the element yet
2. **"Ambiguous match"** - Use more specific selector (e.g., `button.btn-primary` instead of `button`)
3. **"Service not registered"** - Add mock service with `Services.AddSingleton<T>()`
4. **"Parameter required"** - All required `[Parameter]` properties must be set with `.Add()`

### Tips

- Use `cut.Markup` to inspect rendered HTML during debugging
- Use `cut.SaveSnapshot()` to save HTML to file for inspection
- Check bUnit's [troubleshooting guide](https://bunit.dev/docs/misc-test-tips.html)

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
