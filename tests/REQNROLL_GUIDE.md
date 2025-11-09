# Reqnroll Technical Guide

Technical guide for implementing BDD tests with Reqnroll in ViajantesTurismo projects.

## Table of Contents

- [Context Injection](#context-injection)
- [Project Structure](#project-structure)
- [Step Definition Patterns](#step-definition-patterns)
- [Running Tests](#running-tests)
- [Configuration](#configuration)

---

## Context Injection

### Custom POCOs

Create simple context classes to hold shared state:

```csharp
public class BookingContext
{
    public Booking Booking { get; set; } = null!;
    public Result Result { get; set; }  // struct, no initializer
    public Action Action { get; set; } = null!;
}
```

Inject via constructor:

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
}
```

**Benefits:** Type-safe, testable, supports parallel execution, automatic disposal.

### Context Lifecycle

- **Scenario-scoped**: New instance per scenario
- **Constructor injection**: Automatic dependency resolution
- **Disposal**: `IDisposable` contexts disposed after scenario

### Context Property Types

**Reference Types (Classes)**:

```csharp
public Booking Booking { get; set; } = null!;  // Use null!
```

**Value Types (Structs like Result<\T>)**:

```csharp
public Result<Tour> Result { get; set; }  // No initializer for structs
```

### ❌ Avoid: ScenarioContext.Current

```csharp
// ❌ DON'T - Deprecated, breaks parallel execution
ScenarioContext.Current["key"] = value;

// ✅ DO - Constructor injection
public MySteps(ScenarioContext scenarioContext)
{
    _scenarioContext = scenarioContext;
}
```

---

## Project Structure

```text
ViajantesTurismo.Admin.BehaviorTests/
├── Context/              # Custom context classes (POCO)
│   ├── BookingContext.cs
│   └── TourContext.cs
├── Features/             # Gherkin feature files
│   ├── Booking/
│   └── Tour/
├── Steps/                # Step definition classes
│   ├── BookingSteps.cs
│   └── TourSteps.cs
└── reqnroll.json
```

---

## Step Definition Patterns

### Entity-Based Organization

Group steps by domain entity, inject multiple contexts as needed:

```csharp
[Binding]
public class BookingSteps
{
    private readonly BookingContext _bookingContext;
    private readonly TourContext _tourContext;
    
    public BookingSteps(
        BookingContext bookingContext,
        TourContext tourContext)
    {
        _bookingContext = bookingContext;
        _tourContext = tourContext;
    }
    
    [Given(@"a pending booking exists")]
    public void GivenAPendingBookingExists()
    {
        _bookingContext.Booking = CreatePendingBooking();
    }
}
```

### Parameterized Steps

```csharp
// ✅ Reusable
[Given(@"a booking with status {string} exists")]
public void GivenABookingWithStatusExists(string status)
{
    _context.Booking = CreateBookingWithStatus(status);
}
```

### FluentAssertions

```csharp
[Then(@"the booking should be confirmed")]
public void ThenTheBookingShouldBeConfirmed()
{
    _bookingContext.Result.IsSuccess.Should().BeTrue();
    _bookingContext.Result.Value.Status.Should().Be(BookingStatus.Confirmed);
}
```

---

## Running Tests

```powershell
# All tests
dotnet test

# Specific feature
dotnet test --filter "FullyQualifiedName~BookingLifecycle"

# By tag
dotnet test --filter "TestCategory=critical"

# Disable parallelization
dotnet test -- xUnit.parallelizeTestCollections=false
```

---

## Configuration

### reqnroll.json

```json
{
  "$schema": "https://schemas.reqnroll.net/reqnroll-config-latest.json",
  "language": {
    "feature": "en-US"
  },
  "formatters": {
    "html": {
      "outputFilePath": "TestResults/living_documentation.html"
    }
  }
}
```

Generate HTML report: `dotnet test` then `start TestResults/living_documentation.html`

### Cucumber Messages (Optional)

```json
{
  "formatters": {
    "message": {
      "outputFilePath": "TestResults/cucumber_messages.ndjson"
    }
  }
}
```

### xunit.runner.json (Optional)

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeTestCollections": true,
  "maxParallelThreads": -1
}
```

---

## Advanced Topics

### Hooks

```csharp
[Binding]
public class Hooks
{
    [BeforeScenario]
    public void BeforeScenario() { }
    
    [AfterScenario]
    public void AfterScenario() { }
    
    [BeforeScenario("@database")]
    public void BeforeDatabaseScenario() { }
}
```

### Custom DI Containers

- **Autofac**: `Reqnroll.Autofac`
- **Microsoft DI**: `Reqnroll.Microsoft.Extensions.DependencyInjection`

---

## Resources

- [Reqnroll Documentation](https://docs.reqnroll.net/latest/)
- [Context Injection Guide](https://docs.reqnroll.net/latest/automation/context-injection.html)
- [Sharing Data Between Bindings](https://docs.reqnroll.net/latest/automation/sharing-data-between-bindings.html)
- [Reqnroll GitHub](https://github.com/reqnroll/Reqnroll)
