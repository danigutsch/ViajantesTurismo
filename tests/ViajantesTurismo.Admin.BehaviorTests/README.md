# ViajantesTurismo.Admin.BehaviorTests

Behavior-Driven Development tests for the ViajantesTurismo Admin domain using [Reqnroll](https://reqnroll.net/).

## Purpose

Business scenario tests in stakeholder-readable language using Gherkin syntax (Given-When-Then format) to verify domain
business rules and workflows.

## Technology

- **Framework**: [Reqnroll](https://reqnroll.net/) (successor to SpecFlow)
- **Test Runner**: xUnit
- **Language**: Gherkin with C# step definitions
- **Assertions**: FluentAssertions

## Quick Start Guide

**📖 For comprehensive BDD and Reqnroll guidelines, see:**

- **[BDD Guide](../BDD_GUIDE.md)** - Philosophy, feature writing, organization, anti-patterns
- **[Reqnroll Technical Guide](../REQNROLL_GUIDE.md)** - Context injection, project structure, configuration

### Running Tests

```powershell
# All behavior tests
dotnet test

# Specific aggregate tests
dotnet test --filter "TestCategory=Agg:Booking"

# Critical scenarios only
dotnet test --filter "TestCategory=critical"

# Generate living documentation
dotnet test
start TestResults/living_documentation.html
```

### Example Feature

```gherkin
@BC:Admin @Agg:Booking
Feature: Booking Lifecycle
  As a tour operator
  I want to manage booking lifecycle
  So that customers have confirmed reservations

  Rule: Only pending bookings can be confirmed
    
    @critical
    Scenario: Successfully confirm a pending booking
      Given a pending booking exists
      When the operator confirms the booking
      Then the booking status should be "Confirmed"
```

## Project Structure

```text
ViajantesTurismo.Admin.BehaviorTests/
├── Context/              # Domain-specific context classes
│   ├── BookingContext.cs
│   ├── CustomerContext.cs
│   ├── TourContext.cs
│   └── ...
├── specs/                # Gherkin feature files
│   ├── Booking/
│   ├── Tour/
│   └── Customer/
├── Steps/                # Step definition classes
│   ├── BookingSteps.cs
│   ├── TourSteps.cs
│   └── CustomerSteps.cs
└── reqnroll.json         # Configuration
```

## Domain Coverage

### Booking Aggregate

- BookingLifecycle.feature - State transitions
- BookingValidation.feature - Domain rules
- BookingPayments.feature - Payment rules

### Tour Aggregate

- TourCreation.feature - Tour setup
- TourValidation.feature - Date/price validation

### Customer Aggregate

- CustomerRegistration.feature - Customer creation
- CustomerValidation.feature - Contact info validation

## Tagging Conventions

- **`@BC:Admin`** - Bounded Context
- **`@Agg:Booking|Tour|Customer`** - Domain aggregate
- **`@ADR:<number>`** - Architecture Decision Record
- **`@critical`** - Critical scenarios (smoke tests)
- **`@edge_case`** - Edge cases

### Running by Tag

```powershell
# All Booking scenarios
dotnet test --filter "TestCategory=Agg:Booking"

# Critical scenarios
dotnet test --filter "TestCategory=critical"
```

## Writing New Scenarios

### Best Practices for This Project

1. **One aggregate per feature file** - Separate Booking, Tour, Customer
2. **Use domain language** - Match ubiquitous language
3. **Test business rules** - Focus on domain invariants
4. **Keep scenarios short** - 3-5 steps ideal
5. **Tag for traceability** - Always include `@BC:Admin` and `@Agg:<name>`

### Example Pattern

```gherkin
@BC:Admin @Agg:Booking @ADR:005
Feature: [Aggregate] [Capability]
  As a [role]
  I want to [goal]
  So that [business value]

  Rule: [Business rule]
    
    Scenario: [Specific example]
      Given [context]
      When [action]
      Then [outcome]
```

## Resources

**Project-Specific:**

- [Main Test README](../README.md) - Domain validation rules
- [BDD Guide](../BDD_GUIDE.md) - Writing effective features
- [Reqnroll Guide](../REQNROLL_GUIDE.md) - Technical implementation

**External:**

- [Reqnroll Documentation](https://docs.reqnroll.net/latest/)
- [Cucumber BDD Guide](https://cucumber.io/docs/bdd/)
