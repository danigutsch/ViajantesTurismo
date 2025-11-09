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

Feature files are **Living Documentation** - always up-to-date because they're executed as tests.
Unlike traditional documentation:

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
- Avoids the [Feature-coupled step definitions anti-pattern](
  https://cucumber.io/docs/guides/anti-patterns#feature-coupled-step-definitions)

### Tags for Traceability

Tags are essential for organizing features, enabling selective test execution, and maintaining traceability between
tests, domain concepts, and requirements.

#### Tag Categories

**Domain Tags** (Architecture Alignment):

- `@BC:<BoundedContext>` — Bounded context (e.g., `@BC:Admin`)
- `@Agg:<Aggregate>` — Domain aggregate (e.g., `@Agg:Tour`, `@Agg:Booking`, `@Agg:Customer`)
- `@Entity:<Name>` — Domain entity (e.g., `@Entity:Payment`, `@Entity:BookingCustomer`)
- `@VO:<Name>` — Value object (e.g., `@VO:DateRange`, `@VO:Discount`)

**Traceability Tags**:

- `@ADR:<number>` — Architecture Decision Record (e.g., `@ADR:008`, `@ADR:011`)
- `@WI:<workItemId>` — Work item / user story (e.g., `@WI:123`, `@WI:TOUR-456`)
- `@US:<number>` — User story (alternative to @WI if preferred)

**Test Execution Tags**:

- `@smoke` — Critical happy path scenarios, run on every build
- `@regression` — Full regression suite
- `@integration` — Tests requiring infrastructure (DB, external services)
- `@unit` — Pure domain logic tests (no infrastructure dependencies)

**Test Priority Tags**:

- `@critical` — Must always pass, blocks release
- `@high` — Important scenarios
- `@medium` — Standard scenarios
- `@low` — Nice-to-have coverage

**Scenario Type Tags**:

- `@happy_path` — Successful operations
- `@edge_case` — Boundary conditions
- `@error_case` — Validation failures and error handling
- `@security` — Security-related scenarios
- `@performance` — Performance-sensitive scenarios

**Lifecycle Tags**:

- `@wip` — Work in progress, not ready for CI
- `@manual` — Manual testing required
- `@automated` — Fully automated
- `@flaky` — Known to be unstable (investigate and fix)

#### Tag Inheritance

Tags applied at Feature/Rule level are inherited by all scenarios within that scope:

```gherkin
@BC:Admin @Agg:Booking @regression
Feature: Booking Payment Status Management
  Scenarios for managing payment status independently from booking status
  
  Rule: Payment status can be updated at any time
    
    @happy_path @critical
    Scenario: Mark confirmed booking as paid
      Given a confirmed booking exists
      When the operator updates the payment status to "Paid"
      Then the booking payment status should be "Paid"
      
    @edge_case @ADR:011
    Scenario: Record partial payment
      Given a confirmed booking with unpaid status exists
      When the operator records a payment of 500
      Then the booking payment status should be "PartiallyPaid"
```

#### Tag Naming Conventions

**✅ DO:**

- Use PascalCase for multi-word tags: `@EdgeCase`, `@HappyPath`
- Keep tags concise and meaningful
- Use consistent naming across features
- Use domain language (aggregate names from bounded context docs)

**❌ DON'T:**

- Use spaces in tags (invalid Gherkin)
- Create duplicate tags with different names (`@edge_case` vs `@boundary`)
- Over-tag scenarios (5-7 tags maximum per scenario)

#### Running Tests by Tags

**Single Tag:**

```powershell
# Run all Booking aggregate tests
dotnet test --filter "TestCategory=Agg:Booking"

# Run smoke tests only
dotnet test --filter "TestCategory=smoke"
```

**Multiple Tags (OR logic):**

```powershell
# Run smoke OR critical tests
dotnet test --filter "TestCategory=smoke|TestCategory=critical"
```

**Multiple Tags (AND logic):**

```powershell
# Run Booking tests that are also critical
dotnet test --filter "TestCategory=Agg:Booking&TestCategory=critical"

# Run tests for specific ADR
dotnet test --filter "TestCategory=ADR:011&TestCategory=Agg:Booking"
```

**Complex Expressions:**

```powershell
# Run critical tests for Admin BC excluding WIP
dotnet test --filter "(TestCategory=BC:Admin&TestCategory=critical)&TestCategory!=wip"

# Run all tests except flaky
dotnet test --filter "TestCategory!=flaky"
```

#### Coverage Validation

Use tags to verify domain coverage:

```powershell
# Verify all aggregates are tested
dotnet test --filter "TestCategory=Agg:Tour" --list-tests
dotnet test --filter "TestCategory=Agg:Booking" --list-tests
dotnet test --filter "TestCategory=Agg:Customer" --list-tests

# Find features without aggregate tags (potentially missing coverage)
# Use reporting tools or custom scripts to analyze
```

#### CI/CD Pipeline Usage

**Build Pipeline:**

```yaml
# Smoke tests on every commit
- script: dotnet test --filter "TestCategory=smoke"
  displayName: 'Smoke Tests'

# Full regression on PR
- script: dotnet test --filter "TestCategory=regression&TestCategory!=wip"
  displayName: 'Regression Tests'
```

**Release Pipeline:**

```yaml
# Critical tests must pass before deployment
- script: dotnet test --filter "TestCategory=critical"
  displayName: 'Critical Tests'
  
# Integration tests in staging
- script: dotnet test --filter "TestCategory=integration"
  displayName: 'Integration Tests'
```

#### Tag Examples by Use Case

**Domain Coverage Validation:**

```gherkin
@BC:Admin @Agg:Tour @regression
Feature: Tour Creation
  # All Tour aggregate scenarios inherit @BC:Admin @Agg:Tour
  
  @smoke @critical @happy_path
  Scenario: Create tour with valid data
    # Tags: BC:Admin, Agg:Tour, regression, smoke, critical, happy_path
```

**ADR Traceability:**

```gherkin
@BC:Admin @Agg:Booking @ADR:011
Feature: Payment Immutability
  Verify that payment records are immutable per ADR-011
  
  @error_case @critical
  Scenario: Cannot modify existing payment
    # Links scenario to architectural decision
```

**Work Item Tracking:**

```gherkin
@BC:Admin @Agg:Customer @WI:TOUR-234
Feature: Customer Profile Management
  Implement customer update capabilities
  
  @WI:TOUR-235 @happy_path
  Scenario: Update customer contact information
    # Links to specific work items for reporting
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

## Living Documentation

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

### Coverage Validation and Tracking

#### Domain Coverage Matrix

Track which domain concepts are covered by behavioral tests:

**By Aggregate:**

```powershell
# Generate test counts per aggregate
dotnet test --filter "TestCategory=Agg:Tour" --logger "console;verbosity=minimal" | Select-String "Passed"
dotnet test --filter "TestCategory=Agg:Booking" --logger "console;verbosity=minimal" | Select-String "Passed"
dotnet test --filter "TestCategory=Agg:Customer" --logger "console;verbosity=minimal" | Select-String "Passed"
```

**By ADR:**

```powershell
# Verify each ADR has corresponding tests
dotnet test --filter "TestCategory=ADR:001" --list-tests
dotnet test --filter "TestCategory=ADR:008" --list-tests
dotnet test --filter "TestCategory=ADR:011" --list-tests
```

#### Automated Coverage Reports

Create a script to validate coverage (`tests/scripts/ValidateCoverage.ps1`):

```powershell
# Example coverage validation script
$aggregates = @("Tour", "Booking", "Customer")
$missingCoverage = @()

foreach ($agg in $aggregates) {
    $count = (dotnet test --filter "TestCategory=Agg:$agg" --list-tests 2>&1 | 
              Select-String "Test Cases").ToString() -replace '\D+'
    
    if ([int]$count -lt 5) {
        $missingCoverage += "$agg has only $count scenarios (minimum: 5)"
    }
}

if ($missingCoverage.Count -gt 0) {
    Write-Error "Coverage gaps found:"
    $missingCoverage | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "✓ All aggregates have sufficient test coverage" -ForegroundColor Green
```

#### Coverage Badge Generation

Generate markdown coverage badges for `README.md`:

```powershell
# Count scenarios by category
$totalTests = (dotnet test --list-tests 2>&1 | Select-String "Test Cases").ToString() -replace '\D+'
$smokeTests = (dotnet test --filter "TestCategory=smoke" --list-tests 2>&1 | 
               Select-String "Test Cases").ToString() -replace '\D+'

# Output badge markdown
"![Total Tests](https://img.shields.io/badge/Scenarios-$totalTests-blue)"
"![Smoke Tests](https://img.shields.io/badge/Smoke-$smokeTests-green)"
```

#### Traceability Matrix Generation

Generate a comprehensive traceability report linking features to domain concepts:

Create `tests/scripts/GenerateTraceability.ps1`:

```powershell
param(
    [string]$OutputPath = "docs/TRACEABILITY.md"
)

# Parse all feature files
$features = Get-ChildItem -Path "tests" -Filter "*.feature" -Recurse

$matrix = @()

foreach ($feature in $features) {
    $content = Get-Content $feature.FullName -Raw
    
    # Extract tags (simple regex - enhance as needed)
    if ($content -match '@BC:(\w+)') { $bc = $matches[1] } else { $bc = "" }
    if ($content -match '@Agg:(\w+)') { $agg = $matches[1] } else { $agg = "" }
    if ($content -match '@ADR:(\d+)') { $adr = $matches[1] } else { $adr = "" }
    if ($content -match '@WI:([\w\-]+)') { $wi = $matches[1] } else { $wi = "" }
    
    # Extract feature name
    if ($content -match 'Feature:\s*(.+)') { 
        $featureName = $matches[1].Trim() 
    }
    
    # Count scenarios
    $scenarioCount = ($content | Select-String "Scenario:" -AllMatches).Matches.Count
    
    $matrix += [PSCustomObject]@{
        Feature = $featureName
        BoundedContext = $bc
        Aggregate = $agg
        ADR = $adr
        WorkItem = $wi
        Scenarios = $scenarioCount
        File = $feature.FullName -replace [regex]::Escape($PWD), "."
    }
}

# Generate markdown table
$markdown = @"
# Traceability Matrix

Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm")

## Coverage Summary

- Total Features: $($matrix.Count)
- Total Scenarios: $($matrix | Measure-Object -Property Scenarios -Sum).Sum

### By Bounded Context

$(($matrix | Group-Object BoundedContext | ForEach-Object {
    "- **$($_.Name)**: $($_.Count) features, $(($_.Group | Measure-Object -Property Scenarios -Sum).Sum) scenarios"
}) -join "`n")

### By Aggregate

$(($matrix | Group-Object Aggregate | Where-Object { $_.Name } | ForEach-Object {
    "- **$($_.Name)**: $($_.Count) features, $(($_.Group | Measure-Object -Property Scenarios -Sum).Sum) scenarios"
}) -join "`n")

## Detailed Matrix

| Feature | BC | Aggregate | ADR | Work Item | Scenarios | File |
|---------|-----|-----------|-----|-----------|-----------|------|
$(($matrix | Sort-Object BoundedContext, Aggregate, Feature | ForEach-Object {
    "| $($_.Feature) | $($_.BoundedContext) | $($_.Aggregate) | $($_.ADR) | $($_.WorkItem) | $($_.Scenarios) | ``$($_.File)`` |"
}) -join "`n")

## Missing Coverage

### Features without Aggregate tags
$(($matrix | Where-Object { -not $_.Aggregate } | ForEach-Object {
    "- $($_.Feature) (``$($_.File)``)"
}) -join "`n")

### ADRs without test coverage
// TODO: Compare with docs/ARCHITECTURE_DECISIONS.md to find untested ADRs

"@

# Write to file
$markdown | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "✓ Traceability matrix generated: $OutputPath" -ForegroundColor Green
```

Run in CI/CD:

```yaml
# Azure Pipelines example
- script: |
    pwsh -File tests/scripts/ValidateCoverage.ps1
    pwsh -File tests/scripts/GenerateTraceability.ps1
  displayName: 'Validate and Generate Coverage Reports'
  
- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: 'docs/TRACEABILITY.md'
    ArtifactName: 'traceability'
```

#### Verifying Feature Coverage

**Manual Verification:**

1. Tag domain rules using `@Agg:` tags
2. Document domain rules in feature headers
3. Generate HTML reports
4. Filter by aggregate: `dotnet test --filter "TestCategory=Agg:Booking"`

**Automated Verification:**

Create a test that validates all aggregates have coverage:

```csharp
// tests/ViajantesTurismo.Admin.BehaviorTests/CoverageTests.cs
public class CoverageTests
{
    [Theory]
    [InlineData("Tour")]
    [InlineData("Booking")]
    [InlineData("Customer")]
    public void AllAggregates_Should_HaveTestCoverage(string aggregate)
    {
        // This test ensures we remember to add tags
        // Actual validation done by CI/CD scripts
        Assert.NotNull(aggregate);
    }
}
```

#### Tag Consistency Validation

Create a hook to validate tags at runtime:

```csharp
// tests/ViajantesTurismo.Admin.BehaviorTests/Hooks/TagValidationHooks.cs
using Reqnroll;

[Binding]
public class TagValidationHooks
{
    private readonly ScenarioContext _scenarioContext;
    
    public TagValidationHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }
    
    [BeforeScenario(Order = -100)]
    public void ValidateTags()
    {
        var allTags = _scenarioContext.ScenarioInfo.Tags
            .Concat(_scenarioContext.ScenarioInfo.FeatureInfo.Tags ?? Array.Empty<string>())
            .Distinct()
            .ToArray();
        
        // Validate bounded context tag exists
        var bcTag = allTags.FirstOrDefault(t => t.StartsWith("BC:"));
        if (bcTag == null)
        {
            Console.WriteLine($"[WARNING] Scenario '{_scenarioContext.ScenarioInfo.Title}' " +
                            "is missing @BC:<context> tag");
        }
        
        // Validate aggregate tag exists
        var aggTag = allTags.FirstOrDefault(t => t.StartsWith("Agg:"));
        if (aggTag == null)
        {
            Console.WriteLine($"[WARNING] Scenario '{_scenarioContext.ScenarioInfo.Title}' " +
                            "is missing @Agg:<aggregate> tag");
        }
        
        // Log all tags for traceability
        Console.WriteLine($"[TRACE] Tags: {string.Join(", ", allTags)} | " +
                         $"Scenario: {_scenarioContext.ScenarioInfo.Title}");
    }
}
```

### Living Documentation Best Practices

1. **Keep features synchronized with domain** - When domain model changes, update feature files
2. **Generate reports on every CI build** - Make living documentation visible to stakeholders
3. **Review coverage gaps monthly** - Use traceability matrix to identify missing scenarios
4. **Tag consistently** - Use tag validation hooks to enforce standards
5. **Link to ADRs** - Every architectural decision should have test coverage
6. **Track work items** - Link scenarios to user stories for bi-directional traceability

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
11. **Consistent Tagging** - Apply domain tags (BC, Agg) to all features
12. **Coverage Validation** - Use automated scripts to verify domain coverage
13. **Traceability** - Link scenarios to ADRs and work items
14. **Tag Inheritance** - Leverage feature/rule level tags to reduce duplication

---

## Tagging Examples

### Example 1: Customer Aggregate Features

```gherkin
@BC:Admin @Agg:Customer @regression
Feature: Customer Creation
  As a tour operator
  I want to create customers with valid data
  So that customer records are properly validated
  
  Rule: All customer data must be validated
    
    @smoke @critical @happy_path
    Scenario: Creating a customer with complete valid information
      Given I have valid personal information
      And I have valid identification information
      And I have valid contact information
      When I create a customer
      Then the customer should be created successfully
    
    @error_case @ADR:001
    Scenario: Creating a customer with invalid email
      Given I have valid personal information
      And I have an invalid email address
      When I create a customer
      Then the customer creation should fail
      And the error should indicate invalid email format
```

### Example 2: Booking Aggregate with ADR Traceability

```gherkin
@BC:Admin @Agg:Booking @regression
Feature: Booking Payment Status Management
  As a tour operator
  I want to manage booking status and payment status independently
  So that I can handle various real-world scenarios
  
  Related ADRs:
  - ADR-011: Payment Tracking with Immutable Payment Records
  
  Rule: Payment status can be updated independently from booking status
    
    @smoke @critical @happy_path @ADR:011
    Scenario: Confirm booking then mark as paid
      Given a pending booking exists
      When the operator confirms the booking
      And the operator updates the payment status to "Paid"
      Then the booking status should be "Confirmed"
      And the booking payment status should be "Paid"
    
    @edge_case @ADR:011
    Scenario: Partial payment on confirmed booking
      Given a confirmed booking exists
      When the operator updates the payment status to "PartiallyPaid"
      Then the booking status should be "Confirmed"
      And the booking payment status should be "PartiallyPaid"
    
    @error_case @ADR:011
    Scenario: Cannot modify immutable payment records
      Given a booking with an existing payment record
      When the operator attempts to modify the payment record
      Then the modification should be rejected
      And the error should indicate payments are immutable
```

### Example 3: Tour Aggregate with Work Item Tracking

```gherkin
@BC:Admin @Agg:Tour @regression @WI:TOUR-123
Feature: Tour Capacity Management
  As a tour operator
  I want to enforce tour capacity limits
  So that tours are not overbooked
  
  Rule: Confirmed bookings count toward tour capacity
    
    @smoke @critical @happy_path @WI:TOUR-124
    Scenario: Book tour within capacity
      Given a tour with maximum capacity of 10
      And the tour has 8 confirmed bookings
      When I create a new booking for 1 customer
      Then the booking should be confirmed
      And the tour should have 9 confirmed bookings
    
    @edge_case @high @WI:TOUR-125
    Scenario: Reject booking that exceeds capacity
      Given a tour with maximum capacity of 10
      And the tour has 10 confirmed bookings
      When I attempt to create a new booking for 1 customer
      Then the booking should be rejected
      And the error should indicate capacity exceeded
    
    @edge_case @WI:TOUR-126
    Scenario: Pending bookings do not count toward capacity
      Given a tour with maximum capacity of 10
      And the tour has 9 confirmed bookings
      And the tour has 3 pending bookings
      When I create a new booking for 1 customer
      Then the booking should be confirmed
      And the tour should have 10 confirmed bookings
```

### Example 4: Value Object Validation

```gherkin
@BC:Admin @Agg:Tour @VO:Discount @regression
Feature: Discount Validation
  As a tour operator
  I want to validate discounts correctly
  So that pricing calculations are accurate
  
  Related ADR:
  - ADR-010: Discount as Value Object with Audit Trail
  
  Rule: Percentage discounts must be between 0 and 100
    
    @error_case @critical @ADR:010
    Scenario: Reject percentage discount above 100
      Given a booking with a subtotal of 1000
      When I attempt to apply a percentage discount of 150
      Then the discount should be rejected
      And the error should indicate invalid percentage range
    
    @edge_case @ADR:010
    Scenario: Accept 100% discount
      Given a booking with a subtotal of 1000
      When I apply a percentage discount of 100
      Then the discount should be applied
      And the total price should be 0
  
  Rule: Absolute discounts cannot exceed subtotal
    
    @error_case @critical @ADR:010
    Scenario: Reject absolute discount exceeding subtotal
      Given a booking with a subtotal of 500
      When I attempt to apply an absolute discount of 600
      Then the discount should be rejected
      And the error should indicate discount exceeds subtotal
```

### Example 5: Integration and Performance Tags

```gherkin
@BC:Admin @Agg:Booking @integration @performance
Feature: Large Booking Operations
  As a tour operator
  I want the system to handle large bookings efficiently
  So that system performance remains acceptable
  
  @performance @high
  Scenario: Create booking with 100 customers
    Given a tour with capacity for 100 customers
    When I create a booking for 100 customers
    Then the booking should be created within 2 seconds
    And all 100 customers should be properly linked
  
  @integration @smoke
  Scenario: Booking creation persists to database
    Given a valid booking request
    When I create the booking
    Then the booking should be saved to the database
    And I should be able to retrieve the booking by ID
```

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
