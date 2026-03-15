# ViajantesTurismo Tests

Test projects for the ViajantesTurismo Admin domain.

## Test Projects

| Project                                                            | Type         | Scope                                 | Speed       |
|--------------------------------------------------------------------|--------------|---------------------------------------|-------------|
| [Admin.UnitTests](ViajantesTurismo.Admin.UnitTests/)               | Unit         | Domain logic in isolation             | Fast        |
| [Admin.BehaviorTests](ViajantesTurismo.Admin.BehaviorTests/)       | BDD          | Business scenarios (Gherkin)          | Fast–Medium |
| [Admin.IntegrationTests](ViajantesTurismo.Admin.IntegrationTests/) | Integration  | API endpoints with real DB            | Slower      |
| [Admin.WebTests](ViajantesTurismo.Admin.WebTests/)                 | Component    | Blazor components (bUnit)             | Fast        |
| [Admin.E2ETests](ViajantesTurismo.Admin.E2ETests/)                 | E2E          | Full-stack browser tests (Playwright) | Slowest     |
| [ArchitectureTests](ViajantesTurismo.ArchitectureTests/)           | Architecture | Layer boundaries & conventions        | Fast        |
| [Common.UnitTests](ViajantesTurismo.Common.UnitTests/)             | Unit         | Shared types (Result, Option)         | Fast        |

## Running Tests

```powershell
# All tests in the solution
dotnet test --solution ViajantesTurismo.slnx

# Single project
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests/ViajantesTurismo.Admin.UnitTests.csproj

# Filter by method name (use --project to target a specific project)
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests --filter-method "*TourCreation*"

# Filter by class (fully-qualified name)
dotnet test --project tests/ViajantesTurismo.Admin.IntegrationTests --filter-class "ViajantesTurismo.Admin.IntegrationTests.Bookings.BookingApiTests"

# Run multiple test classes at once
dotnet test --project tests/ViajantesTurismo.Admin.E2ETests --filter-class
"ViajantesTurismo.Admin.E2ETests.Tests.ConditionalStateTests" "ViajantesTurismo.Admin.E2ETests.Tests.BookingDeleteAndDialogTests"
```

> **MTP note:** All test projects use xUnit v3 on Microsoft Testing Platform. The legacy VSTest
> `--filter "FullyQualifiedName~..."` syntax does **not** work. Use `--filter-class`,
> `--filter-method`, `--filter-trait`, etc. See
> [TEST_GUIDELINES.md](../docs/TEST_GUIDELINES.md#filtering-tests-mtp) for full reference.

## Code Coverage

```powershell
# Run with coverage
dotnet test --solution ViajantesTurismo.slnx -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml

# Generate HTML report from all per-project coverage files
reportgenerator -reports:"tests/**/TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:"Html"

# Open report (Windows)
Invoke-Item TestResults\CoverageReport\index.html
```

> With MTP, solution-level coverage writes one `coverage.cobertura.xml` file per test project under that
> project's `TestResults` folder. Aggregate those files with the glob above rather than expecting a
> single root-level coverage XML file.
> `reportgenerator` is available via the local tool manifest — run `dotnet tool restore` if missing.

## Test Parallelization (E2E & Integration)

E2E and integration tests use **xUnit v3 Assembly Fixtures** to run tests in parallel while sharing a single Aspire app
instance. This approach reduces total test time by ~30%.

### Pattern

1. **Assembly Fixture** — Shared infrastructure (PostgreSQL, Redis) initialized once per assembly:

   ```csharp
   [assembly: AssemblyFixture(typeof(E2EFixture))]
   ```

2. **Hybrid Strategy**:
   - **Parallel Tests** (default) — Run concurrently; safe to read/write unique data; use API or UI to create their own
     records
   - **Serial Tests** — Opt-in via `[Collection("Domain.Serial", DisableParallelization = true)]`; can assert exact
     counts and perform destructive operations

3. **Principles**:
   - Tests that mutate seeded data must create their own via API
   - Read-only tests can share seeded data
   - Serial tests are for edge cases (DB state verification, exact counts)

### Applying to New Domains

When adding a new domain's E2E or integration tests:

1. Create a **base fixture class** (e.g., `E2EFixture` for E2E, `ApiFixture` for integration):
   - Initialize Aspire, wait for readiness
   - Seed once with idempotent guard check
   - Expose `HttpClient` for API helpers
   - Seed/Clear only relevant to serial tests

2. Create **base test classes**:
   - `DomainE2ETestBase` — inherits fixture via constructor, no per-test seed/clear
   - `DomainE2ESerialTestBase` — includes `[Collection("Domain.Serial")]`, seeds before & clears after each test

3. Add assembly fixture attribute:

   ```csharp
   [assembly: AssemblyFixture(typeof(E2EFixture))]
   ```

4. Create **API helpers** to enable data independence (e.g., `CreateTourAsync`, `CreateBookingAsync`)

5. **Categorize tests**:
   - Read-only → parallel base class
   - Data creators → parallel base class (create unique data)
   - Data mutators / edge cases → serial base class

See [ADR-018](../docs/adr/20260228-test-parallelization-with-assembly-fixtures.md)
for implementation details.

### Parallel-safety checklist (required for new tests)

- Create **owned data** inside each test (API helpers preferred).
- Prefer **ID-based** assertions and navigation over text-only row filters.
- Make assertions **deterministic**: use known IDs, hrefs, unique identifiers,
  or explicit search/filter state.
- If interacting with a paginated list, constrain the dataset or sort/filter state
  first rather than scanning pages until a match appears.
- Do not assert implicit/default ordering unless sorting is explicitly applied first.
- Keep DB-destructive or exact-count scenarios in serial test collections.

### Intentional serial survivors

Serial tests in this repository should be narrow, explicit survivors rather than
the default mode.

The current acceptable survivor categories are:

- thin browser list-interaction smokes that cheaper layers do not yet prove
- one clean-slate import commit smoke for the full upload → preview → confirm
   → summary workflow
- destructive-reset browser smoke coverage after a true database clear
- explicit empty-list API contract smokes for aggregate list endpoints

Keep the E2E suite in a single project unless CI cadence, fixture policy, or
ownership boundaries diverge enough to justify a split.

### Common flakiness anti-patterns

- Looking for a row by `HasText` only when lists are paginated.
- Using `table tbody tr`.First or other page-position selectors as business assertions.
- Clicking unstable grid rows while the UI is re-rendering instead of navigating by known route.
- Depending on seeded entities like fixed tour/customer names in parallel tests.
- Mixing parallel test classes with tests that call DB clear/reset operations.

## Conventions

- **Runner**: xUnit v3 on Microsoft Testing Platform (MTP). Pass test-host options after `--`.
- **Naming**: Natural language with underscores — `Confirming_A_Booking_When_Cancelled_Returns_Failure`
- **Pattern**: Arrange-Act-Assert
- **Validation**: Domain operations use the Result pattern; tests assert on `IsSuccess` / `IsFailure`

## Coverage Goals

| Area                | Target      |
|---------------------|-------------|
| Domain logic        | 100%        |
| API endpoints       | 90%+        |
| Critical edge cases | All covered |

## See Also

- [Test Guidelines](../docs/TEST_GUIDELINES.md) — Strategy and patterns
- [Domain Validation](../docs/DOMAIN_VALIDATION.md) — Business rules reference
- [BDD Guide](BDD_GUIDE.md) — Feature writing and organization
- [Reqnroll Guide](REQNROLL_GUIDE.md) — BDD technical implementation
