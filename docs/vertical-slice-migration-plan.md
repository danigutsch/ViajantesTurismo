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
| Admin | `ViajantesTurismo.Admin.ApiService/{Tours,Bookings,Customers,Documents}`; bookings already have operation-specific endpoint files | `Admin.Application/Bookings/*` and `Tours/*` use operation folders; `Customers/Import` and `Documents` group workflows; `Admin.Application/Mappings` is centralized; `Admin.Contracts.Application` mixes root DTOs with Bookings/Documents folders | `Admin.Infrastructure` owns read/write contexts, stores, configurations, and migrations; `Admin.UnitTests`, `Admin.ApiServiceTests`, `Admin.ContractTests`, `Admin.IntegrationTests`, `Admin.BehaviorTests`, `Admin.SystemTests`, and `Admin.UiIntegrationTests` cover different seams | Endpoint response reloads use broad `IQueryService`; aggregate capacity and booking mutations share transactional rules; import and document workflows cross multiple operations |
| Catalog | `ViajantesTurismo.Catalog.ApiService/CatalogEndpoints.cs` combines management tours, publishing, upload, accessibility, public media, and content routes | `Catalog.Application/{Tours,Media,PublicContent,IntegrationEvents,Projections}` already has capability ownership; wire DTOs/clients live in `Catalog.Contracts.Application` and `Catalog.Contracts.Http`; domain validation remains in `Catalog.Domain` | `Catalog.Infrastructure` has root stores, storage adapters, projection/reconciliation services, and `ModelConfigurations`; `Catalog.UnitTests`, `Catalog.ApiServiceTests`, `Catalog.ContractTests`, `Catalog.InfrastructureTests`, `Catalog.IntegrationTests`, and `Catalog.BehaviorTests` | Event acceptance and projection visibility differ; media commits span object storage and metadata; public cache invalidation spans publishing/content/media |
| Public Web | `ViajantesTurismo.Public.Web/Components/Pages/{Home,Gallery,Tours}` and `Components/Shared/{TourCard,TourGallery,ResponsiveTourImage}.razor` | Pages consume contract-owned Catalog clients; `PublicCultureQuery`, sitemap options, endpoint/cache/security classes, and host composition sit at project root; localized text/assets also use `ViajantesTurismo.Resources` | No application-owned database; `ViajantesTurismo.Public.WebTests` covers rendering and host behavior | Culture normalization, canonical URLs, sitemap, and cache variation must agree; anonymous pages must never acquire management credentials |
| Management Web | `ViajantesTurismo.Management.Web/Components/Pages/{Tours,Bookings,Customers,Catalog,Branding,Documents}`; customer creation is a multi-page wizard; import already has page-local workflow helpers | `Components/Shared` includes booking/payment forms; `Models` groups form models; `Services` owns country/import-header services; root `CustomerCreationState` owns wizard state; HTTP clients remain in Admin/Catalog contract projects | No business DbContext in web; protected distributed ticket/token stores are host infrastructure; `Management.WebTests`, `Management.WebIntegrationTests`, and Admin UI/System suites cover UI and remote seams | Scoped wizard/import state, token exchange, antiforgery, streaming downloads, and accessible focus/error behavior must survive moves |

The full project prefixes in this table are `ViajantesTurismo.`. Inventory evidence includes
[Admin registration](../src/ViajantesTurismo.Admin.Application/ApplicationDependencyInjection.cs),
[Catalog routes](../src/ViajantesTurismo.Catalog.ApiService/CatalogEndpoints.cs),
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
| Customer import preview/commit in `Admin.ApiService/Customers/CustomerImportEndpoints.cs` | Both operations receive multipart files and pass through scanning; assess a server-held import session only if repeated processing is measured as costly | Session IDs would add retention, authorization, expiry, and replay semantics. Keep current contract during slice moves; propose a separate import-session issue with measurements |
| Booking create/update responses in `Admin.ApiService/Bookings` | Mutations reload through `IQueryService`; assess whether narrower booking queries improve ownership without duplicating persistence | Preserve Created Location, response bodies, and not-found/conflict/validation outcomes. Recommend a booking-query seam follow-up only after callers are inventoried |
| Tour presentation/publish/unpublish in `Catalog.ApiService/CatalogEndpoints.cs` | Extract route groups while preserving accepted-but-not-yet-projected behavior | Do not collapse 202 into 200/204. Retain Location headers, retry/client behavior, authorization, and cache invalidation; coordinate with the typed-results work |
| Media upload and accessibility operations in the same Catalog file | Separate upload intake, review, and public delivery ownership | Multipart names, size/scanner controls, 201 body, permissions, and stream disposal are contracts. Coordinate with existing upload-queue work rather than creating duplicate upload issues |
| Public content catch-all route and culture/language query modifiers in Catalog | Make content ownership clear while retaining normalization before cache lookup | Changing key escaping, aliases, invalid culture behavior, or cache variation can leak incorrect content. Add contract/cache characterization before any redesign |
| Document mutation/download routes in `Admin.ApiService/Documents/DocumentEndpoints.cs` | Group lifecycle transitions and artifact delivery without hiding audit/idempotency rules | Preserve revision conflicts, Idempotency-Key, permissions, no-store, download headers, cancellation, and immutable artifacts. Keep lifecycle redesign separate from relocation |

## Admin sequence

1. Capture route/OpenAPI, handler, aggregate, and persistence baseline for the selected operation.
   Start with the already separated booking notes operation; preserve its PATCH route and errors.
2. Align only that endpoint, command, mapping, and matching tests by operation. Keep shared
   `IQueryService`, `IUnitOfWork`, and route-group composition until a later justified extraction.
3. Expand through booking details/discount and then lifecycle/payment operations. Preserve tour
   capacity locking, payment rules, and transaction/outbox atomicity with provider tests.
4. Move tour create/update and customer create/update. Retain contract assemblies and serializer
   metadata; coordinate date-related files with the separate deterministic-time work.
5. Move import as one workflow boundary: parsing, conflict detection/resolution, scan limits, commit,
   and UI handoff. Avoid splitting its state machine between unrelated ownership folders.
6. Move documents last, keeping review/finalization, idempotency, audit, retention, and artifact
   storage seams together. Regeneration is not an ordinary update of a finalized artifact.

Prerequisites: inventory current consumers and test seams; record any intended contract change
separately. Risks: aggregate-wide transaction rules, broad query contracts, generated metadata,
and concurrent date/document work. Gate each step with Admin.UnitTests and the affected
Admin.ApiServiceTests/ContractTests; use HostedTests/Admin/Api and HostedTests/Admin/Infrastructure for
database behavior and BehaviorTests/UI/HostedTests/Admin/System for the workflow actually changed.

## Catalog sequence

1. Extract public content routes/services first, preserving culture normalization and output-cache
   invalidation. Keep public and management route metadata separate.
2. Extract tour read/presentation operations, then publish/unpublish. Keep event acceptance,
   projection-pending responses, slug locking, and public visibility as separate states.
3. Extract upload intake and accessibility review by capability. Preserve stream ownership,
   malware scanning, validation, object-store cleanup, and metadata commit behavior.
4. Align infrastructure store/configuration folders without changing the EF model. Keep projection,
   outbox/idempotency, reconciliation, and worker ownership explicit at their existing boundaries.

Prerequisites: Catalog HTTP snapshots and projection/storage characterization. Risks: public cache
freshness, eventual consistency, object/database compensation, and ongoing upload work. Gate with
Catalog.UnitTests, Catalog.ApiServiceTests, and Catalog.ContractTests; run InfrastructureTests,
IntegrationTests, and BehaviorTests for the provider/event/storage seam touched. Do not combine
worker redesign or storage-provider replacement with these moves.

## Public Web sequence

1. Inventory existing route directives, injected Catalog/Branding clients, resources, and shared
   image/card components. Keep anonymous service discovery and host security setup unchanged.
2. Align tour list/detail pages and their exclusive presentation helpers; retain genuinely shared
   image/gallery components. Preserve loading, empty, missing, and unavailable states.
3. Align gallery and home/public-content ownership. Sequence localized resources and culture
   handling with each page, preserving fallback behavior, alt text, headings, and keyboard access.
4. Review sitemap/canonical/cache helpers after page moves, without changing their host ownership
   unless an actual caller requires it. Preserve Production startup validation.

Prerequisites: public route/rendering and canonical URL baseline. Risks: Razor namespace imports,
resource lookup, culture cache keys, SEO links, and image accessibility. Run Public.WebTests for
component and host scenarios; add a cross-deployable check only when the changed behavior needs
real Catalog interaction. No API/provider test should be replaced by a UI mock.

## Management Web sequence

1. Inventory each page's state lifetime, contracts client, forms, and authorization. Start with a
   read-only tour page before moving multi-step workflows.
2. Move booking forms and exclusive state with their capability, then customer creation wizard
   state and form models. Preserve state lifetime across navigation and validation messages.
3. Move import page/helpers as one workflow, retaining conflict choices, bounded upload handling,
   cancellation, summaries, focus management, and accessible status updates.
4. Align Catalog presentation/upload/accessibility pages, then Branding and document review/download.
   Preserve separate permissions and explicit HTTP outcomes, including projection-pending states.
5. Remove old imports and empty folders only after every caller has moved. Keep contract clients,
   token exchange, protected persistence, and download proxy composition in their existing owners.

Prerequisites: component behavior baseline and inventory of form/state consumers. Risks: scoped
wizard/import data, PII in errors, duplicate submissions, token audience boundaries, antiforgery,
and streaming lifetime. Gate each step with Management.WebTests; run WebIntegrationTests and
Admin.UiIntegrationTests/SystemTests for the actual API/browser seams changed. Coordinate with
the separate authentication-ticket expiry work before touching host authentication files.

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
