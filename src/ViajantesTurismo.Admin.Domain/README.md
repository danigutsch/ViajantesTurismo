# ViajantesTurismo.Admin.Domain

Core business logic for tours, bookings, and customers with domain rule enforcement.

## Aggregates

### Tour (Aggregate Root)

Manages tour definitions (dates, pricing, services) and owns all Booking operations.

**Operations**: `Create`, `UpdateBasicInfo`, `UpdateSchedule`, `UpdatePricing`, `UpdateIncludedServices`,
`AddBooking`, `ConfirmBooking`, `CancelBooking`, `CompleteBooking`,
`UpdateBookingNotes`, `UpdateBookingDiscount`, `UpdateBookingDetails`, `RecordPayment`

### Customer (Aggregate Root)

Customer profiles with personal, contact, medical, and accommodation information.

**Operations**: `Create`, `UpdatePersonalInfo`, `UpdateContactInfo`, `UpdateAddress`,
`UpdatePhysicalInfo`, `UpdateMedicalInfo`, `UpdateIdentificationInfo`,
`UpdateEmergencyContact`, `UpdateAccommodationPreferences`, `UpdateOccupation`

### Booking (Entity, internal)

Bookings are **owned by Tour** — all modifications go through Tour methods. Direct access is blocked by `internal`
modifiers.

```csharp
// ✅ Through aggregate root
tour.AddBooking(customerId, bikeType, ...);
tour.ConfirmBooking(bookingId);

// ❌ Direct modification blocked
```

**State machine:**

```text
Pending → Confirmed → Completed
   ↓          ↓
Cancelled ←───┘
(terminal: Cancelled, Completed)
```

## Patterns

- **Result pattern**: All operations return `Result<T>` / `Result` — no exceptions for business rules.
- **Factory methods**: Static `Create(...)` methods enforce valid state from construction.
- **Calculated properties**: `Subtotal`, `TotalPrice`, `AmountPaid`, `RemainingBalance` — always derived, never stored.
- **Dedicated error classes**: `BookingErrors`, `TourErrors`, `CustomerErrors` with typed factory methods.

## Pricing Model

`TotalPrice = (BasePrice + RoomSupplement + Bike1 + Bike2) − Discount`

- Base price is for double occupancy (not per person).
- Single room adds the supplement.
- Bikes are per-person charges.

## Dependencies

- **ViajantesTurismo.Common** — Result pattern, base types
- **ViajantesTurismo.Admin.Contracts** — Validation constants

## See Also

- [Domain Validation](../../docs/DOMAIN_VALIDATION.md) — Complete business rules reference
- [Aggregates](../../docs/domain/AGGREGATES.md) — Invariants and design rationale
- [Glossary](../../docs/domain/GLOSSARY.md) — Enums, value objects, terminology
- [Architecture Decisions](../../docs/ARCHITECTURE_DECISIONS.md)
