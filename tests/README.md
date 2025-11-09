# ViajantesTurismo Tests

Test projects for the ViajantesTurismo Admin domain.

## Test Projects

### ViajantesTurismo.Admin.UnitTests

- **Purpose**: Unit tests for domain logic and business rules
- **Scope**: Domain entities in isolation (no database, no HTTP)
- **Pattern**: Result pattern for validation
- **Speed**: Fast

### ViajantesTurismo.Admin.BehaviorTests

- **Purpose**: BDD tests using Gherkin syntax
- **Scope**: Business scenarios and workflows
- **Pattern**: Given-When-Then in `.feature` files
- **Speed**: Fast to medium
- **📖 See**: [BDD Guide](BDD_GUIDE.md) | [Reqnroll Technical Guide](REQNROLL_GUIDE.md)

### ViajantesTurismo.IntegrationTests

- **Purpose**: Integration tests for the Admin API
- **Scope**: Full API endpoint testing with database
- **Pattern**: Arrange-Act-Assert with shared fixture
- **Speed**: Slower

## Testing Principles

### Result Pattern

Domain validation uses the **Result pattern** instead of exceptions.

**Benefits**: Explicit error handling, better testability, clear success/failure paths.

### Domain Immutability Rules

- **Completed/Cancelled bookings**: Cannot be updated or deleted
- **Tours with confirmed bookings**: Limited editing allowed

### Validation Rules

**Tour:**

- End date must be after start date
- All prices must be >= 0
- Cannot book tours that have already ended

**Booking:**

- Valid transitions: Pending→Confirmed, Pending→Cancelled, Confirmed→Completed, Confirmed→Cancelled
- Cancelled and Completed are terminal states (immutable)
- Customer and companion cannot be the same person
- BikeType.None not allowed for tour bookings
- Total price must be > 0 after discount
- Discount validation: percentage 0-100%, absolute cannot exceed subtotal
- Payment validation: amount > 0, cannot exceed remaining balance, payment date not in future
- Cannot modify or record payments for Cancelled/Completed bookings

## Running Tests

```powershell
# All tests
dotnet test

# Specific project
dotnet test tests/ViajantesTurismo.Admin.UnitTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Running Tests with Coverage

To run tests and generate a code coverage report:

```powershell
# Run tests with coverage collection using the solution runsettings (excludes Migrations, etc.)
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory:TestResults

# Generate HTML coverage report
dotnet reportgenerator -reports:"TestResults\**\*.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:Html

# Open the report (Windows)
Invoke-Item TestResults\CoverageReport\index.html
```

The coverage report provides detailed insights into:

- Line and branch coverage percentages
- Uncovered code sections
- Coverage by project and class
- Historical coverage trends

**Note:** The `dotnet reportgenerator` tool is included in the repository's local tool manifest
(`.config/dotnet-tools.json`). Run `dotnet tool restore` from the repository root to ensure it's installed.

## Best Practices

### Unit Tests

- One concept per test
- Descriptive names following conventions
- AAA pattern (Arrange-Act-Assert)
- Test both success and failure paths

### Behavior Tests (Reqnroll)

- **Plain English scenarios** focused on business value
- **Independent scenarios** - each can run standalone
- **Declarative over imperative** - Focus on business behavior, not UI details
- **Context Injection** - Use custom POCO classes for sharing data between steps
- **Domain-based organization** - Group features and steps by aggregate/entity
- **Tag strategically** - Use `@BC:`, `@Agg:`, `@ADR:` tags for traceability

**📖 For comprehensive BDD guidelines, see:**

- [BDD Guide](BDD_GUIDE.md) - Philosophy, feature writing, and best practices
- [Reqnroll Technical Guide](REQNROLL_GUIDE.md) - Context injection, project structure, and configuration

### Integration Tests

- Full request/response cycle testing
- Verify status codes and response content
- Test error conditions and edge cases

## Coverage Goals

- Domain Logic: 100%
- API Endpoints: 90%+
- All critical edge cases
