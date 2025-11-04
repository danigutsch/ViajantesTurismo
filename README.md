# ViajantesTurismo 🚴‍♂️🌍

A modern tourism agency application specialising in group bike tours around the world.

## Overview

ViajantesTurismo is a comprehensive platform designed for managing and selling group bike tours globally. The
application enables customers to browse, book, and manage their cycling adventures while providing tour operators with
powerful tools to create and manage tour packages.

## Features

- **Tour Management**: Create and manage bike tour packages with detailed itineraries
- **Customer Management**: Comprehensive customer profiles with personal information, accommodation preferences, and medical details
- **Booking Management**: 
    - Create bookings with room type selection (Single/Double)
  - Optional companion for double rooms (supports single occupancy)
    - Bike type selection (Regular/E-bike) with customer preference pre-population
  - Discount support (percentage or absolute amount with audit trail)
  - Automatic total price calculation (base + room + bikes - discount)
  - Domain-driven operations: Confirm, Cancel, Complete bookings
  - Update booking notes, discount, and details after creation
  - Payment tracking with multiple payment methods and automatic status updates
  - Track booking status and payment status (Unpaid → PartiallyPaid → Paid)
- **Multiple Currency Support**: Handle pricing in Brazilian Real (BRL), Euro (EUR), and US Dollar (USD) with proper
  formatting and display in the web frontend
- **Flexible Pricing**:
    - Base tour pricing for single room (not per person)
    - Double room supplement for larger accommodations
    - Regular bike and E-bike rental pricing
  - Discount system (percentage 0-100% or absolute amount)
    - Calculated total price (transparent, consistent)
  - Payment tracking with configurable payment methods
- **Service Packages**: Customisable included services (hotels, meals, guided tours, etc.)
- **RESTful API**: Modern API-first architecture with behaviour-driven endpoints
- **Blazor Web Frontend**: Modern UI with client-side navigation and currency-aware input fields

## Technology Stack

- **.NET 10** (Preview) - Modern cross-platform framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - Database ORM
- **PostgreSQL** - Primary database
- **.NET Aspire** - Cloud-native orchestration
- **Blazor** - Web frontend with client-side routing and modern UX
- **xUnit** - Testing framework
- **OpenAPI/Swagger** - API documentation

## Project Structure

```
ViajantesTurismo/
├── src/
│   ├── ViajantesTurismo.Admin.Domain/         # Domain entities and business logic
│   ├── ViajantesTurismo.Admin.Application/    # Application layer (mappers, interfaces)
│   ├── ViajantesTurismo.Admin.Infrastructure/ # Infrastructure (EF Core, DB context, stores)
│   ├── ViajantesTurismo.AdminApi.Contracts/   # API contracts and DTOs
│   ├── ViajantesTurismo.Admin.ApiService/     # Main API service
│   ├── ViajantesTurismo.Admin.Web/            # Blazor admin web frontend
│   ├── ViajantesTurismo.AppHost/              # Aspire orchestration
│   ├── ViajantesTurismo.Common/               # Shared domain models and utilities
│   ├── ViajantesTurismo.MigrationService/     # Database migration worker
│   ├── ViajantesTurismo.Resources/            # Resource definitions
│   ├── ViajantesTurismo.ServiceDefaults/      # Service defaults and extensions
│   └── ViajantesTurismo.Web/                  # Blazor public web frontend
└── tests/
    ├── ViajantesTurismo.Admin.UnitTests/      # Domain unit tests
    ├── ViajantesTurismo.Admin.BehaviorTests/  # BDD/Gherkin tests
    ├── ViajantesTurismo.Admin.IntegrationTests/ # API integration tests
    └── ViajantesTurismo.Common.UnitTests/     # Common utilities tests
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
  or [Visual Studio 2026 Preview](https://visualstudio.microsoft.com/vs/preview/)
  or [JetBrains Rider](https://www.jetbrains.com/rider/)

### Running the Application

1. **Clone the repository**
   ```powershell
   git clone https://github.com/danigutsch/ViajantesTurismo.git
   cd ViajantesTurismo
   ```

2. **Restore dependencies**
   ```powershell
   dotnet restore
   ```

3. **Restore .NET tools**
   ```powershell
   dotnet tool restore
   ```

4. **Run with Aspire AppHost**
   ```powershell
   dotnet run --project src/ViajantesTurismo.AppHost
   ```

5. **Access the application**
    - API: `https://localhost:7xxx`
    - Web: `https://localhost:7xxx`
    - Aspire Dashboard: `https://localhost:15xxx`

### Running Tests

```powershell
dotnet test
```

### Using the coverage runsettings

A solution-level Coverlet runsettings file is provided at `coverlet.runsettings` (repository root). It excludes EF Core
Migrations and the MigrationService project from coverage.

- Command line (recommended):
    - Collect coverage with the runsettings applied
      ```powershell
      dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory:TestResults
      ```
    - Generate an HTML report (after running tests above)
      ```powershell
      dotnet reportgenerator -reports:"TestResults\**\*.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:Html
      # Open report (Windows)
      Invoke-Item TestResults\CoverageReport\index.html
      ```

- Visual Studio (Windows):
    1) Test → Configure Run Settings → Select Solution Wide runsettings File… → choose `coverlet.runsettings` at the
       solution root.
    2) Run tests with coverage (Test Explorer → Run → Analyze Code Coverage).

- JetBrains Rider:
    - File → Settings → Tools → Unit Testing → Check "Use .runsettings file" and select `coverlet.runsettings`.
    - Or per Run Configuration: Edit Configuration → Tests → Advanced → Settings file.

- VS Code:
    - Update your test task/command to include the settings file:
      ```powershell
      dotnet test --collect:"XPlat Code Coverage" --settings ${workspaceFolder}/coverlet.runsettings
      ```

Notes:

- Keep forward slashes in the globs inside runsettings (e.g., `**/Migrations/**/*.cs`) even on Windows.
- If you change exclusions, commit `coverlet.runsettings` so all environments stay in sync.

## API Endpoints

### Tours API

- `GET /tours` - Retrieve all available tours
- `GET /tours/{id}` - Get tour by ID
- `POST /tours` - Create a new tour package
- `PUT /tours/{id}` - Update a tour

### Customers API

- `GET /customers` - Retrieve all customers
- `GET /customers/{id}` - Get customer by ID
- `POST /customers` - Create a new customer
- `PUT /customers/{id}` - Update customer information

### Bookings API (Domain-Driven)

- `GET /bookings` - Retrieve all bookings
- `GET /bookings/{id}` - Get booking by ID
- `GET /bookings/tour/{tourId}` - Get bookings for a specific tour
- `GET /bookings/customer/{customerId}` - Get bookings for a specific customer
- `POST /bookings` - Create a new booking with optional discount
- `PUT /bookings/{id}/discount` - Update booking discount
- `PUT /bookings/{id}/details` - Update room type, bikes, and companion
- `POST /bookings/{id}/confirm` - Confirm a pending booking
- `POST /bookings/{id}/cancel` - Cancel a booking
- `POST /bookings/{id}/complete` - Complete a confirmed booking
- `PATCH /bookings/{id}/notes` - Update booking notes
- `POST /bookings/{id}/payments` - Record a payment for the booking
- `DELETE /bookings/{id}` - Delete a booking

## Tour Package Information

Each tour includes:

- **Unique Identifier** - Tour code (e.g., "CUBA2024")
- **Name** - Descriptive tour name
- **Dates** - Start and end dates
- **Pricing** - Base price (single room), double room supplement, bike rentals (all prices displayed and entered with
  correct currency formatting)
- **Currency** - Real, Euro, or US Dollar
- **Included Services** - Hotels, meals, activities, etc.

## Architecture

This project follows **Clean Architecture** and **Domain-Driven Design** principles:

- **Domain Layer**: Entities, value objects, domain logic, business rules
- **Application Layer**: Mappers, query interfaces, application orchestration
- **Infrastructure Layer**: EF Core, database, external services
- **API Layer**: HTTP endpoints, DTOs, request/response handling
- **Web Layer**: Blazor UI, forms, user interactions

Key patterns:

- **CQRS**: Separate read (queries) and write (commands) operations
- **Result Pattern**: Explicit error handling without exceptions
- **Factory Methods**: Domain entities ensure valid state from creation
- **Aggregate Roots**: Tour manages all Booking operations

See [docs/ARCHITECTURE_DECISIONS.md](docs/ARCHITECTURE_DECISIONS.md) for detailed architectural decisions.

## Development

### .NET Local Tools

This project uses a .NET tool manifest (`.config/dotnet-tools.json`) to manage local tools. The following tools are
included:

- **dotnet-ef** - Entity Framework Core command-line tools for migrations and database management
- **dotnet-reportgenerator-globaltool** - Code coverage report generator for analyzing test coverage

After cloning the repository, restore the tools with:

```powershell
dotnet tool restore
```

### Running Tests with Coverage

To run tests and generate a code coverage report:

```powershell
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory:TestResults

# Generate HTML coverage report
dotnet reportgenerator -reports:"TestResults\**\*.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:Html

# Open the report (Windows)
Invoke-Item TestResults\CoverageReport\index.html
```

The coverage report provides detailed insights into:

- Line and branch coverage percentages
- Uncovered code sections
- Coverage by project and class
- Historical coverage trends

### Building the Solution

```powershell
dotnet build
```

### Database Migrations

```powershell
# Add migration
dotnet ef migrations add MigrationName --project src/ViajantesTurismo.ApiService

# Update database
dotnet ef database update --project src/ViajantesTurismo.ApiService
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

See [LICENSE.txt](LICENSE.txt) for details.

## Contact

**ViajantesTurismo** - Your gateway to unforgettable cycling adventures around the globe! 🚴‍♀️✨
