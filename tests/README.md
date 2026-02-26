# ViajantesTurismo Tests

Test projects for the ViajantesTurismo Admin domain.

## Test Projects

| Project | Type | Scope | Speed |
| ------- | ---- | ----- | ----- |
| [Admin.UnitTests](ViajantesTurismo.Admin.UnitTests/) | Unit | Domain logic in isolation | Fast |
| [Admin.BehaviorTests](ViajantesTurismo.Admin.BehaviorTests/) | BDD | Business scenarios (Gherkin) | Fast–Medium |
| [Admin.IntegrationTests](ViajantesTurismo.Admin.IntegrationTests/) | Integration | API endpoints with real DB | Slower |
| [Admin.WebTests](ViajantesTurismo.Admin.WebTests/) | Component | Blazor components (bUnit) | Fast |
| [Admin.E2ETests](ViajantesTurismo.Admin.E2ETests/) | E2E | Full-stack browser tests (Playwright) | Slowest |
| [ArchitectureTests](ViajantesTurismo.ArchitectureTests/) | Architecture | Layer boundaries & conventions | Fast |
| [Common.UnitTests](ViajantesTurismo.Common.UnitTests/) | Unit | Shared types (Result, Option) | Fast |

## Running Tests

```powershell
# All tests in the solution
dotnet test --solution ViajantesTurismo.slnx

# Single project
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests/ViajantesTurismo.Admin.UnitTests.csproj

# Filter by method name (use --project to target a specific project)
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --filter-method "*TourCreation*"

# Filter by class (short name works)
dotnet test --project tests/ViajantesTurismo.Admin.IntegrationTests --filter-class BookingApiTests

# Run multiple test classes at once
dotnet test --project tests/ViajantesTurismo.Admin.E2ETests --filter-class ConditionalStateTests BookingDeleteAndDialogTests
```

> **MTP note:** All test projects use xUnit v3 on Microsoft Testing Platform. The legacy VSTest
> `--filter "FullyQualifiedName~..."` syntax does **not** work. Use `--filter-class`,
> `--filter-method`, `--filter-trait`, etc. See
> [TEST_GUIDELINES.md](../docs/TEST_GUIDELINES.md#filtering-tests-mtp) for full reference.

## Code Coverage

```powershell
# Run with coverage
dotnet test --solution ViajantesTurismo.slnx -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml

# Generate HTML report
dotnet reportgenerator -reports:"**/TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:Html

# Open report (Windows)
Invoke-Item TestResults\CoverageReport\index.html
```

> `dotnet reportgenerator` is in the local tool manifest — run `dotnet tool restore` if missing.

## Conventions

- **Runner**: xUnit v3 on Microsoft Testing Platform (MTP). Pass test-host options after `--`.
- **Naming**: Natural language with underscores — `Confirming_A_Booking_When_Cancelled_Returns_Failure`
- **Pattern**: Arrange-Act-Assert
- **Validation**: Domain operations use the Result pattern; tests assert on `IsSuccess` / `IsFailure`

## Coverage Goals

| Area | Target |
| ---- | ------ |
| Domain logic | 100% |
| API endpoints | 90%+ |
| Critical edge cases | All covered |

## See Also

- [Test Guidelines](../docs/TEST_GUIDELINES.md) — Strategy and patterns
- [Domain Validation](../docs/DOMAIN_VALIDATION.md) — Business rules reference
- [BDD Guide](BDD_GUIDE.md) — Feature writing and organization
- [Reqnroll Guide](REQNROLL_GUIDE.md) — BDD technical implementation
