# Discount as value object with audit trail

**Status**: Accepted — 2025-11-08

## Context

Bookings need flexible discount support for promotions, early bird pricing, and custom negotiations. Discounts must be traceable for audit and accounting purposes, and validation must ensure pricing integrity.

## Decision

Implement **`Discount` as a value object** owned by `Booking`:

```csharp
public sealed class Discount
{
    public DiscountType Type { get; }    // None, Percentage, Absolute
    public decimal Amount { get; }       // 0-100 for percentage, fixed for absolute
    public string? Reason { get; }       // Audit trail: "Early bird", "VIP", etc.
    
    public decimal CalculateDiscountAmount(decimal subtotal) { }
}
```

`DiscountType` enum: `None` (0), `Percentage` (1), `Absolute` (2)

## Consequences

### Pros

- Discount logic encapsulated in a single, testable value object.
- `Reason` field provides audit trail for compliance and reporting.
- Type-safe discount types prevent percentage/absolute confusion.
- Validation ensures discounts don't exceed subtotal or result in negative prices.
- Immutable — changes require creating a new `Discount` instance.

### Cons

- Cannot change discount type without creating a new `Discount` instance (intentional for immutability).
- `Reason` required when discount applied — adds form complexity in UI.

## Alternatives considered

- Store discount as two fields (Type, Amount) in `Booking` — rejected to encapsulate validation logic.
- Allow negative prices after discount — rejected for business rule integrity.

## Links

- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
