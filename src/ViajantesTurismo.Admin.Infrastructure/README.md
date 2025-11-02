# ViajantesTurismo.Admin.Infrastructure

Data access layer implementing repositories and database operations with Entity Framework Core.

## Purpose

Persistence layer managing database operations, migrations, and data seeding. Implements repository pattern for domain
entities.

## Components

### DbContext

- **ApplicationDbContext**: EF Core context with Tour and Customer entities
- Implements `IUnitOfWork` for transactional operations
- Configures entity relationships and constraints

### Repositories

- **TourStore**: Tour aggregate persistence
- **CustomerStore**: Customer persistence
- **QueryService**: Read operations for API queries

### Seeding

- Initial data population for development
- Test data generation

### Migrations

- Database schema versioning
- Schema evolution tracking

## Database

- **Provider**: PostgreSQL via Npgsql.EntityFrameworkCore.PostgreSQL
- **Connection**: Configured via .NET Aspire service discovery

## Dependencies

- **ViajantesTurismo.Admin.Domain**: Domain entities
- **Entity Framework Core**: ORM and migrations
