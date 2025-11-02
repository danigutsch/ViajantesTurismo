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

- `POST /tours/{tourId}/bookings` - Add booking to tour
- `PUT /tours/{tourId}/bookings/{bookingId}` - Update booking
- `DELETE /tours/{tourId}/bookings/{bookingId}` - Remove booking

## Features

- OpenAPI/Swagger documentation
- Result pattern error handling
- Health checks at `/health`
- Service discovery integration

## Dependencies

- **ViajantesTurismo.Admin.Domain**: Business logic
- **ViajantesTurismo.Admin.Infrastructure**: Data access
- **ViajantesTurismo.AdminApi.Contracts**: DTOs
- **ViajantesTurismo.ServiceDefaults**: Shared configuration
