# ViajantesTurismo.Admin.ApiService

RESTful API service for tour management operations - tours, customers, and bookings.

## Purpose

HTTP API exposing domain operations through minimal API endpoints. Implements query and command operations with proper
error handling.

## Endpoints

### Tours

- `GET /tours` - List all tours
- `GET /tours/{id}` - Get tour by ID
- `POST /tours` - Create new tour
- `PUT /tours/{id}` - Update tour
- `DELETE /tours/{id}` - Delete tour

### Customers

- `GET /customers` - List all customers
- `GET /customers/{id}` - Get customer by ID
- `POST /customers` - Create new customer
- `PUT /customers/{id}` - Update customer
- `DELETE /customers/{id}` - Delete customer

### Bookings

- `GET /bookings` - List all bookings
- `GET /bookings/{id}` - Get booking by ID
- `GET /bookings/tour/{tourId}` - Get bookings for tour
- `GET /bookings/customer/{customerId}` - Get bookings for customer
- `POST /bookings` - Create booking with discount support
- `PUT /bookings/{id}/discount` - Update booking discount
- `PUT /bookings/{id}/details` - Update room type, bikes, companion
- `POST /bookings/{id}/confirm` - Confirm booking
- `POST /bookings/{id}/cancel` - Cancel booking
- `POST /bookings/{id}/complete` - Complete booking
- `PATCH /bookings/{id}/notes` - Update booking notes
- `POST /bookings/{id}/payments` - Record payment
- `DELETE /bookings/{id}` - Delete booking

## Features

- OpenAPI documentation
- Result pattern error handling
- Health checks at `/health`
- Service discovery integration

## Dependencies

- **ViajantesTurismo.Admin.Domain**: Business logic
- **ViajantesTurismo.Admin.Infrastructure**: Data access
- **ViajantesTurismo.Admin.Contracts**: DTOs
- **ViajantesTurismo.ServiceDefaults**: Shared configuration
