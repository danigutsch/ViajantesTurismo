# ViajantesTurismo.Common

Shared types and patterns used across all projects.

## Contents

### Functional Patterns

- **`Result` / `Result<T>`** — Railway-oriented error handling for operations that can fail
- **`Option<T>`** — Type-safe handling of optional values

See [FUNCTIONAL_PATTERNS.md](FUNCTIONAL_PATTERNS.md) for usage guide.

### Base Types

- **`Entity<TId>`** — Base class for domain entities with identity
- **`ValueObject`** — Base class for immutable value objects compared by attributes

### Enumerations

Common enums: `BedType`, `BikeType`, `BookingStatus`, `Currency`, `Gender`, `PaymentStatus`, `RoomType`.

See [Domain Glossary](../../docs/domain/GLOSSARY.md) for definitions.

## Dependencies

None — pure .NET BCL.
