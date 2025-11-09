# BDD Guide for ViajantesTurismo

Comprehensive guide to Behavior-Driven Development (BDD) practices using Reqnroll.

## Table of Contents

- [BDD Philosophy](#bdd-philosophy)
- [Writing Gherkin Features](#writing-gherkin-features)
- [Feature Organization](#feature-organization)
- [Step Definitions](#step-definitions)
- [Living Documentation](#living-documentation)
- [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
- [Best Practices Summary](#best-practices-summary)

---

## BDD Philosophy

### What is BDD?

Behavior-Driven Development (BDD) is a collaborative approach that:

- **Bridges the gap** between business stakeholders and technical teams
- **Uses concrete examples** to illustrate how the system should behave
- **Produces executable specifications** that serve as both documentation and automated tests
- **Enables rapid iterations** with continuous feedback

**Three Core Practices:**

1. **Discovery** - Collaborative workshops to explore requirements through real-world examples
2. **Formulation** - Document examples in Gherkin (human + machine readable format)
3. **Automation** - Connect examples to code as automated tests that guide development

### Living Documentation

Feature files are **Living Documentation** - always up-to-date because they're executed as tests. Unlike traditional documentation:

- ✅ **Never out of date** - Tests fail when behavior changes
- ✅ **Stakeholder readable** - Business people can understand and validate
- ✅ **Executable** - Automated tests verify the documentation is accurate
- ✅ **Collaborative** - Shared language between business and technical teams

---

## Writing Gherkin Features

### Feature Structure with Rules

Use the `Rule` keyword to organize scenarios by business rules within a feature:

```gherkin
Feature: Booking Management
  
  Rule: Only pending bookings can be confirmed
    Scenario: Successfully confirm a pending booking
      Given a pending booking exists
      When the operator confirms the booking
      Then the booking status should be "Confirmed"
```

**Benefits:** Groups related scenarios by business rule, provides clear traceability, and aligns with domain model.

### Declarative vs. Imperative Scenarios

**❌ Imperative (UI-focused):**

```gherkin
Given I am on the bookings page
And I click the "Confirm" button
```

**✅ Declarative (Domain-focused):**

```gherkin
Given a pending booking exists
When the operator confirms the booking
Then the booking status should be "Confirmed"
```

**Why:** Survives UI changes, more readable for stakeholders, focuses on **what** not **how**.

### Writing Effective Scenarios

- Keep scenarios short (3-5 steps)
- One `When` step per scenario - test **one behavior**
- Use `And`/`But` for multiple `Given` or `Then` steps

### Background Usage

```gherkin
Background:
  Given the operator is authenticated
  And there is an active tour "Amazon Adventure"
```

- Keep to 3-4 steps maximum
- Use high-level, domain-focused language
- Move technical details into step definitions

---

## Feature Organization

### Organize by Domain Concept

Align feature files with **bounded contexts** and **domain aggregates** (not UI or technical structure):

```text
Features/
├── Booking/
│   ├── BookingLifecycle.feature        # @BC:Admin @Agg:Booking
│   ├── BookingValidation.feature       # @BC:Admin @Agg:Booking
│   └── BookingCancellation.feature     # @BC:Admin @Agg:Booking
├── Tour/
│   ├── TourCreation.feature            # @BC:Admin @Agg:Tour
│   └── TourScheduling.feature          # @BC:Admin @Agg:Tour
└── Customer/
    ├── CustomerRegistration.feature    # @BC:Admin @Agg:Customer
    └── CustomerValidation.feature      # @BC:Admin @Agg:Customer
```

**✅ Organize by Domain Concept** - One file per major domain object or capability
**❌ Avoid Feature-Coupled Organization** - Don't organize by UI page or test scenario name

This structure provides:

- Clear navigation by domain concept
- Easy discovery of which features are tested
- Natural alignment with domain model documentation
- Support for running tests by aggregate or bounded context
- Avoids the [Feature-coupled step definitions anti-pattern](https://cucumber.io/docs/guides/anti-patterns#feature-coupled-step-definitions)

### Tags for Traceability

```gherkin
@BC:Admin @Agg:Booking @ADR:005 @WI:123
Feature: Booking Lifecycle
  
  @critical @smoke
  Scenario: Confirm pending booking
```

**Tag Categories:**

- `@BC:<BoundedContext>` - Bounded context
- `@Agg:<Aggregate>` - Domain aggregate
- `@ADR:<number>` - Architecture Decision Record
- `@WI:<workItemId>` - User story/work item
- `@critical`, `@smoke`, `@edge_case` - Test priority/type

**Running by Tags:**

```powershell
dotnet test --filter "TestCategory=Agg:Booking"
dotnet test --filter "TestCategory=critical|TestCategory=smoke"
```

### Linking Features to Domain Rules

```gherkin
# Related Domain Rules:
# - Booking.Confirm (Admin.Domain/Aggregates/Booking.cs)
# - BookingPolicy.CanConfirm (Admin.Domain/Policies/)

@BC:Admin @Agg:Booking @ADR:005
Feature: Booking Lifecycle Management
```

---

## Step Definitions

### Group by Domain Concept

**Group by domain concept, not by feature:**

```text
Steps/
├── BookingSteps.cs           # All booking-related steps
├── TourSteps.cs              # All tour-related steps
├── CustomerSteps.cs          # All customer-related steps
└── CommonSteps.cs            # Shared steps (Given operator is authenticated)
```

**✅ Domain-based organization** enables step reuse across features
**❌ Don't create** `BookingLifecycleSteps.cs`, `BookingValidationSteps.cs` - causes duplication

### Avoid Step Duplication

Use parameterized steps:

```csharp
// ✅ GOOD
[Given(@"I go to the {string} page")]
public void GivenIGoToPage(string pageName)
{
    _pageFactory.OpenPage(pageName);
}
```

### Write Step Definitions Only When Needed

Don't write step definitions before you have scenarios that use them to avoid code cruft and maintenance burden.

---

## Generating Living Documentation

### HTML Reports

Add to `reqnroll.json`:

```json
{
  "formatters": {
    "html": {
      "outputFilePath": "TestResults/living_documentation.html"
    }
  }
}
```

Generate: `dotnet test` then `start TestResults/living_documentation.html`

### Cucumber Messages Format

For advanced reporting and integration with external tools:

```json
{
  "formatters": {
    "message": {
      "outputFilePath": "TestResults/cucumber_messages.ndjson"
    }
  }
}
```

This generates a standardized format that can be consumed by:

- **Allure Report** - Enterprise reporting solution
- **Custom dashboards** - Build your own coverage visualizations
- **CI/CD integrations** - Azure DevOps, GitHub Actions

### Verifying Feature Coverage

1. Tag domain rules using `@Agg:` tags
2. Document domain rules in feature headers
3. Generate HTML reports
4. Filter by aggregate: `dotnet test --filter "TestCategory=Agg:Booking"`

---

## Anti-Patterns to Avoid

### ❌ Conjunction Steps

Use atomic steps with `And`:

```gherkin
Given I have shades
And I have a brand new Mustang
```

### ❌ Feature-Coupled Step Definitions

Don't organize steps by feature files:

```text
❌ BAD:
Steps/
├── EditWorkExperienceSteps.cs
├── EditLanguagesSteps.cs
└── EditEducationSteps.cs
```

**Organize by domain concept instead:**

```text
✅ GOOD:
Steps/
├── EmployeeSteps.cs
├── EducationSteps.cs
└── WorkExperienceSteps.cs
```

### ❌ Imperative Steps

Use declarative steps:

```gherkin
# ✅ DO
Given I am authenticated as "user@example.com"

# ❌ DON'T
Given I am on the login page
When I enter "user@example.com" in the email field
```

---

## Best Practices Summary

1. **Declarative over Imperative** - Focus on business behavior, not UI interactions
2. **Domain Language** - Use ubiquitous language from your domain model
3. **One Behavior per Scenario** - Keep scenarios focused and short (3-5 steps)
4. **Organize by Domain** - Group features and steps by aggregate/entity
5. **Reusable Steps** - Avoid duplication with parameterized steps
6. **Living Documentation** - Keep scenarios readable for stakeholders
7. **Tag Strategically** - Use tags for traceability and selective test execution
8. **Use Rules** - Group scenarios by business rules within features
9. **Short Backgrounds** - Keep to 3-4 steps, domain-focused
10. **Atomic Steps** - Avoid conjunction steps, use `And`/`But` instead

---

## Resources

### Reqnroll Documentation

- [Reqnroll Documentation](https://docs.reqnroll.net/latest/)
- [Gherkin Reference](https://docs.reqnroll.net/latest/gherkin/gherkin-reference.html)
- [Executing Specific Scenarios](https://docs.reqnroll.net/latest/execution/executing-specific-scenarios.html)
- [Reqnroll Formatters & Living Documentation](https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html)

### BDD & Gherkin Best Practices

- [Cucumber BDD Guide](https://cucumber.io/docs/bdd/)
- [Gherkin Reference](https://cucumber.io/docs/gherkin/reference/)
- [Step Organization](https://cucumber.io/docs/gherkin/step-organization/)
- [Anti-Patterns to Avoid](https://cucumber.io/docs/guides/anti-patterns/)

### Additional Resources

- [Reqnroll GitHub](https://github.com/reqnroll/Reqnroll)
- [Microsoft Docs: Requirements Traceability](https://learn.microsoft.com/en-us/azure/devops/pipelines/test/requirements-traceability)
- [Azure DevOps: End-to-End Traceability](https://learn.microsoft.com/en-us/azure/devops/cross-service/end-to-end-traceability)
