# Payments for solo owners and small teams

The payment workflow keeps obligations in EUR and records already performed EUR or BRL transfers.
It never initiates a bank transfer. Payment obligations require EUR tours; receipts record actual EUR
or BRL settlement through the same financial history.

## Staff workflow

An Admin can perform every payment action alone, including creating and explicitly approving the
same record. Saving a draft never approves it. Confirmation summaries show the current revision,
amounts, recipient and relevant expiry before approval. Creator, approver, timestamps and reason
codes remain in the history; self-approved records are visibly marked.

Two Admins can finish each other's drafts. Optional independent review records the reviewer,
immutable revision and outcome. The creator cannot independently review their own revision.
A request for changes flags attention without withdrawing an existing approval. A changed recipient
or replacement terms require a new revision and fresh review. Version preconditions reject stale
concurrent changes; no staffing or presence check blocks progress.

Operators can read masked payment information and record actual receipts pending reconciliation.
Only Admins access complete recipient details or evidence references, manage accounts, approve,
allocate, reconcile, refund, reverse, generate or download instructions. These restrictions are
also enforced by the API.

1. Open **Payment accounts**, save a recipient draft, independently check it against a bank or
   recipient source, then explicitly attest the method and check time. Identifier validation proves
   format, not ownership. IBAN/BIC, Brazilian bank details and typed Pix identifiers use reusable
   SharedKernel value objects.
2. Open a booking's **Finances** page. Save ordered installments with calendar due dates and exact
   EUR cents that sum to the obligation; approve the summary separately. Approved terms are immutable.
   Price or installment changes need a replacement revision. Existing allocations remain historical
   until an Admin explicitly releases and assigns them to selected replacement installments.
3. For BRL settlement, record an agreed BRL-per-EUR quote, source, agreement reference, agreement
   instant and explicit expiry. Approve it explicitly. Public informational rates are not used as
   agreed transaction quotes. Historical quote evidence can be approved after expiry, but expired
   quotes cannot generate, finalize or deliver new instructions.
4. Generate **Payment instructions** from selected outstanding installments and a verified recipient.
   Approve and finalize the immutable summary, then download printable HTML through the authenticated
   proxy. Rounding uses midpoint away from zero; each installment carries an explicit cumulative
   rounding adjustment. Financial fields cannot be edited as document text.
5. Record gross receipt, fees, actual settlement currency, calendar payment date, bank reference and,
   when evidenced, exact bank settlement instant. Select proposed installments from the latest schedule, including draft terms when still unapproved.
   These selections do not approve terms or settle installments. A receipt remains
   pending until explicit Admin reconciliation approves its EUR credit and allocation.
6. Exact reconciliation requires matching frozen amounts, reference, currency, fees, selected
   installments, timely due date and evidenced settlement instant. Late, short, excess, fee-bearing
   or mismatched receipts require the corresponding explicit reason. Cash and EUR credit remain
   separate. Residual cash can be credited later using the receipt's original agreed conversion
   basis; each additional reconciliation is appended and explicitly approved. Unapplied EUR credit
   can be allocated later or refunded.
7. Record an already performed partial or full refund with its original cash basis, EUR credit to
   reverse, selected original allocations, actual settlement and transfer evidence. BRL refunds
   require a newly agreed refund quote. Approval reverses selected EUR credit; an obligation
   adjustment determines any remaining debt. Processing fees do not prevent refunding the original
   gross receipt. Actual settlement and FX differences remain distinct. Cumulative limits prevent
   overrefunds. Correct an erroneous active refund record explicitly before fully reversing its
   original receipt. Release conflicting allocations before a correction restores previously reversed
   EUR credit. Neither correction nor reversal performs a transfer or deletes history.

Cancellation stops new instructions without erasing debt or calculating fees. Approve an explicit
replacement obligation with a cancellation-adjustment reason. Bookings with financial history cannot
be deleted. Overdue status uses the configured business date, independently of payment state.

Account replacement, retirement, withdrawal, stale terms, cancellation, quote expiry, supersession
and voiding block further affected instruction delivery. Every finalization and download rechecks
eligibility. Regeneration creates an immutable revision. Files already downloaded cannot be revoked.

## Persistence and operation

Apply migration `20261021132243_PaymentLifecycle` before enabling workflows. It creates protected
accounts, financial histories, instruction snapshots and metadata-only audits. Nothing has been
deployed, so the unpublished migration chain no longer creates the old `Payment` table and contains
no import, currency backfill or compatibility workflow. Recreate any development database built
with the earlier unpublished schema; old local payment rows are not imported. Database recreation
discards its local data and requires the operator to choose when to perform it. A rollback with
financial evidence still fails closed.

Receipts exist only inside the protected financial history. They retain settlement method, currency,
gross, fees, net, creator and calendar payment date. Booking balances and status derive only from
approved EUR obligations and unreversed allocations. Before approval there is no inferred debt from
the booking price, and pending cash does not settle installments. Booking pages link to the financial
workflow; the obsolete payment endpoint, DTOs, form, collection and storage model have been removed.
The existing calendar-date behavior remains in place for booking dates, tour schedules, customer
birth dates, installments and receipt dates.

Financial commands and generation require `Idempotency-Key`; retain the exact key and request after
an unknown outcome. A changed payload cannot reuse the key. Resource locks, revision preconditions,
immutable database records and atomic changes/audits prevent concurrent overallocations, overrefunds
and recipient-retirement races. The UI retains pending submissions until their outcome is known.

The new endpoints are `/api/v1/payment-accounts`, `/api/v1/bookings/{id}/finances`, and payment
instruction operations under `/api/v1/documents`. Sensitive responses use `no-store`; downloads also
require authorization, attachment disposition and `nosniff`. Do not log account details, financial
payloads, payment references, request/response bodies or protected ciphertext. Telemetry processors
remove financial and recipient attributes.

### Encryption configuration

The rollout flag is owned by the Admin application maintainers and Operations and defaults to
disabled. Its cleanup review is triggered after encryption and migration
have been verified in production; removing it must preserve all authorization and delivery checks.
Provision these settings through the deployment's existing secret
configuration; do not commit certificate passwords or private keys:

| Setting | Required value |
| --- | --- |
| `Payments:Enabled` | `true` only after rollout gates pass |
| `Payments:BusinessTimeZone` | Explicit supported business time-zone identifier |
| `Payments:Protection:KeyDirectory` | Existing absolute persistent directory |
| `Payments:Protection:CertificatePath` | Absolute PFX path with its private key |
| `Payments:Protection:CertificatePassword` | PFX password when present |
| `Payments:Protection:PreviousCertificates:{index}:Path` | Retained previous private PFX for rotation |
| `Payments:Protection:PreviousCertificates:{index}:Password` | Previous PFX password when present |

Existing `KeyBoundDataProtector` binds ciphertext to each record and separate Admin account, ledger,
snapshot and artifact purposes. Keys persist on disk and are certificate-protected. Startup and
reads fail closed; there is no plaintext fallback. Keep encryption configuration available for
historical reads even if the feature is disabled.

Back up the database, complete historical key ring and every private certificate/password needed
to decrypt it, using separately controlled encrypted storage. **Deleting historical keys or losing
private certificates makes existing records unreadable.** Preserve application name and purpose
identifiers during recovery. Restore these together in an isolated environment and verify historical
accounts, ledger snapshots and finalized HTML before reopening access. During certificate rotation,
install the new certificate and retain previous certificates for decryption; keep historical keys.
Follow [Microsoft's persistence guidance](https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview)
and [key-management guidance](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-management).

### Rollout and retention

Deploy persistent encryption configuration and migrations before enabling the workflow. Verify a
single Admin lifecycle, shared drafts, optional review, permissions, EUR/BRL settlement, concurrency,
restart and key recovery in the target environment. The migration refuses rollback that would erase
new financial records; use a reviewed forward recovery or restore procedure.

Document retention follows the accepted
[retention policy](operations/document-retention-and-legal-hold.md): unfinalized drafts expire after
30 days, metadata audits after 24 months, and finalized artifacts are kept indefinitely. Payment
snapshots follow their document's retention. This implementation makes no GDPR/LGPD compliance claim.

## Follow-ups

Secure customer links, automated delivery, provider-sourced agreed FX quotes and bank reconciliation
remain separate follow-ups. Optional organizational mandatory dual approval can be added later;
it is never a prerequisite for solo or shared operation. Updating remote acceptance criteria for FX and refund work requires separate authorization to
edit the issue.
