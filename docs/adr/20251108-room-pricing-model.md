# ADR-009: Room Pricing Model - Base Price = Double Occupancy

**Status**: Superseded — 2026-02-15 (originally accepted 2025-11-08)

## Context

Initial implementation incorrectly modeled room pricing. The original ADR inverted the pricing model,
treating single rooms as the base and adding a supplement for double rooms. The industry standard is:

- **Double room** (shared occupancy) = base price, no supplement
- **Single room** (solo occupancy) = base price + single room supplement

## Decision

**Base price represents double occupancy (shared room) cost**:

- **Double room**: Base price + 0 supplement (standard, shared room)
- **Single room**: Base price + single room supplement (solo traveler surcharge)
- **Companion** does NOT multiply base price — only adds their bike cost

Code property renamed to `SingleRoomSupplementPrice`. DB column retains legacy name `DoubleRoomSupplementPrice`.
Enum values renamed: `SingleRoom` → `SingleOccupancy`, `DoubleRoom` → `DoubleOccupancy`, `None` removed.

## Consequences

### Pros

- Correct industry-standard model: single rooms cost MORE than double rooms (solo traveler surcharge).
- Transparent pricing for customers — clear itemization.
- Simpler calculation logic — no customer count multiplication.
- Enum names match industry terminology.

### Cons

- Breaking change requiring database migration and seed data updates.
- All existing tests needed updates.
- DB column retains legacy name for migration compatibility.

## Alternatives considered

- Keep per-person pricing model — rejected as it doesn't match tour operator business model.
- Different base prices for single/double — rejected to avoid duplicating base price in database.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
