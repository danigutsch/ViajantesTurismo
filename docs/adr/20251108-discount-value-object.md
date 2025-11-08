# ADR-010: Discount as Value Object with Audit Trail

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Bookings need flexible discount support for promotions, early bird pricing, and custom negotiations. Discounts must be
traceable for audit purposes.

## Decision

Implement `Discount` as a value object owned by `Booking`:

```csharp
public sealed class Discount
{
    public DiscountType Type { get; }  // None, Percentage, Absolute
    public decimal Amount { get; }     // 0-100 for percentage, fixed amount for absolute
    public string? Reason { get; }     // Audit trail: "Early bird", "VIP customer", etc.
    
    public decimal CalculateDiscountAmount(decimal subtotal) { }
}
```

## Consequences

### Positive

- Discount logic encapsulated in single value object
- Reason field provides audit trail
- Type-safe discount types (percentage vs absolute)
- Validation ensures discounts don't exceed subtotal or result in negative price

### Negative

- Cannot change discount type without creating new Discount instance
- Reason required when discount applied (adds form complexity)

## Implementation

- `DiscountType` enum: None (0), Percentage (1), Absolute (2)
- Percentage validation: 0-100%
- Absolute validation: cannot exceed subtotal
- Final price validation: must be > 0 after discount
- UI shows real-time discount calculation

## Links

- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
