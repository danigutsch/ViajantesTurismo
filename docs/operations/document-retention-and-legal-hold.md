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
| Document idempotency entry | First accepted keyed request | No expiry | Kept until the cleanup in the follow-up below exists. A replay after its document is purged returns `404 Not Found`. |

Metadata-only audit records can contain linkable identifiers, which remain personal data where
re-identification is possible. Callers must not place personal data or secrets in idempotency keys.

The current artifact is an in-database HTML byte payload. A future PDF renderer or object store needs
a new policy version before it holds finalized artifacts.

## Legal hold

Finalized artifacts are kept indefinitely, so a hold has nothing to restrain today. Legal hold
matters once any deletion of finalized artifacts, anonymization or an erasure workflow exists.

- No legal-hold workflow exists. Do not add deletion of finalized artifacts before it does.
- A hold may restrict deletion or anonymization only as far as its documented purpose needs.
- Hold checks must be atomic with purge decisions and fail closed: a purge worker must not delete an
  in-scope record when hold state cannot be determined.
- Placement and release record an authorized actor, an opaque hold identifier, purpose, scope,
  effective date and review date, without document content or unnecessary personal data.

## Copies

Replicas, indexes, caches, exports, processor copies and backups follow the same periods. The
generic [backup and restore guidance](production-readiness.md#backup-restore-and-disaster-recovery)
applies; no document-specific rotation, selective deletion or restore suppression exists yet.
Deletion is not complete until every covered store and copy has the approved outcome.

## Follow-up work

Each gap above is tracked as an issue instead of a placeholder in this policy:

- Legal-hold placement, review and release controls, with hold checks on every purge.
- Bounded cleanup of document idempotency entries.
- Document-specific backup rotation and restore suppression.

## Implementation evidence and references

- Draft limits: `src/ViajantesTurismo.Admin.Domain/Documents/DocumentLimits.cs`.
- Audit limits: `src/ViajantesTurismo.Admin.Domain/Documents/DocumentAuditLimits.cs`.
- [Generated travel documents](../generated-travel-documents.md).
- [Generated document workflow ADR](../adr/20260710-generated-travel-document-format-and-workflow.md).
- [Privacy classification and redaction](../privacy-classification.md).
- [Production readiness and backup guidance](production-readiness.md#backup-restore-and-disaster-recovery).
- [GDPR Articles 5 and 17](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng).
- [Brazil LGPD Articles 15 and 16](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm).
