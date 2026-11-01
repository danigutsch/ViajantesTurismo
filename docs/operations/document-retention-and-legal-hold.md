# Document retention and legal-hold policy

| Attribute | Value |
| --- | --- |
| Status | Accepted |
| Policy identifier and version | `DOC-RET-1.0` |
| Supersedes | None |
| Superseded by | None |
| Authority reference | Product owner decision of 2026-10-27 |
| Owner | Product |
| Effective date | 2026-10-27 |
| Next review date | 2027-10-27 |

Allowed statuses are `Pending legal review`, `Accepted`, `Rejected`, and `Superseded`. `Accepted`
does not by itself claim legal compliance.

> This policy is a Product decision. It is not legal advice and makes no claim that the periods
> below satisfy GDPR, LGPD or any other jurisdiction's minimum or maximum. Counsel has not reviewed
> jurisdiction-specific exceptions. Add them as a new policy version when they are known.

Approver identities and any counsel analysis stay in an access-controlled system. This repository
records only the opaque reference above and non-confidential implementation context.

## Scope

The policy covers generated document drafts, staff overrides, source snapshots, finalized artifact
bytes and metadata, document lineages, document audit records, and document idempotency entries.

## Retention rules

| Record class | Trigger | Period | Disposition |
| --- | --- | --- | --- |
| Unfinalized draft and staff overrides | Creation, when never finalized | 30 days (`DocumentLimits.DraftRetentionDays`) | Deleted by a daily worker in batches of 500; empty lineages are removed. |
| Finalized, superseded and voided revisions | None | Kept indefinitely | Finalization clears `RetentionExpiresAt`. No automatic deletion and no deletion endpoint. |
| Document audit record | Audit event occurrence | 24 months (`DocumentAuditLimits.RetentionMonths`) | Deleted by a daily worker in batches of 500. Database protections reject updates, truncation and pre-expiry deletion. |
| Document idempotency entry | Completion, or start when never completed | 30 days (`DocumentCommandIdempotency.ReplayWindow`, equal to `DocumentLimits.DraftRetentionDays`) | Deleted by a daily worker in batches of 500. The stored key is a SHA-256 digest of the caller key, so the raw value is never persisted. A replay after the window is treated as a new request; a replay after its document is purged returns `404 Not Found`. Each entry records its document lineage as the retention owner when it starts. |
| Document audit purge evidence | Each audit-retention purge run | Kept indefinitely | Metadata only: system actor, correlation id, purged count, retention cutoff, outcome and time. It is not removed by the audit-retention worker. |
| Draft and idempotency purge evidence | Each draft or idempotency purge run | Kept indefinitely | Counts only: purge kind, purged count, held count, outcome (`Succeeded` or `Refused`) and time, in `DocumentRetentionPurgeRecords`. Written in the same transaction as the deletion. It is not removed by any worker. |

Metadata-only audit records can contain linkable identifiers, which remain personal data where
re-identification is possible. Callers must not place personal data or secrets in idempotency keys.

The current artifact is an in-database HTML byte payload. A future PDF renderer or object store needs
a new policy version before it holds finalized artifacts.

Branding is captured onto each revision (name, colors, fonts, version and source revision) and the
finalized HTML is stored as bytes, so later branding changes and regeneration never alter a finalized
artifact. The logo is embedded: at capture the Admin Branding projection's stored WebP bytes (at most 40 KiB)
become a `data:image/webp;base64,...` URI that must pass the SharedKernel raster data-URI check. The document stores
that URI, so the finalized HTML contains the image itself, and the logo SHA-256 (not an address) is part of the
recorded branding version. A projection without a logo yields a brand-name-only header. Admin-typed, root-relative
and external logo addresses are never captured and are never rendered, so opening an artifact loads nothing from a
third party or from the Admin host, and replacing or deleting the logo later does not change a finalized artifact.
`DocumentDrafts.BrandingLogoUri` holds only a validated raster data URI or null and is 60,000 characters wide
(`DocumentLimits.MaxBrandingLogoUriLength`). Each captured revision can carry up to about 54 KB of logo text.

## Legal hold

Finalized artifacts are kept indefinitely, so a hold has nothing to restrain among them today. A hold restrains the
purges that do exist: unfinalized drafts, document audit records and document idempotency entries.

### Hold model

- Holds use the SharedKernel `DispositionHold` packages. The hold table is `disposition_hold.holds` in the Admin database.
- A hold has an opaque scope, a neutral basis (`LegalClaim`, `LegalObligation`, `DataSubjectRestriction`,
  `RegulatoryInquiry` or `Investigation`), an opaque purpose code, an opaque reference to the authorizing record, the placing
  actor, effective, review and optional expiry dates, and one explicit release with the releasing actor and an opaque reason code.
- The scope kind is `document-lineage` and the key is the lowercase 32-digit (`N` format) lineage identifier. Every text
  field accepts only ASCII letters, digits and `. _ : -`. Callers must not derive codes from names, e-mail addresses or
  document numbers. Rejected requests never echo the submitted value.
- A hold restrains deletion only. It does not freeze edits to a draft.
- API (Admin, version 1): `POST /api/v1/documents/{id}/holds`, `GET /api/v1/documents/{id}/holds` and
  `POST /api/v1/documents/{id}/holds/{holdId}/release`, where `{id}` is any revision of the lineage. A second release of the
  same hold is `409 Conflict`; a hold of another lineage is `404 Not Found`.

### Hold audit evidence

- Placing and releasing a hold each add exactly one `DocumentAuditRecords` row (`PlaceHold`, `ReleaseHold`) in the same
  transaction as the hold change. An audit record that cannot be formed persists nothing, so no hold changes without its record.
- The row carries the opaque actor, correlation id, document id, hold id, hold basis name and a bounded audit reason code
  (`ManualHoldPlacement`, `ManualHoldRelease`). It never carries the purpose code, authority reference, release reason code
  or any request text. `HoldBasis` accepts ASCII letters only, at most 64 characters.
- A rejected request (unknown document, unknown or foreign hold, double release, invalid field) changes no hold and adds one
  `Rejected` row with `DocumentNotFound`, `HoldNotFound`, `StateConflict` or `ValidationRejected`. A request refused by
  authorization never reaches the handler and adds nothing, like every other document route.
- The rows are stamped with the document lineage and follow the 24-month audit retention; a hold on the lineage restrains
  their purge like any other audit row.

### Who may place and release

- Permission `document.hold` covers placing, releasing and listing. By default only the `Admin` role holds it; `Operator` and
  `Reader` do not, and `document.manage` does not imply it.
- **Product decision pending:** this default awaits Product confirmation. Changing it means editing the role mapping in
  `AdminAuthorization.PermissionsByRole`.
- The placing and releasing actor is the opaque subject identifier of the authenticated principal. It is never read from the
  request body.

### Management UI

- The document details page (`/documents/{id}`) shows each hold's state, basis, review date and recorded actors, and states
  that an active hold blocks deletion, disposition after voiding and anonymization. Admins place a hold through a form
  (basis, purpose code, authority reference, review date, optional expiry) with an explicit confirmation step, and release
  one through a modal dialog that restates the document and hold identifiers and requires an opaque reason code.
- The holds overview (`/documents/holds`, Admin only) lists active, overdue-review and recently released holds (the last 30
  days), filterable by basis and paged. It shows the opaque lineage identifier and links each row to the document page
  of the newest revision (`documentId` in the API item). A lineage with no remaining document, for example one already
  purged, is listed without a link and `documentId` is `null`.
- The UI sends and shows opaque codes, dates and subject identifiers only. A hold takes effect when it is placed; the API has
  no separate effective date.

### Purge behavior and lock order

- Every purge (draft, audit and idempotency) loads its candidates, calls `IDispositionHoldGuard.Check` once with all candidate
  lineage scopes, and deletes inside the same explicit database transaction at read committed. The package refuses any other
  isolation level and refuses to answer outside a transaction.
- Held candidates are skipped and counted. Every purge records the count (`HeldCount`) in its evidence row; the draft and idempotency purges also log
  it. No data-subject identifier is logged.
- If hold state cannot be determined (`DispositionHoldStateUnknownException`), the purge deletes nothing. Each purge records
  `Refused` evidence and rethrows; the worker retries on its next run.
- Lock order: take the locks a command declares first, hold locks last, one `Check` per purge, and no other advisory lock after
  it. Release takes a command lock on the hold identifier before it reads the hold.
- A SQL pre-filter leaves lineages with an effective hold out of the candidate query before the batch limit, so held rows cannot
  starve a batch. It is an optimization only; the guard check on the final candidates stays the authoritative decision.
- Audit records store their lineage so a hold placed after a revision was purged still covers that revision's audit records.
  Every record is stamped when it is inserted: the lineage of its document, `Guid.Empty` (no lineage) when the document does not
  exist at write time, or null only when the record names no document. The immutability trigger rejects every UPDATE and TRUNCATE,
  and the check constraint `CK_DocumentAuditRecords_DocumentLineageStamped` (`DocumentId IS NULL OR DocumentLineageId IS NOT NULL`)
  makes an unstamped record that names a document impossible.
- A request naming a document that does not exist is audited without a document identifier: the record keeps the operation, the
  actor, the `DocumentNotFound` reason and the correlation, and names no resource. The domain rejects a `DocumentNotFound`
  rejection that carries a document, booking or revision identifier, and `DocumentAuditWriter` drops them before it creates the
  record. An identifier that resolved to no document belongs to no lineage, so no hold has anything to cover, and a purged
  revision of a held lineage can no longer leave an audit record that a later hold cannot reach. A request that did load its
  document before a concurrent purge still stamps the lineage it loaded, because the stamp reads the tracked draft first. The
  `Guid.Empty` stamp stays only as a fail-closed fallback for a document that vanished between that read and the save.
- The held counts of the three purges come from the same pre-filter (effective holds by the database clock) plus any candidates
  the guard then reports as held, so a hold placed during the run is counted too. The guard check stays authoritative.
- Idempotency entries record the document lineage as their retention owner when they start (`IIdempotencyStore.TryStart` with
  the lineage id in `N` format), so a hold keeps them after the revision rows were purged. The purge reads the owner column; it no
  longer matches document identifiers inside scope or result strings. An expired entry in a document scope without an owner is
  retained, counted as unattributed and logged as a count (fail-closed) instead of being purged. Such entries can only exist in
  development databases that predate the owner column, because nothing is published (see the initial migration squash); they
  stay until removed by hand. Entries outside document scopes are never touched by this purge.
- Open follow-up: SharedKernel `IIdempotencyRetentionStore.PurgeExpired` is not used yet. It purges the expired entries of every
  scope, including the payment entries that share the table, so it needs a scope restriction in SharedKernel first. This
  repository keeps a document-scoped purge over the same owner column until then.

### Not done

- Backup rotation and restore suppression for held data.
- Retention of finalized artifacts and any deletion workflow for them. Do not add one without checking holds.
- Retention periods, jurisdiction-specific rules and review reminders.

## Copies

Replicas, indexes, caches, exports, processor copies and backups follow the same periods. The
generic [backup and restore guidance](production-readiness.md#backup-restore-and-disaster-recovery)
applies; no document-specific rotation, selective deletion or restore suppression exists yet.
Deletion is not complete until every covered store and copy has the approved outcome.

## Follow-up work

Each gap above is tracked as an issue instead of a placeholder in this policy:

- Document-specific backup rotation and restore suppression.
- Finalized-artifact retention.

## Implementation evidence and references

- Draft limits: `src/ViajantesTurismo.Admin.Domain/Documents/DocumentLimits.cs`.
- Audit limits: `src/ViajantesTurismo.Admin.Domain/Documents/DocumentAuditLimits.cs`.
- [Generated travel documents](../generated-travel-documents.md).
- [Generated document workflow ADR](../adr/20260710-generated-travel-document-format-and-workflow.md).
- [Privacy classification and redaction](../privacy-classification.md).
- [Production readiness and backup guidance](production-readiness.md#backup-restore-and-disaster-recovery).
- [GDPR Articles 5 and 17](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng).
- [Brazil LGPD Articles 15 and 16](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm).
