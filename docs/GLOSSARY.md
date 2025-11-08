# Domain Glossary (Ubiquitous Language)

Add and update terms here. Use exact terms in code, docs, and tests.

## General Terms

| Term    | Definition                                                                       | Notes                                                       |
|---------|----------------------------------------------------------------------------------|-------------------------------------------------------------|
| Admin   | Bounded context responsible for reference data and administrative configuration. | Owners, roles, countries, etc.                              |
| Country | ISO-3166 country metadata used across the system.                                | Source lives under `Admin.Web/wwwroot/data/countries.json`. |
| User    | Person managing Admin functions.                                                 |                                                             |

## Aggregates

| Term     | Definition                                                                                                                                | Notes                                                                                                                |
|----------|-------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| Tour     | **Aggregate Root.** Represents a cycling tour with schedule, pricing, capacity, and included services. Contains and manages all Bookings. | All booking operations must go through Tour methods. Tours are identified by business identifier (e.g., "CUBA2024"). |
| Customer | **Aggregate Root.** Represents a customer with personal, contact, identification, physical, medical, and accommodation information.       | Self-contained entity with no child entities.                                                                        |

## Entities

| Term            | Definition                                                                              | Notes                                                                                                                   |
|-----------------|-----------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| Booking         | A reservation made by a customer for a specific tour. Part of the Tour aggregate.       | Can only be created and modified through Tour methods. Has lifecycle states: Pending → Confirmed → Completed/Cancelled. |
| BookingCustomer | Represents a customer's participation in a booking, including bike selection and price. | Embedded within Booking. Represents either principal or companion customer.                                             |
| Payment         | A financial payment recorded against a booking.                                         | Immutable once created. Payments are managed through Booking.                                                           |

## Value Objects

| Term                     | Definition                                                                                                      | Notes                                                                                         |
|--------------------------|-----------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| DateRange                | An immutable time period with start and end dates.                                                              | Used for tour schedules. Validates that end date is after start date and calculates duration. |
| TourPricing              | Tour pricing information including base price, room supplements, and bike rental prices in a specific currency. | Groups: BasePrice, DoubleRoomSupplementPrice, RegularBikePrice, EBikePrice, Currency.         |
| TourCapacity             | Tour capacity constraints specifying minimum and maximum customers.                                             | Validates that min ≤ max and both are within allowed ranges.                                  |
| Discount                 | Discount information including type, amount, and reason.                                                        | Types: None, Percentage, Absolute. Percentage discounts have max limit (100%).                |
| PersonalInfo             | Customer's personal information (name, date of birth, gender, nationality, profession).                         |                                                                                               |
| ContactInfo              | Contact details (email, mobile, Instagram, Facebook).                                                           | Email is required.                                                                            |
| Address                  | Physical address (street, city, state/province, postal code, country).                                          |                                                                                               |
| PhysicalInfo             | Physical characteristics (height, weight, bike type preference, shoe size).                                     |                                                                                               |
| IdentificationInfo       | Identification documents (passport number, issue/expiry dates, issuing country).                                |                                                                                               |
| MedicalInfo              | Medical conditions, allergies, medications, dietary restrictions.                                               |                                                                                               |
| EmergencyContact         | Emergency contact person (name, relationship, phone numbers).                                                   |                                                                                               |
| AccommodationPreferences | Room and dietary preferences.                                                                                   |                                                                                               |

## Enums

| Term          | Definition                                   | Values                                                                 | Notes                                                     |
|---------------|----------------------------------------------|------------------------------------------------------------------------|-----------------------------------------------------------|
| BookingStatus | Current state of a booking in its lifecycle. | None(0), Pending(1), Confirmed(2), Cancelled(3), Completed(4)          | State transitions enforced through domain methods.        |
| PaymentStatus | Payment completion status for a booking.     | None(0), NotPaid(1), PartiallyPaid(2), Paid(3), Refunded(4)            | Updated as payments are recorded.                         |
| PaymentMethod | Method used for a payment transaction.       | Other(0), CreditCard(1), BankTransfer(2), Cash(3), PayPal(4), Other(5) |                                                           |
| BikeType      | Type of bicycle for a tour participant.      | None(0), Regular(1), EBike(2)                                          | None is invalid for bookings (validation error).          |
| RoomType      | Accommodation room configuration.            | None(0), SingleRoom(1), DoubleRoom(2)                                  | Affects pricing (double room may have supplement).        |
| DiscountType  | Type of discount applied to a booking.       | None(0), Percentage(1), Absolute(2)                                    | Percentage limited to max 100%. Absolute is fixed amount. |
| BedType       | Bed configuration preference within a room.  | SingleBed(0), DoubleBed(1)                                             | Used in accommodation preferences.                        |
| Currency      | Currency for financial amounts.              | EUR(0), USD(1), GBP(2), ARS(3)                                         | All prices in a tour use same currency.                   |

## Domain Operations

| Term                       | Definition                                                                                   | Context        |
|----------------------------|----------------------------------------------------------------------------------------------|----------------|
| AddBooking                 | Creates a new booking for a tour with validation of capacity, bike types, and customer data. | Tour aggregate |
| ConfirmBooking             | Transitions a booking from Pending to Confirmed status.                                      | Tour → Booking |
| CancelBooking              | Transitions a booking to Cancelled status.                                                   | Tour → Booking |
| CompleteBooking            | Transitions a booking from Confirmed to Completed status.                                    | Tour → Booking |
| RecordPayment              | Records a financial payment against a booking with validation.                               | Tour → Booking |
| UpdateBookingNotes         | Updates administrative notes on a booking.                                                   | Tour → Booking |
| UpdateBookingDiscount      | Modifies the discount applied to a booking.                                                  | Tour → Booking |
| UpdateBookingDetails       | Updates room type, bike selections, and companion information.                               | Tour → Booking |
| UpdateBookingPaymentStatus | Updates the payment status of a booking.                                                     | Tour → Booking |

## Business Concepts

| Term                   | Definition                                                               | Notes                                                       |
|------------------------|--------------------------------------------------------------------------|-------------------------------------------------------------|
| Principal Customer     | The primary customer making a booking.                                   | Every booking has exactly one principal customer.           |
| Companion Customer     | An optional second customer sharing a booking with the principal.        | Affects pricing (room sharing, individual bike costs).      |
| Base Price             | The tour price for a single room (not per person).                       | Currency-specific. Basis for booking calculations.          |
| Double Room Supplement | Additional cost when two customers share a double room.                  | May be zero if double rooms are standard.                   |
| Included Services      | List of services included in tour package.                               | Examples: "Hotel", "Breakfast", "Bike Rental", "City Tour". |
| Current Customer Count | Total confirmed customers across all bookings (principals + companions). | Used for capacity management.                               |
| Available Spots        | Remaining capacity (MaxCustomers - CurrentCustomerCount).                | Cannot be negative; enforced on booking creation.           |
| Subtotal               | Sum of base price, room costs, and bike costs before discount.           | Booking-level calculation.                                  |
| Total Price            | Final booking price after applying discount to subtotal.                 | What customer pays.                                         |
| Amount Paid            | Sum of all recorded payments for a booking.                              | Tracks payment progress.                                    |
| Remaining Balance      | Outstanding amount (TotalPrice - AmountPaid).                            | Zero when fully paid.                                       |

## Technical Terms

| Term                 | Definition                                                             | Notes                                          |
|----------------------|------------------------------------------------------------------------|------------------------------------------------|
| Aggregate Root       | The entry point for all modifications to an aggregate.                 | Tour and Customer are aggregate roots.         |
| Consistency Boundary | The scope within which business rules must be consistent.              | Defined by aggregate boundaries.               |
| Factory Method       | Static creation method with validation (e.g., `Tour.Create()`).        | Returns `Result<T>` for error handling.        |
| Ubiquitous Language  | Domain terminology used consistently in code, docs, and conversations. | This glossary defines the ubiquitous language. |
