# Aggregates & Invariants

Document each aggregate with purpose, invariants, commands, and events.

---

## Tour (Admin)

**Purpose**: Manage cycling tour offerings, schedules, pricing, capacity, and all associated bookings.

### Invariants

- Tour identifier must be unique and non-empty (max 128 chars)
- Tour name required (max 128 chars)
- End date must be after start date with minimum 5 days duration
- All prices must be >= 0 and <= 100,000
- Capacity: 1 <= minCustomers <= maxCustomers <= 100
- Cannot exceed maximum customer capacity (confirmed bookings only count)
- Bookings can only be created/modified through Tour methods (aggregate boundary)
- Cannot delete tour with confirmed bookings

### Commands (Tour)

- Create — Initialize tour with identifier, name, schedule, pricing, capacity, included services
- UpdateDetails — Modify identifier and name
- UpdateSchedule — Change start and end dates
- UpdatePricing — Modify base price, room supplements, bike prices, currency
- UpdateBasePrice — Change base price only
- UpdateCapacity — Adjust minimum and maximum customer limits
- UpdateIncludedServices — Modify list of included services

### Commands (Booking Lifecycle via Tour)

- AddBooking — Create new booking with customer, bike, room, discount details
- ConfirmBooking — Transition booking from Pending to Confirmed
- CancelBooking — Transition booking to Cancelled status
- CompleteBooking — Transition booking from Confirmed to Completed
- UpdateBookingNotes — Modify booking administrative notes
- UpdateBookingDiscount — Change discount type, amount, and reason
- UpdateBookingDetails — Modify room type, bike selections, companion
- UpdateBookingPaymentStatus — Change payment status (deprecated — use RecordPayment)
- RemoveBooking — Delete a booking (only if Pending)

### Commands (Payment via Tour → Booking)

- RecordPayment — Record payment with amount, date, method, reference, notes

### Events

- TourCreated, TourDetailsUpdated, TourScheduleUpdated, TourPricingUpdated
- BookingAdded, BookingConfirmed, BookingCancelled, BookingCompleted
- BookingDiscountUpdated, BookingDetailsUpdated
- PaymentRecorded, PaymentStatusChanged

### Entities

- Booking (state machine: Pending → Confirmed → Completed/Cancelled)
- Payment (immutable financial records)
- BookingCustomer (embedded entity for principal/companion)

### Value Objects

- DateRange, TourPricing, TourCapacity, Discount

### Related

- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Tour*.feature`
- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Booking*.feature`
- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Payment*.feature`
- ADRs: [Domain Validation with Factory Methods](../adr/20251108-domain-validation-factory-methods.md)
- ADRs: [Result Pattern Over Exceptions](../adr/20251108-result-pattern-over-exceptions.md)

---

## Customer (Admin)

**Purpose**: Represent customer profiles with comprehensive personal, contact, identification, physical, medical, and
accommodation information.

### Invariants

- Email must be unique and valid format
- Customer must be 18+ years old
- Contact information properly formatted and valid
- All value objects validated on creation and update
- Personal information required (name, DOB, nationality)

### Commands

- Create — Initialize customer with all required value objects
- UpdatePersonalInfo — Modify name, DOB, gender, nationality, profession
- UpdateContactInfo — Change email, mobile, social media contacts
- UpdateAddress — Update street, city, state, postal code, country
- UpdatePhysicalInfo — Modify height, weight, bike preference, shoe size
- UpdateIdentificationInfo — Change passport number, issue/expiry dates, country
- UpdateMedicalInfo — Update conditions, allergies, medications, dietary restrictions
- UpdateEmergencyContact — Modify emergency contact person and phone numbers
- UpdateAccommodationPreferences — Change room and dietary preferences
- UpdateProfession — Modify occupation details

### Events

- CustomerCreated
- CustomerPersonalInfoUpdated, CustomerContactInfoUpdated
- CustomerAddressUpdated, CustomerPhysicalInfoUpdated
- CustomerIdentificationInfoUpdated, CustomerMedicalInfoUpdated
- CustomerEmergencyContactUpdated, CustomerAccommodationPreferencesUpdated

### Entities

- None (self-contained aggregate)

### Value Objects

- PersonalInfo, ContactInfo, Address
- PhysicalInfo, IdentificationInfo, MedicalInfo
- EmergencyContact, AccommodationPreferences, Profession

### Related

- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/Customer*.feature`
- Features: `tests/ViajantesTurismo.Admin.BehaviorTests/specs/*Validation.feature`
- ADRs: [Domain Validation with Factory Methods](../adr/20251108-domain-validation-factory-methods.md)
