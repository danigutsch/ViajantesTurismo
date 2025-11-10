# Aggregates & Invariants

Document each aggregate with purpose, invariants, commands, and events.

---

## Tour (Admin)

**Purpose**: Manage cycling tour offerings, schedules, pricing, capacity, and all associated bookings.

### Invariants

- Tour identifier must be unique and non-empty (max 128 chars)
- Tour name required (max 128 chars)
- End date must be after start date with minimum 5 days duration
- All prices must be > 0 and <= 100,000 (strictly positive)
- Capacity: 1 <= minCustomers <= maxCustomers <= 20
- Cannot exceed maximum customer capacity (confirmed bookings only count)
- Bookings can only be created/modified through Tour methods (aggregate boundary)
- Cannot delete tour with confirmed bookings
- Principal and companion customers cannot be the same person
- BikeType.None cannot be used for bookings (must select Regular or EBike)
- Bookings cannot be modified if status is Cancelled or Completed
- Bookings can only be removed if status is Pending
- Payment amount cannot exceed remaining balance
- Payment date cannot be in the future
- Absolute discount cannot exceed booking subtotal
- Final price after discount must be positive
- Percentage discount cannot exceed 100%

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

- Email must be unique and valid format (max 128 chars)
- Birth date cannot be in the future
- Contact information properly formatted and valid
- All value objects validated on creation and update
- Personal information required (name, DOB, nationality, profession)
- First name and last name required (max 128 chars each)
- Gender required (max 64 chars)
- Nationality required (max 128 chars)
- Profession required (max 128 chars)
- Email and mobile phone required
- Mobile phone max 64 chars
- Instagram and Facebook handles max 64 chars (optional)
- Physical characteristics: weight 1-500 kg, height 50-300 cm
- National ID and issuing nationality required (max 64 chars each)
- Address fields required: street, neighborhood, postal code, city, state, country (max 128 chars)
- Address complement optional (max 128 chars)
- Postal code max 64 chars
- Emergency contact name and mobile required (max 128 and 64 chars respectively)
- Medical info fields optional (allergies and additional info max 500 chars each)

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
