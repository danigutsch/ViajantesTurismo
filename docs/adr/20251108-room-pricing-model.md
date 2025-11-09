# Room pricing model — base price equals single room

**Status**: Accepted — 2025-11-08

## Context

Initial implementation incorrectly modeled room pricing by multiplying base price by customer count and adding a supplement for single rooms. This inverted the actual business model where double rooms cost more than single rooms.

## Decision

**Base price represents a single room cost**:

- **Single room**: Base price + 0 supplement
- **Double room**: Base price + double room supplement
- **Companion** does NOT multiply base price — only adds their bike cost

Rename `SingleRoomSupplementPrice` → `DoubleRoomSupplementPrice` to clarify the model.

## Consequences

### Pros

- Correct business model: double rooms cost MORE than single rooms (reflects reality).
- Transparent pricing for customers — clear itemization.
- Simpler calculation logic — no customer count multiplication.

### Cons

- Breaking change requiring database migration and seed data updates.
- All existing tests needed updates.

## Alternatives considered

- Keep per-person pricing model — rejected as it doesn't match tour operator business model.
- Different base prices for single/double — rejected to avoid duplicating base price in database.

## Links

- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
