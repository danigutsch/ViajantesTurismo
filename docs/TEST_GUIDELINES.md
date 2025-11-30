# Test Guidelines

This document outlines the testing standards and best practices for the ViajantesTurismo project.

## Testing Strategy

Our strategy follows a test pyramid approach:

- **Unit Tests** (fast, in-memory) — Majority of tests
- **Integration Tests** (database, HTTP) — Key API scenarios
- **Behavior Tests** (BDD/Gherkin) — Business-critical scenarios
- **End-to-End/Acceptance** (future) — Thin layer of critical user journeys

**Philosophy:** Fast feedback through comprehensive unit tests, confidence through integration tests, and business
alignment through BDD scenarios.

## Test Project Structure

### Current Projects

- **`ViajantesTurismo.Common.UnitTests`** — Shared utilities: building blocks, Result pattern, sanitizers, extensions.
  Fast, no dependencies.
- **`ViajantesTurismo.Admin.UnitTests`** — Domain logic: entities, aggregates, mappers, business rules. Fast, in-memory
  only.
- **`ViajantesTurismo.Admin.WebTests`** — Blazor Web components: Razor component rendering, UI state, user interactions.
  Uses bUnit for fast, in-memory component testing.
- **`ViajantesTurismo.Admin.IntegrationTests`** — API endpoints with real PostgreSQL database. Slower, tests complete
  request-response cycle.
- **`ViajantesTurismo.Admin.BehaviorTests`** — Behavior-driven tests using Gherkin/SpecFlow for backend domain
  scenarios, written in business language.

### Suggested Structure (Future Evolution)

- `tests/Admin.Domain.Tests/` — Entities, value objects, aggregates (unit tests)
- `tests/Admin.Application.Tests/` — Use cases, policies, application services (unit tests)
- `tests/Admin.Web.Tests/` — Blazor components (bUnit tests) ✅ *Already exists as `Admin.WebTests`*
- `tests/Admin.Acceptance.Tests/` — BDD scenarios calling the Application layer
- `tests/Contract.Tests/` — Consumer-driven contract tests (if services integrate with others)
- `tests/specs/` — Shared `.feature` files (if not colocated in acceptance tests)

## Naming Conventions

### Unit and Integration Tests

**Pattern:** `Method_Name_Context_Description_Expected_Behavior`

Use underscores to separate all words for improved readability (natural language description).

```csharp
// Good
public void Map_To_Currency_Dto_Should_Map_All_Valid_Values()
public void Create_Tour_With_Valid_Data_Should_Return_Created_Status()

// Bad
public void MapToCurrencyDto_ShouldMapAllValidValues()  // Inconsistent
public void TestCreateTour()  // Not descriptive
```

### Feature Files

**Pattern:** `<aggregate>-<capability>.feature`

```text
tour-creation.feature
booking-capacity-management.feature
payment-recording.feature
```

### Behavior Test Step Definitions

Follow Gherkin/SpecFlow conventions with descriptive method names:

```csharp
[Given(@"a tour exists with minimum (.*) and maximum (.*) customers")]
public void Given_A_Tour_Exists_With_Minimum_And_Maximum_Customers(int min, int max)
```

## Test Structure (AAA Pattern)

Follow the **Arrange-Act-Assert** pattern with explicit comments:

```csharp
[Fact]
public void Invalid_Amount_Should_Return_Invalid_Result()
{
    // Arrange
    const decimal invalidAmount = 0m;

    // Act
    var result = PaymentErrors.InvalidAmount(invalidAmount);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Payment amount must be greater than zero", result.ErrorDetails.Detail);
}
```

Benefits: Standard pattern, clear flow, helps identify tests doing too much.

## Blazor Component Testing (bUnit)

For testing Razor components in `ViajantesTurismo.Admin.Web`, we use bUnit following Microsoft's recommended approach
for components without complex JS interop.

### Test Class Pattern

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
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains(expectedCssClass, badge.ClassList);
    }
}
```

### Key Principles

- **Inherit from `BunitContext`** - Provides `Render<T>()` method and test context
- **Use `Render<T>()`** - Renders component with parameters
- **CSS Selectors** - Use `cut.Find()` to query rendered elements
- **ClassList Assertions** - Check individual CSS classes, not combined strings
- **No explicit Arrange** - Component rendering is the arrangement in bUnit tests
- **Semantic Comparison** - Use `cut.MarkupMatches()` for HTML comparison

See [ViajantesTurismo.Admin.WebTests README](../tests/ViajantesTurismo.Admin.WebTests/README.md) for detailed examples
and patterns.

## Test Patterns

### Theory Tests

Use `[Theory]` with `[InlineData]` for multiple inputs:

```csharp
[Theory]
[InlineData(Currency.Real, CurrencyDto.Real)]
[InlineData(Currency.Euro, CurrencyDto.Euro)]
public void Map_To_Currency_Dto_Should_Map_All_Valid_Values(Currency domain, CurrencyDto expected)
{
    var result = TourMapper.MapToCurrencyDto(domain);
    Assert.Equal(expected, result);
}
```

### Enum Coverage Tests

Ensure all enum values are handled:

```csharp
[Fact]
public void Map_To_Currency_Dto_Should_Cover_All_Enum_Values()
{
    var allDomainValues = Enum.GetValues<Currency>();
    foreach (var domainValue in allDomainValues)
    {
        var mappedEnum = TourMapper.MapToCurrencyDto(domainValue);
        Assert.True(Enum.IsDefined(mappedEnum));
    }
}
```

### Error Handling Tests

Always test error conditions:

```csharp
[Fact]
public void Map_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
{
    const Currency invalidValue = (Currency)999;
    var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        TourMapper.MapToCurrencyDto(invalidValue));
    Assert.Contains("Invalid currency value", exception.Message);
}
```

## Test Data & Fixtures

### Approach by Test Type

- **Unit Tests:** Direct instantiation with factory methods (e.g., `Tour.Create()`)
- **Integration Tests:** Seeded database using `Seeder` class with known data
- **Behavior Tests:** Context objects to share state between steps

### Best Practices

- **Prefer object mothers/builders** for complex aggregates and value objects
- **Keep test data in code** (avoid external files unless absolutely necessary)
- **Avoid shared mutable state** between tests
- **Use ephemeral resources** for integration tests (e.g., Testcontainers, in-memory databases)
- **Use FakeTimeProvider** instead of `DateTime.UtcNow` for deterministic time-based tests

**Example Object Mother:**

```csharp
public static class TestHelpers
{
    public static Customer CreateTestCustomerWithNames(
        string firstName = "John",
        string lastName = "Doe",
        DateTime? birthDate = null)
    {
        return Customer.Create(
            firstName,
            lastName,
            birthDate ?? new DateTime(1990, 1, 1),
            "john.doe@example.com",
            "+1234567890",
            /* ... */).Value;
    }
}
```

## Assertions Best Practices

- Use specific assertions (`Assert.Equal`, `Assert.Contains`) over generic `Assert.True`
- Multiple related assertions in one test are acceptable
- Avoid conditional logic in assertions
- Be specific but not fragile (e.g., check message contains key phrase, not exact text)

## Testing Best Practices

### FIRST Principles

- **Fast**: Unit tests < 100ms, Integration ~24s (includes database setup), Behavior < 10s (frontend E2E tests when
  added will be slower)
- **Independent**: Tests run in any order without shared state
- **Repeatable**: Same result every time (use `FakeTimeProvider` instead of `DateTime.UtcNow`)
- **Self-Validating**: Clear pass/fail without manual inspection
- **Timely**: Written with production code

### Guidelines

- One logical assertion per test (test one behavior)
- Test public behavior, not implementation details
- Extract common setup to helper methods (e.g., `TestHelpers.CreateTestCustomerWithNames()`)
- Avoid `Thread.Sleep()` and unnecessary waits
- No test execution order dependencies

## Mapper Testing Pattern

All mapper methods need three tests:

1. **Valid Values** (Theory with InlineData)
2. **Coverage** (Iterate all enum values)
3. **Error Handling** (Invalid value throws ArgumentOutOfRangeException)

## Integration Test Essentials

```csharp
[Collection("Api collection")]
public sealed class ToursApiTests : IDisposable
{
    // Assert status codes
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    // Assert response content
    var tour = await response.Content.ReadFromJsonAsync<GetTourDto>();
    Assert.NotNull(tour);

    // Assert database state
    var dbTour = _dbContext.Tours.FirstOrDefault(t => t.Id == tourId);
    Assert.NotNull(dbTour);
}
```

## Behavior Test Patterns

### Context Class Guidelines

Context classes (POCOs) share state between step definitions via Reqnroll's dependency injection. Follow these rules:

**Result Properties:**

- ❌ Never use `object` for result properties (requires casting, loses type safety)
- ✅ Use specific `Result<T>?` properties for each operation (e.g., `Result<Tour>? CreationResult`)
- ✅ Nullable result properties are acceptable for optional operations
- ✅ Name results by operation: `CreationResult`, `UpdateResult`, `DeleteResult`, etc.

**Input Properties:**

- Use `required` modifier for properties that must be set before steps run
- Initialize collections via field initializers: `ICollection<string> Items { get; } = [];`

**Dependencies:**

- Initialize Fakes and test doubles via field initializers
- Use expression-bodied properties for command handlers: `Handler => new(Store, UnitOfWork);`

**Example (Good Pattern):**

```csharp
public sealed class TourContext
{
    // Input data
    public required string Identifier { get; set; }
    public required DateTime StartDate { get; set; }

    // Typed result properties (nullable for optional operations)
    public Result<Tour>? CreationResult { get; set; }
    public Result<TourCapacity>? CapacityUpdateResult { get; set; }

    // Dependencies
    public FakeTourStore TourStore { get; } = new();
    public CreateTourCommandHandler Handler => new(TourStore, UnitOfWork);
}
```

**Anti-Pattern (Avoid):**

```csharp
// ❌ BAD: Generic object requires casting everywhere
public required object Result { get; set; }

// ❌ BAD: Leads to if/else type-checking in steps
if (context.Result is Result<Tour> tourResult) { ... }
else if (context.Result is Result<TourCapacity> capacityResult) { ... }
```

### Step Definition Guidelines

**When Steps:**

- Always execute the action and store the result
- Never check success/failure inside `When` steps
- Store results in typed context properties

**Then Steps:**

- Check specific typed result properties
- Avoid `if` statements - use separate steps for different outcomes

**Given Steps:**

- Keep setup logic simple and readable
- Extract complex loops/switches to helper methods in `EntityBuilders` or dedicated helpers

## BDD/Gherkin Best Practices

### Writing Features

**Principles:**

- Use **declarative** steps that speak the domain language
- Avoid UI or technical implementation details
- Keep scenarios short and focused
- Prefer `Scenario Outline` with `Examples` for tabular variations
- Group scenarios with `Rule:` blocks per invariant
- Use `Background` sparingly (only for true prerequisites)

**Example:**

```gherkin
@BC:TourManagement @Agg:Tour @ADR:20251108-domain-validation-factory-methods
Feature: Tour Capacity Management

  Rule: Tour cannot accept bookings beyond maximum capacity

    Scenario Outline: Booking when at capacity
      Given a tour exists with minimum <min> and maximum <max> customers
      And the tour has <current> confirmed bookings
      When I attempt to add a new booking
      Then the booking should <result>

    Examples:
      | min | max | current | result                    |
      | 4   | 10  | 9       | succeed                   |
      | 4   | 10  | 10      | fail with "fully booked"  |
```

### Tagging Conventions

Use tags to link scenarios to architectural concepts:

## BDD Coverage & CI

### Coverage Goals

- **Unit Tests:** 80%+ code coverage
- **Integration Tests:** All API endpoints
- **Behavior Tests:** Business-critical scenarios

Focus on happy paths, error paths, and boundary conditions. Don't obsess over 100% coverage or testing simple
getters/setters.

### Coverage Configuration

Use Coverlet settings from `coverlet.runsettings` for consistent reporting:

```powershell
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### CI/CD Integration (Future)

When CI is added:

- Fail the build on acceptance/domain test failures
- Publish coverage reports to dashboard
- Run unit tests on every PR
- Run integration tests on merge to main
- Run full test suite on release branches
  **Pattern:** Match regex and update context objects

```csharp
[Given(@"a tour exists with minimum (.*) and maximum (.*) customers")]
public void Given_A_Tour_Exists_With_Min_And_Max_Customers(int min, int max)
{
    tourContext.Tour = Tour.Create(/* ... */).Value;
}

[Then(@"the booking should fail with ""(.*)"" error")]
public void Then_The_Booking_Should_Fail_With_Error(string expectedError)
{
    Assert.False(bookingContext.Result.IsSuccess);
    Assert.Contains(expectedError, bookingContext.Result.ErrorDetails.Detail);
}
```

**Principles:**

- Step definitions call domain/application layer directly (no UI)
- Use context objects to share state between steps

## Unit & Integration Test Coverage

### Coverage Goals

- **Unit Tests:** 80%+ code coverage
- **Integration Tests:** All API endpoints
- **Behavior Tests:** Business-critical scenarios

Focus on happy paths, error paths, and boundary conditions. Don't obsess over 100% coverage or testing simple
getters/setters.

### Running Tests with Coverage

```powershell
dotnet test                                                                    # All tests
dotnet test tests/ViajantesTurismo.Admin.UnitTests                             # Specific project
dotnet test --filter "FullyQualifiedName~Mapper"                               # Filtered
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings    # With coverage
```

See [Code Quality](CODE_QUALITY.md#test-coverage-tools) for coverage tool configuration and report generation.

### CI/CD Integration (Future)

When CI is added:

- Fail the build on test failures
- Publish coverage reports to dashboard
- Run unit tests on every PR
- Run integration tests on merge to main
- Run full test suite on release branches

## Related Documentation

- [BDD Guide](../tests/BDD_GUIDE.md) — Gherkin/BDD-specific guidelines and best practices
- [Domain Validation](DOMAIN_VALIDATION.md) — Factory methods, Result pattern, validation rules
- [Code Quality](CODE_QUALITY.md) — Tool configuration, linters, formatters, coverage reporting
- [Coding Guidelines](CODING_GUIDELINES.md) — C# coding standards and conventions
- [Architecture Decisions](ARCHITECTURE_DECISIONS.md) — ADRs referenced in test tags

## Summary

Good tests are **Fast**, **Independent**, **Repeatable**, **Self-Validating**, and **Timely** (FIRST). Follow these
guidelines to maintain a high-quality test suite that gives confidence when making changes.

**Key Principles:**

- Test pyramid: More unit tests, fewer integration tests, minimal E2E
- Use declarative BDD scenarios in domain language
- Prefer object mothers/builders over complex test data
- Tag scenarios to link business requirements with technical implementation
- Keep tests fast, independent, and focused on behavior

## Common Anti-Patterns to Avoid

❌ Testing too many things in one test
❌ Fragile tests (exact error message matching)
❌ Using `Thread.Sleep()` or arbitrary delays
❌ Tests depending on execution order or shared state
❌ Complex logic in tests (loops, conditionals)

## Conclusion

Good tests are **Fast**, **Independent**, **Repeatable**, **Self-Validating**, and **Timely** (FIRST). Follow these
guidelines to maintain a high-quality test suite that gives confidence when making changes.
