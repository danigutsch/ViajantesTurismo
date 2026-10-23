# ADR-042: Enforce DDD Domain Standards with SharedKernel Analyzers and Architecture Tests

**Status**: Accepted — 2026-10-24

## Context

[ADR-031](20260703-rich-domain-behavior-and-state-exposure.md) decided that domain state changes through behavior,
setters stay private, collections are exposed read-only, and handlers do not decide outcomes from raw state. Those rules
were enforced only by review and two value-object architecture tests. The DDD standards roadmap work (RM-148 through
RM-152) asked for a documented rule catalog, encapsulation enforcement, a decision on exposed enums, a decision on
repeated external checks, and a first analyzer with a code fix, without false positives on DTOs, read models,
persistence models, serializers, or EF Core materialization.

Repository policy places reusable analyzers, code fixes, and test helpers in SharedKernel
([SharedKernel consumption](../SHAREDKERNEL_CONSUMPTION.md)). The setter and collection rules are generic, so they are
reusable library code. A 2026-10-24 audit found no setter or collection violations, one aggregate-boundary leak, and
several handlers deciding from raw state ([domain model audit](../domain/domain-model-audit.md)).

## Decision

Split enforcement by what each mechanism sees best:

- **Local symbol rules ship as SharedKernel analyzers.** `PandaBear.SharedKernel.Domain.Analyzers` reports `SKDOM001`
  (non-private, non-`init` setters) and `SKDOM002` (mutable collections visible outside the assembly).
  `PandaBear.SharedKernel.Domain.CodeFixes` makes a setter private when nothing outside the declaring type writes it.
- **The package reference is the activation boundary.** Every `src/*.Domain` project references both packages and no
  other project does, so DTO, read-model, persistence, and API projects never run the rules. An architecture test
  checks this. Both rules stay at Warning, which the build treats as an error; the domain projects build clean.
- **Type-level rules are architecture tests.** `DddConventionsTests` checks that child entities change only through
  their aggregate root; `LayerDependencyTests` keeps framework adapters out of domain assemblies.
- **Repeated external checks stay a review and audit concern.** A per-compilation analyzer cannot see the same decision
  repeated across application, API, and Razor projects without high false-positive risk. The `rich-domain-model` agent
  skill and the audit page own this check.
- **No enum analyzer.** Domain enums stay public for display, mapping, and filtering; decisions move behind domain
  predicates when the audit finds them.
- **Suppressions stay exceptional.** None exist. A new suppression needs a code-owner-reviewed justification and an
  update to `AnalyzerSuppressionPolicyTests`; project-wide `NoWarn` and severity downgrades are not allowed.

The [rich domain model guide](../domain/RICH_DOMAIN_MODEL.md) is the canonical modeling guidance, and the
[DDD analyzer rule catalog](../DDD_ANALYZER_RULE_CATALOG.md) holds each rule's examples, exclusions, and status.

## Consequences

### Pros

- Encapsulation rules fail the build in the IDE and CI instead of waiting for review.
- Rules are reusable by other SharedKernel consumers and follow the library's analyzer packaging and release process.
- DTO and persistence projects cannot produce false positives because they never load the analyzers.

### Cons

- Adopting a rule change requires a SharedKernel candidate release and a consumer adoption pull request.
- Every type in a domain project is treated as domain state, so a deliberate exception needs a reviewed suppression.
- Behavior-placement findings still depend on review and periodic audits.

## Alternatives

- **App-owned analyzers in this repository**: faster to change, but they contradict the SharedKernel ownership policy for
  reusable analyzers and test helpers.
- **Architecture tests only**: no new packages, but no IDE feedback and no code fix.
- **Third-party DDD analyzers or NDepend rules**: add a dependency and supply-chain review for generic heuristics that do
  not know the repository's activation boundary.
- **Marker-based detection** (`IEntity<>`, `IAggregateRoot`, `ValueObject`): misses domain types without markers, such as
  plain value records, and reaches into application code that derives from `ValueObject`.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-031: Rich Domain Behavior and State Exposure](20260703-rich-domain-behavior-and-state-exposure.md)
- Related: [ADR-001: Domain Validation with Factory Methods](20251108-domain-validation-factory-methods.md)
- Related: [ADR-026: Domain Materialization and SharedKernel Persistence Boundaries][adr-026]
- Source: [SharedKernel.Domain.Analyzers README][domain-analyzers]

[adr-026]: 20260621-domain-materialization-and-sharedkernel-persistence-boundaries.md
[domain-analyzers]: https://github.com/danigutsch/PandaBear.SharedKernel/blob/main/src/SharedKernel/SharedKernel.Domain.Analyzers/README.md
