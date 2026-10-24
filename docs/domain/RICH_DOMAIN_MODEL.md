# Rich Domain Model

Owner: repository maintainers. Reviewed October 24, 2026.
Scope: tactical modeling rules for the `src/*.Domain` projects: purity, encapsulation, and where business decisions live.
Validation patterns stay in [Domain validation](../DOMAIN_VALIDATION.md). Decisions are recorded in
[ADR-031](../adr/20260703-rich-domain-behavior-and-state-exposure.md) and
[ADR-042](../adr/20261024-ddd-standards-enforcement.md); enforcement rules are cataloged in the
[DDD analyzer rule catalog](../DDD_ANALYZER_RULE_CATALOG.md); current findings are in the
[domain model audit](domain-model-audit.md).

## Contents

- [Vocabulary](#vocabulary)
- [Purity](#purity)
- [Encapsulation](#encapsulation)
- [Behavior placement](#behavior-placement)
- [Smells and fixes](#smells-and-fixes)
- [Refactoring workflow](#refactoring-workflow)
- [Enforcement](#enforcement)
- [Review checklist](#review-checklist)
- [Related Documentation](#related-documentation)

## Vocabulary

- **Rich domain model**: entities and value objects own their validation, calculations, and business rules. Application
  services coordinate work and delegate decisions to the model.
- **Anemic domain model**: objects named after domain nouns that hold data but little behavior, with the rules in service
  classes. Martin Fowler calls this an anti-pattern for complex domains because it pays the cost of a domain model without
  its benefits ([AnemicDomainModel](https://martinfowler.com/bliki/AnemicDomainModel.html)). A deliberately thin model is
  acceptable for read models and simple CRUD-style contexts
  ([Microsoft: rich versus anemic domain model][ms-rich-anemic]).
- **Purity (persistence ignorance)**: domain types do not know how they are stored, transported, serialized, or logged.
- **Encapsulation (always-valid model)**: state changes only through behavior that keeps invariants, and invalid instances
  cannot be constructed.
- **Tell, don't ask**: ask the object to do the work instead of pulling its data out to decide elsewhere. Query methods
  remain useful; co-locating data and behavior is one design factor among several
  ([TellDontAsk](https://martinfowler.com/bliki/TellDontAsk.html)).

## Purity

- Domain projects reference only domain-facing SharedKernel packages (`Domain`, `BuildingBlocks`, `Results`,
  `InputNormalization`, `AuditTrail`, `EventSourcing`, `Domain.SourceGenerator`, `Domain.Analyzers`, `Domain.CodeFixes`)
  and `JetBrains.Annotations`.
- Domain projects never reference EF Core, ASP.NET Core, `System.Text.Json`, DataAnnotations, logging, HTTP, Roslyn, or
  `ViajantesTurismo.*.Contracts.*` projects.
- Store interfaces such as `ITourStore` and `ICustomerStore` are domain ports; their implementations live in Infrastructure.
- Domain code does not read the system clock. Capture the business date or instant at the boundary, or pass the injected
  `TimeProvider` where [Domain validation](../DOMAIN_VALIDATION.md#calendar-dates-and-current-time) allows it. UUIDv7
  identifiers stay with each identity owner.
- EF Core materialization follows [ADR-026](../adr/20260621-domain-materialization-and-sharedkernel-persistence-boundaries.md):
  private parameterless constructors only where EF needs them, and mappings, backing fields, and converters in Infrastructure.

## Encapsulation

- **Properties**: use get-only properties, `private set`, or `init`. Public `init` belongs on records and parameter
  objects; entities keep their constructors private, so callers cannot use object initializers to bypass factories.
- **Collections**: keep a private `List<T>` and expose `IReadOnlyCollection<T>`, or `IReadOnlyList<T>` when order or index
  access is part of the contract, through `.AsReadOnly()`. Add, remove, and reorder through aggregate methods.
- **Construction**: private constructors and a static factory that returns `Result<T>`
  ([ADR-001](../adr/20251108-domain-validation-factory-methods.md)).
- **Aggregate boundary**: child entities change only through their aggregate root. Child factories and mutators are
  `internal`; test projects that exercise them directly get `InternalsVisibleTo`. For example, `Booking` changes through
  `Tour.AddBooking`, `Tour.ConfirmBooking`, and the other `Tour` booking methods.
- **Facts versus decisions**: public state is for display, mapping, persistence, serialization, and simple filtering.
  Decisions are domain methods or calculated facts.

## Behavior placement

| Need | Use | Example |
| --- | --- | --- |
| A pure, stable fact derived from the object's own state | Calculated property | `Tour.AvailableSpots`, `Booking.TotalPrice` |
| A question that decides an outcome or needs context | Method returning `bool` or `Result` | `EditablePublicContent.CanPublish`, `PublicMediaImage.ShouldProcessOriginal` |
| A state change | Intent-revealing method returning `Result` | `Tour.ConfirmBooking` |
| A concept with data and rules | Value object | `TourSchedule`, `Discount` |
| A stable label with little behavior | Enum | `BikeType`, `PaymentMethod` |
| States with different allowed commands, side effects, or errors | Explicit transition methods or a state object | The booking lifecycle behind `Tour` methods |

- Handlers, endpoints, and Razor components must not combine raw domain properties to decide a business outcome. Name the
  decision on the aggregate and call it.
- Enums may be displayed, mapped, sorted, and filtered outside the domain. Branching on a domain enum to decide a business
  outcome belongs behind a domain predicate.
- A rule that depends only on aggregate state belongs in the aggregate, even when one caller needs it today. Rules that need
  persistence or other aggregates stay in application services
  ([ADR-014](../adr/20251112-application-level-invariants.md)).
- Do not add strategy objects, specifications, or state hierarchies for a single simple switch (ADR-031 "Defer").

## Smells and fixes

| Smell | Example | Fix |
| --- | --- | --- |
| Non-private setter on domain state | `public string Name { get; set; }` | Private setter plus an intent-revealing method; `SKDOM001` offers the setter fix |
| Exposed mutable collection | `public IList<string> Services { get; }` | Private list, read-only view, and aggregate methods |
| Child entity changed outside its root | A handler calling `booking.Confirm()` | Make the child member `internal`; call `tour.ConfirmBooking(bookingId)` |
| Handlers repeat a decision from raw state | Two handlers check a document's status and artifact fields | Add one named domain predicate and reuse it |
| UI recomputes domain facts | A page computing available spots from counts | Carry the domain fact on the read DTO |
| Domain rule enforced only in the UI or application layer | A Razor page blocks an operation the aggregate allows | Prove the gap with a failing test, then guard the aggregate |
| Application-layer shadow value object | Request validation that duplicates aggregate rules | Move the rules into the domain type and map once |
| Exceptions for expected business outcomes | Throwing when publication is not ready | Return a `Result` ([ADR-002](../adr/20251108-result-pattern-over-exceptions.md)) |
| Magic numbers that duplicate domain limits | Hard-coded capacity bounds | Use the domain limit constants |

## Refactoring workflow

1. Read the touched flow and the nearest `AGENTS.md`.
2. Prove the gap with a failing test before changing production code: a unit test in the context's `UnitTests` project, a
   behavior specification, or an architecture test. Root `AGENTS.md` requires this for audit findings.
3. Make the smallest domain change. Keep EF mapping changes in Infrastructure and keep display facts public.
4. Move callers to the new behavior and delete the duplicated checks.
5. Run the domain, application, and architecture tests; run Infrastructure or integration tests when persistence shape changes.
6. Record the outcome in the [domain model audit](domain-model-audit.md).

The `rich-domain-model` agent skill in `.agents/skills/rich-domain-model/SKILL.md` follows this workflow.

## Enforcement

| Rule | Mechanism | Where |
| --- | --- | --- |
| No non-private setters | `SKDOM001` analyzer and code fix | `PandaBear.SharedKernel.Domain.Analyzers` and `.CodeFixes`, referenced by every domain project |
| No exposed mutable collections | `SKDOM002`, with CA1002, CA1819, and CA2227 | Same packages plus the .NET SDK analyzers |
| Child entities change through their root | Architecture test | `DddConventionsTests` |
| Purity | Architecture tests | `LayerDependencyTests` |
| Entities and value objects sealed; value objects without public setters | Architecture tests | `DddConventionsTests` |
| Repeated external decisions and enum branching | Review and skill-driven audit | This guide and the [audit](domain-model-audit.md) |

See the [DDD analyzer rule catalog](../DDD_ANALYZER_RULE_CATALOG.md) for examples, exclusions, and suppression policy.

## Review checklist

- [ ] New state changes go through intent-revealing methods that return `Result`.
- [ ] No new non-private setters or exposed mutable collections (the build enforces this).
- [ ] Child entities change only through their aggregate root.
- [ ] Handlers, endpoints, and UI do not combine raw domain state to decide outcomes.
- [ ] Domain projects gain no infrastructure, framework, or contract references.
- [ ] Audit findings were proven with a failing test first.

## Related Documentation

Use the [documentation source-of-truth map](../README.md#source-of-truth-map) for related repository guidance.

[ms-rich-anemic]: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model#rich-domain-model-versus-anemic-domain-model
