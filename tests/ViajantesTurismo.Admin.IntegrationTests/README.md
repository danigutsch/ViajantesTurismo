# ViajantesTurismo.Admin.IntegrationTests

API integration tests focused on minimal endpoint wiring, serialization, and happy/edge-path validation using real
 infrastructure provisioned via .NET Aspire (`ApiFixture`).

## Scope & Strategy

We keep integration tests lean and focused:

- One wiring test per endpoint (basic success + not found) lives in `BookingApiTests`, `CustomerApiTests`,
 and `ToursApiTests`.
- Business rule permutations belong in unit / behavior tests (domain, handlers).
- Advanced scenarios that require multiple endpoint calls to verify cross-endpoint consistency
 (discount updates, payment status transitions, room & companion modifications) are in `BookingApiAdvancedTests`.
- Validation errors are asserted only when they exercise API contract translation
 (Result -> ValidationProblem / NotFound) rather than re‑proving domain invariants.

## Running Tests

### Run all tests (solution level)

```powershell
dotnet test ViajantesTurismo.slnx
```

### Run only integration tests project

```powershell
dotnet test tests/ViajantesTurismo.Admin.IntegrationTests
```

### Filter to booking integration tests

```powershell
dotnet test --filter-method "*BookingApi*"
```

### Example: run advanced discount/payment tests only

```powershell
dotnet test --filter-class "ViajantesTurismo.Admin.IntegrationTests.Bookings.BookingApiAdvancedTests"
```

### Run with coverage

```powershell
dotnet test tests/ViajantesTurismo.Admin.IntegrationTests -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

## Notes

- Infrastructure (Postgres) is launched once per test session via `ApiFixture` using `DistributedApplicationBuilder`.
- Tests avoid mocking; helpers create real Tours/Customers/Bookings via public endpoints.
- Expected prices are computed with a local helper mirroring domain formula: `base + roomSupplement + bikes - discount`.
- Keep additional scenarios minimal—prefer extending unit/behavior tests for deeper rule matrices.

## Future Improvements

- Migrate booking endpoints to application command handlers; when done add handler-level unit tests and
 trim any redundant integration assertions.
- Add a lightweight health check smoke test.
- Consider snapshot assertions for complex DTO graphs (ensure stability of contract).
