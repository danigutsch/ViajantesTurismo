# Test Guidelines

This document outlines the testing standards and best practices for the ViajantesTurismo project.

## Test Project Structure

### `ViajantesTurismo.Common.UnitTests`

Tests for shared utilities: building blocks, Result pattern, sanitizers, extensions. Fast (< 1s), no dependencies.

### `ViajantesTurismo.Admin.UnitTests`

Tests for domain logic: entities, aggregates, mappers, business rules. Fast (< 2s), in-memory only.

### `ViajantesTurismo.Admin.IntegrationTests`

Tests for API endpoints with real PostgreSQL database. Slower (~24s), tests complete request-response cycle.

### `ViajantesTurismo.Admin.BehaviorTests`

Behavior-driven tests using Gherkin/SpecFlow. Medium speed (~3s), written in business language.

## Test Naming Conventions

### Unit and Integration Tests

**Pattern:** `Method_Name_Context_Description_Expected_Behavior`

Use underscores to separate all words for improved readability.

```csharp
// Good
public void Map_To_Currency_Dto_Should_Map_All_Valid_Values()
public void Create_Tour_With_Valid_Data_Should_Return_Created_Status()

// Bad
public void MapToCurrencyDto_ShouldMapAllValidValues()  // Inconsistent
public void TestCreateTour()  // Not descriptive
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

## Test Data Approaches

**Unit Tests:** Direct instantiation with factory methods
**Integration Tests:** Seeded database using `Seeder` class
**Behavior Tests:** Context objects to share state between steps

## Assertions Best Practices

- Use specific assertions (`Assert.Equal`, `Assert.Contains`) over generic `Assert.True`
- Multiple related assertions in one test are acceptable
- Avoid conditional logic in assertions
- Be specific but not fragile (e.g., check message contains key phrase, not exact text)

## Testing Best Practices

### FIRST Principles

- **Fast**: Unit tests < 100ms, Integration < 5s, Behavior < 10s
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

**Feature files** use Gherkin syntax with Background, Scenario, Given/When/Then steps.

**Step definitions** match regex patterns and update context objects:

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

## Test Coverage Goals

- **Unit Tests:** 80%+ code coverage
- **Integration Tests:** All API endpoints
- **Behavior Tests:** Business-critical scenarios

Focus on happy paths, error paths, and boundary conditions. Don't obsess over 100% coverage or testing simple
getters/setters.

## Running Tests

```powershell
dotnet test                                          # All tests
dotnet test tests/ViajantesTurismo.Admin.UnitTests  # Specific project
dotnet test --filter "FullyQualifiedName~Mapper"    # Filtered
dotnet test --collect:"XPlat Code Coverage"         # With coverage
```

## Common Anti-Patterns to Avoid

❌ Testing too many things in one test
❌ Fragile tests (exact error message matching)
❌ Using `Thread.Sleep()` or arbitrary delays
❌ Tests depending on execution order or shared state
❌ Complex logic in tests (loops, conditionals)

## Summary

Good tests are **Fast**, **Independent**, **Repeatable**, **Self-Validating**, and **Timely** (FIRST). Follow these
guidelines to maintain a high-quality test suite that gives confidence when making changes.
