# Specifications (Behavior Tests)

This directory contains Gherkin feature files that define the behavior specifications for the ViajantesTurismo Admin
domain.

## Purpose

Feature files serve as:

- **Living Documentation** — Human-readable specifications of domain behavior
- **Executable Tests** — Automated tests via Reqnroll
- **Ubiquitous Language** — Shared vocabulary between domain experts and developers
- **Requirements Traceability** — Direct link between requirements and implementation

## Organization

Feature files are organized by domain concept:

### Tour Aggregate

- `Tour*.feature` — Tour creation, updates, pricing, scheduling
- `Booking*.feature` — Booking lifecycle, validation, state transitions
- `Payment*.feature` — Payment recording, tracking, status transitions
- `Companion*.feature` — Companion booking scenarios

### Customer Aggregate

- `Customer*.feature` — Customer creation and updates
- `*Validation.feature` — Validation rules for customer value objects (PersonalInfo, ContactInfo, etc.)

## Test Execution

The feature files are executed as part of the behavior test suite:

```powershell
# Run all behavior tests
dotnet test

# Run specific feature
dotnet test --filter "FullyQualifiedName~TourCreation"
```

## Conventions

- Use **Given-When-Then** structure for scenarios
- Write scenarios from the user/business perspective
- Use ubiquitous language from `docs/GLOSSARY.md`
- Keep scenarios focused on a single behavior
- Use Scenario Outlines for data-driven tests

## Related Documentation

- [Domain Glossary](../../../docs/GLOSSARY.md) — Ubiquitous language definitions
- [Aggregates](../../../docs/domain/AGGREGATES.md) — Aggregate design and invariants
- [Test Guidelines](../../../docs/TEST_GUIDELINES.md) — Testing strategy and patterns
