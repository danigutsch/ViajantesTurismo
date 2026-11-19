# ADR-047: Tour retirement with retained booking history

## Status

Accepted, 2026-11-19. The Admin API owner approved the retirement semantics, role grant and
Catalog completion contract. The existing retention and hold policy applies to retained records.

## Context

The Admin API exposes Tour create, read and update. The internal `DeleteTourCommand` is a
generated mediator `ICommand<Result>`. Its handler loads the aggregate through the ordinary
store, calls `Tour.Delete`, removes the row, and saves. The current domain guard rejects any
attached financial history and any confirmed booking. It can therefore permit deletion with only
pending, cancelled or completed bookings and no financial history. The behavior tests explicitly
cover the pending case and leave terminal booking archival to a business decision.

The Admin migration, model and a read-only schema check against a freshly migrated PostgreSQL
test database show these relationships.

| Dependent | Principal | Delete action |
| --- | --- | --- |
| `Booking.TourId` | `Tours.Id` | Cascade |
| `BookingPaymentHistories.BookingId` | `Booking.Id` | Restrict |
| `PaymentInstructionSnapshots.BookingId` | `Booking.Id` | Restrict |
| `DocumentDrafts.DocumentLineageId` | `DocumentLineages.Id` | Cascade within documents |

Document lineages, revisions and audit records carry booking identifiers without a database
foreign key to the booking. A physical Tour delete could remove bookings that still provide
source data for those documents. A payment foreign key can separately reject the cascade.
[PostgreSQL foreign key actions](https://www.postgresql.org/docs/18/ddl-constraints.html#DDL-CONSTRAINTS-FK)

Tour creation sends an outbox event and Catalog can materialize a draft from it. Catalog owns
publication and its public projection in a separate database. The current Admin delete path
does not announce a lifecycle change to Catalog.

## Decision

Expose retirement of the active Tour resource through `DELETE /api/v1/tours/{id:guid}`.
Retirement stores the transition on the original row, retains its identifier and relationships,
and withdraws its Catalog visibility. HTTP DELETE supports archiving the underlying data.
[HTTP DELETE semantics](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.3.5)

### Authorization and responses

Compose the operation under `MapToursEndpoints` and the existing Admin API group. Add the
distinct `tour.delete` policy and default it to the Admin role, matching `booking.delete`.
Apply the existing mutation rate limit and the Admin problem response format. Authorization
precedes command dispatch and resource lookup. The request uses the path identifier.

| Status | Meaning |
| --- | --- |
| `202 Accepted` | Admin has committed retirement. Catalog visibility is still being reconciled. `Location` identifies `/api/v1/tours/{id:guid}/retirement`. |
| `204 No Content` | The retirement and Catalog reconciliation are already complete. |
| `404 Not Found` | The identifier has no matching Tour, including the empty identifier. |
| `409 Conflict` | A pending or confirmed booking requires resolution before retirement. |
| `401`, `403`, `429` | The established authentication, authorization and request-limit outcomes. |

The status read uses `tour.delete`, returns the durable pending or completed state, and uses
`no-store`. Return `404` when there is no accepted retirement for that identifier. Admin active
Tour reads exclude retired rows. Historical booking, payment and document operations still
resolve their source Tour. In particular, keep a historical read for `DocumentSourceVersionReader`
when narrowing the active Tour query. Keep existing identifier and name uniqueness.

### Admin transaction and history

Reuse `DeleteTourCommand` through `ISender` and the shared Admin Result transaction behavior.
Resolve `Option<Tour>` from `ITourCommandStore` after obtaining the Tour row lock. Keep the
eligibility rule on the aggregate and apply it to the bookings loaded in that transaction.
Introduce a monotonic retirement marker and a server-captured UTC time. Store the audit evidence
with the authenticated opaque actor, Tour identifier, operation identifier, result and time.
Commit retirement, its audit record and its Catalog outbox entry as one unit.

Use the same Tour row lock for operations that add a booking, change retirement eligibility, or
change the active Tour. Acquire it before any booking-level lock and evaluate the current state
after the lock. Apply the retired-state guard to new bookings, confirmation and active Tour edits.
The current Create Booking handler needs this locking lookup before an endpoint can be exposed.
An ordinary read followed by an insert can observe an active Tour before retirement commits.
[PostgreSQL row locks](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS)

A pending or confirmed booking produces a conflict without an automatic cancellation. A Tour
without active bookings can retire, including one with cancelled or completed bookings. Preserve
all booking rows, payment history and snapshots, document lineages, artifacts, audit rows, and
holds. Keep authorized historical activities such as refunds and document hold actions available.
Concurrent hold placement or release continues on its existing lineage lock. Retirement changes
availability and performs no retention disposition of the history. Govern any future physical
purge separately through an approved retention and hold policy.

If the execution strategy replays after a durable commit, load the retained marker and return
success for the same retirement. The stable transition identifier prevents a second audit or
outbox record. Rollback or cancellation before commit leaves the Tour active and emits nothing.

### Catalog completion

Add a minimal Tour retirement event to the existing transport. Catalog records a durable fence
keyed by the source Tour identifier. Creation, publication and retirement respect the same local
fence at their commit boundary. A retirement delivery ahead of creation records the fence,
so a delayed create or replay cannot restore public visibility. Catalog withdraws the materialized
view and retains the event history. A previously started publish can finish before Catalog applies
the fence. The status endpoint reports pending during this interval.

The first deployment uses no-store for Catalog's active tour list and slug reads and for Public
Web's home, tour list, detail, gallery, sitemap and visibility-checked renditions. A read started
before the visibility fence can finish without populating a response cache. Catalog commits its
idempotent completion with the visibility fence and sends it through the existing transport.
Failures leave the operation pending, with observable retries and the existing authorized
worker recovery path.

When upgrading a deployment that served cacheable tour responses, drain the old replicas and
purge cached responses for those routes before granting the retirement permission. This ensures
Catalog can acknowledge completion as soon as the visibility transaction commits.

## Consequences

Implementation needs a backward-compatible migration, a historical query path and two durable
cross-context events. Extend the existing Tour behavior tests to encode the changed pending rule.
Keep the rollout behind the new operator permission until the transport and projection are proven.

Required proofs include role boundaries and effective HTTP metadata, all four booking states,
unchanged historical record counts and hold ownership, and real PostgreSQL concurrency in both
orders for booking creation and retirement. Check failed and thrown transaction outcomes and
durable commit replay. Exercise Catalog creation or publication racing with retirement, reordered
or duplicated deliveries, and cache or worker recovery before marking the operation complete.

## Alternatives

Physical deletion would require a governed purge, a complete dependency inventory, safe handling
of historical references and holds, and a Catalog withdrawal protocol. The current cascade and
restrict constraints make this a different retention operation. An Admin-only retirement would
leave Catalog free to publish retained tours while Admin considers them retired.

## Links

- [Architecture decision index](../ARCHITECTURE_DECISIONS.md)
- [Bounded context databases and outbox ownership](20260705-bounded-context-databases-and-outbox-ownership.md)
- [Admin vertical slice migration plan](../vertical-slice-migration-plan.md#admin-sequence)
- [Document retention and legal hold](../operations/document-retention-and-legal-hold.md)
