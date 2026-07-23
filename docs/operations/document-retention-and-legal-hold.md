# Document retention and legal-hold policy proposal

| Attribute | Value |
| --- | --- |
| Status | Pending legal review |
| Policy identifier and version | `[TBD]` |
| Supersedes | `[TBD: prior policy identifier/version, or none]` |
| Superseded by | `[TBD: successor policy identifier/version, when applicable]` |
| Authority reference | `[TBD: opaque restricted-system identifier]` |
| Owner | `[TBD: named role or team]` |
| Effective date | Not effective |
| Next review date | `[TBD: YYYY-MM-DD]` |

Allowed statuses are `Pending legal review`, `Accepted`, `Rejected`, and `Superseded`. `Accepted`
requires an authority reference and effective date; it does not by itself claim legal compliance.

> This document is a decision proposal for Product, counsel, Privacy, and Operations review. It is
> not legal advice and does not establish a jurisdiction, lawful basis, controller or processor role,
> retention period, deletion authority, or legal-hold authority. The current 30-day draft and
> 24-month audit behaviors are provisional technical behavior, not GDPR or LGPD minima and not an
> approved policy.

This repository copy is a non-authoritative engineering summary. The authoritative approval record,
approver identities or signatures, counsel analysis, jurisdiction-specific exceptions, and matter
details must remain in an access-controlled legal or governance system. This document records only an
opaque authority reference, approval roles, and non-confidential implementation context.

## Production release gate

Before enabling this document lifecycle for production personal data, named Product and counsel
owners must approve a versioned policy and its jurisdiction-specific exceptions. Until then:

- do not claim that the document lifecycle or its current retention defaults are GDPR/LGPD compliant
- do not change or backfill retention clocks, purge existing records, or delete finalized artifacts
  based on this proposal
- do not place or release a legal hold, or rely on a hold to deny erasure, without approved authority
- do not treat document immutability as authority for indefinite retention

If the lifecycle is already deployed with real personal data, obtain an approved interim instruction.
Do not unilaterally continue, disable, or change destructive behavior.

This proposal does not complete `#945` or `#992`. `#945` remains blocked on approved finalized-artifact
retention and legal-hold rules. `#992` remains blocked on an approved audit lifecycle and its
jurisdiction-specific exceptions. Neither parent issue is closed by this proposal.

## Purpose and scope

This proposal covers generated document drafts, staff overrides, source snapshots, finalized artifact
bytes and metadata, document lineages, document audit records, document idempotency entries, and
copies in backups, replicas, indexes, caches, exports, restores, and processors.

The policy must be decided per record class, processing purpose, and applicable jurisdiction. Where
rules differ, counsel must define the conflict rule; engineering must not assume that the longest or
shortest period wins.

## Required approval metadata

| Decision field | Proposed value | Approval owner |
| --- | --- | --- |
| Policy identifier and version | `[TBD]` | Policy owner |
| Supersession links | `[TBD: prior and successor policy identifiers/versions; preserve prior records]` | Policy owner |
| Restricted authority reference | `[TBD: opaque identifier; no counsel narrative or case data]` | Policy owner |
| Status and effective date | `Pending legal review / not effective`; legal review and effective date `[TBD]` | Product and counsel |
| Processing purposes | `[TBD: contractual, travel operations, accounting, support, dispute, and security purposes as applicable]` | Product and counsel |
| Controller/processor role | `[TBD per processing activity and jurisdiction]` | Counsel |
| Lawful basis or other authority | `[TBD per purpose and jurisdiction]` | Counsel |
| Applicable jurisdictions | `[TBD, including data-residency constraints]` | Product and counsel |
| Policy owner | `[TBD: named role or team]` | Product |
| Approval roles | `[TBD: Product, counsel, Privacy, and Operations roles; identities and signatures remain restricted]` | Policy owner |
| Review date and cadence | `[TBD: YYYY-MM-DD and cadence]` | Policy owner |

No universal retention minimum is asserted. Each approved period or objective criterion must include
its necessity rationale, trigger, disposition, exceptions, and review owner.

## Current technical behavior

The following behavior exists today. Describing it does not approve it.

| Record class | Current behavior | Approval gap |
| --- | --- | --- |
| Unfinalized drafts | `RetentionExpiresAt` is set to creation plus 30 days. A daily worker deletes eligible drafts in batches of 500 and removes empty lineages. | The trigger, period, exceptions, and production authority are unapproved. |
| Finalized, superseded, and voided revisions | Finalization clears `RetentionExpiresAt`. Finalized HTML bytes and metadata remain in the Admin database; no deletion or legal-hold workflow exists. | Retention trigger, duration, disposition, and hold interaction are unresolved. |
| Document audit records | Metadata-only records receive an expiry 24 months after occurrence. A daily worker deletes eligible records in batches of 500. Database protections reject updates, truncation, and pre-expiry deletion. | The period, exceptions, and production authority are unapproved. Linkable identifiers remain personal data where re-identification is possible. |
| Document idempotency entries | `messaging.idempotency_keys` stores the normalized caller key, a resource-specific scope containing a booking or document identifier, processing timestamps/state, and the result document identifier. No expiry or purge exists. A replay after its document is purged returns `404 Not Found`. | The bounded replay window, cleanup trigger, relationship to document expiry, and raw-key handling are unresolved. Clients must not place personal data or secrets in keys. |
| Backups and replicas | The generic production-readiness guidance requires backup and restore planning, but no document-specific retention, hold propagation, or deletion behavior is selected. | Inventory, rotation, deletion latency, restore suppression, and evidence are unresolved. |

The current artifact is an in-database HTML byte payload. This proposal must not claim that a future
PDF renderer or object store is already deployed.

## Retention schedule decisions

Product and counsel must select or replace each candidate. A candidate is not effective until the
approval checklist is complete and engineering validates the approved rule.

| Record class | Trigger | Candidate decision | Expiry action | Required rationale |
| --- | --- | --- | --- | --- |
| Unfinalized draft and staff overrides | Creation, when never finalized | **Candidate A:** retain the current 30-day period. **Candidate B:** `[TBD]` days or an objective criterion. | `[TBD: delete, irreversibly anonymize, or another approved disposition]` | Operational review/recovery need, data minimization, and jurisdiction-specific requirements |
| Finalized revision and artifact | `[TBD: finalization, contract completion, supersession, voiding, or another event]` | `[TBD: duration or objective criterion]` | `[TBD, including supervised deletion and evidence]` | Contractual, accounting, dispute, and statutory analysis by jurisdiction |
| Superseded or voided finalized revision | `[TBD]` | `[TBD: same as or distinct from active finalized records]` | `[TBD]` | Necessity of historical revisions and effect of voiding |
| Document audit record | Audit event occurrence | **Candidate A:** retain the current 24-month period. **Candidate B:** `[TBD]` months or an objective criterion. | `[TBD: delete or approved irreversible anonymization]` | Security, accountability, dispute, and jurisdiction-specific analysis |
| Document idempotency entry | First accepted keyed request and completion | `[TBD: bounded replay window and relationship to associated document expiry]`; decide whether to store a digest rather than the normalized caller token. | Delete after the approved replay window and applicable hold checks. | Retry contract, associated-resource availability, abuse prevention, data minimization, and backup behavior |

## Legal-hold decisions

A hold may restrict ordinary deletion or anonymization only to the approved extent necessary for its
documented purpose. It must not authorize unrelated processing or indefinite retention by default.

| Required control | Decision required |
| --- | --- |
| Hold authority | `[TBD: authorized role, jurisdiction, delegation, and restricted approval reference]` |
| Hold identifier | Opaque matter or hold ID; keep case details and personal data outside public repository records. |
| Scope | `[TBD: exact documents, bookings, data subjects, record classes, systems, and processors]` |
| Permitted processing and access | `[TBD: restricted uses, roles, and access-review cadence]` |
| Placement evidence | Record authority, purpose, scope, effective date, review date, and actor without document content or unnecessary personal data. |
| Review and optional expiry | `[TBD: review cadence; automatic expiry only if counsel expressly approves it]` |
| Release authority | `[TBD: authorized role, required confirmation, reason, and timestamp]` |
| Post-release disposition | Resume the approved lifecycle and record completion evidence within `[TBD]`. |
| Erasure/correction/dispute interaction | `[TBD: jurisdiction-specific process; preserve only what approved authority requires and record the response rationale]` |

After approval, hold checks must be atomic with purge decisions and fail closed. A purge worker must
not delete an in-scope record when hold state cannot be determined.

## Stores, copies, and deletion evidence

| Location or copy | Required approved outcome |
| --- | --- |
| Primary draft rows, staff overrides, source snapshots, lineage links, and finalized bytes | Apply the approved disposition unless an applicable hold prevents it. Record policy version and completion evidence. |
| Audit records and linkable identifiers | Apply their independently approved lifecycle; do not assume metadata is anonymous. |
| Idempotency entries and resource-linked scopes | Use non-sensitive opaque keys, apply the approved bounded replay window, and delete entries consistently with associated-resource expiry and hold rules. |
| Replicas, indexes, caches, exports, and processor copies | `[TBD: owner, deletion or anonymization method, maximum latency, verification, and hold propagation]` |
| Backups | `[TBD: rotation period, encryption/access controls, selective-deletion capability or expiry model, and hold handling]` |
| Restored backups | Prevent reintroduction of records whose approved disposition has completed, including after any hold release, through `[TBD: suppression/replay process and evidence]`. |

Deletion must not be described as complete until the approved outcome for every covered store and copy
has been verified. Pseudonymized or opaque identifiers remain personal data when they are linkable.

## Engineering work after approval

After approval, engineering must:

1. Version the approved rules and map each record class to its trigger, period or criterion,
   disposition, jurisdiction, and exceptions.
2. Add authorized legal-hold placement, review, and release controls with least-privilege access.
3. Make hold checks and purge decisions atomic and fail closed.
4. Plan migrations and backfills without silently deleting or extending existing records.
5. Cover primary stores, replicas, backups, restores, exports, caches, indexes, and processors.
6. Add synthetic-data tests for boundary dates, jurisdiction conflicts, holds, release before
   retention expiry, post-release disposition, erasure, restoration, retries, and operational
   evidence.
7. Monitor purge failures and overdue held-record reviews without logging document or customer data.
8. Add bounded idempotency-entry cleanup and test replay before, at, and after both key and document
   expiry without retaining raw personal data or secrets.

## Approval checklist

- [ ] Product approves each processing purpose, lifecycle trigger, period or objective criterion,
      disposition, owner, and review date.
- [ ] Counsel approves controller/processor roles, lawful bases or authorities, jurisdictions,
      necessity rationales, and conflict rules.
- [ ] Counsel approves hold authority, scope, restricted processing, review, release, optional expiry,
      and erasure interaction.
- [ ] Privacy and Operations approve the store/copy inventory, backup and restore behavior, deletion
      latency, processor handling, and verification evidence.
- [ ] Product, Privacy, and Operations approve the idempotency replay window, key representation,
      cleanup trigger, and interaction with document expiry and legal hold.
- [ ] Engineering maps approved rules to versioned configuration, implementation, migrations,
      monitoring, runbooks, and tests.
- [ ] The policy owner records approver identities/signatures, approval date, effective date, and
      next review date in the restricted authority record; Git retains only its opaque reference.
- [ ] The `Status` and `Effective date` attributes change only after every applicable item is
      complete.

Until these items are complete, this proposal remains non-approved and cannot authorize production
retention, deletion, or legal-hold behavior.

## Implementation evidence and references

- Draft limits: `src/ViajantesTurismo.Admin.Domain/Documents/DocumentLimits.cs`.
- Audit limits: `src/ViajantesTurismo.Admin.Domain/Documents/DocumentAuditLimits.cs`.
- [Generated travel documents](../generated-travel-documents.md).
- [Generated document workflow ADR](../adr/20260710-generated-travel-document-format-and-workflow.md).
- [Privacy classification and redaction](../privacy-classification.md).
- [Production readiness and backup guidance](production-readiness.md#backup-restore-and-disaster-recovery).
- [GDPR Articles 5 and 17](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng).
- [Brazil LGPD Articles 15 and 16](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm).
