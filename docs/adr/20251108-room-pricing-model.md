# ADR-009: Room Pricing Model - Base Price = Single Room

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Initial implementation incorrectly multiplied base price by customer count and added supplement for single rooms.

## Decision

Base price represents a single room cost:

- Single room: Base price + 0 supplement
- Double room: Base price + double room supplement
- Companion does NOT multiply base price, only adds bike cost

## Consequences

### Positive

- Correct business model: double rooms cost MORE than single rooms
- Transparent pricing for customers
- Simpler calculation logic

### Negative

- Breaking change requiring data migration
- All existing tests needed updates

## Implementation

- Changed `SingleRoomSupplementPrice` → `DoubleRoomSupplementPrice`
- Updated `CalculateRoomAdditionalCost()` to return supplement only for double rooms
- Removed customer count multiplication from price calculation

## Links

- Related: [ADR-008: TotalPrice as Calculated Property](20251108-totalprice-calculated-property.md)
