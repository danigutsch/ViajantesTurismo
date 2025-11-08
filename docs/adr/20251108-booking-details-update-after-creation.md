# ADR-012: Booking Details Update After Creation

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Customers may change accommodation or bike preferences after initial booking. Bookings should be modifiable unless in
terminal state (Cancelled/Completed).

## Decision

Add `Booking.UpdateDetails()` method allowing post-creation modifications:

```csharp
public Result UpdateDetails(
    RoomType roomType,
    decimal roomAdditionalCost,
    BookingCustomer principalCustomer,
    BookingCustomer? companionCustomer,
    Discount discount)
{
    // Validate not in terminal state
    if (Status is BookingStatus.Cancelled or BookingStatus.Completed)
        return BookingErrors.CannotModifyCancelledOrCompletedBooking(Id, Status);
    
    // Update properties with validation
    // Price recalculates automatically
}
```

## Consequences

### Positive

- No need to cancel/recreate bookings for simple changes
- Price automatically recalculates with new selections
- Preserves payment history and booking ID
- Customer experience improved (flexible changes)

### Negative

- UI more complex (edit forms, confirmation dialogs)
- Price changes may affect payment status
- No audit trail of what changed (future enhancement needed)

## Implementation

- `PUT /bookings/{id}/details` endpoint
- Can change: room type, bikes, companion, discount
- Cannot change: tour, principal customer, booking date
- Terminal states (Cancelled/Completed) prevent all modifications
- UI shows warning when removing companion from double room
