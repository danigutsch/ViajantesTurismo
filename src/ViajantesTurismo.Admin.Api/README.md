# ViajantesTurismo.Admin.Api

RESTful API service for tour management operations - tours, customers, and bookings.

## Purpose

HTTP API exposing domain operations through minimal API endpoints. Implements query and command operations with proper
error handling.

## Endpoints

Booking mutations and Tour create and update live in operation directories under `Bookings` and
`Tours`. Each feature's route group owns its reads and maps the operations at startup.

`Customers` owns its create, update, and import operations. `MapCustomerEndpoints` composes the
customer routes, including import upload binding and authorization.

### Tours

- `GET /tours/page` - Read a page of active tour summaries
- `GET /tours/{id}` - Get tour by ID
- `POST /tours` - Create new tour
- `PUT /tours/{id}` - Update tour
- `DELETE /tours/{id}` - Delete tour

### Customers

- `GET /customers/page` - Read a page of customer summaries
- `GET /customers/{id}` - Get customer by ID
- `POST /customers` - Create new customer
- `PUT /customers/{id}` - Update customer
- `DELETE /customers/{id}` - Delete customer

### Bookings

- `GET /bookings/page` - Read a page of bookings
- `GET /bookings/{id}` - Get booking by ID
- `GET /bookings/tour/{tourId}/page` - Read a page of bookings for a tour
- `GET /bookings/customer/{customerId}/page` - Read a page of bookings for a customer
- `POST /bookings` - Create booking with discount support
- `PUT /bookings/{id}/discount` - Update booking discount
- `PUT /bookings/{id}/details` - Update room type, bikes, companion
- `POST /bookings/{id}/confirm` - Confirm booking
- `POST /bookings/{id}/cancel` - Cancel booking
- `POST /bookings/{id}/complete` - Complete booking
- `PATCH /bookings/{id}/notes` - Update booking notes
- `POST /bookings/{id}/payments` - Record payment
- `DELETE /bookings/{id}` - Delete booking

See the [Admin bounded-read migration](../../docs/admin-bounded-read-migration.md) for caller changes.

## Features

- OpenAPI documentation
- Result pattern error handling
- Health checks at `/health`
- Service discovery integration

## Dependencies

- **ViajantesTurismo.Admin.Domain**: Business logic
- **ViajantesTurismo.Admin.Infrastructure**: Data access
- **ViajantesTurismo.Admin.Contracts.Application**: DTOs
- **ViajantesTurismo.Admin.Contracts.Http**: HTTP contract artifacts
- **ViajantesTurismo.ServiceDefaults**: Shared configuration
