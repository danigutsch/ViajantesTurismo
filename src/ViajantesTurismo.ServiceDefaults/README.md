# ViajantesTurismo.ServiceDefaults

Shared service configuration and defaults for all application services.

## Purpose

Common service configurations used across API and Web projects. Provides consistent setup for telemetry, health checks,
resilience, and service discovery.

## Features

### OpenTelemetry

- Distributed tracing
- Metrics collection
- Logging integration
- ASP.NET Core instrumentation
- HTTP client instrumentation

### Health Checks

- `/health` - Full health check endpoint
- `/alive` - Liveness probe for orchestration

### Resilience

- HTTP client retry policies
- Circuit breaker patterns
- Timeout handling

### Service Discovery

- .NET Aspire service discovery integration
- Automatic service endpoint resolution

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();  // Adds all defaults

var app = builder.Build();
app.MapDefaultEndpoints();  // Adds /health and /alive
```

## Dependencies

- **.NET Aspire**: Service defaults and discovery
- **OpenTelemetry**: Observability
- **Microsoft.Extensions.Http.Resilience**: HTTP resilience
