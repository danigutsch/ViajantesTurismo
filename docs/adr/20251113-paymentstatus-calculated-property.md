# PaymentStatus as calculated property

**Status**: Accepted — 2025-11-13

## Context

Booking payment status is currently stored in the database as a persisted property. This creates potential
inconsistencies between the stored status and the actual payment state calculated from the payments collection.

The payment status should always reflect the current state:

- `Unpaid`: No payments recorded (AmountPaid == 0)
- `PartiallyPaid`: Some payments recorded but total < TotalPrice (0 < AmountPaid < TotalPrice)
- `Paid`: Full payment received (AmountPaid >= TotalPrice)

Storing this state separately from the source data (payments collection) violates the single source of truth principle.

## Decision

Make `PaymentStatus` a **calculated property** (not stored in DB):

```csharp
public PaymentStatus PaymentStatus
{
    get
    {
        var amountPaid = AmountPaid;
        var totalPrice = TotalPrice;

        return amountPaid switch
        {
            0 => PaymentStatus.Unpaid,
            _ when amountPaid >= totalPrice => PaymentStatus.Paid,
            _ => PaymentStatus.PartiallyPaid
        };
    }
}
```

EF Core configuration: `entity.Ignore(booking => booking.PaymentStatus)`

Remove `UpdatePaymentStatus()` method and `UpdatePaymentStatusFromPayments()` private method as they are
no longer needed.

## Consequences

### Pros

- Payment status is always correct and consistent with actual payments.
- Single source of truth: payments collection drives status.
- Cannot be manually set to incorrect value — prevents data integrity issues.
- Transparent calculation visible in code.
- Status automatically updates when payments are added.
- Eliminates bug where status wasn't being persisted correctly after second payment.

### Cons

- Cannot query directly on PaymentStatus in database (requires filtering in memory or using computed columns).
- Requires database migration to remove `PaymentStatus` column.
- May need to add database index on related columns if filtering by status becomes a performance concern.

## Alternatives considered

1. **Keep stored status with better change tracking** — Rejected because it still allows inconsistency.
2. **Use database computed column** — Considered for future optimization if query performance requires it.
3. **Event sourcing approach** — Overkill for current requirements.

## Migration strategy

1. Update domain model to make PaymentStatus a calculated property.
2. Update EF Core configuration to ignore PaymentStatus.
3. Remove UpdatePaymentStatus() and UpdatePaymentStatusFromPayments() methods.
4. Update Tour.cs RecordPayment endpoint to not call UpdatePaymentStatus.
5. Create migration to drop PaymentStatus column.
6. Update all tests to expect calculated status.
7. Verify QueryService still maps status correctly (from calculated property).

## Links

- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
- Related: [ADR-011: Payment Tracking with Immutable Records](20251108-payment-tracking-immutable-records.md)
