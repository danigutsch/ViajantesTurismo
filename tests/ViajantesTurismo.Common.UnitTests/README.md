# ViajantesTurismo.Common.UnitTests

Unit tests for shared types and patterns - Result pattern and common enumerations.

## Purpose

Fast, isolated tests for the Result pattern implementation and shared domain types. No dependencies, no external
resources.

## What We Test

### Result Pattern

- Success and failure creation
- Value retrieval and error handling
- Pattern matching behavior
- Type safety guarantees

### Enumerations

- Valid value ranges
- String conversions
- Serialization behavior

## Running Tests

```powershell
# All tests
dotnet test

# Watch mode
dotnet test --watch

# Specific class (MTP filter syntax)
dotnet test --filter-method "*ResultTests*"

# With coverage
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```
