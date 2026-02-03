# ADR-012: Booking Details Update After Creation

**Status**: Accepted — 2025-11-08

## Context

Customers may change accommodation (room type) or bike preferences after initial booking creation.
Forcing customers to cancel and recreate bookings for simple changes is poor UX and loses payment
history.

## Decision

Add a **`Booking.UpdateDetails()`** method allowing post-creation modifications:

```csharp
public Result UpdateDetails(
    RoomType roomType,
    decimal roomAdditionalCost,
    BookingCustomer principalCustomer,
    BookingCustomer? companionCustomer,
    Discount discount)
{
    if (Status is BookingStatus.Cancelled or BookingStatus.Completed)
        return BookingErrors.CannotModifyCancelledOrCompletedBooking(Id, Status);

    // Update properties with validation
    // TotalPrice recalculates automatically
}
```

**Can change**: room type, room cost, bikes, companion, discount
**Cannot change**: tour, principal customer, booking date, booking ID
**Blocked states**: Cancelled, Completed

## Consequences

### Pros

- No need to cancel/recreate bookings for simple changes — better UX.
- `TotalPrice` automatically recalculates with new selections (calculated property).
- Preserves payment history and booking ID.
- Customer flexibility improved (modify bookings as plans change).

### Cons

- UI more complex (edit forms, confirmation dialogs, warnings).
- Price changes may affect payment status (partially paid → unpaid).
- No audit trail of what changed (future enhancement: domain events or history table).

## Alternatives considered

- Cancel and recreate bookings — rejected due to lost payment history and poor UX.
- Allow updates in any status — rejected to preserve data integrity for completed/cancelled bookings.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
