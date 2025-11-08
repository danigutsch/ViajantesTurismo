# ADR-011: Payment Tracking with Immutable Payment Records

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Bookings need to track multiple payments over time (down payment, installments, final payment). Payment records must be
immutable for audit and accounting purposes.

## Decision

Implement `Payment` as immutable entity with full audit trail:

```csharp
public sealed class Payment : Entity<long>
{
    public long BookingId { get; }
    public decimal Amount { get; }
    public DateTime PaymentDate { get; }
    public PaymentMethod Method { get; }  // CreditCard, BankTransfer, Cash, Check, PayPal, Other
    public string? ReferenceNumber { get; }
    public string? Notes { get; }
    public DateTime RecordedAt { get; }  // Audit: when payment was recorded in system
}
```

Payment status automatically calculated:

- `Unpaid`: No payments recorded
- `PartiallyPaid`: AmountPaid < TotalPrice
- `Paid`: AmountPaid >= TotalPrice

## Consequences

### Positive

- Complete payment history preserved
- Cannot edit/delete payments (immutability ensures audit integrity)
- Automatic payment status transitions
- Prevents overpayment (validation: amount <= remaining balance)
- Multiple payment methods supported

### Negative

- Cannot correct payment errors (must add offsetting payment)
- No refund workflow (would need separate Refund entity)

## Implementation

- Payments stored in separate collection: `Booking._payments`
- `AmountPaid` calculated property: `_payments.Sum(p => p.Amount)`
- `RemainingBalance` calculated property: `TotalPrice - AmountPaid`
- `TimeProvider` parameter for testable timestamps
- `RecordedAt` captures when payment entered into system
