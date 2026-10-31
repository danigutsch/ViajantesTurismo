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
| Document idempotency entry | Completion, or start when never completed | 30 days (`DocumentCommandIdempotency.ReplayWindow`, equal to `DocumentLimits.DraftRetentionDays`) | Deleted by a daily worker in batches of 500. The stored key is a SHA-256 digest of the caller key, so the raw value is never persisted. A replay after the window is treated as a new request; a replay after its document is purged returns `404 Not Found`. |
| Document audit purge evidence | Each audit-retention purge run | Kept indefinitely | Metadata only: system actor, correlation id, purged count, retention cutoff, outcome and time. It is not removed by the audit-retention worker. |

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
Stored legacy values that are not data URIs read as no logo. The `WidenDocumentBrandingLogoUri` migration widens
`DocumentDrafts.BrandingLogoUri` to 60,000 characters and is one-way in practice: narrowing it back fails once data
URIs exist. Each captured revision can carry up to about 54 KB of logo text.

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

### Who may place and release

- Permission `document.hold` covers placing, releasing and listing. By default only the `Admin` role holds it; `Operator` and
  `Reader` do not, and `document.manage` does not imply it.
- **Product decision pending:** this default awaits Product confirmation. Changing it means editing the role mapping in
  `AdminAuthorization.PermissionsByRole`.
- The placing and releasing actor is the opaque subject identifier of the authenticated principal. It is never read from the
  request body.

### Purge behavior and lock order

- Every purge (draft, audit and idempotency) loads its candidates, calls `IDispositionHoldGuard.Check` once with all candidate
  lineage scopes, and deletes inside the same explicit database transaction at read committed. The package refuses any other
  isolation level and refuses to answer outside a transaction.
- Held candidates are skipped and counted. The audit purge records the count (`HeldCount`) in its evidence; the other purges log
  counts only. No data-subject identifier is logged.
- If hold state cannot be determined (`DispositionHoldStateUnknownException`), the purge deletes nothing. The audit purge records
  `Refused` evidence and rethrows; the worker retries on its next run.
- Lock order: take the locks a command declares first, hold locks last, one `Check` per purge, and no other advisory lock after
  it. Release takes a command lock on the hold identifier before it reads the hold.
- A SQL pre-filter leaves lineages with an effective hold out of the candidate query before the batch limit, so held rows cannot
  starve a batch. It is an optimization only; the guard check on the final candidates stays the authoritative decision.
- Audit records store their lineage so a hold placed after a revision was purged still covers that revision's audit records.
  A record written before that column existed whose lineage could not be derived (its revision was already purged) is
  undecidable: it is left out of the candidate query so it cannot occupy a batch, kept (fail closed), and counted separately as
  `UndecidableCount` in the purge evidence. Whether to keep or purge such legacy records is a Product decision.
- Residual: an audit record written for a document that no longer exists is stamped as having no lineage and stays purgeable. A
  draft purged between the handler's read and the audit write could therefore leave one audit record that a later hold on the
  same lineage does not cover.
- The held counts of the three purges come from the same pre-filter (effective holds by the database clock) plus any candidates
  the guard then reports as held, so a hold placed during the run is counted too. The guard check stays authoritative.
- Idempotency entries are matched to a lineage through the document identifier in their scope or stored result. An entry whose
  document row was already purged cannot be matched and is purged on schedule; it holds a hashed key and an identifier only.

### Not done

- Management UI for holds.
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

- Hold management UI.
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
