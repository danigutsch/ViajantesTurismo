# ViajantesTurismo.Admin.BehaviorTests

BDD tests using [Reqnroll](https://reqnroll.net/) and Gherkin syntax for domain business scenarios.

## Scope

Business scenario tests in stakeholder-readable language verifying domain rules and workflows.

### Domain Coverage

- **Booking**: Lifecycle, validation, payments, companions
- **Tour**: Creation, pricing, scheduling
- **Customer**: Registration, validation

## Project Structure

```text
ViajantesTurismo.Admin.BehaviorTests/
├── Context/     # Domain-specific POCO context classes
├── specs/       # Gherkin feature files (by aggregate)
└── Steps/       # Step definition classes
```

## Running

```powershell
# All behavior tests
dotnet test --project tests/ViajantesTurismo.Admin.BehaviorTests

# By aggregate tag
dotnet test --filter "TestCategory=Agg:Booking"

# Living documentation
dotnet test ; start TestResults/living_documentation.html
```

## See Also

- [tests/README.md](../README.md) — Running tests, coverage, conventions
- [BDD Guide](../BDD_GUIDE.md) — Philosophy, feature writing, organization
- [Reqnroll Guide](../REQNROLL_GUIDE.md) — Context injection, project structure
