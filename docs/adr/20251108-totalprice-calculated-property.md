# ADR-008: TotalPrice as Calculated Property

**Status**: Accepted — 2025-11-08

## Context

Booking total price was stored in the database and could be manually updated, leading to inconsistencies
with actual component prices (base price, room supplement, bike prices, discounts).
No single source of truth existed for pricing logic.

## Decision

Make `TotalPrice` a **calculated property** (not stored in DB):

```csharp
public decimal TotalPrice => CalculateTotalPrice(
    BasePrice, RoomAdditionalCost, PrincipalCustomer, CompanionCustomer, Discount);
```

Formula: **`Subtotal - DiscountAmount`** where:

- `Subtotal = BasePrice + RoomAdditionalCost + PrincipalCustomer.BikePrice + (CompanionCustomer?.BikePrice ?? 0)`
- `DiscountAmount = Discount.CalculateDiscountAmount(Subtotal)`

EF Core configuration: `entity.Ignore(booking => booking.TotalPrice)`

## Consequences

### Pros

- Price is always correct and consistent with component prices.
- Single source of truth for pricing logic in domain.
- Cannot be manually overridden — prevents data integrity issues.
- Transparent calculation visible in code and to users.
- Price automatically updates when components change.

### Cons

- Cannot store historical prices if component prices change over time (future: consider price snapshots).
- Requires database migration to remove `TotalPrice` column.

## Alternatives considered

- Store `TotalPrice` in DB and recalculate on update — rejected due to risk of inconsistency.
- Snapshot prices at booking creation — deferred until historical pricing is required.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-009: Room Pricing Model](20251108-room-pricing-model.md)
- Related: [ADR-010: Discount as Value Object](20251108-discount-value-object.md)
