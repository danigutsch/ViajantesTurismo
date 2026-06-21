# ViajantesTurismo.Management.Web

Blazor Server web application for internal management workflows across bounded contexts.

## Purpose

Interactive internal portal for operators to manage tours, customers, and bookings. The portal composes workflows
across bounded-context backends through typed API clients and shared UX components. It provides forms, validation,
and real-time updates using Blazor Server components.

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
- **API Clients**: Typed HTTP clients for API communication
    - Parse 400 Bad Request validation errors
    - Map server errors to form field validation
- **Services**: `CustomerCreationState`, `CountryService`
- **Models**: View models (`BookingFormModel`) with client-side validation
- **Helpers**: `ValidationErrorHelper`, `EditContextValidationHelper`

## Module Boundary Governance

`ViajantesTurismo.Management.Web` is the internal management portal for operator workflows. It can compose data from
multiple bounded-context backends, but bounded-context ownership still belongs to backend contracts and typed clients.
This follows [ADR-020](../../docs/adr/20260523-web-frontends-by-audience-not-by-bounded-context.md) and implements the
governance described by [issue #128](https://github.com/danigutsch/ViajantesTurismo/issues/128) and
[issue #270](https://github.com/danigutsch/ViajantesTurismo/issues/270).

### Folder Ownership

Keep page-level workflow code under the existing Blazor component structure:

- `Components/Pages/<Workflow>/`: routable pages and page-specific child components for one operator workflow.
- `Components/Shared/`: reusable UI components that are not owned by a single workflow.
- `Components/Layout/`: app shell, navigation, and layout-only components.
- `Models/<Workflow>/`: form and view models used by one workflow.
- `Services/<Workflow>/`: UI state or orchestration services used by one workflow.
- `Clients/<Context>/`: typed API client interfaces and implementations for one backend bounded context.

Avoid sharing workflow-specific components by moving them to `Components/Shared/` prematurely. Promote a component only
after a second workflow needs the same behavior and the shared API can stay independent of the original workflow.

### Typed API Clients

All backend calls must go through typed clients; components and UI state services should not call raw `HttpClient`
directly. Name clients by backend/context ownership, not by page ownership:

- `IAdminToursApiClient` for Admin tour operations.
- `IAdminBookingsApiClient` for Admin booking operations.
- `ICatalogTourPresentationApiClient` for future Catalog presentation operations.

Typed clients own HTTP paths, request/response DTO calls, validation-error parsing, and service-discovery endpoint names.
Components own user interaction, loading state, and presentation-specific mapping. If one page needs data from two
contexts, inject two typed clients into a page-level service or component instead of creating one mixed-purpose client.

### Cross-Context Composition

Cross-context composition is allowed only at page or workflow orchestration boundaries. A page can show Admin booking
state beside Catalog presentation state when that is the operator workflow, but Admin-specific components should not
directly reference Catalog-specific components or services.

Use these dependency directions:

- Pages may depend on workflow services, typed clients, models, and shared components.
- Workflow services may depend on typed clients and workflow models.
- Shared components may depend on generic parameters, callbacks, or simple view models, not typed clients.
- Typed clients may depend on generated or hand-written contracts for their backend context.

Avoid these dependency directions:

- `Components/Shared/` depending on `Clients/<Context>/`.
- One workflow service depending on another workflow service.
- A typed client combining endpoints owned by different backend bounded contexts.
- Public website concerns or public SEO/content models leaking into `Management.Web`.

### Concrete Example

A future Catalog presentation editor should keep Catalog-specific code separate from existing Admin booking UI:

```text
src/ViajantesTurismo.Management.Web/
├── Clients/
│   ├── Admin/
│   │   ├── IAdminToursApiClient.cs
│   │   └── AdminToursApiClient.cs
│   └── Catalog/
│       ├── ICatalogTourPresentationApiClient.cs
│       └── CatalogTourPresentationApiClient.cs
├── Components/
│   ├── Pages/
│   │   ├── Bookings/
│   │   └── CatalogPresentation/
│   └── Shared/
├── Models/
│   ├── Bookings/
│   └── CatalogPresentation/
└── Services/
    ├── Bookings/
    └── CatalogPresentation/
```

If a `CatalogPresentation` page needs Admin tour facts, it should use `IAdminToursApiClient` alongside
`ICatalogTourPresentationApiClient`. It should not move Admin tour calls into the Catalog client.

### PR Review Checklist

Use this checklist when reviewing `Management.Web` changes:

- Does each new page or component have a clear owning workflow?
- Are backend calls routed through typed `I*ApiClient` abstractions?
- Is each typed client scoped to one backend/context owner?
- Are shared components free of backend clients and workflow-specific state?
- Are cross-context dependencies composed at page/workflow boundaries instead of inside lower-level components?
- Does the change avoid coupling `Management.Web` to public website concerns?
- Would the same folder and client ownership still work when a second bounded context is added?

## Dependencies

- **ViajantesTurismo.Admin.ApiService**: Backend API
- **ViajantesTurismo.Admin.Contracts**: Shared DTOs and contracts
- **ViajantesTurismo.Resources**: Shared resource names
- **ViajantesTurismo.ServiceDefaults**: Service discovery and defaults
- **Redis**: Output caching
