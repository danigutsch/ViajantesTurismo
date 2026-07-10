# Branding

Branding values are shared between Management.Web, Public.Web, and the Branding API adapter.

## Responsibility map

### `SharedKernel.Branding`

`SharedKernel.Branding` owns host-agnostic branding rules and contract types:

- safe brand-name normalization and validation;
- safe palette values, starting with narrow color formats such as `#RRGGBB`;
- typography allow-list contracts for approved font-family values;
- optional safe logo URI validation;
- public-safe Branding DTO/client contract shapes that can move with the reusable library boundary;
- relative Branding route contract segments that hosts compose with their own API prefix.

`SharedKernel.Branding` must not own ViajantesTurismo defaults, route mounting or API version
prefixes, database schemas, tenant/application policy, cache policy, or UI workflow decisions.

### ViajantesTurismo adapter

ViajantesTurismo owns the app-specific adapter:

- Branding API endpoint hosting and typed client base-address wiring used by Management.Web and
  Public.Web;
- persistence, `BrandingDbContext` schema/migrations, seeding, and ViajantesTurismo default values;
- server-side validation integration using `SharedKernel.Branding` rules;
- cache invalidation for public branding reads.

The adapter keeps a separate `BrandingDbContext` so schema and migration ownership stay clear. This
slice intentionally stores Branding tables in the existing Catalog PostgreSQL database rather than
creating a separate physical Branding database; the extra physical database can wait until isolation,
tenancy, throughput, or retention requirements justify it.

### Catalog relationship

Catalog does not own Branding fields, validation rules, workflow states, route contracts, clients,
or public-rendering behavior. New branding work belongs to `SharedKernel.Branding` plus the
ViajantesTurismo Branding API adapter.

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
5. Valid settings are saved by the ViajantesTurismo Branding adapter.
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

- [Catalog bounded context](bounded-contexts/Catalog.md)
- [API client boundaries](API_CLIENT_BOUNDARIES.md)
- [Caching strategy](caching-strategy.md)
- [Generated endpoint route map](architecture/generated-endpoint-route-map.md)
