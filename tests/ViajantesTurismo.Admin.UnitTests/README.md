# ViajantesTurismo.Admin.UnitTests

Unit tests for the Admin domain layer - business logic and domain rules in isolation.

## Purpose

Fast, isolated tests for domain entities. No database, no HTTP, no file system.

## What We Test

### Tour Rules
- End date must be after start date
- All prices >= 0
- Identifier and name not empty

### Booking Rules
- Valid state transitions only
- Price must be > 0
- Cannot update completed/cancelled bookings
- Customer and companion cannot be same person

### State Transition Matrix

| From      | To        | Allowed |
|-----------|-----------|--------|
| Pending   | Confirmed | ✅     |
| Pending   | Cancelled | ✅     |
| Confirmed | Completed | ✅     |
| Confirmed | Cancelled | ✅     |
| Cancelled | Any       | ❌     |
| Completed | Any       | ❌     |

## Test-Driven Development

1. **Red**: Write failing test
2. **Green**: Minimal code to pass
3. **Refactor**: Improve while keeping tests green

## Test Naming

Use natural language with underscores for readability.

Example: `Confirming_A_Booking_When_Its_Status_Is_Cancelled_Returns_Failure`

## Running Tests

```powershell
# All unit tests
dotnet test

# Watch mode
dotnet watch test

# Specific class
dotnet test --filter "FullyQualifiedName~TourCreationTests"
```


