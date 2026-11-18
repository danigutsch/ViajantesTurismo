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

- **Components**: Feature-owned pages and workflow components, shared controls, and layout
- **API Clients**: Typed HTTP clients for API communication
    - Parse 400 Bad Request validation errors
    - Map server errors to form field validation
- **State**: The scoped `CustomerCreationState` belongs to the customer creation wizard
- **Reference data**: `CountryService` supplies the shared country selector
- **Helpers**: `UserFacingErrorMessage`, `ValidationSummaryItems`; server validation binding, error summary,
  HTTP outcome copy, and handled-failure reporting come from `PandaBear.SharedKernel.Blazor`

## Module Boundary Governance

`ViajantesTurismo.Management.Web` is the internal management portal for operator workflows. It can compose data from
multiple bounded-context backends, but bounded-context ownership still belongs to backend contracts and typed clients.
This follows [ADR-020](../../docs/adr/20260523-web-frontends-by-audience-not-by-bounded-context.md) and implements the
documented module-boundary and typed-client governance.

### Folder Ownership

Application workflow code lives under `Components/Pages/<Workflow>/`. The component test
project follows the same paths for workflow-specific tests.

| Workflow | Pages, local state, validation, and mapping | Contract clients and test coverage |
| --- | --- | --- |
| Tours | List, detail, create, and edit. Tour pages own their form mapping and use booking components for booking actions | Admin tours, bookings, and customers. See `Components/Pages/Tours` tests |
| Bookings | List, detail, edit, booking forms, badges, pricing preview, and redirect state. Tour and customer details reuse booking forms | Admin bookings, customers, and documents. See `Components/Pages/Bookings` tests |
| Customers | List, detail, edit, and create. Create and edit share customer form models, and the wizard owns `Create` state and progress | Admin customers with bookings and tours on details. See `Components/Pages/Customers` and `Customers/Create` tests |
| Customer import | `Customers/Import` owns the upload page, CSV/header mapping, preview, conflict resolution, and cancellation state | Admin customers. See `Components/Pages/Customers/Import` tests |
| Payments | Account, finance, and instruction pages with their form models and allocation editor | Admin booking payments, payment instructions, accounts, and bookings. See `Components/Pages/Payments` tests |
| Catalog | Tour presentation, media upload and accessibility review, and public content | Catalog tours and public content. See `Components/Pages/Catalog` tests |
| Branding | Theme and logo management under the Branding authorization policy | Branding management. See `Components/Pages/Branding` tests |
| Documents | Review and hold pages, hold validation and controls. The host maps the artifact download proxy | Admin documents. See `Components/Pages/Documents` and artifact proxy endpoint tests |

`Components/Shared/` holds shared controls such as the country selector, confirmation,
validation, and toast components. `Components/Layout/` holds the app shell and navigation.
The host owns security, antiforgery, ticket and token persistence, and the download proxy.
Customers, Catalog, and Payments compose their related registrations through feature startup
extensions. Individual client registrations remain in `Program.cs`. Typed clients and DTOs
belong to their backend contract projects.

### Typed API Clients

All backend calls must go through typed clients; components and UI state services should not call raw `HttpClient`
directly. The Admin contracts provide concise names for their clients.

- `IToursApiClient` for Admin tour operations.
- `IBookingsApiClient` for Admin booking operations.
- `ICustomersApiClient` for Admin customer operations.

Use context-prefixed names for new backend contexts or when a name would otherwise collide:

- `ICatalogToursApiClient` for Catalog presentation and media operations.
- `IAdminToursApiClient` only if an additional `IToursApiClient` owner makes the current concise name ambiguous.

Typed client implementations own HTTP paths, request/response DTO calls, validation-error parsing, and service-discovery
endpoint names. Client interfaces and DTOs stay in the owning backend context's `*.Contracts` project. Components own
user interaction, loading state, and presentation-specific mapping. If one page needs data from two contexts, inject two
typed clients into a page-level service or component instead of creating one mixed-purpose client.

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

- `Components/Shared/` depending on a workflow's typed client or state.
- One workflow service depending on another workflow service.
- A typed client combining endpoints owned by different backend bounded contexts.
- Public website concerns or public SEO/content models leaking into `Management.Web`.

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

- **ViajantesTurismo.Admin.Api**: Backend API
- **ViajantesTurismo.Admin.Contracts.Application**: Shared Admin DTOs
- **ViajantesTurismo.Admin.Contracts.Http**: Admin typed HTTP clients
- **ViajantesTurismo.Catalog.Contracts.Application**: Shared Catalog DTOs
- **ViajantesTurismo.Catalog.Contracts.Http**: Catalog typed HTTP clients
- **ViajantesTurismo.Resources**: Shared resource names
- **ViajantesTurismo.ServiceDefaults**: Service discovery and defaults
- **Redis**: Output caching
