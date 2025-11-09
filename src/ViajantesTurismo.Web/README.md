# ViajantesTurismo.Web

Public-facing Blazor web application for customers to browse and book tours.

## Purpose

Customer-facing website where users can explore available tours, view details, and potentially book their cycling adventures. Separate from the admin application.

## Technology

- **Blazor Server** - Server-side rendering with interactive components
- **ASP.NET Core** - Web framework
- **.NET 10** - Runtime platform

## Features

- Browse available tours
- View tour details
- Responsive design for mobile and desktop

## Structure

```bash
ViajantesTurismo.Web/
├── Components/        # Blazor components
│   ├── Pages/        # Routable pages
│   ├── Layout/       # Layout components
│   └── Shared/       # Shared/reusable components
└── Models/           # View models and form models
```

## Dependencies

- **ViajantesTurismo.AdminApi.Contracts**: Shared DTOs
- **ViajantesTurismo.ServiceDefaults**: Service configuration

## Development Status

This is the public website for customers (separate from ViajantesTurismo.Admin.Web which is for tour operators).

## Related Projects

- **ViajantesTurismo.Admin.Web**: Admin UI for tour operators
- **ViajantesTurismo.Admin.ApiService**: Backend API
