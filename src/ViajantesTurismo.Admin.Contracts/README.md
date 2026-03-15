# ViajantesTurismo.Admin.Contracts

Data Transfer Objects (DTOs) and contracts for the Admin API.

## Purpose

Shared contract definitions between API and clients. Provides DTOs with validation attributes for request/response
serialization.

## Contents

### DTOs

- **Tour DTOs**: `CreateTourDto`, `UpdateTourDto`, `TourDto`
- **Customer DTOs**: `CreateCustomerDto`, `UpdateCustomerDto`, `CustomerDto`
- **Booking DTOs**: `CreateBookingDto`, `UpdateBookingDto`, `BookingDto`
- **Supporting DTOs**: `AddressDto`, `ContactInfoDto`, `PersonalInfoDto`, `AccommodationPreferencesDto`

### Enumerations

- `BookingStatusDto`, `PaymentStatusDto`, `BedTypeDto`, `BikeTypeDto`

### Constants

- **ContractConstants**: Validation constraints (max lengths, price ranges)

## Validation

DTOs include data annotations for:

- Required fields
- String length limits
- Value ranges
- Format validation

## Dependencies

Zero dependencies - pure contract definitions for API boundary.
