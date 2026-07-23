# Admin Bounded Context

The Admin bounded context manages the core tour operations business for ViajantesTurismo, including tour operations
management, customer relationship management, and booking lifecycle.

## Context Overview

**Domain:** Tour Operations & Customer Management
**Teams:** Operations, Sales, Customer Service
**Primary Users:** Tour operators, booking agents, customer service representatives

## Responsibilities

- **Tour Operations Management** — Define and maintain cycling tour offerings with schedules, pricing, and capacity
- **Customer Relationship Management** — Maintain comprehensive customer profiles and preferences
- **Booking Lifecycle** — Handle reservations from creation through confirmation, payment, and completion
- **Payment Tracking** — Record and track payments against bookings

## Bounded Context Map

### Upstream Dependencies

None (Admin is a core context)

### Downstream Consumers

- **Catalog** — Consumes selected tour operation facts for public website presentation
- **Operations** (future) — Consumes confirmed bookings for logistics planning
- **Accounting** (future) — Consumes payment records for financial reporting

### Integration Events Published

- `AdminTourCreatedIntegrationEvent` — durable Catalog draft creation.

The [generated event and message flow map](../architecture/generated-event-message-flow-map.md) is the
current source-derived contract, producer, consumer, and handler inventory. Future event families stay
out of this current-contract list until implemented.

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

Admin application slices own tour, booking/payment, customer/import, and document workflows. Their
source folders and the [generated endpoint route map](../architecture/generated-endpoint-route-map.md)
are the current inventories; this bounded-context page documents responsibilities rather than
duplicating every command, query, or endpoint type.

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

- **Database:** PostgreSQL (Entity Framework Core via Npgsql)
- **Stores:** `ITourStore`, `ICustomerStore`, `IDocumentStore`
- **Unit of Work:** `IUnitOfWork` for transactional consistency

### API

- **Style:** Minimal APIs (ASP.NET Core)
- **Endpoints:** versioned Minimal APIs listed in the
  [generated endpoint route map](../architecture/generated-endpoint-route-map.md)
- **Contracts:** split across `ViajantesTurismo.Admin.Contracts.Application`,
  `ViajantesTurismo.Admin.Contracts.Http`, and `ViajantesTurismo.Admin.Contracts.IntegrationEvents`

## Testing

Admin behavior, unit, API, contract, infrastructure, integration, and system test projects validate
their matching runtime boundaries. See [test guidelines](../TEST_GUIDELINES.md) and the
[tests README](../../tests/README.md) instead of maintaining another project inventory here.

## Related Documentation

Use the [documentation source-of-truth map](../README.md#source-of-truth-map) for related repository guidance.
