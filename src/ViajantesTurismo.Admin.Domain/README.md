# ViajantesTurismo.Admin.Domain

Domain logic for tour management system.

## Purpose

Core business logic for tours, bookings, and customers with business rule enforcement.

## Domain Entities

### Aggregate Roots

- **Tour**: Tour definitions with dates, pricing, services (**AGGREGATE ROOT**)
  - Contains: Booking collection
  - Responsibilities: Manages all booking operations and lifecycle
  
- **Customer**: Customer information (**AGGREGATE ROOT**)
  - Self-contained entity with no child entities

### Entities (Non-Root)

- **Booking**: Customer bookings with state machine (Pending → Confirmed → Completed/Cancelled)
  - **⚠️ IMPORTANT**: `Booking` has `internal` modifiers and can ONLY be modified through its aggregate root `Tour`
  - Direct modification of `Booking` entities is prohibited - all operations must go through `Tour` methods
  - Example: To update booking payment status, use `tour.UpdateBooking()`, not `booking.UpdatePaymentStatus()`

### Aggregate Design Pattern

This domain follows the **Aggregate Pattern** from Domain-Driven Design:

- **Tour is the Aggregate Root** for the Tour-Booking aggregate
- Bookings cannot exist without a Tour (consistency boundary)
- All changes to Bookings must go through Tour methods
- Payments managed through Booking, accessed via Tour

**Correct Usage:**
```csharp
// ✅ Modify through aggregate root
var result = tour.AddBooking(customerId, bikeType, companionId, companionBikeType, roomType, ...);
tour.ConfirmBooking(bookingId);
tour.RecordPayment(bookingId, amount, date, method, timeProvider, ...);

// ❌ Direct booking modification blocked by internal modifiers
```

### Value Objects

## Result Pattern

All domain operations return `Result<T>` or `Result` instead of throwing exceptions:

```csharp
// Success/Failure
var result = tour.AddBooking(customerId, companionId, totalPrice, notes);
if (result.IsFailure)
    return BadRequest(result.Error);

var booking = result.Value;
```

**Benefits**: Explicit error handling, no hidden exceptions, better testability.

### Value Objects

- **BookingCustomer**: Customer ID, bike type, bike price (immutable)
- **Discount**: Type (None/Percentage/Absolute), Amount, Reason (immutable)
- **Customer Value Objects** (all immutable):
    - PersonalInfo, ContactInfo, Address, PhysicalInfo, MedicalInfo
    - IdentificationInfo, EmergencyContact, AccommodationPreferences, Profession

### Enumerations

All with exhaustive `Enum.IsDefined()` validation:

- BikeType: None (0), Regular, EBike
- RoomType: SingleRoom (0), DoubleRoom
- BookingStatus: Pending (0), Confirmed, Cancelled, Completed
- PaymentStatus: Unpaid (0), PartiallyPaid, Paid
- PaymentMethod: Other (0), CreditCard, BankTransfer, Cash, Check, PayPal
- DiscountType: None (0), Percentage, Absolute
- Currency: Euro (0), UsDollar, Real

## Business Rules

### Tour Rules

- End date must be after start date (minimum 5 days duration)
- All prices must be >= 0 and <= 100,000
- Identifier and name required (max 128 characters)
- Cannot delete tour with confirmed bookings

### Booking Rules
- Customer and companion cannot be the same person
- BikeType.None not allowed (must select Regular or EBike)
- Base price must be > 0
- Final price after discount must be > 0
- Discount validation:
    - Percentage: 0-100%
    - Absolute: Cannot exceed subtotal
- Room type validation:
    - Single room: No companion allowed
    - Double room: Companion optional (supports single occupancy)
- State transitions (idempotent):
    - Pending → Confirmed, Cancelled
    - Confirmed → Completed, Cancelled
    - Cancelled/Completed: Terminal states (no further changes)
- Cannot modify Cancelled or Completed bookings
- Cannot record payments for Cancelled or Completed bookings

### Payment Rules

- Amount must be > 0
- Cannot exceed remaining balance
- Payment date cannot be in future
- Payment method must be valid enum value
- Immutable: Cannot edit or delete after creation
- Automatic status updates:
    - AmountPaid = 0 → Unpaid
    - 0 < AmountPaid < TotalPrice → PartiallyPaid
    - AmountPaid >= TotalPrice → Paid

### Pricing Model

- **Base Price**: Cost for single room (NOT per person)
- **Double Room**: Base price + double room supplement
- **Bikes**: Separate per-person charges
- **Calculation**: `(BasePrice + RoomCost + Bike1 + Bike2) - Discount`

### State Machine
```
Pending → Confirmed → Completed
   ↓          ↓
Cancelled ←───┘
(terminal states: Cancelled, Completed)
```

## Domain Patterns

### Factory Methods

All aggregate roots use static factory methods returning `Result<T>`:

```csharp
public static Result<Tour> Create(string identifier, string name, ...)
{
    if (string.IsNullOrWhiteSpace(identifier))
        return TourErrors.EmptyIdentifier();
    
    return new Tour(identifier, name, ...);
}

private Tour(...) { /* Constructor */ }
[UsedImplicitly] private Tour() { /* EF Core */ }
```

### Calculated Properties

Derived values always calculated, never stored:

```csharp
public decimal Subtotal => CalculateSubtotal(...);
public decimal TotalPrice => Subtotal - Discount.CalculateDiscountAmount(Subtotal);
public decimal AmountPaid => _payments.Sum(p => p.Amount);
public decimal RemainingBalance => TotalPrice - AmountPaid;
```

### Error Classes

Dedicated error classes per entity:

```csharp
public static class BookingErrors
{
    public static Result<Booking> ZeroOrNegativeBasePrice(decimal price) { }
    public static Result CannotModifyCancelledOrCompletedBooking(long id, BookingStatus status) { }
}
```

## Domain Operations

### Tour Operations

- `Create()`, `UpdateBasicInfo()`, `UpdateSchedule()`, `UpdatePricing()`, `UpdateIncludedServices()`
- `AddBooking()`, `ConfirmBooking()`, `CancelBooking()`, `CompleteBooking()`
- `UpdateBookingNotes()`, `UpdateBookingDiscount()`, `UpdateBookingDetails()`
- `RecordPayment()`

### Booking Operations (via Tour)

- State transitions: `Confirm()`, `Cancel()`, `Complete()`
- Modifications: `UpdateNotes()`, `UpdateDiscount()`, `UpdateDetails()`
- Payment: `RecordPayment()`

### Customer Operations

- `Create()`, `UpdatePersonalInfo()`, `UpdateContactInfo()`, `UpdateAddress()`
- `UpdatePhysicalInfo()`, `UpdateMedicalInfo()`, `UpdateIdentificationInfo()`
- `UpdateEmergencyContact()`, `UpdateAccommodationPreferences()`, `UpdateProfession()`

## Dependencies

- **ViajantesTurismo.Common**: Result pattern, base types, sanitizers
- **ViajantesTurismo.AdminApi.Contracts**: Validation constants (`ContractConstants`)

## See Also

- [Architecture Decisions](../../docs/ARCHITECTURE_DECISIONS.md)
- [Domain Validation](../../docs/DOMAIN_VALIDATION.md)
- [Result Pattern](../ViajantesTurismo.Common/RESULT_PATTERN.md)
