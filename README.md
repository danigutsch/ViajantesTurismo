# ViajantesTurismo 🚴‍♂️🌍

[![CI](https://github.com/danigutsch/ViajantesTurismo/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/danigutsch/ViajantesTurismo/actions/workflows/ci.yml)
[![Secret Scan](https://github.com/danigutsch/ViajantesTurismo/actions/workflows/secret-scan.yml/badge.svg?branch=main)](https://github.com/danigutsch/ViajantesTurismo/actions/workflows/secret-scan.yml)
[![SonarCloud](https://sonarcloud.io/api/project_badges/measure?project=danigutsch_ViajantesTurismo&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=danigutsch_ViajantesTurismo)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=danigutsch_ViajantesTurismo&metric=coverage)](https://sonarcloud.io/summary/new_code?id=danigutsch_ViajantesTurismo)
[![License](https://img.shields.io/github/license/danigutsch/ViajantesTurismo)](https://github.com/danigutsch/ViajantesTurismo/blob/main/LICENSE.txt)

A modern tourism agency application specialising in group bike tours around the world.

## Overview

ViajantesTurismo is a platform for operating and selling group bike tours. It combines an
admin API, a Blazor frontend, and supporting services so teams can manage tours,
travellers, bookings, and payments in one place.

## Features

- **Tours and services**: Create tour packages, itineraries, and included service bundles.
- **Customer profiles**: Store traveller details, preferences, and operational notes.
- **Booking lifecycle**: Create bookings, choose room and bike options, apply discounts,
    and move reservations through confirm, cancel, and complete flows.
- **Pricing and payments**: Support BRL, EUR, and USD pricing, room supplements, bike
    rental options, and payment status tracking.
- **Admin surfaces**: Work through a resource-oriented API and a Blazor-based web
    frontend.

## Technology Stack

- **Application**: .NET 10, ASP.NET Core, Blazor Server, and .NET Aspire for the API,
    admin UI, orchestration, and observability.
- **Persistence**: Entity Framework Core with PostgreSQL for relational storage and
    migrations.
- **Quality**: xUnit v3 and Microsoft.Testing.Platform for unit, integration, behavior,
    and end-to-end testing.
- **Contracts**: OpenAPI for endpoint discovery and API exploration.

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
├── tests/
│   ├── ViajantesTurismo.Admin.UnitTests/           # Domain unit tests
│   ├── ViajantesTurismo.Admin.BehaviorTests/       # BDD/Gherkin tests
│   ├── ViajantesTurismo.Admin.IntegrationTests/    # API integration tests
│   └── ViajantesTurismo.Common.UnitTests/          # Common utilities tests
└── benchmarks/
    └── SharedKernel.Mediator.Benchmarks/           # Source-generator benchmark harness
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
- ✅ Verify PowerShell availability for Playwright browser installation
- ✅ Explain the Playwright browser install step (`bash scripts/install-playwright.sh` after build)
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

# 4. Build once so the generated Playwright installer exists
dotnet build ViajantesTurismo.slnx --no-restore

# 5. Install Playwright browsers (requires pwsh on Linux/macOS)
bash scripts/install-playwright.sh

# 6. Install Node.js dependencies (optional)
npm install

# 7. Install PowerShell linting (optional, Windows only)
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser

# 8. Install git hooks (optional)
.\scripts\install-git-hooks.ps1   # Windows (PowerShell)
bash scripts/install-git-hooks.sh # Unix/Linux/macOS (Bash)
```

On Linux, `pwsh` is required for the generated Playwright installer, and Aspire HTTPS
trust may also require `SSL_CERT_DIR` to include `$HOME/.aspnet/dev-certs/trust`.
The setup scripts check both conditions.

On Linux distributions where `bash scripts/install-playwright.sh` skips
`install --with-deps` (for example Ubuntu 25.10), browser download alone is not
enough. Install the runtime libraries manually before running E2E tests:

```bash
sudo apt-get update
sudo apt-get install -y libnspr4 libnss3 libasound2t64
```

See `setup-dev.ps1` or `setup-dev.sh` for detailed steps.

### Optional: Dev Containers

If you prefer a containerized development environment, the repository includes a
VS Code Dev Container configuration. For prerequisites, lifecycle behavior, the shared
local smoke command, and the minimum validation checklist, see
[docs/DEVCONTAINERS.md](docs/DEVCONTAINERS.md).

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

**Run SharedKernel mediator benchmarks:**

```powershell
dotnet run --project benchmarks/SharedKernel.Mediator.Benchmarks/SharedKernel.Mediator.Benchmarks.csproj -c Release -- --filter *DiscoveryBenchmarks*
```

**NuGet lock files:**

This repository commits `packages.lock.json` for its .NET projects so CI can use
locked-mode restore and built-in NuGet caching. If you add, remove, or update a
NuGet package or a project reference that affects package resolution, regenerate the
lock files from the repository root before opening a pull request:

```powershell
dotnet restore ViajantesTurismo.slnx --force-evaluate
```

After updating the lock files, verify the solution still restores and builds cleanly:

```powershell
dotnet restore ViajantesTurismo.slnx --locked-mode
dotnet build ViajantesTurismo.slnx --no-restore
```

**Run All Quality Checks:**

```powershell
npm run lint:all
```

See [docs/CODE_QUALITY.md](docs/CODE_QUALITY.md) for tool configuration and linting usage,
[docs/TEST_GUIDELINES.md](docs/TEST_GUIDELINES.md) for testing strategy and patterns, and
[tests/README.md](tests/README.md) for coverage collection, MTP filtering, and test-project
specific guidance.

## Continuous Integration

Every pull request and push to `main` is validated by GitHub Actions. The main validation workflow
is `.github/workflows/ci.yml`, with additional governance workflows for dependency review, secret
scanning, workflow linting, and supplemental devcontainer checks.

Protected-branch governance also requires verified signed commits for merges to `main`.
This repository documents GPG as the recommended contributor signing path while still
accepting other GitHub-verified signature types. See
[CONTRIBUTING.md](CONTRIBUTING.md) and [docs/ci/governance.md](docs/ci/governance.md)
for the workflow details and merge-method constraints.

The required checks on `main` are:

- `Build and Test` — build, tests, coverage, and integrated SonarCloud analysis; docs-only changes
    use a lightweight success path
- `Lint` — repository lint suite (`npm run lint:all`)
- `Dependency Review`
- `Secret Scan`
- `SonarCloud`

To reproduce the core checks locally, run:

```powershell
dotnet build ViajantesTurismo.slnx
dotnet test --solution ViajantesTurismo.slnx
npm run lint:all
```

For CI internals and maintainer-facing policy — including workflow structure, docs-only
optimizations, artifact behavior, permissions, fork PR handling, branch protection, and
SonarCloud operational details — see [docs/ci/overview.md](docs/ci/overview.md).

## API Endpoints

The Admin API exposes resource-oriented endpoints for tours, customers, bookings, and
payments. When the application is running, OpenAPI is available at `/openapi/v1.json`.

Primary endpoint groups include:

- **Tours**: Tour packages, itineraries, and pricing data
- **Customers**: Traveller profiles and preferences
- **Bookings**: Creation plus confirm, cancel, and complete operations
- **Payments**: Payment recording and status tracking

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

See the Native AOT Compatibility section in
[docs/CODING_GUIDELINES.md](docs/CODING_GUIDELINES.md) for Native AOT guidance and
[docs/ARCHITECTURE_DECISIONS.md](docs/ARCHITECTURE_DECISIONS.md) for the deeper architecture
record.

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
```

Always use `ViajantesTurismo.MigrationService` as the startup project for EF Core commands. For
additional migration and seeding guidance, see
[src/ViajantesTurismo.MigrationService/README.md](src/ViajantesTurismo.MigrationService/README.md).

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for the pull request workflow,
Conventional Commits policy, and local validation expectations.

## License

See [LICENSE.txt](LICENSE.txt) for details.

## Contact

**ViajantesTurismo** - Your gateway to unforgettable cycling adventures around the globe! 🚴‍♀️✨
