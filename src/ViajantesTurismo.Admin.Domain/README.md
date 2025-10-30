# ViajantesTurismo.Admin.Domain

Domain logic for tour management system.

## Purpose

Core business logic for tours, bookings, and customers with business rule enforcement.

## Domain Entities

- **Tour**: Tour definitions with dates, pricing, services (aggregate root)
- **Booking**: Customer bookings with state machine (Pending → Confirmed → Completed/Cancelled)
- **Customer**: Customer information (8-step registration wizard data)

## Result Pattern

All domain operations return `Result<T>` or `Result` instead of throwing exceptions:

```csharp
// Success/Failure
var result = tour.AddBooking(customerId, companionId, totalPrice, notes);
if (result.IsFailure)
    return BadRequest(result.Error);

var booking = result.Value;
```

**Benefits**: Explicit error handling, no hidden exceptions, better testability.

## Business Rules

### Tour Rules
- End date must be after start date
- All prices must be positive (> 0)
- Identifier and name are required

### Booking Rules
- Customer and companion cannot be the same person
- Total price must be positive
- Valid state transitions only (Pending → Confirmed → Completed/Cancelled)
- Cannot update completed or cancelled bookings
- Cannot confirm cancelled bookings
- Cannot cancel completed bookings

### State Machine
```
Pending → Confirmed → Completed
   ↓          ↓
Cancelled ←───┘
(terminal states: Cancelled, Completed)
```

## Dependencies

- **ViajantesTurismo.Common**: Result pattern, shared enums

