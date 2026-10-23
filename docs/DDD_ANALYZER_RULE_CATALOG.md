# DDD Analyzer Rule Catalog

Owner: repository maintainers. Reviewed October 24, 2026.
Scope: enforcement rules for the domain-model standards in the [rich domain model guide](domain/RICH_DOMAIN_MODEL.md)
and [ADR-031](adr/20260703-rich-domain-behavior-and-state-exposure.md). The enforcement split is decided in
[ADR-042](adr/20261024-ddd-standards-enforcement.md). Rules ship only when they are precise enough to avoid DTO,
read-model, persistence, serializer, and EF materialization false positives.

## Contents

- [Adoption policy](#adoption-policy)
- [Rule catalog](#rule-catalog)
- [Choosing a mechanism](#choosing-a-mechanism)
- [Exclusions](#exclusions)
- [Suppression policy](#suppression-policy)
- [Prioritization](#prioritization)
- [Rule details](#rule-details)
- [Research summary](#research-summary)
- [Follow-up issue template](#follow-up-issue-template)
- [Related Documentation](#related-documentation)

## Adoption policy

1. Prefer architecture tests or existing analyzers when they already express the rule.
2. Ship a Roslyn analyzer only for recurring, local, low-noise source patterns.
3. Add analyzer tests before production analyzer changes.
4. Add a code fix only when remediation is mechanical and safe.
5. Keep DDD rules out of test-only analyzer packages. Reusable analyzers and code fixes belong in SharedKernel.

## Rule catalog

| ID | Rule | Mechanism | Severity | Code fix | Status | Roadmap |
| --- | --- | --- | --- | --- | --- | --- |
| DDD001 | Domain events implementing `IDomainEvent` end with `DomainEvent` | `SKSTYLE008` in `PandaBear.SharedKernel.Style.Analyzers` | Warning | Rename when no conflict exists | Shipped | — |
| DDD002 | Integration events end with `IntegrationEvent` | Analyzer candidate | Warning | Rename when no conflict exists | Candidate | — |
| DDD003 | Domain types do not expose non-private, non-`init` setters | `SKDOM001` in `PandaBear.SharedKernel.Domain.Analyzers` | Warning | Make setter private when safe | Shipped | RM-149, RM-152 |
| DDD004 | Domain types do not expose mutable collections outside their assembly | `SKDOM002`, plus SDK rules CA1002, CA1819, and CA2227 | Warning | None | Shipped | RM-149 |
| DDD005 | Application, API, and UI code does not repeat domain decisions from raw state | Review and skill-driven audit | — | None | Decided: no analyzer | RM-151 |
| DDD006 | Child entities change only through their aggregate root | `DddConventionsTests` architecture test | Test failure | None | Shipped | RM-149 |
| DDD007 | Domain enums do not drive business decisions outside the domain | Review and skill-driven audit | — | None | Decided: no analyzer | RM-150 |
| DDD008 | Domain assemblies do not reference framework adapters | `LayerDependencyTests` architecture test | Test failure | None | Shipped | — |

Writes to entity state from outside the owning type are covered by DDD003 together with C# accessibility: once every
setter is private, the compiler rejects external writes. Reflection-based materialization in Infrastructure is out of
scope by design.

## Choosing a mechanism

- **Analyzer**: the rule is visible in one symbol or syntax node, gives value at edit time, and can be scoped by the
  project that references it. DDD003 and DDD004 qualify.
- **Architecture test**: the rule needs whole types, relationships between types, or assembly metadata, and running it
  once per build is enough. DDD006 and DDD008 qualify.
- **Review and audit**: the rule needs design judgment or spans compilations, so an analyzer would be noisy or blind.
  DDD005 and DDD007 qualify; the `rich-domain-model` agent skill runs the audit.

## Exclusions

- **Project scope**: only `src/*.Domain` projects reference the domain analyzer packages, and an architecture test keeps
  it that way. DTOs, read models, EF configuration, projections, deserialization models, and test fixtures live in other
  projects, so they never load the rules.
- **Generated code**: excluded through `GeneratedCodeAnalysisFlags.None` (generated file names, auto-generated headers,
  and `[GeneratedCode]`).
- **EF materialization**: private setters, private constructors, and backing fields do not report.
- **Event replay**: event-sourced aggregates assign state in private `Apply` methods through private setters, which do
  not report.
- **Parameter objects and records**: `init` accessors and positional records do not report.
- **Overrides and type parameters**: overrides do not repeat the base diagnostic; properties typed by a type parameter
  do not report DDD004.

## Suppression policy

No domain-rule suppressions exist. A new suppression must target one declaration, carry a code-owner-reviewed
justification, and extend the allowlist in `AnalyzerSuppressionPolicyTests`, which currently permits only exact-site
`VTARCH001` pragmas. Project-wide `NoWarn`, `.editorconfig` severity downgrades, and `SuppressMessage` attributes are not
allowed for these rules.

## Prioritization

| Rule | Value | False-positive risk | Cost | Outcome |
| --- | --- | --- | --- | --- |
| DDD003 | High: blocks the most common encapsulation leak | Low with project scope | Low; mechanical fix | Shipped first, with the code fix |
| DDD004 | Medium: closes the gap the SDK rules leave for `IList<T>`-style interfaces | Low with project scope and visibility check | Low | Shipped |
| DDD006 | High: found a real bypass of a capacity invariant | Low; children are inferred from exposure | Low | Shipped |
| DDD008 | Medium: guards purity that is clean today | Low | Low | Shipped |
| DDD005 | Medium | High for a per-compilation analyzer | High | Audit and review |
| DDD007 | Low: risky sites concentrated in a few handlers | High for a blanket enum rule | Medium | Audit and review |
| DDD002 | Low | Low | Low | Candidate |

## Rule details

### DDD001 domain event suffix

```csharp
public sealed record TourCreated(Guid TourId) : IDomainEvent;            // reports
public sealed record TourCreatedDomainEvent(Guid TourId) : IDomainEvent; // does not report
```

Suppress only for externally named contracts that cannot be renamed.

### DDD003 non-private setters (`SKDOM001`)

```csharp
public sealed class Tour
{
    public string Name { get; set; }          // reports
    public string Identifier { get; private set; } // does not report
    public string Code { get; init; }         // does not report
}
```

Reports public, internal, protected, protected internal, and private protected setters, including setters required by a
domain interface. The code fix makes the setter private only when no code outside the declaring type or its nested types
writes the property anywhere in the solution. Package documentation:
[SharedKernel.Domain.Analyzers](https://github.com/danigutsch/PandaBear.SharedKernel/blob/main/src/SharedKernel/SharedKernel.Domain.Analyzers/README.md).

### DDD004 mutable collections (`SKDOM002`)

```csharp
public sealed class Tour
{
    private readonly List<string> includedServices = [];

    public IList<string> Services { get; } = new List<string>();                         // reports
    public IReadOnlyList<string> IncludedServices => includedServices.AsReadOnly();    // does not report
}
```

Reports properties visible outside the assembly whose type is or implements `ICollection<T>` or `ICollection`. Arrays,
`List<T>`, settable collections, and visible fields stay with CA1819, CA1002, CA2227, and CA1051. Internal persistence
snapshots are not visible outside the assembly and do not report.

### DDD005 repeated external domain checks

Pattern: a condition outside the domain combines two or more members of the same domain object to decide a business
outcome, and the same decision appears in two or more places. Scope is the whole solution, excluding tests, query
projections, DTO copies, and display formatting. Suggest a name only when an existing domain term fits; otherwise give a
manual refactoring hint.

- Positive: `DocumentMapper.cs:36` and `GetFinalizedDocumentArtifactHandler.cs:28` both decide "finalized artifact
  available" from a document's status and artifact fields.
- Positive: seven document command handlers repeat `document.Type == DocumentType.PaymentInstructions` to route
  payment-instruction documents to their own workflow.
- Negative: `QueryService.cs:160` orders bookings by status in a query projection.
- Negative: `MediaImageAccessibilityDraftService.cs:35` rejects an unset language as input validation.
- Negative: `DevelopmentDataInitializer.cs:364` filters seed data.

Decision: no analyzer. Repetition spans projects that a per-compilation analyzer cannot see together, and projection or
display code would make it noisy. Revisit only if two audits find recurring misses that review did not catch.

### DDD006 child entity boundary

A child entity is an `IEntity<>` type that is not an `IAggregateRoot` and that another entity exposes through a public
property, directly or as an `IEnumerable<T>` element. Child entities must not declare public constructors or public
methods returning `Result` or `Result<T>`. Today the children are `Booking` and `DocumentDraft`.

### DDD007 domain enums

Keep stable labels and audit vocabulary as enums. Lifecycle enums stay public for display, mapping, sorting, and
filtering; branching on them outside the domain to decide a business outcome moves behind a domain predicate. A blanket
enum analyzer is rejected: the risky sites are concentrated in a few handlers, and no automatic fix is safe. The
[domain model audit](domain/domain-model-audit.md#domain-enum-inventory) holds the inventory.

### DDD008 framework adapters

Domain assemblies must not reference System.Text.Json, DataAnnotations, System.Net.Http, EF Core, ASP.NET Core,
Microsoft.Extensions.Logging or Http, or Roslyn assemblies. Package-level adapter bans stay in the layer dependency tests.

## Research summary

- The .NET SDK rules cover part of DDD004: CA1002 flags exposed `List<T>`, CA1819 flags array properties, CA2227 flags
  settable collection properties, and CA1051 flags visible fields
  ([CA2227](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2227),
  [CA1002](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1002),
  [CA1819](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1819)). None of them flags a
  get-only `IList<T>`, `ICollection<T>`, or `Dictionary<TKey, TValue>` property.
- Meziantou.Analyzer MA0016 recommends collection abstractions such as `IList<T>` over implementations; it targets API
  shape, not read-only domain exposure
  ([MA0016](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0016.md)).
- Generic DDD analyzers and NDepend queries exist but would add a dependency for heuristics that cannot see the
  repository's project-scoped activation.
- EF Core binds constructors and maps private setters and backing fields
  ([constructors](https://learn.microsoft.com/ef/core/modeling/constructors),
  [backing fields](https://learn.microsoft.com/ef/core/modeling/backing-field)), so private setters do not block
  persistence.
- Smart-enum libraries move behavior onto enumeration classes, but ADR-031 prefers value objects, predicates, or explicit
  transitions without a new dependency.
- Fowler's anemic domain model and the Microsoft rich-versus-anemic guidance define the target; see the
  [guide vocabulary](domain/RICH_DOMAIN_MODEL.md#vocabulary).

## Follow-up issue template

Each implementation issue should include:

- rule ID and exact symbol or syntax target
- examples and non-examples
- false-positive exclusions
- severity and rollout plan
- code-fix decision
- analyzer tests and public API or release-note updates

Open follow-ups: DDD002 analyzer if integration-event naming drifts, and the behavior-placement fixes listed in the
[domain model audit](domain/domain-model-audit.md#findings).

## Related Documentation

Use the [documentation source-of-truth map](README.md#source-of-truth-map) for related repository guidance.
