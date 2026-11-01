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

## Country and Currency Reference Data

Inventory of where country and currency data is used, and where each concern belongs.

### Country usages

| Project | Usage | Kind |
| --- | --- | --- |
| `Management.Web` `wwwroot/data/countries.json` | ISO 3166 alpha-2 code keyed list with names and metadata | Static data file |
| `Management.Web` `CountryService`, `ICountryService` | Loads the list once, falls back to ten common countries, maps demonyms to names | Lookup, singleton |
| `Management.Web` `CountrySelector` | Nationality and identification-country pickers on customer create and edit pages | UI |
| `Management.Web` customer import | Header matching for a `Country` column | Import mapping |
| `Admin.Domain` `Address`, `PersonalInfo`, `IdentificationInfo` | Free text, required, sanitized, 128 characters at most | Domain validation |
| `Admin.Contracts.Application` `AddressDto`, customer DTOs | Plain string, no code list | Contract |

The domain accepts any non-empty string, so a stored value may be a name, a demonym, or a code.
Only the Management Web picker constrains input, and only for people using that form.

### Currency usages

| Project | Usage | Kind |
| --- | --- | --- |
| `Admin.Domain` `Currency` | `Real`, `Euro`, `UsDollar`; tour pricing carries one currency | Domain enum |
| `Admin.Domain` payments | Settlement is EUR or BRL only; BRL needs an approved FX quote; `AgreedFxQuote` rounds to cents | Business rules |
| `Admin.Contracts.Application` `CurrencyDto` | Mirror of the enum for the HTTP contract | Contract |
| `Admin.Application` `TourMapper` | Maps `Currency` to `CurrencyDto` for tours | Mapping |
| `Admin.Infrastructure` payment configurations | Persist the enum on payment account and payment history rows | Persistence |
| `Management.Web` `CurrencyFormatter` | Symbol, symbol position, and two decimals in invariant culture | Formatting |
| `Management.Web` tour and payment pages | Currency pickers and labels per enum value | UI |

Catalog, Branding, and Public Web hold no country or currency data.

### Ownership boundary

| Concern | Owner | Notes |
| --- | --- | --- |
| Which currencies exist and which settle payments | `Admin.Domain` | A business rule, not reference data; keep it in the enum and payment types |
| Rounding, FX conversion, per-currency validity | `Admin.Domain` | Already encapsulated in payment types |
| Currency symbol, symbol position, number format | `Management.Web` | Presentation only; depends on the viewer, not the data |
| ISO country list and display names | `Management.Web` | Static lookup for pickers; not a domain concept today |
| Whether a stored country must be a valid code | Undecided | Today a UI-only constraint; the domain would need a code type to enforce it |

### Lifetime and caching

- The country list is immutable per deployment, so a process-lifetime singleton loaded on first use is
  enough. No expiry or invalidation is needed. `CountryService` is registered that way.
- Currency symbols and positions are compile-time constants and need no cache.
- Neither list is served over HTTP, so the response caching rules in
  [Caching strategy](../caching-strategy.md) do not apply.

### Follow-up suggestions

These are suggestions, not committed work.

- Decide whether the domain should store a country as an ISO code value object. This changes persistence,
  import, and existing free-text rows, so it needs a migration plan and an owner decision first.
- Move the demonym table out of `CountryService` if the code decision above is taken, since codes make it
  unnecessary for new data.
- Add a currency-to-symbol parity test over every `CurrencyDto` value, so a new currency cannot fall
  through to the generic symbol in `CurrencyFormatter`.
- Keep a shared library out of scope until a second application needs the same list.

## Testing

Admin behavior, unit, API, contract, infrastructure, integration, and system test projects validate
their matching runtime boundaries. See [test guidelines](../TEST_GUIDELINES.md) and the
[tests README](../../tests/README.md) instead of maintaining another project inventory here.

## Related Documentation

Use the [documentation source-of-truth map](../README.md#source-of-truth-map) for related repository guidance.
