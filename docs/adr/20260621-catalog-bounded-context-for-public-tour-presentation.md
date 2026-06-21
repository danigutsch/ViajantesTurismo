# ADR-021: Catalog Bounded Context for Public Tour Presentation

**Status**: Accepted - 2026-06-21

## Context

Admin currently owns operational tour management, including schedules, pricing, capacity, bookings,
customers, and payments. The public website needs customer-facing tour presentation with SEO,
publication state, galleries, itineraries, and marketing content.

Using Admin tour data directly for the public website would couple public presentation to operational
workflow details and make content editing harder to version independently.

## Decision

Introduce a Catalog bounded context for customer-facing tour presentation.

Catalog owns public tour content, publication workflow, public read models, and management editing
for customer-facing fields. Admin remains the source of operational tour facts. Admin synchronizes
selected tour facts into Catalog through explicit integration events.

Management.Web can edit Catalog presentation fields through Catalog APIs. Public.Web reads published
Catalog projections only.

## Consequences

- Admin operational tour management remains separate from public presentation concerns.
- Catalog can evolve customer-facing content without changing Admin aggregate rules.
- Public.Web is protected from Admin implementation and operator-only dependencies.
- Admin-to-Catalog synchronization requires integration event contracts and idempotent consumers.
- Management.Web must use distinct typed clients for Admin operations and Catalog presentation.

## Alternatives Considered

1. **Reuse Admin tour data directly in Public.Web**
   Rejected because public presentation needs separate lifecycle, SEO, gallery, publication, and
   versioning concerns.

2. **Add public presentation fields to the Admin Tour aggregate**
   Rejected because it would grow the operational aggregate with marketing/content concerns.

3. **Create one frontend-only content model without a bounded context**
   Rejected because Catalog has its own data ownership, integration, and versioning needs.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-020: Web Frontends by Audience, Not by Bounded Context](20260523-web-frontends-by-audience-not-by-bounded-context.md)
- Related: [Catalog Bounded Context](../bounded-contexts/Catalog.md)
