# ViajantesTurismo.Resources

Shared resource name constants for .NET Aspire service discovery.

## Purpose

Centralized resource names used across all projects for service discovery and configuration. Ensures consistent naming
in Aspire orchestration.

## Constants

- **Cache**: Redis cache resource (`"cache"`)
- **DatabaseServer**: PostgreSQL server (`"database-server"`)
- **Database**: Application database (`"database"`)
- **Api**: Admin API service (`"api"`)
- **WebApp**: Blazor web application (`"web-app"`)
- **MigrationService**: Migration service (`"migration"`)

## Usage

```csharp
builder.Services.AddHttpClient<ApiClient>(client => 
    client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"));

builder.AddRedisOutputCache(ResourceNames.Cache);
```

## Dependencies

Zero dependencies - pure constant definitions.
