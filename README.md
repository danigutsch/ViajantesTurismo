# ViajantesTurismo 🚴‍♂️🌍

A modern tourism agency application specializing in group bike tours around the world.

## Overview

ViajantesTurismo is a comprehensive platform designed for managing and selling group bike tours globally. The
application enables customers to browse, book, and manage their cycling adventures while providing tour operators with
powerful tools to create and manage tour packages.

## Features

- **Tour Management**: Create and manage bike tour packages with detailed itineraries
- **Multiple Currency Support**: Handle pricing in Brazilian Real (BRL), Euro (EUR), and US Dollar (USD) with proper
  formatting and display in the web frontend
- **Flexible Pricing**:
    - Base tour pricing per person
    - Single room supplement options
    - Regular bike and E-bike rental pricing
- **Service Packages**: Customizable included services (hotels, meals, guided tours, etc.)
- **RESTful API**: Modern API-first architecture for seamless integrations
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
│   ├── ViajantesTurismo.Admin.Domain/         # Domain interfaces and models
│   ├── ViajantesTurismo.Admin.Infrastructure/ # Infrastructure (EF Core, DB context, stores)
│   ├── ViajantesTurismo.AdminApi.Contracts/   # API contracts and DTOs
│   ├── ViajantesTurismo.ApiService/           # Main API service
│   ├── ViajantesTurismo.AppHost/              # Aspire orchestration
│   ├── ViajantesTurismo.Common/               # Shared domain models
│   ├── ViajantesTurismo.MigrationService/     # Database migration worker
│   ├── ViajantesTurismo.Resources/            # Resource definitions
│   ├── ViajantesTurismo.ServiceDefaults/      # Service defaults and extensions
│   └── ViajantesTurismo.Web/                  # Blazor web frontend
└── tests/
    └── ViajantesTurismo.IntegrationTests/     # Integration tests
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

## API Endpoints

### Tours API

- `GET /tours` - Retrieve all available tours
- `POST /tours` - Create a new tour package

## Tour Package Information

Each tour includes:

- **Unique Identifier** - Tour code (e.g., "CUBA2024")
- **Name** - Descriptive tour name
- **Dates** - Start and end dates
- **Pricing** - Base price, single room supplement, bike rentals (all prices displayed and entered with correct currency
  formatting)
- **Currency** - Real, Euro, or US Dollar
- **Included Services** - Hotels, meals, activities, etc.

## Development

### .NET Local Tools

This project uses a .NET tool manifest (`.config/dotnet-tools.json`) to manage local tools. The following tools are
included:

- **dotnet-ef** - Entity Framework Core command-line tools for migrations and database management

After cloning the repository, restore the tools with:

```powershell
dotnet tool restore
```

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
