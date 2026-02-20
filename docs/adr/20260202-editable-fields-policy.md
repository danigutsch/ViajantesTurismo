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

- **Immutable**: `Customer` association is locked after creation. Changing the principal customer would fundamentally
  change the nature of the booking.
- **Editable**: `Companion`, `Notes`, `Discount Type`, `Discount Amount`, and `Discount Reason`. The companion may be
  changed before the tour takes place.
- **Audit Requirement**: Any `Discount` change requires a `Discount Reason` (10-500 characters).

### 2. Customers

- **Editable**: Personal data (Name, DOB, Gender, etc.), contact info, physical measurements (Weight/Height)
  for logistics (bike sizing), and `Companion ID` (a customer may change their preferred companion at any time).

### 3. Tours

- **Restricted**: `Tour Identifier` (slug) and `Currency` should be locked if any bookings
  already exist for the tour.
- **Warning**: `Tour Dates` (Start/End) are editable but the UI shows a warning when bookings exist,
  since schedule changes may affect booked customers.
- **Editable**: `Tour Name`, `Base Price`, `Single Room Supplement`, `Bike Prices`, `Included Services`,
  `Min/Max Customers`. Pricing fields are safe to update because bookings capture a price snapshot at creation time.

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
