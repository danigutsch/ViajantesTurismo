# ADR-020: Web Frontends by Audience, Not by Bounded Context

**Status**: Accepted — 2026-05-23

## Context

The current web frontend project is `ViajantesTurismo.Admin.Web`, scoped to Admin use cases.

As the solution grows into more bounded contexts, creating one frontend per bounded context would increase:

- project sprawl and duplicated UI/platform concerns
- coordination overhead for shared UX patterns
- cross-frontend navigation friction for operator workflows
- backend calls required to render composite pages when data spans contexts

The product needs two clear audience-facing web surfaces:

- an internal management experience for operators/backoffice workflows
- a public website for customer-facing discovery and interactions

Those audience boundaries are stable. Bounded contexts are backend/domain boundaries and should not force one frontend project per context.

## Decision

Adopt audience-oriented web frontends:

1. Rename and evolve `ViajantesTurismo.Admin.Web` into `ViajantesTurismo.Management.Web`.
2. Use `ViajantesTurismo.Management.Web` as the internal frontend that can consume APIs across multiple bounded contexts.
3. Introduce `ViajantesTurismo.Public.Web` as the public-facing website.
4. Do not create one website per bounded context by default.

### Architectural intent

- Keep bounded contexts as backend ownership and API contract boundaries.
- Allow frontend composition across bounded-context backends through typed clients and explicit integration boundaries.
- Optimize page rendering flows by enabling one frontend to orchestrate data from multiple context services when required by UX.

## Consequences

### Pros

- Reduces frontend project proliferation as bounded contexts increase.
- Centralizes internal UX capabilities (auth shell, navigation, design system, shared components) in one management app.
- Enables cross-context pages without forcing users through multiple web apps.
- Preserves domain/backend separation while improving frontend delivery velocity.

### Cons

- Management frontend can accumulate too many responsibilities if module boundaries are not enforced.
- Requires stronger internal frontend architecture (feature modules, ownership boundaries, governance).
- Public and management apps still need disciplined separation of authentication, routing, and deployment concerns.

## Guardrails

- Keep one audience = one frontend app unless a strong non-functional constraint demands separation.
- Enforce feature/module ownership inside `Management.Web` to avoid a new monolith.
- Prefer typed API clients per backend/context integration.
- Keep public-facing concerns isolated to `Public.Web`; do not leak operator-only dependencies.

## Alternatives Considered

1. **One website per bounded context**
   Rejected: scales poorly in frontend count and increases operational/UX fragmentation.

2. **Single website for all audiences**
   Rejected: increases risk of coupling public and internal concerns and complicates security boundaries.

3. **Keep Admin.Web and add more bounded-context-specific sites over time**
   Rejected: naming and architecture direction conflict with intended multi-context internal portal.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [Epic #45 — vertical slice migration](https://github.com/danigutsch/ViajantesTurismo/issues/45)
- Related: [Issue #43 — Admin BC vertical-slice alignment](https://github.com/danigutsch/ViajantesTurismo/issues/43)
