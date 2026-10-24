# Branding

Branding is an independent logical module used by Management.Web, Public.Web, and Admin document
generation. The current runtime has a standalone Branding API. The accepted target co-hosts the
module in `ViajantesTurismo.Catalog.ApiService` without moving it into the Catalog bounded context.

<!-- doc-fact:branding-architecture:start -->
- current-decision: `admin-local-projection`
- current-decision: `app-owned-contracts`
- current-decision: `atomic-reseed`
- current-decision: `branding-owned-outbox`
- current-decision: `catalog-api-cohost`
- current-decision: `catalog-transport-table`
- current-decision: `exact-route-audiences`
- current-decision: `independent-logical-module`
- current-decision: `no-ordinary-api-http`
- current-decision: `openapi-retarget-gate`
- current-decision: `positive-event-revision`
- current-decision: `production-platform-undecided`
- current-decision: `public-cache-60-300`
- current-decision: `put-etag-omitted-after-normalization`
- current-decision: `recovery-gates-document-generation`
- current-decision: `reseed-only-recovery-lane`
- current-decision: `rfc9110-strong-if-match`
- current-decision: `shared-process-blast-radius`
- current-decision: `transport-ack-boundary`
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
does not reference Branding. This is logical separation only: both modules share one process, failure
domain, memory space, and the blast radius of every credential available to that process.

## Current and target runtime

- **Current**: `ViajantesTurismo.Branding.ApiService` hosts Branding HTTP; Public.Web,
  Management.Web, and Admin call it. This remains the rollback host during migration.
- **Target**: Public.Web and Management.Web use the same logical Branding HTTP contract on
  `ViajantesTurismo.Catalog.ApiService`. Admin reads an Admin-owned local projection populated by
  `Admin.IntegrationEventWorker` and has no Catalog/Branding HTTP or database credential.
- **Contraction**: the standalone executable is removed only after forward-compatible schema,
  dual-host writer, token/session drain, zero-traffic observation, rollback drill, retained release
  artifact, private-exposure, recovery, and Branding OpenAPI retarget gates pass.

Ordinary production API-to-API HTTP is not a Branding state-distribution mechanism. A future gRPC
seam requires demonstrated synchronous need and a separate decision.

## State and delivery

Branding stores the current document with a source epoch and monotonically increasing positive
revision. Each write appends an immutable full-state revision and enqueues
`BrandingSettingsChangedIntegrationEvent`, carrying that epoch, positive revision, and full immutable snapshot, in
the same Branding transaction. No earlier Branding integration event exists. Only document rows made
before rollout may have nullable Branding revision provenance.

Implemented so far: the settings row carries `SourceEpoch` and `SourceRevision` (an optimistic
concurrency token), each change appends a row to `branding."BrandingSettingsRevisions"` and enqueues
the event in the same `SaveChanges`, and an unchanged save writes nothing. A writer holding a stale
revision gets `409 Conflict` from Management `PUT`. MigrationService runs the one-time bootstrap after
Branding migrations: an existing valid row becomes revision 1, a missing row becomes the defaults at
revision 1, and invalid persisted settings stop initialization.

`Admin.IntegrationEventWorker` applies each event to `branding_projection.settings` and records it in
`branding_projection.inbox_messages` in one Admin transaction. Redelivered events are no-ops, older
revisions are acknowledged without change, and a newer revision closes any gap. An unexpected epoch, a
same-revision payload conflict, or an invalid envelope is recorded as `Rejected` with a sanitized reason
code and acknowledged, so it never blocks later revisions; infrastructure failures are retried by the
transport. Document capture, recovery, and the `If-Match` contract described below are still to come.

Branding owns `branding.outbox_messages`. Its relay atomically inserts the `ConsumerName = 'admin'`
envelope into the Catalog database's default `messaging.transport_messages` table and marks the
outbox row published. A crash before commit leaves neither change; a crash after commit leaves both.
SharedKernel EF messaging supplies context-qualified registration/mapping mechanics rather than one
universal outbox table.

`Admin.IntegrationEventWorker` claims and updates transport lease/retry/poison/`ProcessedAt` state in the
Catalog database, then applies through an Admin inbox to the local projection. Admin inbox,
projection, and epoch/revision watermark commit atomically. A crash after that Admin commit but before
transport acknowledgement redelivers after lease expiry; inbox identity and watermark comparison make
recovery idempotent. Retry is finite, exhausted rows become poison, and replay is audited.

Projection reseed sets Admin state to `Recovering`, gates document generation, and pauses ordinary
relay polling and claims. A persisted recovery authorization permits only its exact reseed outbox and
`admin` transport event through a one-shot publish/claim/delivery lane; every ordinary row remains
fenced. The Admin transaction atomically applies that full snapshot with inbox, projection, watermark,
old/new audit, and return to `Ready`; failure leaves the authorized row retryable in `Recovering`.
A source restore that rewinds revision rotates the source epoch through a privileged audited operation;
ordinary delivery cannot change epochs. Source rewind stops Management writes and the relay, supersedes
restored old-epoch pending outbox rows, and quarantines old-epoch transport before reseed. Lost
projection/inbox/watermark recovery republishes the current full snapshot with a new event identity.
Writes, relay, claims, and document generation resume only after atomic return to `Ready`.

This is event-carried state transfer. Branding current state remains authoritative; append-only
full-state revisions are not an event-sourced aggregate, and the Admin projection needs no projection
framework.

## Authorization boundary

Catalog.ApiService uses `CatalogBearer` for exact `catalog-api` audience routes and `BrandingBearer`
for exact `branding-api` audience routes. Public Branding reads remain explicitly anonymous;
Management routes preserve `branding.read` and `branding.write`. No route accepts both audiences.
Standalone and co-hosted Branding use the same Branding audience while Management retargets its base
address; standalone removal waits through the maximum token/session lifetime and zero-traffic window.
The mounted module retains its configured CORS allow-list, public/mutation rate limits, management
`no-store` behavior, validation, and authenticated fallback policy. Local Keycloak remains local/CI
infrastructure; the production identity provider and concrete roles are undecided.

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

The target keeps this route and full-state replacement shape but adds optimistic concurrency.
Management `GET` returns `SourceEpoch`, `SourceRevision`, and strong opaque
`ETag: "<epoch>:<positive-revision>"`. `PUT` requires `If-Match`; absence returns 428. RFC 9110
strong comparison applies: any strongly matching member of a valid entity-tag list succeeds, while a
valid nonmatching list or weak tag returns 412. `If-Match: *` succeeds for the existing singleton and
would return 412 if it did not exist. Only malformed field syntax, including an invalid mixture of
the alternative `*` and list forms, returns 400.

A false precondition writes no current/history/outbox state and is not retried automatically. The
editor reloads and deliberately reapplies or merges changes. A successful normalized `PUT` returns the
epoch and new revision but no `ETag`; the client follows with `GET` to obtain the next opaque validator.
The `GET` atomically replaces the editor's document, epoch, revision, and validator, so an intervening
writer is observed rather than paired with stale local content. Branding OpenAPI advertises `ETag` on
Management `GET`, not successful `PUT`, and describes the
conditional request outcomes. Tests cover normalization plus validator refresh, two editors,
an intervening write between `PUT` and `GET`, matching/nonmatching lists, wildcard, weak tags, missing
headers, and malformed syntax.

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

## OpenAPI ownership through co-hosting

`dotnet tool run sharedkernel-ci -- openapi generate --target branding` remains the stable generation
command. During contraction the `branding` target in `.config/sharedkernel-ci.json` changes from the standalone project to
Catalog.ApiService's Branding-only document generation mode. Canonical `branding`, `public-branding`,
and Branding-only `v1` artifacts remain under
`ViajantesTurismo.Branding.Contracts.Http/OpenApi` and remain owned by Branding contracts; co-hosting
does not merge them into Catalog contract ownership.

The standalone executable cannot be removed while the `branding` OpenAPI target, generated filename mapping,
Branding drift guards, refresh hints, or contract tests still reference it. Generation from the
Catalog host must retain the trusted no-network/fail-closed metadata rules and prove the Branding
artifacts contain only the intended Branding routes.

## Related documentation

- [ADR-040: Branding Module Co-hosting and State Distribution](adr/20260724-branding-module-co-hosting-and-state-distribution.md)
- [Catalog bounded context](bounded-contexts/Catalog.md)
- [API client boundaries](API_CLIENT_BOUNDARIES.md)
- [Caching strategy](caching-strategy.md)
- [Production readiness](operations/production-readiness.md)
- [Generated endpoint route map](architecture/generated-endpoint-route-map.md)
