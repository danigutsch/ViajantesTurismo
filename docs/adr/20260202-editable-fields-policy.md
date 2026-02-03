# ADR-017: Editable Fields Policy for Bookings, Customers, and Tours

**Status**: Accepted — 2026-02-02

## Context

The Admin portal allows administrators to manage Bookings, Customers, and Tours. To maintain data integrity, financial
 accuracy, and operational safety, we need a clear policy on which fields remain editable after an entity has been
 created. Some fields are critical to the identity of the record or have downstream financial implications
 (e.g., Tour pricing after bookings exist).

## Decision

We establish the following editability rules across the system:

### 1. Bookings

- **Immutable**: `Customer` and `Companion` associations are locked after creation. Changing these would fundamentally
- change the nature of the booking.
- **Editable**: `Notes`, `Discount Type`, `Discount Amount`, and `Discount Reason`.
- **Audit Requirement**: Any `Discount` change requires a `Discount Reason` (10-500 characters).

### 2. Customers

- **Editable**: Personal data (Name, DOB, Gender, etc.), contact info, and physical measurements (Weight/Height)
 for logistics (bike sizing).
- **Immutable**: `Companion ID` is locked after creation to preserve group integrity.

### 3. Tours

- **Restricted**: `Tour Identifier` (slug), `Currency`, `Base Price`, and `Tour Dates` should be locked if any bookings
 already exist for the tour.
- **Editable**: `Tour Name`, `Included Services`, `Min/Max Customers`.

## Consequences

### Pros

- Prevents accidental changes that would break financial consistency.
- Ensures a mandatory audit trail for discounts.
- Maintains operational safety by allowing updates to medical/sizing information.
- Protects external links and reports by locking the Tour Identifier.

### Cons

- Administrators may occasionally need to bypass these rules for data correction (requires developer intervention or a
 specific "Super Admin" role in the future).
- Increased complexity in the Domain and UI layers to enforce these states.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-012: Booking Details Update After Creation](20251108-booking-details-update-after-creation.md)
