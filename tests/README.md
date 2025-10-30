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
- Total price must be > 0

## Running Tests

```powershell
# All tests
dotnet test

# Specific project
dotnet test tests/ViajantesTurismo.Admin.UnitTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Best Practices

- **Unit Tests**: One concept per test, descriptive names, AAA pattern, test success and failure
- **Behavior Tests**: Plain English scenarios, focus on business value, independent scenarios
- **Integration Tests**: Full request/response cycle, verify status codes, test error conditions

## Coverage Goals

- Domain Logic: 100%
- API Endpoints: 90%+
- All critical edge cases
