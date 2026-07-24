# Branding

Branding is an independent logical module used by Management.Web, Public.Web, and Admin document
generation. The current runtime has a standalone Branding API. The accepted target co-hosts the
module in `ViajantesTurismo.Catalog.ApiService` without moving it into the Catalog bounded context.

<!-- doc-fact:branding-architecture:start -->
- current-decision: `admin-local-projection`
- current-decision: `app-owned-contracts`
- current-decision: `branding-owned-outbox`
- current-decision: `catalog-api-cohost`
- current-decision: `independent-logical-module`
- current-decision: `no-ordinary-api-http`
- current-decision: `production-platform-undecided`
- current-decision: `public-cache-60-300`
<!-- doc-fact:branding-architecture:end -->

## Responsibility map

### `SharedKernel.Branding`

`SharedKernel.Branding` owns only host-agnostic, pure branding validation and normalization:

- safe brand-name normalization and validation;
- safe palette values, starting with narrow color formats such as `#RRGGBB`;
- typography allow-list validation for approved font-family values; and
- optional safe logo URI normalization and validation.

It does not own ViajantesTurismo HTTP DTOs or clients, integration events, route segments or API
prefixes, store interfaces, defaults, database schemas, application policy, cache policy, or UI
workflow decisions. Existing app-specific types in `SharedKernel.Branding` are migration source, not
the target ownership boundary.

### ViajantesTurismo Branding module

ViajantesTurismo owns the complete application-specific module:

- Branding HTTP and integration-event contracts, typed HTTP clients, and route segments;
- application behavior and full-state revision policy;
- persistence, `BrandingDbContext` schema/migrations, and ViajantesTurismo fallback default values;
- server-side validation integration using `SharedKernel.Branding` rules;
- the ASP.NET endpoint/authorization module mounted by a host; and
- host-local cache invalidation for public branding reads.

`BrandingDbContext` and the `branding` schema remain separate so schema, migration, history, and
outbox ownership stay clear. Branding tables remain in the Catalog physical PostgreSQL database;
co-location does not permit shared Catalog tables or a shared Catalog outbox.

### Catalog relationship

`ViajantesTurismo.Catalog.ApiService` is the target process host. Catalog does not own Branding
fields, validation rules, workflows, contracts, permissions, tables, or public-rendering behavior.
The Catalog host composes the Branding ASP.NET module and its separate DbContext; the Catalog domain
does not reference Branding.

## Current and target runtime

- **Current**: `ViajantesTurismo.Branding.ApiService` hosts Branding HTTP; Public.Web,
  Management.Web, and Admin call it. This remains the rollback host during migration.
- **Target**: Public.Web and Management.Web use the same logical Branding HTTP contract on
  `ViajantesTurismo.Catalog.ApiService`. Admin reads an Admin-owned local projection populated by
  `IntegrationEventWorker` and has no Catalog/Branding HTTP or database credential.
- **Contraction**: the standalone executable is removed only after forward-compatible schema,
  dual-host writer, token/session drain, zero-traffic observation, rollback drill, retained release
  artifact, and private-exposure gates pass.

Ordinary production API-to-API HTTP is not a Branding state-distribution mechanism. A future gRPC
seam requires demonstrated synchronous need and a separate decision.

## State and delivery

Branding stores the current document with a monotonically increasing revision. Each write appends an
immutable full-state revision and enqueues a full-state integration event in the same Branding
transaction. Branding owns its outbox mapping/table. SharedKernel EF messaging supplies generic
context-qualified mapping and registration mechanics; it does not choose the module's schema or table.

`IntegrationEventWorker` applies events through an Admin inbox to an Admin projection. A versioned
event replaces the projection only when its revision exceeds the persisted watermark. A legacy event
has a `null` revision, remains idempotent by event identity, does not advance the watermark, and cannot
overwrite a versioned projection. Retry is bounded; exhausted events enter visible poison state and
are replayed only by an audited operator after correction.

This is event-carried state transfer. Branding current state remains authoritative; append-only
full-state revisions are not an event-sourced aggregate, and the Admin projection needs no projection
framework.

## Authorization boundary

Catalog-hosted Branding routes use named, route-specific audience and authorization policies. Public
reads remain explicitly anonymous. Management routes preserve `branding.read` and `branding.write`;
sharing a process does not grant Catalog permissions. The mounted module retains its configured CORS
allow-list, public/mutation rate limits, management `no-store` behavior, validation, and authenticated
fallback policy. Temporary legacy/target audience acceptance is removed after token and session drain.
Local Keycloak remains local/CI infrastructure; the production identity provider and concrete roles
are undecided.

## Styling safety

The migration must not introduce arbitrary user-editable CSS. Editors choose safe branding values;
the server stores normalized values; renderers project those values into fixed CSS variable names.

## Editable values and validation rules

Editable Branding settings are intentionally narrow:

| Value | Editable? | Validation owner | Rules |
| --- | --- | --- | --- |
| Brand name | Yes | `SharedKernel.Branding` | Normalize and trim; require non-empty public display text within the contract maximum. |
| Palette | Yes | `SharedKernel.Branding` | Accept only safe color tokens such as `#RRGGBB`; reject named colors, functions, variables, comments, and CSS fragments. |
| Typography | Yes | `SharedKernel.Branding` | Accept only exact values from the approved font-family allow-list after normalization. |
| Logo URI | Optional | `SharedKernel.Branding` | Accept only public-safe HTTPS or application-owned relative URIs; reject script, data, file, credentialed, and CSS-like values. |
| Icons | Deferred | Future brand-assets epic | Favicons, touch icons, app icons, social-preview images, and icon sets require asset metadata, dimensions, media-type checks, and public head-tag rendering before becoming editable. |

Reject values that look like CSS or script payloads, including semicolons, braces, selectors, `url()`,
`var()`, `calc()`, comments, `<script>`, `javascript:`, `data:`, and raw style attributes.

## Management workflow

Current workflow:

1. Management.Web loads Branding settings through the Branding API typed client.
2. An editor changes brand name, palette, typography, or logo URI.
3. Management.Web submits the full Branding settings payload to the Branding API.
4. The server validates all values with `SharedKernel.Branding` rules.
5. Valid settings are saved by the ViajantesTurismo Branding module.
6. Invalid settings return a validation problem; callers show safe field-level messages.

Management.Web treats Branding as operational configuration. It should keep the edit UI functional:
clear forms, validation messages, simple swatches, and previews of the saved values. It should not
inherit the richer public marketing presentation as its own UI style.

Preview, review, scheduled publish, and multi-step approval are deferred until implemented. Do not
document them as current behavior or add UI/API assumptions for them before their feature slices exist.

Icon and brand-asset management is also deferred. Branding includes icons conceptually, but this slice
only stores a single optional logo URI. A follow-up epic should model favicons, application/touch icons,
social preview images, asset dimensions, media types, upload/select workflows, and public `<head>`
rendering separately.

## Public rendering contract

Public rendering uses a public-safe DTO only:

- brand name for display text;
- optional logo URI for image rendering;
- palette values already normalized into safe tokens;
- typography values from the allow-list;
- no internal identifiers, audit metadata, drafts, validation errors, editor names, or PII.

Public.Web renders Branding settings in an SSR-friendly way by assigning values to a fixed set of CSS
custom properties, such as brand palette and typography variables. The variable names are code-owned;
only the variable values come from validated Branding settings.

Public.Web owns the customer-facing presentation layer on top of those shared tokens. It can use the
same brand name, logo, palette, and typography to produce polished marketing layout, soft surfaces,
cards, navigation, and responsive spacing without adding public-only styling fields to
`SharedKernel.Branding`.

Public renderers must not emit user-provided CSS blocks, selectors, style attributes, `<style>`
payloads, scripts, or arbitrary URL-bearing CSS.

## Related documentation

- [ADR-040: Branding Module Co-hosting and State Distribution](adr/20260724-branding-module-co-hosting-and-state-distribution.md)
- [Catalog bounded context](bounded-contexts/Catalog.md)
- [API client boundaries](API_CLIENT_BOUNDARIES.md)
- [Caching strategy](caching-strategy.md)
- [Production readiness](operations/production-readiness.md)
- [Generated endpoint route map](architecture/generated-endpoint-route-map.md)
