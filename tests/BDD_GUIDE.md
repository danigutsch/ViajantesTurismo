# BDD Guide for ViajantesTurismo

Project-specific BDD practices and conventions using Reqnroll.

## Overview

This guide covers **ViajantesTurismo-specific** BDD patterns, tagging conventions, and workflows.

**For general BDD/Gherkin concepts, see:**

- **[Reqnroll Documentation](https://docs.reqnroll.net/latest/)** - Complete Reqnroll guide
- **[Gherkin Reference](https://docs.reqnroll.net/latest/gherkin/gherkin-reference.html)** - Syntax and best practices
- **[Cucumber BDD Guide](https://cucumber.io/docs/bdd/)** - BDD philosophy
- **[Anti-Patterns](https://cucumber.io/docs/guides/anti-patterns/)** - Common pitfalls

## Table of Contents

- [Quick Start](#quick-start)
- [Project Tagging Conventions](#project-tagging-conventions)
- [Feature Organization](#feature-organization)
- [Step Organization](#step-organization)
- [Living Documentation](#living-documentation)
- [Gherkin Linting](#gherkin-linting)

---

## Quick Start

**Writing Features:**

1. Organize by domain aggregate
2. Tag with `@BC:<context>` and `@Agg:<aggregate>` (mandatory)
3. Write declarative scenarios (**what**, not **how**)
4. Keep scenarios short (3-5 steps)

**Running Tests:**

```powershell

# All behavior tests

dotnet test tests/ViajantesTurismo.Admin.BehaviorTests

# By aggregate

dotnet test --filter "TestCategory=Agg:Booking"

# By ADR

dotnet test --filter "TestCategory=ADR:011"
```

See [Test Guidelines](../docs/TEST_GUIDELINES.md) for general testing patterns.

---

## Project Tagging Conventions

### Mandatory Tags (Feature Level)

Every feature **must** have:

- `@BC:<BoundedContext>` - e.g., `@BC:Admin`
- `@Agg:<Aggregate>` - e.g., `@Agg:Tour`, `@Agg:Booking`, `@Agg:Customer`

**Example:**

```gherkin
@BC:Admin @Agg:Booking
Feature: Booking Lifecycle Management
  Manage booking state transitions
```

### Optional Tags

**Traceability:**

- `@ADR:<number>` - Links to Architecture Decision Record
- `@WI:<id>` - Links to work item/user story

**Test Execution:**

- `@smoke` - Critical happy paths
- `@regression` - Full regression suite
- `@critical` - Must pass before release

**Scenario Type:**

- `@happy_path` - Successful operations
- `@error_case` - Validation failures
- `@edge_case` - Boundary conditions

**Lifecycle:**

- `@wip` - Work in progress (excluded from CI)

### Running Tests by Tag

```powershell

# Single tag

dotnet test --filter "TestCategory=Agg:Booking"

# Multiple tags (OR)

dotnet test --filter "TestCategory=smoke|TestCategory=critical"

# Multiple tags (AND)

dotnet test --filter "TestCategory=Agg:Booking&TestCategory=critical"

# Exclude tags

dotnet test --filter "TestCategory!=wip"
```

See [Reqnroll: Executing Specific Scenarios][exec-scenarios] for advanced filtering.

[exec-scenarios]: https://docs.reqnroll.net/latest/execution/executing-specific-scenarios.html

---

## Feature Organization

### Organize by Domain Aggregate

```text
Features/
├── Booking/
│   ├── booking-lifecycle.feature
│   ├── booking-validation.feature
│   └── booking-cancellation.feature
├── Tour/
│   ├── tour-creation.feature
│   └── tour-capacity.feature
└── Customer/
    ├── customer-registration.feature
    └── customer-validation.feature
```

**Naming:** `<aggregate>-<capability>.feature` (lowercase-with-hyphens)

**Benefits:**

- Aligns with domain model
- Enables test filtering by aggregate
- Avoids [feature-coupled step definitions][anti-patterns]

[anti-patterns]: https://cucumber.io/docs/guides/anti-patterns#feature-coupled-step-definitions

---

## Step Organization

### Group Steps by Domain Aggregate

```text
Steps/
├── BookingSteps.cs       # All booking-related steps
├── TourSteps.cs          # All tour-related steps
├── CustomerSteps.cs      # All customer-related steps
└── CommonSteps.cs        # Shared authentication/setup
```

**✅ DO:** Organize by domain aggregate (enables step reuse)
**❌ DON'T:** Create per-feature step files (causes duplication)

### Write Reusable Parameterized Steps

```csharp
// ✅ GOOD - Reusable across features
[Given(@"a tour exists with minimum (.*) and maximum (.*) customers")]
public void GivenATourExists(int min, int max)
{
    _tourContext.Tour = Tour.Create(/*...*/).Value;
}
```

See [Reqnroll: Step Definitions][step-defs] for detailed guidance.

[step-defs]: https://docs.reqnroll.net/latest/automation/step-definitions.html

---

## Living Documentation

### HTML Reports

Configure in `reqnroll.json`:

```json
{
  "formatters": {
    "html": {
      "outputFilePath": "TestResults/living_documentation.html"
    }
  }
}
```

Generate:

```powershell
dotnet test
start TestResults/living_documentation.html
```

### Coverage Validation

Track domain coverage by aggregate:

```powershell

# List all Tour tests

dotnet test --filter "TestCategory=Agg:Tour" --list-tests

# Verify ADR coverage

dotnet test --filter "TestCategory=ADR:011" --list-tests
```

See [Reqnroll: Reporting][reporting] for advanced reporting options.

[reporting]: https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html

---

## Gherkin Linting

### Enforced Rules

All `.feature` files are linted using `gherkin-lint` with project-specific rules:

**Mandatory:**

- `@BC:<BoundedContext>` tag on every feature
- `@Agg:<Aggregate>` tag on every feature
- Consistent indentation (Feature: 0, Rule/Scenario: 2, Steps: 4)
- No unnamed features/scenarios

**Anti-Patterns Prevented:**

- Conjunction steps
- Use of `@skip` (use `@wip` instead)

### Running the Linter

```powershell

# Validate all feature files

npm run lint:gherkin

# Runs automatically in pre-commit hook

```

**Note:** gherkin-lint validates but does not auto-fix. Errors must be corrected manually.

**Configuration:** Rules are defined in `.gherkin-lintrc` at the repository root.

### Pre-Commit Hook

Gherkin linting runs automatically before each commit and **blocks commit** if errors found.

Install hook:

```powershell

# Windows

.\scripts\install-git-hooks.ps1

# Unix/Linux/macOS

bash scripts/install-git-hooks.sh
```

---

## Best Practices Summary

1. **Tag every feature** with `@BC:` and `@Agg:` (enforced by linter)
2. **Organize by aggregate**, not by feature file
3. **Write declarative scenarios** (business behavior, not UI)
4. **Keep scenarios short** (3-5 steps, one `When` per scenario)
5. **Reuse steps** via parameterization
6. **Link to ADRs** for traceability
7. **Generate living documentation** regularly

---

## Resources

**Reqnroll:**

- [Reqnroll Documentation](https://docs.reqnroll.net/latest/)
- [Gherkin Reference](https://docs.reqnroll.net/latest/gherkin/gherkin-reference.html)
- [Step Definitions](https://docs.reqnroll.net/latest/automation/step-definitions.html)
- [Context Injection](https://docs.reqnroll.net/latest/automation/context-injection.html)
- [Executing Specific Scenarios](https://docs.reqnroll.net/latest/execution/executing-specific-scenarios.html)
- [Reporting & Formatters](https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html)

**BDD Best Practices:**

- [Cucumber BDD Guide](https://cucumber.io/docs/bdd/)
- [Gherkin Best Practices](https://cucumber.io/docs/gherkin/)
- [Anti-Patterns to Avoid](https://cucumber.io/docs/guides/anti-patterns/)
- [Step Organization](https://cucumber.io/docs/gherkin/step-organization/)

**Project Documentation:**

- [Test Guidelines](../docs/TEST_GUIDELINES.md) - General testing strategy
- [Domain Glossary](../docs/domain/GLOSSARY.md) - Ubiquitous language
- [Architecture Decisions](../docs/ARCHITECTURE_DECISIONS.md) - ADR index
