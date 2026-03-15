# ViajantesTurismo.Admin.Web

Blazor Server web application for tour administration - managing tours, customers, and bookings.

## Purpose

Interactive web UI for tour operators to manage the tour business. Provides forms, validation, and real-time updates
using Blazor Server components.

## Features

- **Interactive UI**: Blazor Server with real-time updates
- **Forms**: Tour creation, customer management, booking operations
- **Booking Management**:
    - Create bookings with room type selection and bike preferences
    - Apply discounts (percentage or absolute amount)
    - Real-time price calculation display
    - Record payments with multiple payment methods
    - Update booking details (room, bikes, companion, discount)
    - Domain operations: Confirm, Cancel, Complete
- **Validation**: Client and server-side validation with field-level error display
- **State Management**: Scoped services for form state
- **Error Handling**: Server validation error parsing and EditContext integration
- **Caching**: Redis output cache integration

## Architecture

- **Components**: Reusable Blazor components in `Components/`
    - `BookingCreateForm.razor`: Booking creation with discount support
    - Shared forms and layout components
- **API Clients**: HTTP clients for API communication
    - Parse 400 Bad Request validation errors
    - Map server errors to form field validation
- **Services**: `CustomerCreationState`, `CountryService`
- **Models**: View models (`BookingFormModel`) with client-side validation
- **Helpers**: `ValidationErrorHelper`, `EditContextValidationHelper`

## Dependencies

- **ViajantesTurismo.Admin.ApiService**: Backend API
- **ViajantesTurismo.Admin.Contracts**: Shared DTOs and contracts
- **ViajantesTurismo.Resources**: Shared resource names
- **ViajantesTurismo.ServiceDefaults**: Service discovery and defaults
- **Redis**: Output caching
