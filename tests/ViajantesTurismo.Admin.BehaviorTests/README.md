# ViajantesTurismo.Admin.BehaviorTests

Behavior-Driven Development tests using Gherkin syntax for business scenarios.

## Purpose

Business scenario tests in stakeholder-readable language (Given-When-Then format).

## Feature File Example

```gherkin
Feature: Booking Lifecycle
  
  Scenario: Confirming a pending booking
    Given a pending booking exists
    When the operator confirms the booking
    Then the booking status should be "Confirmed"

  Scenario: Cannot confirm a cancelled booking
    Given a cancelled booking exists
    When the operator tries to confirm the booking
    Then the operation should fail
```

## Running Tests

```powershell
# All behavior tests
dotnet test

# Specific feature
dotnet test --filter "FullyQualifiedName~BookingLifecycle"
```
