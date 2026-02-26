# ViajantesTurismo.Admin.UnitTests

Unit tests for the Admin domain layer — business logic and domain rules in isolation.

## Scope

Fast, isolated tests for domain entities. No database, no HTTP, no file system.

- Tour creation, scheduling, pricing rules
- Booking state transitions and validation
- Customer creation and updates
- Discount and payment rules

### Booking State Transitions

| From      | To        | Allowed |
|-----------|-----------|---------|
| Pending   | Confirmed | ✅      |
| Pending   | Cancelled | ✅      |
| Confirmed | Completed | ✅      |
| Confirmed | Cancelled | ✅      |
| Cancelled | Any       | ❌      |
| Completed | Any       | ❌      |

## See Also

- [tests/README.md](../README.md) — Running tests, coverage, conventions
- [Domain README](../../src/ViajantesTurismo.Admin.Domain/README.md) — Full business rules
