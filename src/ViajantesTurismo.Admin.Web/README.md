# ViajantesTurismo.Admin.Web

Blazor Server web application for tour administration - managing tours, customers, and bookings.

## Purpose

Interactive web UI for tour operators to manage the tour business. Provides forms, validation, and real-time updates
using Blazor Server components.

## Features

- **Interactive UI**: Blazor Server with real-time updates
- **Forms**: Tour creation, customer management, booking operations
- **Validation**: Client and server-side validation
- **State Management**: Scoped services for form state
- **Caching**: Redis output cache integration

## Architecture

- **Components**: Reusable Blazor components in `Components/`
- **API Clients**: HTTP clients for API communication
- **Services**: `CustomerCreationState`, `CountryService`
- **Models**: View models and DTOs

## Dependencies

- **ViajantesTurismo.Admin.ApiService**: Backend API
- **ViajantesTurismo.Resources**: Shared resource names
- **ViajantesTurismo.ServiceDefaults**: Service discovery and defaults
- **Redis**: Output caching
