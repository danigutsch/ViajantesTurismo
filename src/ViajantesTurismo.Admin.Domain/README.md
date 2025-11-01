# ViajantesTurismo.Admin.Domain

Domain logic for tour management system.

## Purpose

Core business logic for tours, bookings, and customers with business rule enforcement.

## Domain Entities

### Aggregate Roots

- **Tour**: Tour definitions with dates, pricing, services (**AGGREGATE ROOT**)
  - Contains: Booking collection
  - Responsibilities: Manages all booking operations and lifecycle
  
- **Customer**: Customer information (**AGGREGATE ROOT**)
  - Self-contained entity with no child entities

### Entities (Non-Root)

- **Booking**: Customer bookings with state machine (Pending → Confirmed → Completed/Cancelled)
  - **⚠️ IMPORTANT**: `Booking` has `internal` modifiers and can ONLY be modified through its aggregate root `Tour`
  - Direct modification of `Booking` entities is prohibited - all operations must go through `Tour` methods
  - Example: To update booking payment status, use `tour.UpdateBooking()`, not `booking.UpdatePaymentStatus()`

### Aggregate Design Pattern

This domain follows the **Aggregate Pattern** from Domain-Driven Design:

- **Tour is the Aggregate Root** for the Tour-Booking aggregate
- Bookings cannot exist without a Tour (consistency boundary)
- All changes to Bookings must go through Tour methods (invariant enforcement)
- External code should never hold references to Booking entities directly
- Tests must interact with Bookings only through the Tour aggregate root

**Aggregate Boundary Visualization:**
```
┌─────────────────────────────────────────────┐
│ Tour (Aggregate Root)                       │
│ ┌─────────────────────────────────────────┐ │
│ │ - Id, Identifier, Name                  │ │
│ │ - Dates, Pricing, Currency              │ │
│ │ - IncludedServices                      │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ ┌─────────────────────────────────────────┐ │
│ │ Bookings Collection                     │ │
│ │ ┌─────────────────────────────────────┐ │ │
│ │ │ Booking (Entity - internal)         │ │ │
│ │ │ - Status, PaymentStatus, Price      │ │ │
│ │ │ - CustomerId, CompanionId           │ │ │
│ │ └─────────────────────────────────────┘ │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ Public Methods (Aggregate Interface):       │
│ • AddBooking(...)                          │
│ • ConfirmBooking(bookingId)                │
│ • CancelBooking(bookingId)                 │
│ • UpdateBooking(bookingId, ...)            │
│ • RemoveBooking(bookingId)                 │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ Customer (Aggregate Root)                   │
│ - Self-contained, no child entities         │
└─────────────────────────────────────────────┘
```

```csharp
// ✅ CORRECT: Modify through aggregate root
tour.ConfirmBooking(bookingId);
tour.UpdateBooking(bookingId, price, notes, status, paymentStatus);

// ❌ INCORRECT: Direct modification (will not compile - methods are internal)
booking.Confirm();  // Compilation error
booking.UpdatePaymentStatus(PaymentStatus.Paid);  // Compilation error
```

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

