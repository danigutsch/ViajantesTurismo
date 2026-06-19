# ViajantesTurismo.Common

Shared types and patterns used across all projects.

## Contents

See [SharedKernel.Results](../SharedKernel/SharedKernel.Results/README.md) for result and option usage guidance.

### Base Types

- **`Entity<TId>`** — Base class for domain entities with identity
- **`ValueObject`** — Base class for immutable value objects compared by attributes

### Enumerations

Common enums: `BedType`, `BikeType`, `BookingStatus`, `Currency`, `Gender`, `PaymentStatus`, `RoomType`.

See [Domain Glossary](../../docs/domain/GLOSSARY.md) for definitions.

## Dependencies

None — pure .NET BCL.
