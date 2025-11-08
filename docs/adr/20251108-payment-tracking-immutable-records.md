# Payment tracking with immutable payment records

**Status**: Accepted — 2025-11-08

## Context
Bookings must track multiple payments over time (down payment, installments, final payment). Payment records must be immutable for audit, accounting, and compliance purposes. Payment history is critical for financial reconciliation.

## Decision
Implement **`Payment` as an immutable entity** with full audit trail:
```csharp
public sealed class Payment : Entity<long>
{
    public long BookingId { get; }
    public decimal Amount { get; }
    public DateTime PaymentDate { get; }
    public PaymentMethod Method { get; }      // Enum: CreditCard, BankTransfer, Cash, etc.
    public string? ReferenceNumber { get; }
    public string? Notes { get; }
    public DateTime RecordedAt { get; }       // Audit: when entered into system
}
```
Payment **status automatically calculated** based on `AmountPaid` vs `TotalPrice`:
- `Unpaid`: No payments recorded
- `PartiallyPaid`: `AmountPaid < TotalPrice`
- `Paid`: `AmountPaid >= TotalPrice`

## Consequences
**Pros**
- Complete payment history preserved — no deletions allowed.
- Immutability ensures audit integrity (cannot edit/delete payments).
- Automatic payment status transitions based on calculated totals.
- Prevents overpayment with validation: `amount <= remaining balance`.
- Multiple payment methods supported.
- `RecordedAt` timestamp provides system audit trail.

**Cons**
- Cannot correct payment errors — must add offsetting (reversal) payment.
- No refund workflow built in (future: requires separate `Refund` entity or negative payments).

## Alternatives considered
- Allow payment edits/deletes — rejected due to audit requirements.
- Single payment per booking — rejected as it doesn't support installments.
- Store payment status in DB — rejected in favor of calculated property for consistency.

## Links
- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
