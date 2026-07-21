# DDD Analyzer Rule Catalog

This catalog defines candidate Roslyn rules for domain-model standards. Rules should ship only when
they are precise enough to avoid DTO, read-model, persistence, serializer, and EF materialization
false positives.

## Adoption policy

1. Prefer architecture tests or existing analyzers when they already express the rule.
2. Ship a Roslyn analyzer only for recurring, local, low-noise source patterns.
3. Add analyzer tests before production analyzer changes.
4. Add a code fix only when remediation is mechanical and safe.
5. Keep DDD rules out of test-only analyzer packages.

## Initial rule catalog

| ID | Rule | Severity | Code fix | Notes |
| --- | --- | --- | --- | --- |
| DDD001 | Domain events implementing `IDomainEvent` should end with `DomainEvent` | Warning | Rename type when no conflict exists | Shipped as `SKSTYLE008`. |
| DDD002 | Integration events should end with `IntegrationEvent` | Warning | Rename type when no conflict exists | Applies only to integration-event contracts. |
| DDD003 | Domain entities should not expose public setters for state | Warning | None initially | Must avoid DTOs, EF models, and serializers. |
| DDD004 | Aggregate collections should not expose mutable collection types | Warning | None initially | Prefer architecture tests first. |
| DDD005 | Application layer should not repeat domain invariant checks already named by the domain | Warning | None | Requires design judgment; likely docs/review only. |

## Rule details

### DDD001 domain event suffix

Examples that should report:

```csharp
public sealed record TourCreated(Guid TourId) : IDomainEvent;
```

Examples that should not report:

```csharp
public sealed record TourCreatedDomainEvent(Guid TourId) : IDomainEvent;
```

Suppress only for externally named contracts that cannot be renamed.

### DDD003 entity public setters

Report only domain-layer entity types, not DTOs, read models, EF configuration types, projection
models, deserialization models, generated code, or test fixtures. A safe implementation needs a
domain-project/path filter and tests for each exception surface.

## Follow-up issue template

Each implementation issue should include:

- rule ID and exact symbol/syntax target
- examples and non-examples
- false-positive exclusions
- severity and rollout plan
- code-fix decision
- analyzer tests and public API/release-note updates
