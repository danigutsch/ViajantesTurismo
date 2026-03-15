# ViajantesTurismo.AppHost

.NET Aspire orchestration host for the application - coordinates all services, databases, and dependencies.

## Purpose

Application orchestrator using .NET Aspire. Defines service dependencies, health checks, and startup order for local
development and deployment.

## Services Orchestrated

### Infrastructure

- **PostgreSQL**: Database server with PgWeb admin interface
- **Redis**: Caching server with RedisInsight admin interface

### Application Services

- **MigrationService**: Database migrations and seeding
- **Admin.ApiService**: REST API (waits for database + migrations)
- **Admin.Web**: Blazor web UI (waits for API + cache)

## Service Dependencies

```text
PostgreSQL → Database → MigrationService
                     ↓
                  ApiService → Admin.Web ← Redis
```

## Features

- Service discovery
- Health check monitoring at `/health`
- Dependency orchestration with `WaitFor()` and `WaitForCompletion()`
- Admin tools (PgWeb, RedisInsight)

## Running

```powershell
# Preferred when the Aspire CLI is available
aspire run

# Alternative using only the .NET SDK
dotnet run --project src/ViajantesTurismo.AppHost
```

Opens Aspire dashboard showing all services, logs, traces, and metrics.

## Dependencies

- **.NET Aspire**: Orchestration framework
- **ViajantesTurismo.Resources**: Resource name constants
