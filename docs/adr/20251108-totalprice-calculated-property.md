# ADR-008: TotalPrice as Calculated Property

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Booking total price was stored in the database and could be manually updated, leading to inconsistencies with actual
component prices (base price, room cost, bike prices).

## Decision

Make `TotalPrice` a calculated property:

```csharp
public decimal TotalPrice => CalculateTotalPrice(BasePrice, RoomAdditionalCost, PrincipalCustomer, CompanionCustomer);
```

## Consequences

### Positive

- Price is always correct and consistent
- Single source of truth for pricing logic
- Cannot be manually overridden
- Transparent calculation visible to users

### Negative

- Cannot store historical prices if components change
- Migration required to remove TotalPrice column

## Implementation

- Removed `TotalPrice` column from database
- Added `entity.Ignore(booking => booking.TotalPrice)` in EF Core configuration
- Formula: `Subtotal - DiscountAmount` where:
    - `Subtotal = BasePrice + RoomAdditionalCost + PrincipalCustomer.BikePrice + CompanionCustomer?.BikePrice ?? 0`
    - `DiscountAmount = Discount.CalculateDiscountAmount(Subtotal)`
- Base price is for single room (not per person)
- Double room adds supplement cost
- Discounts applied after subtotal calculation

## Links

- Related: [ADR-009: Room Pricing Model](20251108-room-pricing-model.md)
- Related: [ADR-010: Discount as Value Object](20251108-discount-value-object.md)
