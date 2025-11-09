# ViajantesTurismo.Admin.Application

Application layer providing mapping logic, query interfaces, and application orchestration between domain and
infrastructure layers.

## Purpose

Separates application concerns (mapping, queries, transactions) from pure domain logic. Implements the Application layer
in Clean Architecture, coordinating between domain entities and API/infrastructure layers.

## Components

### Interfaces

- **IQueryService**: Read-only query operations returning DTOs
    - GetAllTours(), GetTourById(), GetAllCustomers(), GetCustomerById()
    - GetAllBookings(), GetBookingById(), GetBookingsByTourId(), GetBookingsByCustomerId()
    - Optimized for read operations with direct DTO projections

- **IUnitOfWork**: Transaction management interface
    - SaveEntities(CancellationToken) - Commits all changes as a single transaction
    - Implemented by ApplicationDbContext in Infrastructure layer

- **ITourStore**: Tour aggregate repository interface
    - GetById(), GetAll(), Add(), Remove()
    - GetByBookingId() - Retrieves tour by contained booking ID

- **ICustomerStore**: Customer aggregate repository interface
    - GetById(), GetAll(), Add(), Remove()

### Mappers

Static mapper classes for domain ↔ DTO conversions:

- **TourMapper**: Tour entity ↔ TourDto, CreateTourDto, UpdateTourDto
    - Maps Currency enum, included services, pricing properties

- **CustomerMapper**: Customer entity ↔ CustomerDto, CreateCustomerDto, UpdateCustomerDto
    - Maps complex value objects (PersonalInfo, ContactInfo, Address, etc.)

- **BookingMapper**: Booking entity ↔ BookingDto, CreateBookingDto
    - Maps BikeType, RoomType, BookingStatus, PaymentStatus enums
    - Handles BookingCustomer value object mapping

## CQRS Pattern

This layer enforces **Command Query Responsibility Segregation**:

### Queries (Reads)

- Use **IQueryService** only
- Return DTOs optimized for presentation
- No writes, no business logic
- Used by GET endpoints

### Commands (Writes)

- Use **Stores** (ITourStore, ICustomerStore) only
- Work with full aggregate roots
- Enforce domain business rules
- Used by POST, PUT, PATCH, DELETE endpoints

**Important**: API layer should NEVER use both IQueryService and Stores in the same method. Queries are read-only;
commands use stores.

## Mapping Strategy

Mappers are static classes with static methods to avoid unnecessary instance creation:

```csharp
// Domain → DTO
var dto = TourMapper.MapToDto(tour);

// DTO → Domain (via factory method)
var result = Tour.Create(dto.Identifier, dto.Name, ...);

// Enum mapping
var bikeType = BookingMapper.MapToBikeType(dto.BikeType);
var roomType = BookingMapper.MapToRoomType(dto.RoomType);
```

## Dependencies

- **ViajantesTurismo.Admin.Domain**: Domain entities and business logic
- **ViajantesTurismo.AdminApi.Contracts**: DTOs and contract definitions

## Architecture Position

```text
┌─────────────────────────────────────┐
│     API Layer (HTTP Endpoints)      │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│   Application Layer (THIS PROJECT)  │
│   • Mappers (DTO ↔ Domain)         │
│   • IQueryService (Read ops)       │
│   • IUnitOfWork (Transactions)     │
│   • Store Interfaces               │
└─────────┬───────────────┬───────────┘
          │               │
┌─────────▼─────────┐ ┌──▼────────────┐
│  Domain Layer     │ │ Infrastructure│
│  (Business Logic) │ │ (EF Core, DB) │
└───────────────────┘ └───────────────┘
```

## Usage Example

```csharp
// Query operation (GET endpoint)
private static async Task<Ok<IReadOnlyList<GetTourDto>>> GetAllTours(
    IQueryService queryService,
    CancellationToken ct)
{
    var tours = await queryService.GetAllTours(ct);
    return TypedResults.Ok(tours);
}

// Command operation (POST endpoint)
private static async Task<Results<Created<GetTourDto>, ValidationProblem>> CreateTour(
    CreateTourDto dto,
    ITourStore tourStore,
    IQueryService queryService,
    IUnitOfWork unitOfWork,
    CancellationToken ct)
{
    // 1. Map DTO to domain
    var result = Tour.Create(
        dto.Identifier,
        dto.Name,
        dto.StartDate,
        dto.EndDate,
        dto.Price,
        dto.DoubleRoomSupplementPrice,
        dto.RegularBikePrice,
        dto.EBikePrice,
        TourMapper.MapToCurrency(dto.Currency),
        dto.IncludedServices
    );

    if (result.IsFailure)
        return result.ToValidationProblem();

    // 2. Persist through store
    var tour = result.Value;
    tourStore.Add(tour);
    await unitOfWork.SaveEntities(ct);

    // 3. Return DTO via query service
    var createdTourDto = await queryService.GetTourById(tour.Id, ct);
    return TypedResults.Created($"/tours/{tour.Id}", createdTourDto);
}
```

## Design Principles

- **Single Responsibility**: Each mapper handles one aggregate type
- **Stateless**: All methods are static (no instance state)
- **Explicit Conversions**: No implicit casting between domain and DTOs
- **Type Safety**: Enum mappings prevent invalid values
- **Clean Architecture**: Application layer sits between API and Domain/Infrastructure

## See Also

- [Domain Layer README](../ViajantesTurismo.Admin.Domain/README.md)
- [Infrastructure Layer README](../ViajantesTurismo.Admin.Infrastructure/README.md)
- [API Layer README](../ViajantesTurismo.Admin.ApiService/README.md)
- [Architecture Decisions](../../docs/ARCHITECTURE_DECISIONS.md)
- [Coding Guidelines](../../docs/CODING_GUIDELINES.md)
