# ViajantesTurismo.Admin.BehaviorTests

Behavior-Driven Development tests using [Reqnroll](https://reqnroll.net/) - the .NET BDD framework.

## Purpose

Business scenario tests in stakeholder-readable language using Gherkin syntax (Given-When-Then format).

## Technology

- **Framework**: [Reqnroll](https://reqnroll.net/) (successor to SpecFlow)
- **Test Runner**: xUnit
- **Language**: Gherkin with C# step definitions

## Feature File Example

```gherkin
Feature: Booking Lifecycle
  
  Scenario: Confirming a pending booking
    Given a pending booking exists
    When the operator confirms the booking
    Then the booking status should be "Confirmed"

  Scenario: Cannot confirm a cancelled booking
    Given a cancelled booking exists
    When the operator tries to confirm the booking
    Then the operation should fail
```

## Reqnroll Best Practices

### Data Sharing Between Steps

Reqnroll provides several approaches for sharing data between step definitions. We use **Context Injection** as the
recommended best practice.

#### ✅ Recommended: Context Injection with Custom POCOs

**Create simple context classes** to hold shared state:

```csharp
public class BookingContext
{
    public Booking Booking { get; set; } = null!;
    public Result Result { get; set; }  // struct, no initializer
    public Action Action { get; set; } = null!;
}
```

**Inject via constructor** in binding classes:

```csharp
[Binding]
public class BookingSteps
{
    private readonly BookingContext _context;
    
    public BookingSteps(BookingContext context)
    {
        _context = context;
    }
    
    [Given(@"a pending booking exists")]
    public void GivenAPendingBookingExists()
    {
        _context.Booking = CreatePendingBooking();
    }
    
    [When(@"the operator confirms the booking")]
    public void WhenTheOperatorConfirmsTheBooking()
    {
        _context.Result = _context.Booking.Confirm();
    }
}
```

**Benefits:**

- ✅ Type-safe
- ✅ Testable
- ✅ Supports parallel execution
- ✅ Clear dependencies
- ✅ Automatic disposal if implementing `IDisposable`

#### Context Lifecycle Rules

1. **Scenario-scoped**: New instance created for each scenario
2. **Constructor injection**: Reqnroll resolves dependencies automatically
3. **Recursive resolution**: Injected classes can have their own dependencies
4. **Disposal**: Contexts implementing `IDisposable` are disposed after scenario execution

#### ❌ Avoid: ScenarioContext.Current

```csharp
// DON'T DO THIS - Obsolete and breaks parallel execution
[When(@"I do something")]
public void WhenIDoSomething()
{
    ScenarioContext.Current["key"] = value;  // ❌ Deprecated
}
```

**Use instead:**

```csharp
// DO THIS - Constructor injection
public class MySteps
{
    private readonly ScenarioContext _scenarioContext;
    
    public MySteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;  // ✅ Injected
    }
}
```

### Project Structure

```
ViajantesTurismo.Admin.BehaviorTests/
├── Context/              # Custom context classes (POCO)
│   ├── BookingContext.cs
│   ├── CustomerContext.cs
│   ├── TourContext.cs
│   └── PersonalInfoContext.cs
├── Features/             # Gherkin feature files
│   ├── BookingLifecycle.feature
│   ├── CustomerManagement.feature
│   └── TourManagement.feature
└── Steps/                # Step definition classes
    ├── BookingSteps.cs
    ├── CustomerSteps.cs
    └── TourSteps.cs
```

### Organizing Step Definitions

**Entity-based organization** - Group steps by domain entity:

```csharp
[Binding]
public class BookingLifecycleSteps
{
    private readonly BookingContext _bookingContext;
    private readonly TourContext _tourContext;
    
    // Multiple contexts can be injected
    public BookingLifecycleSteps(
        BookingContext bookingContext,
        TourContext tourContext)
    {
        _bookingContext = bookingContext;
        _tourContext = tourContext;
    }
    
    // Related steps in one class
    [Given(@"a confirmed booking for tour {string}")]
    [When(@"the booking is cancelled")]
    [Then(@"the booking status should be {string}")]
    public void Steps() { /* ... */ }
}
```

### Context Types

**Reference Types (Classes)**:

```csharp
public Booking Booking { get; set; } = null!;  // Use null!
```

**Value Types (Structs like Result<T>)**:

```csharp
public Result<Tour> Result { get; set; }  // No initializer for structs
```

### Advanced: Custom DI Containers

For complex scenarios, Reqnroll supports custom DI frameworks:

- Autofac: `Reqnroll.Autofac`
- Microsoft DI: `Reqnroll.Microsoft.Extensions.DependencyInjection`
- Windsor: `Reqnroll.Windsor`

## Running Tests

```powershell
# All behavior tests
dotnet test

# Specific feature
dotnet test --filter "FullyQualifiedName~BookingLifecycle"

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Resources

- [Reqnroll Documentation](https://docs.reqnroll.net/latest/)
- [Context Injection Guide](https://docs.reqnroll.net/latest/automation/context-injection.html)
- [Sharing Data Between Bindings](https://docs.reqnroll.net/latest/automation/sharing-data-between-bindings.html)
- [Reqnroll GitHub](https://github.com/reqnroll/Reqnroll)
