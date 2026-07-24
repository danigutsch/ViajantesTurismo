# ADR-040: Branding Module Co-hosting and State Distribution

**Status**: Accepted - 2026-07-24

## Context

Branding is a small business capability with its own settings, permissions, persistence, public reads,
and management writes. The current implementation gives it a separate `Branding.ApiService` process,
while some ViajantesTurismo HTTP contracts and route segments are in `SharedKernel.Branding`. Admin
document generation currently reads Branding through API-to-API HTTP.

The process boundary is more expensive than the capability requires, but moving Branding into Catalog's
domain or tables would erase useful ownership. Admin also needs a durable local view without receiving a
Catalog database credential or adding an ordinary synchronous dependency on Catalog availability.

The production hosting platform, concrete runtime identities, database role names, and identity provider
are not selected. This decision therefore defines required boundaries and removal gates; it does not claim
that the target topology is deployed or production-ready.

## Decision

### Module and host boundaries

- Branding remains an independent logical module and owns the `branding` schema. It is co-hosted in
  `ViajantesTurismo.Catalog.ApiService`; co-hosting does not transfer Branding behavior, contracts, data,
  or permissions to the Catalog bounded context.
- The standalone Branding executable remains available during an expand/contract migration. It is removed
  only after every rollback and exposure gate below passes.
- `SharedKernel.Branding` retains only host-agnostic, pure validation and normalization rules. Branding's
  HTTP contracts, integration-event contracts, application behavior, infrastructure, defaults, and ASP.NET
  endpoint module are ViajantesTurismo-owned.
- Generic API-versioning helpers may compose a host route group, but the Branding module owns its concrete
  route segments and the Catalog host mounts them. SharedKernel does not own application route prefixes.

### State, revisions, and outbox

- Branding stores one authoritative current-state record with a monotonically increasing source revision.
  Each successful write also appends an immutable full-state revision record.
- Dual-host writes use database-enforced optimistic concurrency against the expected current revision.
  Revision allocation, current-state replacement, history append, and outbox enqueue are one transaction;
  a conflict is returned or retried explicitly rather than losing an update through a process-local lock.
- The current row, full-state revision, and outbound integration event commit in one Branding transaction.
  Branding owns its outbox table and mapping in its schema; it does not share Catalog's outbox table merely
  because both modules use one process and physical database.
- SharedKernel EF messaging supplies generic, context-qualified mappings and registrations so two DbContexts
  in one host can select distinct schema/table ownership. The consuming module supplies the context identity,
  storage names, relay composition, and operational policy.
- This is event-carried state transfer, not event sourcing. The current-state record is authoritative; the
  append-only revisions provide immutable history and replay inputs, not aggregate reconstruction.

### Admin projection and delivery policy

- A Branding state-transfer event carries its source revision and the complete immutable Branding document
  snapshot. During compatibility rollout, a legacy event has a `null` source revision.
- `IntegrationEventWorker` delivers Branding events through an Admin-owned inbox into an Admin-owned local
  projection. Inbox completion, projection replacement, and consumer-watermark advancement are one Admin
  transaction.
- A non-null revision applies only when it is greater than the persisted Admin watermark. Duplicate or older
  revisions are acknowledged without replacing state. Because every event carries full state, a newer
  revision may safely close a revision gap.
- A legacy `null` revision is idempotent by event identity, does not advance the revision watermark, and must
  not overwrite a projection after any non-null revision has applied. Cutover seeds at least one versioned
  full-state event before the legacy path is retired.
- Consumer health records the highest observed source revision, highest applied source revision, last
  successful apply time, retry state, and poison backlog. Operations compare producer delivery and consumer
  watermarks without giving Admin direct source-store access. Exact production alert thresholds remain an
  SLO/platform decision and must be set before standalone-host removal.
- Delivery uses bounded retries. Exhausted messages move to an explicit poison state; they do not retry
  forever or block unrelated later full-state revisions. Replay is an audited operator action by immutable
  envelope/event identity after the cause is corrected, and remains safe through inbox idempotency and
  revision checks.
- Admin API reads only its local Branding projection. It receives no Catalog/Branding database credential and
  no Branding HTTP credential.

### HTTP, caching, and synchronous seams

- Public.Web and Management.Web retain Branding HTTP contracts, hosted by Catalog in the target. Public reads
  keep a 60-second fresh lifetime and may be served stale for another 300 seconds while revalidating.
- Management reads and writes are uncached. Branding writes evict the host-local public Branding output cache.
  No Redis or other distributed cache is added without evidence of a stricter freshness, scale, or
  multi-instance invalidation requirement.
- Ordinary production API-to-API HTTP is prohibited for Branding state sharing. gRPC is reserved for a future
  demonstrated synchronous seam that cannot use local state or asynchronous transfer; this decision creates
  no such seam.

### Authorization and identity

- Catalog-hosted Branding routes use named, route-specific audience and authorization policies. Public reads
  remain explicitly anonymous; protected routes retain `branding.read` and `branding.write` permission checks
  and do not inherit broader Catalog permissions merely because they share a host.
- The mounted module preserves Branding validation, the configured CORS allow-list, separate public-read and
  management-mutation rate limits, management `no-store` responses, and the authenticated fallback policy.
  Host defaults must not make a protected Branding route anonymous or broaden an allowed origin.
- During dual-host cutover, only explicitly named legacy and target audiences may be accepted on the matching
  Branding routes. The fallback policy remains fail closed. Old-audience acceptance is removed only after
  token lifetime and active-session drain are complete.
- Local Keycloak remains a local/CI conformance provider. Production uses a separately selected and provisioned
  identity provider; this ADR does not claim that provider, its clients, or its roles exist.

### Expand/contract and executable removal gates

Removal of `ViajantesTurismo.Branding.ApiService` is a later contraction step. Before removal, the rollout must:

1. apply forward-compatible additive schema changes that both the rollback artifact and upgraded writer can use;
2. deploy the upgraded Branding writer in both standalone and Catalog hosts against the same transactional
   revision, history, and outbox rules;
3. verify state-transfer events, Admin inbox/projection updates, watermark telemetry, bounded retry, poison,
   and replay behavior;
4. move Public.Web and Management.Web traffic to the Catalog-hosted routes while retaining the standalone host;
5. drain legacy-audience tokens and sessions, then remove temporary audience acceptance;
6. observe zero standalone Branding traffic for the agreed rollback window;
7. complete a rollback drill that routes traffic to the retained standalone release artifact and proves
   read, write, event delivery, and Admin projection recovery;
8. retain a deployable previous standalone release artifact until the contraction release is accepted; and
9. prove the Catalog API and its Branding routes have the intended private/public exposure, with no accidental
   backend ingress expansion caused by co-hosting.

Only after all gates pass may a later change remove the executable, AppHost resource, old service-discovery
reference, legacy audience policy, and rollback artifact.

### Required production roles, still undecided

Concrete platform identities and role names remain undecided. The selected platform must provide separate
least-privilege capabilities for:

- a Branding runtime writer that can read/write current state, append revisions, and enqueue only into the
  Branding outbox;
- a deployment-time migration identity that can apply approved Branding schema changes but is not used by the
  runtime;
- producer/relay access that can claim and update Branding outbox delivery state without broad Catalog-domain
  table access;
- `IntegrationEventWorker` consumer access that can read the transport envelope and write only Admin Branding
  inbox, projection, watermark, retry, and poison state;
- Admin API read access to its local Branding projection only;
- audited operator access for poison inspection and replay, separate from ordinary application identities; and
- route identities whose permissions remain scoped to `branding.read` and `branding.write`.

These are requirements for a future deployment design, not evidence that production roles are provisioned.

### Explicit non-decisions

This seam adds no SignalR, SSE, WebSockets, HTTP/3 requirement, distributed cache, event-sourcing model,
projection framework, or new real-time transport. Existing technologies elsewhere in the solution are not
expanded into Branding by this decision.

## Consequences

### Positive

- Branding keeps clear data, contract, permission, and migration ownership while one small executable can
  eventually be removed.
- Admin document generation becomes independent of Catalog HTTP and database availability after its local
  projection is seeded.
- Full-state events, monotonic revisions, transactional outbox delivery, inbox idempotency, and watermarks make
  out-of-order delivery and replay explicit without an event-sourcing framework.
- Co-hosting reuses the existing Catalog process and host-local output cache without adding Redis or another
  protocol.

### Negative

- The expand/contract period runs two hosts and temporary audience compatibility, increasing rollout and
  observability work.
- Full-state revision and event records duplicate a small document by design and require retention sizing.
- Admin may serve its last applied Branding projection while delivery is delayed; watermark and poison health
  must make that staleness visible.
- SharedKernel EF messaging needs context-qualified composition before Branding and Catalog outboxes can safely
  coexist in one host.

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
- Related: [ADR-034: Bounded Context Databases and Outbox Ownership](20260705-bounded-context-databases-and-outbox-ownership.md)
- Related: [Provider-neutral OIDC BFF authorization](20260711-provider-neutral-oidc-bff-authorization.md)
