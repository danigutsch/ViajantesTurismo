# ViajantesTurismo 🚴‍♂️🌍

A modern tourism agency application specialising in group bike tours around the world.

## Overview

ViajantesTurismo is a comprehensive platform designed for managing and selling group bike tours globally. The
application enables customers to browse, book, and manage their cycling adventures while providing tour operators with
powerful tools to create and manage tour packages.

## Features

- **Tour Management**: Create and manage bike tour packages with detailed itineraries
- **Customer Management**: Comprehensive customer profiles with personal information, accommodation preferences,
  and medical details
- **Booking Management**:
    - Create bookings with room type selection and optional companion
    - Bike type selection with customer preference pre-population
    - Discount support with audit trail
    - Automatic total price calculation
    - Domain-driven operations: Confirm, Cancel, Complete bookings
    - Update booking notes, discount, and details after creation
    - Payment tracking with multiple payment methods and automatic status updates
- **Multiple Currency Support**: Handle pricing in Brazilian Real (BRL), Euro (EUR), and US Dollar (USD) with proper
  formatting and display in the web frontend
- **Flexible Pricing**:
    - Base tour pricing for double occupancy (not per person)
    - Single room supplement for solo travellers
    - Bike rental pricing options
    - Discount system with percentage or absolute amount of support
    - Calculated total price (transparent, consistent)
        - Payment tracking with configurable payment methods
- **Service Packages**: Customisable included services (hotels, meals, guided tours, etc.)
- **RESTful API**: Modern API-first architecture with behaviour-driven endpoints
- **Blazor Web Frontend**: Modern UI with client-side navigation and currency-aware input fields

## Technology Stack

### Frameworks & Runtime

- **.NET 10** - Modern cross-platform framework
- **ASP.NET Core** - Web API with Native AOT preparation
- **Blazor Server** - Web frontend with client-side routing
- **.NET Aspire** - Cloud-native orchestration and observability

### Data & Persistence

- **Entity Framework Core** - ORM with compiled models for AOT
- **PostgreSQL** - Primary database

### Testing

- **xUnit v3** - Unit, integration, behavior, and E2E testing framework
- **Microsoft.Testing.Platform (MTP)** - Test host/runner integration with built-in coverage support

### API & Documentation

- **OpenAPI** - API documentation and exploration

## Project Structure

```text
ViajantesTurismo/
├── src/
│   ├── ViajantesTurismo.Admin.Domain/              # Domain entities and business logic
│   ├── ViajantesTurismo.Admin.Application/         # Application layer (mappers, interfaces)
│   ├── ViajantesTurismo.Admin.Infrastructure/      # Infrastructure (EF Core, DB context, stores)
│   ├── ViajantesTurismo.Admin.Contracts/           # API contracts and DTOs
│   ├── ViajantesTurismo.Admin.ApiService/          # Main API service
│   ├── ViajantesTurismo.Admin.Web/                 # Blazor admin web frontend
│   ├── ViajantesTurismo.AppHost/                   # Aspire orchestration
│   ├── ViajantesTurismo.Common/                    # Shared domain models and utilities
│   ├── ViajantesTurismo.MigrationService/          # Database migration worker
│   ├── ViajantesTurismo.Resources/                 # Resource definitions
│   └── ViajantesTurismo.ServiceDefaults/           # Service defaults and extensions
└── tests/
    ├── ViajantesTurismo.Admin.UnitTests/           # Domain unit tests
    ├── ViajantesTurismo.Admin.BehaviorTests/       # BDD/Gherkin tests
    ├── ViajantesTurismo.Admin.IntegrationTests/    # API integration tests
    └── ViajantesTurismo.Common.UnitTests/          # Common utilities tests
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) - Version specified in `global.json`
- [Node.js](https://nodejs.org/) (LTS) - For Markdown linting and documentation tools
- Container runtime (for PostgreSQL):
    - [Podman](https://podman.io/) (recommended, open source) or
    - [Docker Desktop](https://www.docker.com/products/docker-desktop)
- IDE:
    - [Visual Studio Code](https://code.visualstudio.com/) (recommended, open source) or
    - [Visual Studio 2022](https://visualstudio.microsoft.com/) or
    - [Visual Studio 2026 Preview](https://visualstudio.microsoft.com/vs/preview/) or
    - [JetBrains Rider](https://www.jetbrains.com/rider/)

**Note for VS Code users:** Open the workspace using `ViajantesTurismo.code-workspace`
for the best development experience.

### Quick Setup

Run the automated setup script to install all required dependencies:

```powershell
# Windows (PowerShell)
.\setup-dev.ps1

# Unix/Linux/macOS (Bash)
bash setup-dev.sh
```

This script will:

- ✅ Verify .NET SDK version (from `global.json`)
- ✅ Restore .NET dependencies (`dotnet restore`)
- ✅ Restore .NET local tools (`dotnet tool restore` - includes dotnet-ef, reportgenerator)
- ✅ Install PSScriptAnalyzer for PowerShell linting (PowerShell only)
- ✅ Install npm packages (markdownlint-cli, shellcheck, shfmt)
- ✅ Install git pre-commit hook for automatic code quality checks

**Options (PowerShell only):**

```powershell
# Skip git hook installation
.\setup-dev.ps1 -SkipGitHook

# Skip npm installation
.\setup-dev.ps1 -SkipNpm
```

### Manual Setup (Alternative)

If you prefer manual setup or the automated script doesn't work:

```bash
# 1. Verify .NET SDK version matches global.json
dotnet --version

# 2. Clone and navigate to repository
git clone https://github.com/danigutsch/ViajantesTurismo.git
cd ViajantesTurismo

# 3. Restore .NET dependencies and tools
dotnet restore
dotnet tool restore

# 4. Install Node.js dependencies (optional)
npm install

# 5. Install PowerShell linting (optional, Windows only)
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser

# 6. Install git hooks (optional)
.\scripts\install-git-hooks.ps1   # Windows (PowerShell)
bash scripts/install-git-hooks.sh # Unix/Linux/macOS (Bash)
```

See `setup-dev.ps1` or `setup-dev.sh` for detailed steps.

### Running the Application

```powershell
# Preferred when using the repo-pinned local .NET tool manifest
dotnet tool run aspire run

# Alternative using only the .NET SDK
dotnet run --project src/ViajantesTurismo.AppHost

# If you installed Aspire CLI globally or via the install script
aspire run
```

`aspire.cli` is pinned in `.config/dotnet-tools.json` as a **local .NET tool**, so the repository-safe command is
`dotnet tool run aspire run`. If you install Aspire globally or via the official install script, it adds a standalone
`aspire` command to your shell `PATH`, and then `aspire run` works directly.

**Access the application:**

- API and web endpoints are assigned dynamically by Aspire
- Aspire Dashboard: shown in terminal output when the app starts

For service-specific details, see:

- [AppHost README](src/ViajantesTurismo.AppHost/README.md)
- [Admin API README](src/ViajantesTurismo.Admin.ApiService/README.md)
- [Admin Web README](src/ViajantesTurismo.Admin.Web/README.md)

### Development Workflow

**Code Formatting:**

```powershell
dotnet format
```

**Run Tests:**

```powershell
# All tests in the solution
dotnet test --solution ViajantesTurismo.slnx

# Single project (recommended when iterating)
dotnet test --project tests/ViajantesTurismo.Admin.UnitTests/ViajantesTurismo.Admin.UnitTests.csproj
```

**With Code Coverage:**

```powershell
dotnet test --solution ViajantesTurismo.slnx -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml
```

With **xUnit v3 + Microsoft.Testing.Platform (MTP)**, a solution-level test run writes one
`coverage.cobertura.xml` file **per test project** into that project's `TestResults` folder.
It does **not** produce a single root-level coverage file.

To generate one human-readable HTML report from all test projects, aggregate those per-project
Cobertura files with `reportgenerator`:

```powershell
reportgenerator -reports:"tests/**/TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:"Html"
```

Open `TestResults/CoverageReport/index.html` after generation.

**MTP/xUnit v3 note:** When passing test-host options (coverage, Playwright launch options, etc.), place them **after**
 `--`.
For project-specific runs, prefer `--project <path-to-csproj>` instead of positional project paths.

**Run All Quality Checks:**

```powershell
npm run lint:all
```

See [docs/CODE_QUALITY.md](docs/CODE_QUALITY.md) for detailed tool configuration and usage, and
[docs/TEST_GUIDELINES.md](docs/TEST_GUIDELINES.md) for testing strategy and patterns.

## Continuous Integration

Every pull request and push to `main` is validated by a GitHub Actions workflow
(`.github/workflows/ci.yml`). The workflow runs two parallel jobs:

| Job | What it does |
| --- | --- |
| **Build and Test** | Provisions .NET and Node, restores, builds, trusts HTTPS dev cert, runs all tests, uploads test result artifacts |
| **Lint** | Provisions Node, installs npm dependencies, runs `npm run lint:all` |

### Reproducing CI locally

The CI commands map directly to local commands:

```powershell
# Build and test (mirrors the Build and Test job)
dotnet restore ViajantesTurismo.slnx
dotnet build ViajantesTurismo.slnx --no-restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
dotnet test --solution ViajantesTurismo.slnx --no-build

# Lint (mirrors the Lint job)
npm ci
npm run lint:all
```

### Required status checks

Once branch protection is configured, require both of these job names:

- `Build and Test`
- `Lint`

See [docs/CI_GOVERNANCE_ROLLOUT.md](docs/CI_GOVERNANCE_ROLLOUT.md) for the action versioning policy and
governance details.

## API Endpoints

The API provides RESTful endpoints for managing tours, customers, and bookings.

**Key endpoints:**

- **Tours**: CRUD operations for tour packages
- **Customers**: Customer profile management
- **Bookings**: Domain-driven booking lifecycle (create, confirm, cancel, complete)
- **Payments**: Payment recording and tracking

See the OpenAPI documentation at `/openapi/v1.json` when running the application.

For detailed business rules and domain operations, see:

- [Domain Validation](docs/DOMAIN_VALIDATION.md) - Validation patterns and rules
- [Aggregates](docs/domain/AGGREGATES.md) - Business invariants and operations
- [Glossary](docs/domain/GLOSSARY.md) - Domain terminology and concepts

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
- **Factory Methods**: Domain entities ensure a valid state from creation
- **Aggregate Roots**: Tour manages all Booking operations
- **AOT Compatibility**: Library projects prepared for Native AOT with trim analyzers enabled

### Native AOT Readiness

The solution is prepared for Native AOT compilation:

- **Library projects**: `IsAotCompatible=true` with no trim warnings
- **API project**: Uses `CreateSlimBuilder`, JSON source generators, and Request Delegate Generator
- **EF Core**: Compiled models generated for performance
- **Blocked**: Full AOT publishing blocked by EF Core reflection requirements (tracked in PBI-003)

See [docs/CODING_GUIDELINES.md](docs/CODING_GUIDELINES.md#native-aot-compatibility) for AOT development guidelines.

See [docs/ARCHITECTURE_DECISIONS.md](docs/ARCHITECTURE_DECISIONS.md) for detailed architectural decisions.

## Development

### Building the Solution

```powershell
dotnet build ViajantesTurismo.slnx
```

### Database Migrations

```powershell
# Add migration (run from repository root)
dotnet ef migrations add MigrationName --project src/ViajantesTurismo.Admin.Infrastructure --startup-project src/ViajantesTurismo.MigrationService

# Update database (run from repository root)
dotnet ef database update --project src/ViajantesTurismo.Admin.Infrastructure --startup-project src/ViajantesTurismo.MigrationService

# Alternative: Navigate to Infrastructure project directory
cd src/ViajantesTurismo.Admin.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../ViajantesTurismo.MigrationService
dotnet ef database update --startup-project ../ViajantesTurismo.MigrationService
```

**Note:** Always use `ViajantesTurismo.MigrationService` as the startup project for EF Core commands. Migrations are
stored in `ViajantesTurismo.Admin.Infrastructure/Migrations/`.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

See [LICENSE.txt](LICENSE.txt) for details.

## Contact

**ViajantesTurismo** - Your gateway to unforgettable cycling adventures around the globe! 🚴‍♀️✨
