# ViajantesTurismo.IntegrationTests

End-to-end tests validating the complete application stack.

## Purpose

Test the full system including database, API, and infrastructure.

## Running Tests

### Run all integration tests

```powershell
dotnet test tests/ViajantesTurismo.IntegrationTests
```

### Run specific test class

```powershell
dotnet test --filter "FullyQualifiedName~BookingApiTests"
```
