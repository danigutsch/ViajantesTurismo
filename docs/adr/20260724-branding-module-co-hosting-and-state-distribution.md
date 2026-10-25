# ADR-040: Branding Module Co-hosting and State Distribution

**Status**: Accepted - 2026-07-24

**Amended**: 2026-10-24 - worker hosts follow bounded-context ownership. The Branding-to-Admin consumer runs
in an Admin-owned `Admin.IntegrationEventWorker`, not in the Catalog-owned worker that runs Catalog consumers;
see [Worker ownership](#worker-ownership).

**Amended**: 2026-10-25 - implementation state and two recorded deviations; see
[Implementation state](#implementation-state).

**Amended**: 2026-10-25 - contraction complete. With no production deployment, the standalone
`ViajantesTurismo.Branding.ApiService` host and Admin's Branding HTTP edge were removed directly; the
expand/contract removal gates no longer apply. See [Executable removal](#executable-removal).

## Context

Branding is a small business capability with its own settings, permissions, persistence, public reads,
and management writes. Before this decision it ran in a separate `Branding.ApiService` process, some
ViajantesTurismo HTTP contracts and route segments were in `SharedKernel.Branding`, and Admin document
generation read Branding through API-to-API HTTP.

The process boundary is more expensive than the capability requires, but moving Branding into Catalog's
domain or tables would erase useful ownership. Admin also needs a durable local view without receiving a
Catalog database credential or adding an ordinary synchronous dependency on Catalog availability.

The production hosting platform, concrete runtime identities, database role names, and identity provider
are not selected. This decision therefore defines required boundaries; it does not claim that the topology
is deployed or production-ready.

## Decision

### Module and host boundaries

- Branding remains an independent logical module and owns the `branding` schema. It is co-hosted in
  `ViajantesTurismo.Catalog.ApiService`; co-hosting does not transfer Branding behavior, contracts, data,
  or permissions to the Catalog bounded context.
- Catalog and Branding deliberately share one process, failure domain, memory space, and credential blast
  radius. Separate DbContexts, connection roles, schemas, transactions, route policies, and module dependencies
  reduce accidental coupling; they do not provide process isolation after code execution is compromised.
- Catalog.ApiService is the only Branding HTTP host. There is no standalone Branding executable.
- `SharedKernel.Branding` retains only host-agnostic, pure validation and normalization rules. Branding's
  HTTP contracts, integration-event contracts, application behavior, infrastructure, defaults, and ASP.NET
  endpoint module are ViajantesTurismo-owned.
- Generic API-versioning helpers may compose a host route group, but the Branding module owns its concrete
  route segments and the Catalog host mounts them. SharedKernel does not own application route prefixes.

### State, revisions, and outbox

- Branding stores one authoritative current-state record with a non-empty source epoch and a monotonically
  increasing positive source revision. Each successful write also appends an immutable full-state revision
  record. Expansion assigns the existing settings row an initial epoch and revision before emitting the first
  state-transfer event.
- Branding writes use database-enforced optimistic concurrency against the expected current revision.
  Revision allocation, current-state replacement, history append, and outbox enqueue are one transaction;
  a conflict is returned rather than losing an update through a process-local lock or blind retry.
- The current row, full-state revision, and outbound integration event commit in one Branding transaction.
  Branding owns its outbox table and mapping in its schema; it does not share Catalog's outbox table merely
  because both modules use one process and physical database.
- SharedKernel EF messaging supplies generic, context-qualified mappings and registrations so two DbContexts
  in one host can select distinct schema/table ownership. The consuming module supplies the context identity,
  storage names, relay composition, and operational policy.
- This is event-carried state transfer, not event sourcing. The current-state record is authoritative; the
  append-only revisions provide immutable history and replay inputs, not aggregate reconstruction.

### PostgreSQL transport and acknowledgement boundaries

- Branding-to-Admin envelopes use the existing SharedKernel default PostgreSQL transport mapping in the
  **Catalog database**, `messaging.transport_messages`, with stable consumer name `admin`. Branding owns
  `branding.outbox_messages`; it does not create a second Branding transport table. The Catalog Infrastructure
  migration is the single Catalog-database migration owner for this transport table; Branding and the worker
  transport DbContext map it with migrations excluded.
- The Branding outbox relay owns the complete producer handoff. It first acquires the existing durable outbox
  claim lease. It then uses one `BrandingDbContext` transaction to insert the envelope into
  `messaging.transport_messages` and mark the outbox row published. The unique `(ConsumerName, EventId)`
  transport key is retained as a duplicate guard.
- There is no committed "transport enqueue but outbox not acknowledged" state. A crash or injected failure before
  that handoff transaction commits leaves neither the transport insert nor `PublishedAt`; the durable outbox
  claim remains until its lease expires and the relay retries. A crash after commit leaves both durable and needs
  no republish.
- `Admin.IntegrationEventWorker` connects to the Catalog database to claim `ConsumerName = 'admin'` rows with the
  existing lease/`FOR UPDATE SKIP LOCKED` pattern. It updates transport claim, retry, poison, and final
  `ProcessedAt` acknowledgement state there. A finite configured attempt limit moves an exhausted row to an
  explicit poison state; poison rows are never selected as ordinary pending work.
- The worker's source credential has `SELECT`/`UPDATE` only on Catalog-database
  `messaging.transport_messages`; it cannot read Branding current/history/outbox tables or Catalog domain tables.
  The relay credential can claim/update `branding.outbox_messages` and insert into the transport table, but cannot
  write Admin data. The worker's separate Admin credential can write only the Branding inbox, projection,
  watermark, recovery audit, and poison/replay audit surfaces needed by the consumer.
- Admin projection/inbox commit and Catalog transport acknowledgement are intentionally separate database
  transactions. If handling fails before the Admin commit, the transport row records a retry. If the process
  crashes after the Admin commit but before `ProcessedAt`, the claim lease expires and the row is redelivered;
  Admin inbox identity plus epoch/revision comparison makes the second delivery a no-op before the worker
  acknowledges transport.

### Worker ownership

- A worker host belongs to the bounded context that owns its consumers' business rules and data. Consumers
  share a host only when they share that owner, scaling, availability, deployment lifecycle, and credentials.
- `Catalog.IntegrationEventWorker` runs only Catalog consumers and background services and holds no Admin
  projection credential. `Admin.IntegrationEventWorker` runs the Branding-to-Admin consumer and holds the
  Catalog-database transport credential plus the Admin projection credential described below; it holds no
  Catalog domain credential.

### Admin projection and delivery policy

- `BrandingSettingsChangedIntegrationEvent` carries a non-empty source epoch, a positive source revision, and the complete
  immutable Branding document snapshot. No Branding integration event predates this contract and no valid event
  has a null or zero revision.
- `Admin.IntegrationEventWorker` delivers Branding events through an Admin-owned inbox into an Admin-owned local
  projection. Inbox completion, projection replacement, and consumer-watermark advancement are one Admin
  transaction.
- Within the accepted source epoch, a revision applies only when it is greater than the persisted Admin
  watermark. Duplicate or older revisions are acknowledged without replacing state. Because every event carries
  full state, a newer revision may safely close a revision gap. An unexpected epoch fails closed into recovery;
  ordinary delivery never switches source lineage implicitly.
- Only Admin document rows created before this rollout may have nullable `BrandingSourceRevision` provenance.
  Null means the immutable document snapshot predates revision capture; it is not an event ordering rule and
  must not be synthesized into a `BrandingSettingsChangedIntegrationEvent` envelope.
- Consumer health records the accepted source epoch, highest observed revision, highest applied revision, last
  successful apply time, retry state, and poison backlog. Operations compare producer delivery and consumer
  epoch/revision watermarks without giving Admin direct source-store access. Exact production alert thresholds
  remain an SLO/platform decision and must be set before production deployment.
- Delivery uses bounded retries. Exhausted messages move to an explicit poison state; they do not retry
  forever or block unrelated later full-state revisions. Replay is an audited operator action by immutable
  envelope/event identity after the cause is corrected, and remains safe through inbox idempotency and
  revision checks.
- Admin API reads only its local Branding projection. It receives no Catalog/Branding database credential and
  no Branding HTTP credential.

### Projection recovery and source rewind

- Admin persists a projection state of `Ready` or `Recovering`. Document generation and regeneration fail
  closed with an explicit recovery-unavailable outcome whenever Branding projection state is not `Ready`; they
  never capture a stale or partially reseeded snapshot. Each generation transaction reads `Ready`, projection,
  epoch, and revision together before recording its immutable document snapshot.
- Recovery first commits the `Recovering` fence, waits for in-flight document-generation transactions to finish,
  pauses new Branding transport claims, and lets active claim leases finish or expire. An audited operator
  request names the expected old epoch/watermark, reason, source backup or incident reference, and intended new
  epoch/revision.
- For a lost Admin projection/inbox/watermark with an intact source lineage, Branding republishes its current
  immutable snapshot with the same epoch and positive revision under a new event identity. For an intentional
  source restore that rewinds revision, operations first stop Management writes and the Branding relay. A
  privileged Branding recovery transaction rotates the source epoch, marks restored old-epoch pending outbox
  rows superseded, appends the new-epoch full-state reseed revision, records the rewind audit, and enqueues the
  restored current snapshot with its positive revision. Normal writes cannot rotate the epoch.
- `Recovering` fences ordinary relay polling and transport claims, but does not fence its own repair. The
  recovery authorization persists the exact reseed outbox and event identities. A one-shot recovery publisher
  may claim only that outbox row, and the worker's recovery claim path may deliver only the matching `admin`
  transport row. Every other row remains ineligible until `Ready`. Failure leaves the projection in
  `Recovering` and the authorized row retryable; it never opens an ordinary-delivery bypass.
- The same-epoch recovery path also rebuilds a lost/corrupt projection when inbox and watermark survived; only
  this authorized reseed may reapply the current equal revision. The ordinary consumer still requires a greater
  revision.
- The Admin reseed transaction verifies the authorized same-epoch reseed or epoch transition, records the reseed
  envelope in the inbox, replaces the projection, sets the epoch/revision watermark, appends old/new watermark
  and operator audit data, and returns projection state to `Ready` atomically. It does not delete prior inbox or
  recovery history.
- Pending rows from an old epoch are quarantined as poison before claims resume. Replaying the authorized reseed
  event is idempotent: the inbox identity and epoch/revision watermark preserve the already committed projection.
  The worker acknowledges the transport row only after the Admin reseed transaction commits. Management writes
  and the relay resume only after Admin returns to `Ready`.

### Management optimistic concurrency contract

- Management `GET` returns the full settings document with `SourceEpoch`, `SourceRevision`, and a strong opaque
  `ETag` whose canonical quoted value contains the stable epoch token and invariant positive revision, for
  example `ETag: "<epoch>:42"`. Epoch participation prevents a restored source lineage from reusing a
  pre-restore validator.
- Management `PUT` keeps full-state replacement semantics and the existing route, but requires `If-Match`.
  Absence returns `428 Precondition Required` with `branding.precondition_required`.
- The server parses `If-Match` using RFC 9110 grammar and evaluates it with strong comparison inside the write
  transaction. Multiple entity tags form a valid list; any strongly matching member makes the condition true.
  Weak tags are valid list members but never strongly match; a valid list with no strong match returns
  `412 Precondition Failed`.
- `If-Match: *` succeeds because the singleton Branding settings resource exists after expansion; it would return
  412 if the resource did not exist. A list and `*` are the grammar's alternative forms, so a field value that
  combines them is malformed rather than a list match.
- Only malformed conditional-header syntax returns `400 Bad Request` with `branding.invalid_if_match`. A
  syntactically valid but false condition—including a stale strong tag, a nonmatching list, or weak-only
  matches—returns 412 with `branding.precondition_failed`, performs no state/history/outbox write, and is never
  retried blindly. The editor reloads and deliberately reapplies or merges its changes.
- A successful `PUT` returns the saved document with its `SourceEpoch` and new `SourceRevision`, but omits
  `ETag`. Branding normalizes values and allocates a revision, so the stored representation is not guaranteed to
  be byte-for-byte identical to the request representation; RFC 9110 therefore does not permit a response
  validator. Management performs a follow-up `GET` before another edit and atomically replaces its document,
  epoch, revision, and opaque validator with that one response. It never combines the earlier `PUT` body with a
  later validator; an intervening writer therefore becomes visible instead of being overwritten.
- Branding OpenAPI documents retain the route, document `ETag` on Management `GET`, and add `If-Match`, `400`,
  `412`, and `428` metadata to `PUT` without advertising a successful-`PUT` validator. Contract/client tests
  cover two editors reading revision N, editor A writing N+1, editor B receiving 412 without overwriting A,
  normalization followed by validator refresh through `GET`, wildcard, matching/nonmatching lists, weak tags,
  and malformed syntax.

### HTTP, caching, and synchronous seams

- Public.Web and Management.Web retain Branding HTTP contracts, hosted by Catalog. Public reads
  keep a 60-second fresh lifetime and may be served stale for another 300 seconds while revalidating.
- Management reads and writes are uncached. Branding writes evict the host-local public Branding output cache.
  No Redis or other distributed cache is added without evidence of a stricter freshness, scale, or
  multi-instance invalidation requirement.
- Ordinary production API-to-API HTTP is prohibited for Branding state sharing. gRPC is reserved for a future
  demonstrated synchronous seam that cannot use local state or asynchronous transfer; this decision creates
  no such seam.

### Authorization and identity

- Catalog.ApiService registers separate named bearer schemes: `CatalogBearer` validates only
  `catalog-api`, and `BrandingBearer` validates only `branding-api`. Catalog routes select
  `CatalogBearer`; protected Branding routes select `BrandingBearer`, retain `branding.read` and
  `branding.write`, and do not inherit Catalog permissions merely because they share a host. Public Branding
  reads remain explicitly anonymous.
- The mounted module preserves Branding validation, the configured CORS allow-list, separate public-read and
  management-mutation rate limits, management `no-store` responses, and the authenticated fallback policy.
  Host defaults must not make a protected Branding route anonymous or broaden an allowed origin.
- Co-hosting does not introduce a second Branding audience or a policy that accepts both API audiences.
  Management.Web exchanges tokens for the exact `branding-api` audience and calls the Catalog-hosted Branding
  routes. The fallback policy remains fail closed.
- Local Keycloak remains a local/CI conformance provider. Production uses a separately selected and provisioned
  identity provider; this ADR does not claim that provider, its clients, or its roles exist.

### Executable removal

The removal is done. With no production deployment there was no traffic to drain or roll back, so the
standalone `ViajantesTurismo.Branding.ApiService` executable, its `branding-api` AppHost resource and
service-discovery references, and Admin's Branding HTTP client were removed directly. The `branding-api` token
audience remains the exact audience of the Catalog-hosted Branding routes. Branding `branding` and
`public-branding` OpenAPI artifacts are generated from Catalog.ApiService.

A future production deployment must still prove that the Catalog API and its Branding routes have the intended
private/public exposure, with no accidental backend ingress expansion caused by co-hosting.

### Required production roles, still undecided

Concrete platform identities and role names remain undecided. The selected platform must provide separate
least-privilege capabilities for:

- a Branding runtime writer that can read/write current state, append revisions, and enqueue only into the
  Branding outbox;
- a deployment-time migration identity that can apply approved Branding schema changes and the
  Catalog-database `messaging.transport_messages` migration owned by Catalog Infrastructure, but is not used by
  the runtime;
- producer/relay access that can claim/update `branding.outbox_messages` and insert
  `ConsumerName = 'admin'` rows into Catalog-database `messaging.transport_messages`, without Catalog-domain or
  Admin access;
- an `Admin.IntegrationEventWorker` Catalog-database credential limited to `SELECT`/`UPDATE` on
  `messaging.transport_messages`, plus a separate Admin credential limited to Branding inbox, projection,
  watermark, recovery, and replay-audit DML;
- Admin API read access to its local Branding projection only;
- audited operator access for poison inspection, replay, reseed authorization, and source-epoch rotation,
  separate from ordinary application identities; and
- route identities whose permissions remain scoped to `branding.read` and `branding.write`.

These are requirements for a future deployment design, not evidence that production roles are provisioned.

### Shared-process extraction criteria

Co-hosting accepts that a Catalog.ApiService compromise, memory leak, resource exhaustion, or bad deployment can
affect both modules and reach every credential granted to that process. Logical schemas and DI boundaries are
maintainability and least-privilege controls, not a security sandbox.

Reconsider a standalone Branding process or database when a demonstrated requirement needs credentials that
must not coexist in one process, independent availability/scaling/deployment, tenant or regulatory isolation,
different backup/retention policy, or measured resource/blast-radius contention. Extraction must preserve the
same Branding contracts, source revision/epoch, outbox, transport, and Admin projection semantics; it is not
triggered solely by folder count or hypothetical reuse.

### Explicit non-decisions

This seam adds no SignalR, SSE, WebSockets, HTTP/3 requirement, distributed cache, event-sourcing model,
projection framework, or new real-time transport. Existing technologies elsewhere in the solution are not
expanded into Branding by this decision.

### Required implementation tests

- PostgreSQL relay tests inject failure after the transport entity is added but before SaveChanges and after
  commit; they prove atomic transport/outbox acknowledgement, lease recovery, and one logical transport event.
- Worker tests cover failure before Admin commit and a crash after Admin inbox/projection/watermark commit but
  before transport `ProcessedAt`; redelivery must be idempotent and eventually acknowledge transport. Retry-limit
  tests prove transition to poison and audited replay.
- Contract tests prove every `BrandingSettingsChangedIntegrationEvent` has a non-empty epoch and positive revision, while only
  pre-rollout document rows may retain null Branding revision provenance.
- Management API/client/OpenAPI tests verify epoch/revision strong ETags, missing-header 428, malformed-syntax
  400, valid nonmatch 412, weak-tag strong comparison, existing-resource `*`, matching/nonmatching lists, no
  blind retry, omission of `ETag` from normalized `PUT` responses, follow-up `GET` validator refresh, and the
  two-editor lost-update scenario. An interleaving test performs writer A's `PUT`, writer C's update, and A's
  follow-up `GET`, then proves A replaces the whole editor state before another conditional write.
- Recovery tests cover source restore with epoch rotation, current-snapshot republish, lost projection with
  retained inbox/watermark state, complete projection/inbox/watermark rebuild, atomic reseed audit, old-epoch
  outbox supersession/transport quarantine, ordinary document-generation/write/relay gating, and the sole
  authorized transition `Recovering` -> reseed-only publish/claim/delivery -> `Ready`. Failure injection before
  recovery publish, after publish/before claim, and during delivery proves authorization is retained, the exact
  reseed row remains retryable, projection state stays `Recovering`, and unrelated rows remain fenced.
- OpenAPI tool and drift tests prove the `branding` OpenAPI target generates from Catalog.ApiService while
  preserving Branding-owned artifact names/content boundaries. Authorization tests prove Catalog and Branding
  routes select only their named exact-audience scheme.

### Implementation state

As of 2026-10-25 this decision is implemented in code; no production deployment exists.

- Admin document generation, regeneration, and payment instructions read Branding only from the local Admin
  projection through `DocumentBrandingSource` and fail closed (`503`, audit reason `BrandingUnavailable`) while
  the projection is missing or `Recovering`. Document drafts store `BrandingSourceRevision`. Admin has no HTTP
  dependency on Branding.
- The Management conditional-write contract (strong `ETag`, required `If-Match`, 400/412/428 problem codes,
  no validator on a successful `PUT`, refresh with `GET`) is implemented in the shared Branding module and
  Management.Web.
- Recovery persists `Ready`/`Recovering`, an operator authorization for exactly one reseed event, and an
  append-only recovery audit on both sides. Branding provides same-epoch republish and epoch rotation.
- Catalog.ApiService co-hosts the shared `ViajantesTurismo.Branding.AspNetCore` module with `CatalogBearer` and
  `BrandingBearer` schemes; no scheme authenticates by default. Catalog default and fallback policies select
  `CatalogBearer`; Branding policies select `BrandingBearer`. Public.Web and Management.Web call Branding
  through Catalog service discovery. Catalog.ApiService relays the Branding outbox to the Catalog-database
  transport for consumer `admin`, and `Admin.IntegrationEventWorker` applies it to the `branding_projection`
  schema.
- Branding `branding` and `public-branding` OpenAPI artifacts are generated from Catalog.ApiService with
  `dotnet tool run sharedkernel-ci -- openapi refresh --target branding`. Catalog's own `v1` document includes
  the co-hosted Branding routes; there is no Branding-only `v1` artifact.
- `VTARCH001` is enabled in Admin.ApiService and Catalog.ApiService; Branding HTTP clients resolve to the Catalog
  API destination.

Recorded deviations:

1. Restored old-epoch outbox rows are not marked superseded at the source, because the SharedKernel outbox has
   no supersession state. Admin quarantines old-epoch events on arrival instead (inbox `Rejected`,
   `superseded-epoch`), using the epochs retired by audited epoch transitions. The delivery outcome is the
   same: they never reach the projection.
2. The Admin Branding consumer keeps unbounded retry. The recovery fence leaves unauthorized events pending by
   failing their delivery; a consume-attempt limit would poison them merely for waiting. The Catalog consumer
   uses the bounded retry and poison state, and operators release poisoned messages with
   `IntegrationEventTransportPoison`.

## Consequences

### Positive

- Branding keeps clear data, contract, permission, and migration ownership without its own executable.
- Admin document generation becomes independent of Catalog HTTP and database availability after its local
  projection is seeded.
- Full-state events, monotonic revisions, transactional outbox delivery, inbox idempotency, and watermarks make
  out-of-order delivery and replay explicit without an event-sourcing framework.
- Co-hosting reuses the existing Catalog process and host-local output cache without adding Redis or another
  protocol.

### Negative

- Full-state revision and event records duplicate a small document by design and require retention sizing.
- Admin may serve its last applied Branding projection while delivery is delayed; watermark and poison health
  must make that staleness visible.
- SharedKernel EF messaging needs context-qualified composition before Branding and Catalog outboxes can safely
  coexist in one host.
- Catalog and Branding share one process and all credentials available to it; logical module boundaries cannot
  contain a full process compromise or process-wide availability failure.

## Alternatives Considered

1. **Keep a permanent standalone Branding service** - rejected because current scale does not justify the
   process, deployment, credential, and synchronous-call overhead.
2. **Fold Branding into the Catalog domain and tables** - rejected because process co-location does not justify
   losing Branding's logical, schema, permission, and migration ownership.
3. **Let Admin call Catalog HTTP or read the Catalog database** - rejected because it adds availability coupling
   or cross-context credentials and weakens least privilege.
4. **Event-source Branding or adopt a projection framework** - rejected because one current document plus
   immutable full-state revisions and a small consumer are sufficient.
5. **Add distributed cache, gRPC, or real-time push** - rejected because the accepted freshness window and
   asynchronous state transfer meet current requirements.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- [Branding](../branding.md)
- [Catalog bounded context](../bounded-contexts/Catalog.md)
- [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
- [Caching strategy](../caching-strategy.md)
- [Runtime wiring and deployment mapping](../architecture/runtime-wiring-and-deployment.md)
- [Production readiness and operations](../operations/production-readiness.md)
- Related: [ADR-024: CloudEvents as Integration Event Adapter](20260621-cloudevents-as-integration-event-adapter.md)
- Related: [ADR-026: Domain Materialization and SharedKernel Persistence Boundaries](20260621-domain-materialization-and-sharedkernel-persistence-boundaries.md)
- Related: [ADR-034: Bounded Context Databases and Outbox Ownership](20260705-bounded-context-databases-and-outbox-ownership.md)
- Related: [ADR-037: Deterministic, No-Network Build-Time OpenAPI Generation](20260715-deterministic-no-network-build-time-openapi-generation.md)
- Related: [Provider-neutral OIDC BFF authorization](20260711-provider-neutral-oidc-bff-authorization.md)
