# Architecture Decisions (ADR Index)

This is the index of architecture decisions for **ViajantesTurismo**. We use short, linkable, versioned ADRs in
`docs/adr/`.

## Conventions

- One decision per file
- File name: `YYYYMMDD-title.md` (e.g., `20251108-domain-validation-factory-methods.md`)
- Sections: Context · Decision · Consequences · Alternatives (if applicable) · Status · Links
- When a decision is replaced, add a **Superseded by** link in the older ADR and a **Supersedes** link in the new one

## Index

### Domain & Validation

- **[ADR-001](adr/20251108-domain-validation-factory-methods.md)** — Domain Validation with Factory Methods
- **[ADR-002](adr/20251108-result-pattern-over-exceptions.md)** — Result Pattern Over Exceptions
- **[ADR-003](adr/20251108-validation-constants-contracts-project.md)** — Validation Constants in Contracts Project
- **[ADR-004](adr/20251108-dedicated-error-classes.md)** — Dedicated Error Classes per Entity

### Code Quality & Testing

- **[ADR-005](adr/20251108-no-comments-domain-code.md)** — No Comments in Domain Code
- **[ADR-006](adr/20251108-type-safety-test-step-definitions.md)** — Type Safety in Test Step Definitions

### Architecture & Layers

- **[ADR-007](adr/20251108-application-layer-mappers-queries.md)** — Application Layer for Mappers and Query Interfaces
- **[ADR-013](adr/20251105-value-objects-tour-creation.md)** — Value Objects for Tour Creation Parameters

### Business Logic & Pricing

- **[ADR-008](adr/20251108-totalprice-calculated-property.md)** — TotalPrice as Calculated Property
- **[ADR-009](adr/20251108-room-pricing-model.md)** — Room Pricing Model - Base Price = Single Room
- **[ADR-010](adr/20251108-discount-value-object.md)** — Discount as Value Object with Audit Trail
- **[ADR-011](adr/20251108-payment-tracking-immutable-records.md)** — Payment Tracking with Immutable Payment Records
- **[ADR-012](adr/20251108-booking-details-update-after-creation.md)** — Booking Details Update After Creation

## Principles

These architectural decisions establish a robust foundation:

### Core Patterns

1. **Factory methods** ensure entities are always in valid state
2. **Result pattern** makes errors explicit and type-safe
3. **Contract constants** provide single source of truth
4. **Error classes** centralize error creation
5. **Value objects** group related parameters and encapsulate validation

### Quality Standards

6. **No comments** keeps code clean and self-documenting (use XML docs for public APIs)
7. **Type-safe testing** handles multiple Result types correctly
8. **Application layer** separates mapping and query concerns from domain

### Business Rules

9. **Calculated properties** ensure derived values are always consistent
10. **Correct pricing model** reflects actual business rules (base price = single room)
11. **Discount value objects** provide flexible pricing with audit trails
12. **Immutable payments** ensure financial integrity and complete history
13. **Post-creation updates** improve customer experience without compromising data integrity

### Architecture Priorities

- Compile-time safety over runtime checks
- Explicit over implicit error handling
- Domain logic centralization
- Clean Architecture layering
- Data consistency and correctness
- Testability and maintainability
