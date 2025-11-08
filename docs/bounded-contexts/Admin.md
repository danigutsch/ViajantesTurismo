# Admin Bounded Context

**Responsibility**: Tour and customer management for cycling tour operations, including bookings, payments, and administrative reference data.

## Core Domain

### Tour Aggregate

**Aggregate Root: Tour**
- Manages cycling tour offerings with schedules, pricing, capacity, and included services
- Controls all booking operations and lifecycle within its consistency boundary
- Business identifier (e.g., "CUBA2024"), date range, pricing model, capacity constraints
- Enforces invariants: tour duration minimum 5 days, prices within valid range, capacity management

**Entities**
- **Booking** — Customer reservations with state machine (Pending → Confirmed → Completed/Cancelled). Can only be modified through Tour methods. Tracks room type, bikes, principal/companion customers, discounts, payments. Pricing calculated as: base price + room supplement + bike costs - discount.
- **Payment** — Immutable financial records. Tracks amount, date, method, reference number, system recording timestamp. Payment status automatically calculated: Unpaid → PartiallyPaid → Paid.
- **BookingCustomer** — Embedded entity representing customer participation (principal or companion). Links customer ID, bike selection, and bike price.

**Value Objects**
- **DateRange** — Tour schedule with start/end dates and duration
- **TourPricing** — Base price, room supplements, bike rental prices, currency
- **TourCapacity** — Minimum and maximum customer limits
- **Discount** — Type (None/Percentage/Absolute), amount, reason

**Business Rules**
- Base price represents single room cost (double room adds supplement)
- BikeType.None invalid for bookings (must select Regular or EBike)
- Discounts validated: percentage 0-100%, absolute cannot exceed subtotal
- Room rules: single room forbids companion, double room allows optional companion
- Payments immutable after creation, cannot exceed remaining balance
- State transitions idempotent with terminal states (Cancelled/Completed)
- Capacity management: confirmed bookings count toward tour maximum

### Customer Aggregate

**Aggregate Root: Customer**
- Self-contained entity representing customer profiles
- Comprehensive information: personal, contact, identification, physical, medical, accommodation preferences
- No child entities — all customer data encapsulated in value objects

**Value Objects**
- **PersonalInfo** — Name, date of birth, gender, nationality, profession
- **ContactInfo** — Email, mobile, Instagram, Facebook
- **Address** — Street, city, state/province, postal code, country
- **PhysicalInfo** — Height, weight, bike preference, shoe size
- **IdentificationInfo** — Passport number, issue/expiry dates, issuing country
- **MedicalInfo** — Conditions, allergies, medications, dietary restrictions
- **EmergencyContact** — Name, relationship, phone numbers
- **AccommodationPreferences** — Room and dietary preferences

**Business Rules**
- Email must be unique and valid format
- Customer must be 18+ years old
- All value objects validated on creation and update

## Projects

**ViajantesTurismo.Admin.Domain**  
Core business logic with DDD patterns: aggregate roots, entities, value objects, factory methods, Result pattern for validation

**ViajantesTurismo.Admin.Application**  
Application layer following CQRS: IQueryService (read-only DTOs), ITourStore/ICustomerStore (command operations), IUnitOfWork (transactions), mapping between domain and DTOs

**ViajantesTurismo.Admin.Infrastructure**  
EF Core persistence with ApplicationDbContext, repository implementations (TourStore, CustomerStore, QueryService), migrations, seeding

**ViajantesTurismo.Admin.ApiService**  
Minimal API endpoints organized by aggregate: ToursEndpoints, BookingsEndpoints, CustomerEndpoints. Command/query separation enforced.

**ViajantesTurismo.Admin.Web**  
Blazor Server UI for administrative operations, API clients (ToursApiClient, CustomersApiClient, BookingsApiClient), country reference data service

**ViajantesTurismo.Admin.Contracts**  
DTOs for API contracts, validation constants (ContractConstants), shared enums and data contracts

## Architecture Patterns

**Domain-Driven Design**
- Aggregate pattern with Tour as root for booking operations
- Factory methods returning Result<T> for all entity creation
- Dedicated error classes per aggregate (TourErrors, BookingErrors, CustomerErrors)
- Ubiquitous language enforced across code, docs, and tests

**CQRS Separation**
- IQueryService: read-only operations returning optimized DTOs
- Stores (ITourStore, ICustomerStore): command operations on full aggregates
- Explicit separation: queries use IQueryService, commands use stores + IUnitOfWork

**Result Pattern**
- All domain operations return Result/Result<T> instead of throwing exceptions
- Explicit error handling with ResultStatus categorization
- No performance overhead for expected validation failures

**Clean Architecture**
- Domain layer free of infrastructure concerns
- Application layer coordinates use cases without business logic
- Infrastructure implements interfaces defined in application
- API and Web layers depend on abstractions

## External Dependencies

**Database** (SQL Server via EF Core)
- Tours, Bookings, Payments, Customers tables
- Migrations managed via dedicated MigrationService
- Transactional consistency enforced at aggregate boundaries

**Message Broker** (for future integration)
- Outbox pattern for reliable domain event publication
- Integration events for cross-context communication

**Reference Data**
- Country metadata (ISO-3166) served from Admin.Web static JSON

## Interactions

**Downstream**: None currently — Admin is foundation context

**Upstream**: Future contexts will consume:
- Customer reference data
- Tour availability information  
- Booking confirmation events

## Key Endpoints

**Tours**: GET/POST /tours, GET/PUT/DELETE /tours/{id}, PUT /tours/{id}/schedule, PUT /tours/{id}/pricing  
**Bookings**: GET /bookings, GET /bookings/{id}, GET /tours/{tourId}/bookings, POST /tours/{tourId}/bookings, PUT /bookings/{id}/confirm|cancel|complete  
**Customers**: GET/POST /customers, GET/PUT/DELETE /customers/{id}

## Testing Strategy

- Behavior tests (SpecFlow/Reqnroll) for domain scenarios
- Unit tests for value objects and business rules
- Integration tests for Infrastructure layer (repositories, DbContext)
- Architecture tests enforce aggregate boundaries and layer dependencies
