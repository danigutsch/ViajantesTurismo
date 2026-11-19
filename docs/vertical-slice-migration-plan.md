# Application vertical slice migration plan

Status: proposed migration sequence; current project boundaries remain authoritative.
Owner: application maintainers. Reviewed against repository source on October 20, 2026.
This manually maintained plan covers Admin, Catalog, Public Web, and Management Web.
Update it when a slice moves or its contract changes.

## Contents

- [Boundary decision](#boundary-decision)
- [Current inventory](#current-inventory)
- [Folder conventions](#folder-conventions)
- [Composition contracts](#composition-contracts)
- [Endpoint redesign candidates](#endpoint-redesign-candidates)
- [Admin sequence](#admin-sequence)
- [Catalog sequence](#catalog-sequence)
- [Public Web sequence](#public-web-sequence)
- [Management Web sequence](#management-web-sequence)
- [Validation and rollback](#validation-and-rollback)

## Boundary decision

Organize each use case within its owning layer and bounded context. Preserve Domain,
Application, Infrastructure, API, and contract projects and their allowed references.
A vertical slice is an ownership and navigation convention, not permission to combine projects,
move business rules into endpoints, or share a DbContext across contexts.

This plan supersedes the earlier proposal on this page to merge Admin.Domain and
Admin.Application into one project. That proposal conflicts with the repository-wide architecture
rules and is not a prerequisite for feature ownership. Any future project-boundary change needs
its own reviewed architecture decision.

Keep reusable library contracts intact. None of the planned moves requires SharedKernel changes.
Keep migration plans separate from executing migrations: the sequences below define candidate
work, dependencies, and evidence, not a claim that source has already moved.

## Current inventory

Paths below are relative to `src/`; test project names are relative to `tests/`.
These are curated observations from source, not an automatically evaluated dependency graph.

| Context | Endpoints and UI | Commands, DTOs, validation, and mapping | Persistence and tests | Coupling or blocker |
| --- | --- | --- | --- | --- |
| Admin | `ViajantesTurismo.Admin.Api/{Tours,Bookings,Customers,Documents}`; bookings already have operation-specific endpoint files | `Admin.Application/Bookings/*` and `Tours/*` use operation folders; `Customers/Import` and `Documents` group workflows; `Admin.Application/Mappings` is centralized; `Admin.Contracts.Application` mixes root DTOs with Bookings/Documents folders | `Admin.Infrastructure` owns read/write contexts, stores, configurations, and migrations; `Admin.UnitTests`, `Admin.ApiServiceTests`, `Admin.ContractTests`, `Admin.BehaviorTests`, and `HostedTests` cover different seams | Endpoint response reloads use broad `IQueryService`; aggregate capacity and booking mutations share transactional rules; import and document workflows cross multiple operations |
| Catalog | `Catalog.Api/{Tours,Media,PublicContent}` owns versioned routes by feature | `Catalog.Application/{Tours,Media,PublicContent}` owns feature services and event handlers. Wire DTOs and clients live in Catalog contract projects. Domain validation stays in `Catalog.Domain` | `Catalog.Infrastructure/{Tours,Media,PublicContent}` owns the feature stores and persistence configuration. Tests cover the owning feature and cross-host contracts | Tour responses distinguish event acceptance from projection visibility. Media commits span object storage and metadata |
| Public Web | `ViajantesTurismo.Public.Web/Components/Pages/{Home,Gallery,Tours}` and `Components/Shared` | Catalog and Branding compose their anonymous clients locally. The host owns culture normalization, sitemap, cache, and security. Localized resources also use `ViajantesTurismo.Resources` | No application-owned database. `ViajantesTurismo.Public.WebTests` covers feature rendering and host behavior | Culture normalization, canonical URLs, sitemap, and cache variation must agree. Anonymous pages use only public clients |
| Management Web | `ViajantesTurismo.Management.Web/Components/Pages/{Tours,Bookings,Customers,Catalog,Branding,Documents,Payments}` owns operator workflows | Booking forms and document hold controls live with their pages. Customer form models, wizard state, and import preview and mapping code live under `Customers`. Contract projects own the HTTP clients | No business DbContext in web. Protected ticket/token stores are host infrastructure. `Management.WebTests` covers workflow components and host seams. `HostedTests/Management` exercises the browser seam | Preserve the scoped wizard lifetime, customer data handling, token exchange, antiforgery, and streaming downloads |

The full project prefixes in this table are `ViajantesTurismo.`. Inventory evidence includes
[Admin registration](../src/ViajantesTurismo.Admin.Application/ApplicationDependencyInjection.cs),
[Catalog routes](../src/ViajantesTurismo.Catalog.Api/CatalogEndpoints.cs),
[Public host](../src/ViajantesTurismo.Public.Web/Program.cs), and
[Management host](../src/ViajantesTurismo.Management.Web/Program.cs).

Layer-owned seams include authentication, host middleware, DbContext lifetime, transactions,
contract serializers, and service discovery. Feature-owned seams include operation mapping,
request validation, handlers, form state, and focused tests. Preserve shared host seams until
there is a concrete reason to change their ownership.

## Folder conventions

Use business capability first and operation second. Keep existing names when they are already
clear. Do not create a catch-all `Features` folder or duplicate a DTO solely to mirror a directory.
Use one top-level C# type per file and retain the repository method naming conventions.

| Artifact | Target placement and ownership |
| --- | --- |
| HTTP endpoint | Owning API project, `Bookings/CreateBooking/CreateBookingEndpoint.cs`, when moving an operation; existing operation-specific files may remain until that slice is touched |
| Wire request/response DTO and JSON metadata | Existing owning `Contracts.Application` or `Contracts.Http` project; group by capability when moved, retaining namespace/API compatibility or explicitly reviewing a break |
| Command and handler | Owning Application project, `Bookings/CreateBooking/`; no HTTP binding types in handlers |
| Validation | Wire shape in contract/API boundary; use-case validation beside handler; business invariants in Domain aggregate/value object |
| Mapping | Operation-specific mapping beside its endpoint or handler in the appropriate layer; mappings with multiple real consumers remain capability-owned |
| Persistence | Owning Infrastructure project, capability folder for store/configuration; DbContext and migration ownership remain centralized |
| Razor page and local state | Existing `Components/Pages/<Capability>/<Operation>/`; keep routable page names/routes stable and move its exclusive helpers together |
| Shared UI | Capability-owned components where only that capability uses them; `Components/Shared` only for actual reuse |
| Tests | Existing test project with matching capability/operation folders; never embed test types into a production project |

API example: align the CreateBooking endpoint and application command under `Bookings/CreateBooking`
in their separate projects, leaving booking rules under Admin.Domain and persistence under
Admin.Infrastructure. Keep `CreateBookingDto` in its existing contracts assembly.

Web example: keep the customer import page and its exclusive workflow/conflict helpers under
`Components/Pages/Customers/Import/` when migrated. Keep a genuinely reused country selector
shared, and keep customer HTTP serialization in the Admin contracts assembly. Preserve the
page's `@page` route, authorization, scoped state, and accessible status announcements.

Folder moves do not require simultaneous namespace, public type, route, assembly, or schema renames.
Review Razor discovery and `_Imports.razor` scope when moving a component.

## Composition contracts

Retain the existing `AddApplication`, `AddCatalogApplication`, infrastructure registration, and
`Map*Endpoints` entrypoints. They are the composition roots for current callers. A feature with
several registrations may expose one explicit `AddBookings`-style extension in the owning layer;
a feature with route mappings may expose one explicit `MapBookings`-style extension in its API.
These are target naming examples, not new required wrappers for a single registration.

Host startup calls existing layer entrypoints; those call feature composition only when extraction
makes an existing group clearer. Do not introduce module discovery, reflection scanning, generic
registries, service locators, marker interfaces, or a feature framework.

- Register handlers/stores with the existing scoped lifetime. Do not capture them in singletons.
- Bind and validate options at startup through the owning registration path. Infrastructure owns
  provider and connection options; Application remains provider-independent.
- Let the existing DbContext apply capability configurations explicitly. Never create one context
  or migration history per operation. Preserve schema constants, transactions, event interception,
  outbox ownership, and existing model conventions.
- Apply version, authorization, rate limits, cache/no-store, and OpenAPI metadata at the existing
  route group or operation boundary. Extraction must preserve effective metadata, not just URLs.
- Keep web authentication, audience exchange, ticket/token storage, security headers, service
  discovery, and antiforgery in host composition. Pages consume contract-owned clients.

The current model wiring is visible in
[AdminWriteDbContext](../src/ViajantesTurismo.Admin.Infrastructure/AdminWriteDbContext.cs) and
[CatalogDbContext](../src/ViajantesTurismo.Catalog.Infrastructure/CatalogDbContext.cs).

## Endpoint redesign candidates

These candidates need an explicit decision before implementation. A folder move alone does not
approve a wire-contract change. Preserve [request semantics](API_REQUEST_SHAPES.md) and
[client ownership](API_CLIENT_BOUNDARIES.md).

| Candidate and current source | Rationale and decision | Compatibility risk and follow-up recommendation |
| --- | --- | --- |
| Customer import preview/commit in `Admin.Api/Customers/CustomerImportEndpoints.cs` | Both operations receive multipart files and pass through scanning; assess a server-held import session only if repeated processing is measured as costly | Session IDs would add retention, authorization, expiry, and replay semantics. Keep current contract during slice moves; propose a separate import-session issue with measurements |
| Booking create/update responses in `Admin.Api/Bookings` | Mutations reload through `IQueryService`; assess whether narrower booking queries improve ownership without duplicating persistence | Preserve Created Location, response bodies, and not-found/conflict/validation outcomes. Recommend a booking-query seam follow-up only after callers are inventoried |
| Tour presentation, publish, and unpublish in `Catalog.Api/Tours/CatalogTourEndpoints.cs` | Keep accepted-but-not-yet-projected behavior distinct from completed writes | Preserve 202 Location headers, retry/client behavior, authorization, and cache invalidation |
| Media upload and accessibility operations in the same Catalog file | Separate upload intake, review, and public delivery ownership | Multipart names, size/scanner controls, 201 body, permissions, and stream disposal are contracts. Coordinate with existing upload-queue work rather than creating duplicate upload issues |
| Public content catch-all route and culture/language query modifiers in Catalog | Make content ownership clear while retaining normalization before cache lookup | Changing key escaping, aliases, invalid culture behavior, or cache variation can leak incorrect content. Add contract/cache characterization before any redesign |
| Document mutation/download routes in `Admin.Api/Documents/DocumentEndpoints.cs` | Group lifecycle transitions and artifact delivery without hiding audit/idempotency rules | Preserve revision conflicts, Idempotency-Key, permissions, no-store, download headers, cancellation, and immutable artifacts. Keep lifecycle redesign separate from relocation |

## Admin sequence

Booking mutations and Tour create and update have matching operation folders in the API and
Application. Booking mapping and focused tests follow their owning feature. The Tour aggregate
store, locking command store, and EF mappings live together in Admin Infrastructure. The
application host composes the Booking and Tour route groups and registers the two scoped Tour
stores through infrastructure. Explicit store registrations can supply either store before that
composition. The shared write context owns the transaction and the mapping model.

1. Move customer create/update. Retain contract assemblies and serializer metadata. Coordinate
   date-related files with the separate deterministic-time work.
2. Move import as one workflow boundary: parsing, conflict detection/resolution, scan limits, commit,
   and UI handoff. Avoid splitting its state machine between unrelated ownership folders.
3. Continue with payment and document workflow ownership. Keep review/finalization, idempotency,
   audit, retention, and artifact storage together. Regeneration is a separate workflow.

Prerequisites: inventory current consumers and test seams; record any intended contract change
separately. Risks: aggregate-wide transaction rules, broad query contracts, generated metadata,
and concurrent date/document work. Gate each step with Admin.UnitTests and the affected
Admin.ApiServiceTests/ContractTests; use HostedTests/Admin/Api and HostedTests/Admin/Infrastructure for
database behavior and BehaviorTests/UI/HostedTests/Admin/System for the workflow actually changed.

## Catalog sequence

`CatalogEndpoints` composes three modules on the versioned and management route groups.
Tours owns presentation, publication, the Admin tour event handler, projection, and read-model
persistence. Media owns upload, accessibility, delivery, original-stored processing, reconciliation,
and object storage. Public content owns editable and public content routes, services, and persistence.

The host and worker share the Catalog application and infrastructure registrations. Tours and
Media expose their own registration methods. Catalog persistence owns the database contexts,
outbox, and inbox, and the worker explicitly starts projection and reconciliation. Consumers can
register a clock or tour read-model store before the defaults are applied.

Run `Catalog.UnitTests`, `Catalog.ApiServiceTests`, and `Catalog.ContractTests` for changes to
the Catalog surface. The API suite exercises upload and stream disposal over Kestrel. The hosted
Catalog tests cover projections, reconciliation, and storage with provider-backed fixtures.

## Public Web sequence

The [Public Web ownership inventory](../src/ViajantesTurismo.Public.Web/README.md#feature-ownership)
maps Home, Gallery, and Tours to their page and test folders. The pages keep their existing Razor
component identities and route templates. Home and the tour list share the tour card. Gallery
and tour detail share the image rendition mapping with the card.

Catalog and Branding each register their anonymous client in their owning module. Host
composition continues to own canonical origin, sitemap, locale redirects, cache, and security
boundaries. Run `Public.WebTests` for feature rendering, localized metadata, outbound client
behavior, and routing. The architecture suite checks ownership for public pages, client
registration, and feature tests.

## Management Web sequence

The [Management Web ownership inventory](../src/ViajantesTurismo.Management.Web/README.md#folder-ownership)
maps pages, clients, state, validation, and tests to their workflows. Booking forms and list
components remain usable from tour and customer details. Customer create and edit share the
customer form models. The creation wizard owns its scoped state and progress UI, and the
import operation owns its page, conflict selection, CSV mapping, workflow state, and tests.

Catalog, Branding, Payments, Tours, and document pages retain their existing routes and
feature directories. Catalog, Customers, and Payments each group related startup registrations
locally. The host composes the other individual client registrations and owns authentication,
token exchange, antiforgery, protected persistence, and the document download proxy.

Run `Management.WebTests` for workflow and host changes. For changes spanning API or browser
seams, run the relevant `Admin.ApiServiceTests` and `HostedTests` cases, including Management
browser and Admin system scenarios. The architecture suite checks the placement of the moved
state and components. Coordinate changes to host authentication with the separate
authentication-ticket expiry work.

## Validation and rollback

For every implementation slice, record old/new paths and affected contracts in its PR. Run focused
tests, then build the solution and run the required repository checks. Contract snapshots must
remain identical for relocation-only work; review any serializer type-name or generated metadata
change. Provider-backed persistence moves must show no unintended model/migration diff.

Do not weaken authorization, validation, accessibility, privacy, stream cleanup, or tests to make a
move pass. New behavior or audit gaps require failing-first regression coverage before production
changes. This documentation-only plan requires documentation/link/conformance validation rather
than starting databases or changing runtime resources.

Keep each move reversible in one PR. If validation fails, restore the moved files and composition
calls together before proceeding; do not leave duplicate routes or mixed registrations. A source
revert does not roll back a deployed schema or data transform, so those changes need a separate
migration/recovery plan. Complete a context only when old paths have no callers, contracts remain
reviewed, and its relevant suites pass.
