# ViajantesTurismo.Admin.Contracts.Application

Data Transfer Objects (DTOs) and application-facing contracts for the Admin API.

## Purpose

Shared request/response contract definitions between API, application, web, and tests. Provides DTOs
with validation attributes for request/response serialization.

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

Zero dependencies - pure application contract definitions for API boundary.
