# Aggregates & Invariants

This document catalogs all aggregates across ViajantesTurismo bounded contexts. Each aggregate is documented with its
purpose, invariants, commands, events, and related artifacts.

## Documentation Template

When documenting a new aggregate, follow this structure:

```markdown
## AggregateName (BoundedContext)

**Purpose**: Brief description of business capability protected by this aggregate

### Invariants

Business rules that must always hold true, organized by entity:

- Rule 1 (describe the constraint)
- Rule 2 (include valid ranges, formats, relationships)
- State transition rules (if applicable)

### Commands

List all state-changing operations:

- Create — Description
- Update* — Description
- Delete — Description (conditions)

### Events

Domain events emitted on state changes:

- EventName1, EventName2, EventName3

### Entities

Child entities within the aggregate boundary:

- EntityName (description or state machine)

### Value Objects

Immutable value objects used by the aggregate:

- ValueObjectName1, ValueObjectName2

### Related

Links to feature files, ADRs, and other documentation:

- Features: `path/to/*.feature`
- ADRs: [ADR Title](../adr/filename.md)
```

---

## Admin Bounded Context

The Admin context manages tour operations, customer information, and booking lifecycle.

### Tour

**Purpose**: Manage cycling tour offerings, schedules, pricing, capacity, and all associated bookings.

#### Invariants

**Tour Entity:**

- Tour identifier must be unique (application-level) and non-empty (max 128 chars)
- Tour name required (max 128 chars)
- End date must be after start date with minimum 5 days duration
- All prices must be > 0 and <= 100,000 (strictly positive)
- Capacity: 1 <= minCustomers <= maxCustomers <= 20
- Cannot reduce maximum capacity below current confirmed bookings
- Bookings can only be created/modified through Tour methods (aggregate boundary)
- Cannot delete tour with confirmed bookings

**Booking Entity:**

- Base price: Must be > 0 and <= 100,000
- Room additional cost: Must be >= 0 and <= 100,000
- Notes: Max 2000 characters
- BikeType: Cannot be `BikeType.None` for principal or companion
- Companion: Cannot be same as principal customer
- Companion bike: Cannot specify companion bike type without a companion customer
- Discount: If absolute, cannot exceed subtotal
- Final price: Must be > 0 after discount
- Percentage discount: Cannot exceed 100%
- Cannot modify Cancelled or Completed bookings (returns Conflict status)
- Bookings can only be removed if status is Pending

**State Transitions:**

- Pending → Confirmed: Allowed
- Pending → Cancelled: Allowed
- Confirmed → Completed: Allowed
- Confirmed → Cancelled: Allowed
- Cancelled → *: Blocked (terminal state)
- Completed → *: Blocked (terminal state)

**Payment Entity:**

- Amount: Must be > 0
- Amount: Cannot exceed remaining balance
- Payment date: Cannot be in the future
- Payment method: Must be valid enum value (Other, CreditCard, BankTransfer, Cash, Check, PayPal)

#### Commands (Tour)

- Create — Initialize tour with identifier, name, schedule, pricing, capacity, included services
- UpdateDetails — Modify identifier and name
- UpdateSchedule — Change start and end dates
- UpdatePricing — Modify base price, room supplements, bike prices, currency
- UpdateBasePrice — Change base price only
- UpdateCapacity — Adjust minimum and maximum customer limits
- UpdateIncludedServices — Modify list of included services
- Delete — Remove tour from system (only if no confirmed bookings exist)

#### Commands (Booking Lifecycle via Tour)

- AddBooking — Create new booking with customer, bike, room, discount details
- ConfirmBooking — Transition booking from Pending to Confirmed
- CancelBooking — Transition booking to Cancelled status
- CompleteBooking — Transition booking from Confirmed to Completed
- UpdateBookingNotes — Modify booking administrative notes
- UpdateBookingDiscount — Change discount type, amount, and reason
- UpdateBookingDetails — Modify room type, bike selections, companion
- UpdateBookingPaymentStatus — Change payment status (deprecated — use RecordPayment)
- RemoveBooking — Delete a booking (only if Pending)

#### Commands (Payment via Tour → Booking)

- RecordPayment — Record payment with amount, date, method, reference, notes

#### Events

- TourCreated, TourDetailsUpdated, TourScheduleUpdated, TourPricingUpdated
- BookingAdded, BookingConfirmed, BookingCancelled, BookingCompleted
- BookingDiscountUpdated, BookingDetailsUpdated
- PaymentRecorded, PaymentStatusChanged

#### Entities

- Booking (state machine: Pending → Confirmed → Completed/Cancelled)
- Payment (immutable financial records)
- BookingCustomer (embedded entity for principal/companion)

#### Value Objects

- DateRange, TourPricing, TourCapacity, Discount

#### Related

- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Tour*.feature`
- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Booking*.feature`
- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Payment*.feature`
- ADRs: [Domain Validation with Factory Methods](../adr/20251108-domain-validation-factory-methods.md)
- ADRs: [Result Pattern Over Exceptions](../adr/20251108-result-pattern-over-exceptions.md)

### Customer

**Purpose**: Represent customer profiles with comprehensive personal, contact, identification, physical, medical, and
accommodation information.

#### Invariants

**Personal Information:**

- FirstName, LastName: Not empty, max 128 characters
- Age: Must be at least 10 years old (calculated from birth date)
- Birth date: Cannot be in the future
- Gender: Required (max 64 chars)
- Nationality: Required (max 128 chars)
- Occupation: Required (max 128 chars)

**Contact Information:**

- Email: Must match format `^[^@\s]+@[^@\s]+\.[^@\s]+$` (no spaces allowed), max 256 characters
- Email: Must be unique (application-level invariant enforced in handlers via repository checks)
- Mobile phone: Required (max 64 chars)
- Phone: Must match format `^[\d\s\-\(\)\+]+$` (digits, spaces, hyphens, parentheses, plus sign)
- Instagram and Facebook handles: Optional (max 64 chars each)

**Address Information:**

- Street, neighborhood, postal code, city, state, country: Required (max 128 chars each)
- Address complement: Optional (max 128 chars)
- Postal code: Max 64 chars

**Physical Information:**

- Weight: 1-500 kg
- Height: 50-300 cm
- Bike preference and shoe size tracked

**Identification:**

- National ID and issuing nationality: Required (max 64 chars each)
- Passport information validated

**Medical & Emergency:**

- Emergency contact name and mobile: Required (max 128 and 64 chars respectively)
- Allergies and additional medical info: Optional (max 500 chars each)

#### Validation

All value objects validated on creation and update.

#### Commands

- Create — Initialize customer with all required value objects
- UpdatePersonalInfo — Modify name, DOB, gender, nationality, occupation
- UpdateContactInfo — Change email, mobile, social media contacts
- UpdateAddress — Update street, city, state, postal code, country
- UpdatePhysicalInfo — Modify height, weight, bike preference, shoe size
- UpdateIdentificationInfo — Change passport number, issue/expiry dates, country
- UpdateMedicalInfo — Update conditions, allergies, medications, dietary restrictions
- UpdateEmergencyContact — Modify emergency contact person and phone numbers
- UpdateAccommodationPreferences — Change room and dietary preferences
- UpdateOccupation — Modify occupation details

#### Events

- CustomerCreated
- CustomerPersonalInfoUpdated, CustomerContactInfoUpdated
- CustomerAddressUpdated, CustomerPhysicalInfoUpdated
- CustomerIdentificationInfoUpdated, CustomerMedicalInfoUpdated
- CustomerEmergencyContactUpdated, CustomerAccommodationPreferencesUpdated

#### Entities

- None (self-contained aggregate)

#### Value Objects

- PersonalInfo, ContactInfo, Address
- PhysicalInfo, IdentificationInfo, MedicalInfo
- EmergencyContact, AccommodationPreferences, Occupation

#### Related

- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Customer*.feature`
- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/*Validation.feature`
- ADRs: [Domain Validation with Factory Methods](../adr/20251108-domain-validation-factory-methods.md)

---

## Future Bounded Contexts

As new bounded contexts are added to the system, document their aggregates here following the template above.

**Potential contexts to add:**

- **Sales/Marketing** — Public tour catalog, promotional campaigns, customer inquiries
- **Operations** — Guide scheduling, equipment management, logistics
- **Accounting** — Financial reporting, invoicing, expense tracking
- **Analytics** — Business intelligence, customer insights, tour performance
