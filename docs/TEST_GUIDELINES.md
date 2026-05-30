# Test Guidelines

This document outlines the testing standards and best practices for the ViajantesTurismo project.

## Testing Strategy

Our strategy follows a test pyramid approach:

- **Unit Tests** (fast, in-memory) — Majority of tests
- **UI Integration Tests** (hosted UI layer, no full browser workflow focus) — Key web composition and route scenarios
- **Contract Tests** (published boundary artifacts) — Stable compatibility checks for external consumers
- **Integration Tests** (database, HTTP) — Key API scenarios
- **Behaviour Tests** (BDD/Gherkin) — Business-critical scenarios
- **End-to-End/Acceptance** (Playwright) — Thin layer of critical user journeys through the real UI

**Philosophy:** Fast feedback through comprehensive unit tests, confidence through API and UI integration tests,
business alignment through BDD scenarios, durable boundary confidence through contract tests, and a small deterministic E2E lane for critical user flows.

## Canonical Admin Test Boundaries

Use `tests/README.md` as the canonical quick-reference matrix for Admin test scopes, host models, and tagging dimensions.

The most important architectural distinction is:

- **open-box test lanes** interact through in-process or fixture-owned seams such as direct type construction,
  Reqnroll contexts, bUnit rendering, or typed API-client seams
- **closed-box test lanes** interact with an externally hosted application surface, such as Playwright against the
  running web app or the Aspire-hosted path proven in this repository

Aspire-hosted testing is the canonical model for full-host Admin test execution.

Contract tests are different from full-host integration tests: they protect a
published boundary artifact that matters to external consumers, such as an
OpenAPI document, a serialized payload shape, or a schema fragment. If the goal
is to prove runtime behavior through persistence and request handling, use
integration tests instead.

## Recommended Tagging Model

When tags or traits are used, keep them orthogonal:

- `Scope`: unit, behavior, component, contract, ui-integration, integration, e2e, architecture
- `Surface`: domain, application, api, web, workflow, solution
- `Area`: bookings, customers, tours, payments, shared
- `Category`: smoke, regression, happy-path, edge-case
- `Host`: in-memory, aspire, browser

## Test Platform: MTP + xUnit v3

This repository uses **xUnit v3** on top of
[**Microsoft.Testing.Platform (MTP)**](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)

With MTP, each test project compiles into a standalone executable. `dotnet test` orchestrates
discovery and execution but delegates to the test-host process. This means:

- **`dotnet test`** is a thin orchestrator — the real options come from xUnit v3 and MTP extensions.
- The legacy **VSTest** switches (`--filter "FullyQualifiedName~..."`, `--logger`, `--settings`, etc.)
  are **not supported** and will silently return zero tests or produce errors.
- Use `--project` or `--solution` (named options, not positional arguments) to specify what to run.

| Command                                        | Purpose                          |
|------------------------------------------------|----------------------------------|
| `dotnet test --solution ViajantesTurismo.slnx` | Run all tests in the solution    |
| `dotnet test --project <path-to-.csproj>`      | Run tests in a single project    |
| `dotnet test --project <path> -- <args>`       | Pass extra args to the test host |

Example — run E2E tests in headed mode:

```powershell
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests/ViajantesTurismo.Admin.SystemTests.csproj -- Playwright.LaunchOptions.Headless=false
```

Official references:

- [dotnet test with MTP](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp)
- [MTP overview](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
- [xUnit v3 on MTP](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)

### Code Coverage with MTP + xUnit v3

This repository uses the MTP-native coverage collection for xUnit v3 test projects.

#### Canonical commands

Generate fresh Cobertura coverage for the whole solution:

```powershell
dotnet test --solution ViajantesTurismo.slnx -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml
```

Generate a single HTML report from all per-project coverage files:

```powershell
reportgenerator -reports:"tests/**/TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:"Html"
```

Open `TestResults/CoverageReport/index.html` to inspect the aggregated report.

#### Important MTP behavior

When you run coverage at the **solution** level, MTP writes one
`coverage.cobertura.xml` file per test project inside that project's `TestResults` folder.

Do **not** assume a single file such as:

```text
TestResults/Coverage/coverage.cobertura.xml
```

it exists after the solution runs.

Instead, aggregate the per-project files using the glob pattern above:

```text
tests/**/TestResults/**/coverage.cobertura.xml
```

#### Why the known filename still matters

Passing `--coverage-output coverage.cobertura.xml` ensures each project emits a predictable filename,
which makes `reportgenerator` aggregation stable even though the files are produced in different
project output folders.

### Filtering Tests MTP

xUnit v3 on MTP uses its own filter switches. The following **do NOT work** and will return
zero tests or an error:

- `--filter "FullyQualifiedName~..."` (VSTest syntax)
- `--treenode-filter` (MSTest-only MTP extension)

With **.NET 10+** (this repo), xUnit filter switches go **directly on `dotnet test`** as extra
args — no `--` separator needed.
With .NET 8/9, they must be passed after `--` (e.g. `dotnet test -- --filter-class ClassName`).

See [xUnit v3 MTP docs](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) for full reference.

#### xUnit v3 filter switches

| Switch                   | Purpose                                           | Example                                                       |
|--------------------------|---------------------------------------------------|---------------------------------------------------------------|
| `--filter-method`        | Run a given test method (wildcards `*` supported) | `--filter-method "*BookingApi*"`                              |
| `--filter-not-method`    | Exclude a method                                  | `--filter-not-method "SlowTest"`                              |
| `--filter-class`         | Run all tests in a class (fully-qualified name)   | `--filter-class "Namespace.TourCreationTests"`                |
| `--filter-not-class`     | Exclude a class                                   | `--filter-not-class "SlowTests"`                              |
| `--filter-namespace`     | Run all tests in a namespace                      | `--filter-namespace "ViajantesTurismo.Admin.UnitTests.Tours"` |
| `--filter-not-namespace` | Exclude a namespace                               | `--filter-not-namespace "ViajantesTurismo.Admin.SystemTests"` |
| `--filter-trait`         | Run tests matching a trait                        | `--filter-trait "Category=Fast"`                              |
| `--filter-not-trait`     | Exclude tests with a trait                        | `--filter-not-trait "Category=Slow"`                          |

> **Multiple values:** pass several quoted values to one switch instead of repeating it:
> `--filter-class "Ns.Tests.FooTests" "Ns.Tests.BarTests"` runs both classes.

```powershell
# Run a single test method (wildcards supported)
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --filter-method "*TourCreation*"

# Run all tests in a specific class (fully-qualified name)
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --filter-class "ViajantesTurismo.Admin.UnitTests.Tours.TourCreationTests"

# Run multiple test classes at once
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests --filter-class "ViajantesTurismo.Admin.SystemTests.Tests.ConditionalStateTests" "ViajantesTurismo.Admin.SystemTests.Tests.BookingDeleteAndDialogTests"

# Run all tests in a namespace
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --filter-namespace "ViajantesTurismo.Admin.UnitTests.Tours"

# Exclude slow tests by trait
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --filter-not-trait "Category=Slow"
```

#### Useful MTP platform options

These are built-in MTP options available on all test projects:

| Option                         | Purpose                                                |
|--------------------------------|--------------------------------------------------------|
| `--list-tests`                 | List discovered tests without running them             |
| `--timeout <value>[h\|m\|s]`   | Global test execution timeout (e.g. `--timeout 5m`)    |
| `--minimum-expected-tests <N>` | Fail if fewer than N tests run (default: 1)            |
| `--maximum-failed-tests <N>`   | Stop after N failures                                  |
| `--help`                       | Show all available options including extension options |
| `--info`                       | Show registered extensions and their options           |
| `--diagnostic`                 | Enable diagnostic trace logging to file                |

```powershell
# List all tests in a project without executing them
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --list-tests

# Run with a 10-minute timeout
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests --timeout 10m

# See all available options (including xUnit and extension switches)
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --help
```

## Test Project Structure

### Current Projects

- **`ViajantesTurismo.Common.UnitTests`** — Shared utilities: building blocks, Result pattern, sanitisers, extensions.
  Fast, no dependencies.
- **`ViajantesTurismo.Admin.UnitTests`** — Domain logic: entities, aggregates, mappers, business rules. Fast, in-memory
  only.
- **`ViajantesTurismo.Management.WebTests`** — Blazor Web components: Razor component rendering, UI state, user interactions.
  Uses bUnit for fast, in-memory component testing.
- **`ViajantesTurismo.Admin.IntegrationTests`** — API endpoints with real PostgreSQL database. Slower, tests complete
  request-response cycle through fixture-owned HTTP clients.
- **`ViajantesTurismo.Admin.ContractTests`** — Public Admin API compatibility checks. Protects boundary artifacts such
  as generated OpenAPI shapes or serialized contract slices without turning into a second integration lane.
- **`ViajantesTurismo.Admin.BehaviorTests`** — Behaviour-driven tests using Gherkin/SpecFlow for backend domain
  scenarios, written in business language.
- **`ViajantesTurismo.Admin.SystemTests`** — Playwright-driven UI flows against the real Admin web app and supporting
  backend resources.
- **`ViajantesTurismo.Admin.Testing`** — Shared test-only contracts, traits, and reusable helpers; not a runtime
  host in its own right.

### Suggested Structure (Future Evolution)

- `tests/Admin.Domain.Tests/` — Entities, value objects, aggregates (unit tests)
- `tests/Admin.Application.Tests/` — Use cases, policies, application services (unit tests)
- `tests/Management.Web.Tests/` — Blazor components (bUnit tests) ✅ *Already exists as `Management.WebTests`*
- `tests/Admin.Acceptance.Tests/` — BDD scenarios calling the Application layer
- `tests/Contract.Tests/` — Public boundary compatibility tests such as OpenAPI, payload, schema, or consumer-provider checks
- `tests/specs/` — Shared `.feature` files (if not colocated in acceptance tests)

## Naming Conventions

### Unit and Integration Tests

**Pattern:** descriptive natural-language phrase with underscores

Use underscores to separate all words for improved readability. Do not append fixed
suffixes like `Expected_Behavior`.

```csharp
// Good
public void Maps_currency_values_for_the_response_model()
public void Creates_a_tour_when_the_request_is_valid()

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

### Behaviour Test Step Definitions

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

### Assertion readability

Keep method calls and computed values out of assertion arguments when practical.
Assign them to local variables first so failures are easier to inspect while debugging.

```csharp
// Good
var errorType = span.GetTagItem("error.type");
Assert.Equal("InvalidOperationException", errorType);

// Avoid
Assert.Equal("InvalidOperationException", span.GetTagItem("error.type"));
```

## Blazor Component Testing (bUnit)

For web projects that use Razor components, bUnit is an appropriate lower-cost UI test tool for components without complex JS interop.

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

See the relevant web test project's README for stack-specific examples and patterns.

## Test Patterns

### Theory Tests

Use `[Theory]` with `[InlineData]` for multiple inputs:

```csharp
[Theory]
[InlineData(Currency.Real, CurrencyDto.Real)]
[InlineData(Currency.Euro, CurrencyDto.Euro)]
public void Maps_all_supported_currency_values(Currency domain, CurrencyDto expected)
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
- **Integration Tests:** Fixture-owned HTTP entrypoints plus named lifecycle methods such as `Seed` and `Reset`
- **Behaviour Tests:** Context objects to share state between steps
- **E2E Tests:** Playwright plus deterministic helper/page abstractions backed by named fixture lifecycle methods

### Best Practices

- **Prefer object mothers/builders** for complex aggregates and value objects
- **Keep test data in code** (avoid external files unless absolutely necessary)
- **Avoid shared mutable state** between tests
- **Use fixture-owned ephemeral resources** for integration and E2E tests instead of exposing raw container or DI
  plumbing to test bodies
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
- Be specific but not fragile (e.g. check that the message contains key phrase, not exact text)

## Testing Best Practices

## Parallel-Safe Test Design

Parallel execution is the default expectation for most tests in this repository.
Use these rules to avoid flaky behaviour:

- **Own your data per test**: create tours/customers/bookings inside the test (or via helper methods)
  instead of relying on shared seeded rows.
- **Use stable identifiers**: prefer ID-based navigation and selectors (`/tours/{id}`, `/bookings/{id}`)
  over non-unique text filters (first name, title text only).
- **All assertions must be deterministic**: assert against a known entity, route, unique identifier,
  or stable semantic locator rather than whichever row happens to be visible.
- **Avoid first-page assumptions** in paginated lists: if list presence is asserted, constrain the dataset
  or UI state first (for example, by explicit sort/filter/search) rather than assuming row visibility on page 1.
- **Do not depend on default unsorted order** from data grids or backend queries.
- **Separate concerns**: route/title checks should not depend on list ordering assertions.
- **Use serial collections only when required** (exact-count assertions, destructive DB resets,
  or scenarios that intentionally mutate the shared baseline state).
- **Document intentional exceptions**: if a test remains serial, seeded, or exact-dataset dependent on purpose,
  record why it remains and what would allow it to be rewritten.
- **Do not scan paginated lists for non-pagination behaviour**: if the scenario is not about paging,
  navigate directly by known ID/route or use deterministic owned-data row targeting.

Antipatterns to avoid:

- `table tbody tr`.First
- text-only row matching when multiple rows could qualify
- iterating paginator buttons until an item is eventually found
- assertions that only pass because the item happened to land on page 1
- pagination traversal helpers that click through pages until a matching row eventually appears

### E2E-specific base class guidance

- Use `E2ETestBase` for tests that can safely run in parallel with owned data.
- Use `E2ESerialTestBase` for tests that require per-test `ClearDatabase + Seed` isolation.

### Intentional serial exception patterns

Serial execution should remain the exception, not the default.
When a test stays serial on purpose, it should be both thin and clearly justified.

In this repository, the acceptable survivor patterns are:

- **exact-dataset browser interaction smokes** when cheaper layers do not prove the
  same signal, such as real sort-click or paginator-preserves-sort behaviour
- **single clean-slate import commit smokes** when the workflow confidence depends
  on a real browser upload, preview, commit, and final summary path
- **destructive-reset browser smokes** when the UI must be verified after a true
  database clear/reset rather than component-only rendering
- **explicit empty-list API contract smokes** when a real empty database response
  must be verified for status code and array contract shape

The current audited repository survivors are:

- `ListInteractionTests` for exact-dataset browser list interaction smokes
- `ErrorHandlingTests` for destructive-reset browser empty-state smoke coverage
- `CustomerImportSerialTests` for the single clean-slate browser import commit smoke
- `GetAllToursEmptyListTests`, `GetAllCustomersEmptyListTests`, and `GetAllBookingsEmptyListTests`
  for explicit empty-list API contract smokes

If a test does not fit one of those patterns, prefer rewriting it to owned-data
parallel execution before accepting a serial exception.

If a test intermittently fails only in full-suite runs but passes alone, treat it as a parallel-safety smell and
apply the rules above before increasing timeouts.

### FIRST Principles

- **Fast**: Unit tests < 100 ms, Integration ~24 s (includes database setup), Behaviour < 10s (frontend E2E tests when
  added will be slower)
- **Independent**: Tests run in any order without a shared state
- **Repeatable**: Same result every time (use `FakeTimeProvider` instead of `DateTime.UtcNow`)
- **Self-Validating**: Clear pass/fail without manual inspection
- **Timely**: Written with production code

### Guidelines

- One logical assertion per test (test one behaviour)
- Test public behaviour, not implementation details
- Extract common setup to helper methods (e.g., `TestHelpers.CreateTestCustomerWithNames()`)
- Before implementing multistep logic that is not the core behaviour under test,
  look for an existing helper method or helper class first.
- If no suitable helper exists and the logic is repeated or hurts readability,
  prefer creating a helper and then using it instead of inlining the plumbing.
- Extract shared helpers only for genuinely duplicated mechanics.
- Keep one-off readability helpers local to the test class until a second real consumer exists.
- Prefer dedicated helper classes for reusable test helper methods instead of keeping them inside
  test classes.
- Keep private methods inside a test class only when they are truly local to that class and would
  not be appropriate or useful anywhere else.
- Keep the behaviour under test and assertions visible in the test body;
  move only non-test-critical setup, navigation, and mechanical steps into helpers.
- Avoid narration-style comments when a simple Arrange / Act / Assert structure or a small helper method
  makes the intent clear.
- Avoid conditional logic in test bodies unless the conditional behaviour itself is what the test is proving.
- Avoid `Thread.Sleep()` and unnecessary waits
- No test execution order dependencies

### E2E selector strategy

Prefer selectors in this order:

1. **Accessible semantic selectors**
   - role + accessible name
   - label-based field lookup
   - heading/link/button names
2. **Business-key selectors**
   - known `href`
   - known entity ID in the route
   - unique identifier text owned by the test
3. **Explicit test hooks** only when semantic and business-key selectors are insufficient

Avoid introducing raw HTML IDs or brittle CSS traversal solely to compensate for weak test setup.

## Mapper Testing Pattern

All mapper methods need three tests:

1. **Valid Values** (Theory with InlineData)
2. **Coverage** (Iterate all enum values)
3. **Error Handling** (Invalid value throws ArgumentOutOfRangeException)

## Integration and E2E Host Essentials

### Integration tests

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

Integration tests should:

- exercise the API through `HttpClient`
- expose typed contract clients for bookings, customers, and tours as the ideal SUT entrypoints
- use a narrow shared host seam only when multiple hosted fixtures need the same setup/reset contract
- keep database lifecycle operations behind named fixture methods
- avoid direct test-body dependence on `IServiceProvider` or generic scope runners
- prefer Aspire-managed host ownership as the target direction when full application hosting is required

### UI integration tests

UI integration tests should:

- exercise the running web application below full browser E2E workflow depth
- focus on hosted route/page composition and application wiring
- keep the browser-system entrypoint separate from non-browser setup/reset support seams
- avoid drifting into either raw component tests or full user-journey E2E coverage

### E2E tests

E2E tests should:

- exercise the system through Playwright and visible business behavior
- treat the browser-visible web entrypoint as the SUT seam
- keep deterministic setup or reset work behind fixture-owned support seams, using a
  shared hosted contract only when it is genuinely useful
- navigate by deterministic IDs, routes, and semantic selectors
- keep application-host details behind `E2EFixture`, page objects, and workflow helpers
- treat direct service access as fixture-internal plumbing, not as a public test seam
- avoid expanding the hybrid host model further when Aspire-managed execution would better fit new work

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
- Initialise collections via field initialisers: `ICollection<string> Items { get; } = [];`

**Dependencies:**

- Initialise Fakes and test doubles via field initialisers
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
- **Behaviour Tests:** Business-critical scenarios

Focus on happy paths, error paths, and boundary conditions. Don't obsess over 100% coverage or testing simple
getters/setters.

### Coverage Configuration

Code coverage is collected using **Microsoft Testing Platform (MTP)** with the
[`Microsoft.Testing.Extensions.CodeCoverage`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-extensions-code-coverage)
extension (already referenced in all test projects). Settings are defined in `coverage.settings.xml` at the solution
root.

See [xUnit v3 Code Coverage with MTP](https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp) for the
official guide.

```powershell
# Collect coverage (cobertura XML)
dotnet test --solution ViajantesTurismo.slnx -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml

# `coverage.settings.xml` excludes migrations and common generated-source patterns during collection
```

After a solution-level run, MTP writes one `coverage.cobertura.xml` file per test project under that
project's `TestResults` folder. Aggregate them with `reportgenerator` using the glob documented above.

### CI/CD Integration (Future)

When CI is added:

- Fail the build on acceptance/domain test failures
- Publish coverage reports to the dashboard
- Run unit tests on every PR
- Run integration tests on merge to main
- Run the full test suite on release branches
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

## Running Tests

### Running Tests

```powershell
# All tests in the solution
dotnet test --solution ViajantesTurismo.slnx

# Single project
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests/ViajantesTurismo.Admin.UnitTests.csproj

# Filter to specific methods (MTP/xUnit v3 syntax — see "Filtering Tests" above)
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --filter-method "*Mapper*"

# Run specific test classes
dotnet test --project tests/ViajantesTurismo.Admin.SystemTests --filter-class ConditionalStateTests BookingDeleteAndDialogTests
```

> **Reminder:** Do NOT use `--filter "FullyQualifiedName~..."` — that is VSTest syntax and
> will silently return zero tests under MTP. Use `--filter-class`, `--filter-method`, etc.

### Running Tests with Coverage

```powershell
# All tests with cobertura coverage (coverage flags are test-host args, passed after --)
dotnet test --solution ViajantesTurismo.slnx -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml

# Generate HTML report from all per-project coverage files (requires reportgenerator tool)
reportgenerator -reports:"tests/**/TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:"Html"
```

> If `coverage.settings.xml` changed, regenerate the Cobertura files before building the HTML report.

See [Code Quality](CODE_QUALITY.md#test-coverage-tools) for full coverage tool configuration and report generation.

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
- Keep tests fast, independent, and focused on behaviour

## Common Anti-Patterns to Avoid

❌ Testing too many things in one test
❌ Fragile tests (exact error message matching)
❌ Using `Thread.Sleep()` or arbitrary delays
❌ Tests depending on execution order or shared state
❌ Complex logic in tests (loops, conditionals)
