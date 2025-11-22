# Admin Bounded Context

The Admin bounded context manages the core tour operations business for ViajantesTurismo, including tour catalog
management, customer relationship management, and booking lifecycle.

## Context Overview

**Domain:** Tour Operations & Customer Management
**Teams:** Operations, Sales, Customer Service
**Primary Users:** Tour operators, booking agents, customer service representatives

## Responsibilities

- **Tour Catalog Management** — Define and maintain cycling tour offerings with schedules, pricing, and capacity
- **Customer Relationship Management** — Maintain comprehensive customer profiles and preferences
- **Booking Lifecycle** — Handle reservations from creation through confirmation, payment, and completion
- **Payment Tracking** — Record and track payments against bookings

## Bounded Context Map

### Upstream Dependencies

None (Admin is a core context)

### Downstream Consumers

- **Sales/Marketing** (future) — Consumes tour catalog for public website
- **Operations** (future) — Consumes confirmed bookings for logistics planning
- **Accounting** (future) — Consumes payment records for financial reporting

### Integration Events Published

- `TourCreated`, `TourUpdated` — Tour catalog changes
- `BookingConfirmed`, `BookingCancelled`, `BookingCompleted` — Booking lifecycle events
- `PaymentRecorded` — Payment transactions
- `CustomerCreated`, `CustomerUpdated` — Customer profile changes

## Aggregates

For detailed aggregate documentation (invariants, commands, events), see
[domain/AGGREGATES.md](../domain/AGGREGATES.md#admin-bounded-context).

**Tour Aggregate:**

- Root: `Tour`
- Entities: `Booking`, `Payment`, `BookingCustomer`
- Value Objects: `DateRange`, `TourPricing`, `TourCapacity`, `Discount`

**Customer Aggregate:**

- Root: `Customer`
- Entities: None (self-contained)
- Value Objects: `PersonalInfo`, `ContactInfo`, `Address`, `PhysicalInfo`, `IdentificationInfo`, `MedicalInfo`,
  `EmergencyContact`, `AccommodationPreferences`, `Occupation`

## Application Services

### Commands (CQRS Write Side)

**Tour Commands:**

- `CreateTourCommand`, `UpdateTourDetailsCommand`, `UpdateTourScheduleCommand`
- `UpdateTourPricingCommand`, `UpdateTourCapacityCommand`
- `DeleteTourCommand`

**Booking Commands:**

- `AddBookingCommand`, `ConfirmBookingCommand`, `CancelBookingCommand`, `CompleteBookingCommand`
- `UpdateBookingNotesCommand`, `UpdateBookingDiscountCommand`, `UpdateBookingDetailsCommand`
- `RemoveBookingCommand`

**Payment Commands:**

- `RecordPaymentCommand`

**Customer Commands:**

- `CreateCustomerCommand`
- `UpdateCustomerPersonalInfoCommand`, `UpdateCustomerContactInfoCommand`
- `UpdateCustomerAddressCommand`, `UpdateCustomerPhysicalInfoCommand`
- `UpdateCustomerIdentificationInfoCommand`, `UpdateCustomerMedicalInfoCommand`
- `UpdateCustomerEmergencyContactCommand`, `UpdateCustomerAccommodationPreferencesCommand`

### Queries (CQRS Read Side)

**Tour Queries:**

- `GetTourByIdQuery`, `GetAllToursQuery`, `GetToursByDateRangeQuery`

**Customer Queries:**

- `GetCustomerByIdQuery`, `GetAllCustomersQuery`, `GetCustomerByEmailQuery`

**Booking Queries:**

- `GetBookingByIdQuery`, `GetBookingsByTourQuery`, `GetBookingsByCustomerQuery`

## Domain Validation

See [DOMAIN_VALIDATION.md](../DOMAIN_VALIDATION.md) for patterns and [domain/AGGREGATES.md](../domain/AGGREGATES.md)
for specific invariants.

**Key Patterns:**

- Factory methods with `Result<T>` return types
- Application-level uniqueness checks (Tour identifier, Customer email)
- Aggregate boundary enforcement (Bookings only via Tour)
- State machine validation (Booking status transitions)

## Infrastructure

### Persistence

- **Database:** SQL Server (Entity Framework Core)
- **Repositories:** `ITourRepository`, `ICustomerRepository`
- **Unit of Work:** `IUnitOfWork` for transactional consistency

### API

- **Style:** Minimal APIs (ASP.NET Core)
- **Endpoints:** `/tours`, `/customers`, `/bookings`
- **Contracts:** Shared via `ViajantesTurismo.Admin.Contracts` project

## Testing

**Behavior Tests:** `tests/ViajantesTurismo.Admin.BehaviorTests/specs/`

- `Tour*.feature` — Tour aggregate scenarios
- `Booking*.feature` — Booking lifecycle scenarios
- `Payment*.feature` — Payment recording scenarios
- `Customer*.feature` — Customer aggregate scenarios
- `*Validation.feature` — Cross-cutting validation scenarios

**Unit Tests:** `tests/ViajantesTurismo.Admin.UnitTests/`

**Integration Tests:** `tests/ViajantesTurismo.Admin.IntegrationTests/`

## Related Documentation

- **[Aggregates Documentation](../domain/AGGREGATES.md#admin-bounded-context)** — Detailed invariants and business rules
- **[Domain Validation](../DOMAIN_VALIDATION.md)** — Factory methods, Result pattern, validation patterns
- **[Architecture Decisions](../ARCHITECTURE_DECISIONS.md)** — ADRs affecting this context
- **[Test Guidelines](../TEST_GUIDELINES.md)** — Testing strategy and BDD patterns
