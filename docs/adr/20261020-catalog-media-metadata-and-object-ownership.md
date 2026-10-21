# ADR-041: Catalog Media Metadata and Object Ownership

**Status**: Accepted ownership decision — 2026-10-20. Provider convergence and deletion hardening
remain implementation work; this ADR does not claim they already pass conformance tests.
Owner: Catalog maintainers.

## Context

Catalog keeps editorial/publication metadata in its database and binary originals/variants in local
or S3-compatible storage. Object headers duplicate some database fields, but the local adapter does
not round-trip all S3 metadata. A database transaction cannot atomically commit object-store writes.
We need one repair authority without making a storage listing a public-content catalogue.

Current evidence:

- [PublicMediaImage](../../src/ViajantesTurismo.Catalog.Domain/Media/PublicMediaImage.cs) owns source
  identity, checksum, dimensions, processing status, accessibility, variants, and tour links.
- [Upload intake](../../src/ViajantesTurismo.Catalog.Application/Media/MediaImageUploadIntake.cs)
  coordinates validated storage and metadata persistence.
- [LocalMediaObjectStore](../../src/ViajantesTurismo.Catalog.Infrastructure/LocalMediaObjectStore.cs)
  writes through a temporary file and replaces an existing destination; reads infer content type
  from the key and do not return a persisted application checksum.
- [SeaweedFsMediaObjectStore](../../src/ViajantesTurismo.Catalog.Infrastructure/SeaweedFsMediaObjectStore.cs)
  sends content headers and optional checksum metadata; Put does not assert create-only semantics.
- [Reconciliation](../../src/ViajantesTurismo.Catalog.Application/Media/MediaObjectReconciliationService.cs)
  compares referenced keys with inventory and supports optional orphan deletion with a grace period.
  Its scan is not a cross-store transaction or proof that a concurrent upload is inactive.

## Decision

Catalog's database is canonical for identity, editorial meaning, relationships, lifecycle, and public
eligibility. Object storage is canonical for bytes that actually exist and provider transport state.
Object metadata is advisory to application decisions and never grants publication or access.

### Field ownership and deliberate duplication

| Field | Canonical owner | Permitted duplication and reconciliation direction |
| --- | --- | --- |
| Media ID, source/variant association, tour links, gallery order, tags | Catalog database | Do not reconstruct these from a bucket listing or object metadata |
| Alt text, caption, attribution, copyright, language, human/AI review state | Catalog database | Do not copy into object headers; update through governed Catalog commands |
| Processing state and publication eligibility | Catalog domain/database, combined with owning tour publication state | Storage presence is necessary but insufficient; missing variants fail closed for public delivery |
| Original and derived byte content | Object store | Database records describe validated content but cannot recreate a missing original |
| Object key and role | Catalog database records/variant references | Storage uses the same opaque key to address bytes; a listed key alone creates no media record |
| Content type | Validated Catalog metadata for the application response | Object Content-Type may mirror validated type; mismatch requires re-probing bytes before repair, never trusting a client header |
| Byte length and dimensions | Values measured from accepted bytes and persisted by Catalog | Storage reports actual length; mismatch quarantines/reprocesses the object instead of silently editing editorial records |
| Content checksum and algorithm | Digest computed over accepted bytes, associated with the Catalog source/variant | Copy to object metadata for diagnostics/integrity verification; a metadata string alone is not proof of bytes. Current source checksum contract must be inspected before adding algorithm/version fields |
| Creation/review/processing timestamps | Catalog for business lifecycle | Provider LastModified is inventory/transport evidence only; it must not overwrite the business timeline |
| ETag, provider version ID, transport last-modified | Object provider | Optional opaque evidence in adapter/operation records; never interpret an ETag as a universal content hash |
| Private/public URI construction | Deployment/adapter configuration | Store keys rather than expiring or environment-specific signed URLs as durable identity |

Do not repair business metadata from a storage header. To repair a safe transport header, derive it
from validated database/byte evidence and use a reviewed provider operation. Do not mutate bytes
under a referenced key to reconcile metadata.

### Original, variant, and access policy

Originals are private processing inputs and may contain EXIF or other personal data. Public delivery
serves only approved processed variants after sanitization/EXIF removal and accessibility/publication
checks. Do not expose bucket listings, original URLs, credentials, or signed URLs through logs.
Variants have their own measured type/length/dimensions and must not borrow the original's checksum.

Use opaque application-generated relative keys with traversal validation and no personal names or
uploaded filenames. Storage-native lifecycle policies must not delete live database references.
Database-backed serving and application-managed reconciliation remain authoritative until a reviewed
provider lifecycle rule proves it respects references, in-flight work, retention, and recovery.

### Writes, retries, deletes, and deduplication

Treat each accepted original/variant identity as immutable: new content gets a new key and a new
reviewed reference. A retry may succeed as a no-op only when the intended identity and verified bytes
match; a different payload for an existing key is a conflict. Current adapters allow replacement, so
create-only/verified-retry behavior needs failing-first tests and a separate implementation change.
Do not advertise provider equivalence for that behavior today.

Write bytes before committing the corresponding ready/reference state, and preserve compensation
for database failure. Partial output never becomes publicly Ready. Retry transient failures within
bounded operation limits; preserve cancellation and report terminal failures without payloads.

Deduplication is a future intake optimization, not permission to share mutable keys. Compare a
verified digest plus algorithm/length and appropriate authorization scope, never ETag or filename
alone. Before sharing a physical object, define reference ownership and concurrent delete safety;
unlinking one media record must not delete bytes referenced by another. Keep current per-upload
ownership until that design and its tests are implemented.

Deletion requires explicit owned intent and a fresh check of all original/variant references plus
in-flight work. Use an agreed grace period; inventory age is a candidate signal, not deletion
approval. Do not delete from a potentially incomplete scan or after failed pagination. No deletion
is authorized by accepting this ADR.

### Failure and repair authority

| Failure | Repair authority and action |
| --- | --- |
| Object write fails before metadata commit | Intake/processor reports failure; retain no ready reference; retry with the same intended identity only after verifying whether bytes were written |
| Object exists but database commit failed | Intake compensation removes only its owned unreferenced output; otherwise reconciliation reports an orphan candidate for later guarded cleanup |
| Database references a missing original | Catalog marks/unpublishes unsafe availability through its normal lifecycle, reports sanitized failure, and restores from approved backup or requests re-upload; never fabricate bytes |
| Variant missing or corrupt | Processor rebuilds to a new key from the retained trusted original, validates output, then commits references; do not serve corrupt content meanwhile |
| Same key with conflicting digest/length/type | Stop automatic repair and quarantine/escalate; inspect authoritative bytes and accepted metadata; never overwrite to make counts agree |
| Listing/delete failure | Preserve references and failed-operation evidence; retry boundedly; never treat inaccessible/partial inventory as an empty bucket |
| Delete races with upload/reference creation | Preserve bytes until active ownership is resolved; grace alone is insufficient. Implement claim/recheck protection before enabling unattended destructive cleanup |

### Provider equivalence and conformance

Local and S3-compatible providers must offer the same observable success/not-found, key validation,
stream disposal, cancellation, atomic visibility, pagination, and approved retry/deletion semantics.
They need not expose identical native headers or metadata capabilities. Missing capabilities are
explicit, not silently synthesized as trustworthy evidence. AWS documentation is background for S3
semantics, not proof SeaweedFS supports every AWS option.

Existing coverage to preserve:

- [Local object store tests](../../tests/ViajantesTurismo.Catalog.InfrastructureTests/LocalMediaObjectStoreTests.cs):
  existing-reader stability across replacements.
- [SeaweedFS adapter tests](../../tests/ViajantesTurismo.Catalog.UnitTests/SeaweedFsMediaObjectStoreTests.cs):
  checksum metadata, not-found, pagination, key safety, and provisioning behavior.
- [Reconciliation tests](../../tests/ViajantesTurismo.Catalog.UnitTests/MediaObjectReconciliationServiceTests.cs):
  missing/orphan reports, explicit deletion, grace, and retryable failures.

Required conformance follow-ups: byte/hash round-trip, create-only conflict and identical retry,
partial/canceled write visibility, concurrent reference/delete races, duplicate shared-reference
protection, complete inventory failure, EXIF-stripped variants, private originals, and public-ready
filtering. Add failing tests before changing each production gap; provider mocks do not prove a
real storage server's conditional-write behavior.

### Migration and reconciliation sequence

1. Inventory existing database references and object headers read-only, with counts and sanitized
   mismatch categories. Verify backups and processing jobs before changing keys or storage settings.
2. Establish checksum algorithm/byte evidence and provider conformance for new writes. Existing
   unverified checksum strings remain unverified; backfill only from trusted actual bytes.
3. Introduce immutable keys for new outputs behind the existing application boundary. Migrate an
   existing object by copy, verify, then atomically update its database references. Keep old bytes
   during the recovery window; never overwrite the only copy.
4. Run reconciliation in report mode, resolve missing/corrupt references, and implement concurrent
   claim/recheck protection. Enable deletion only after a separately approved dry-run and recovery
   evidence. A code rollback cannot undo deleted bytes.

## Consequences

Editorial review and publication cannot be bypassed by manipulating object headers. Provider swaps
have a concrete contract and repair direction. Byte/metadata duplication is small and intentional.
The database is still not a backup of original bytes, and cross-store operations require compensation,
monitoring, and recovery. Existing overwrite/deletion-race gaps remain visible instead of being
hidden by an ownership document.

## Alternatives

- Object storage as the metadata catalogue: rejected because listings cannot express governed
  relationships, review history, public eligibility, and transactional editorial updates.
- Duplicate all editorial fields into object metadata: rejected because it creates conflicting
  authorities, privacy exposure, and provider-specific update behavior.
- Store all media bytes in the database: not selected; it changes backup/scaling behavior without
  a demonstrated need and does not remove the need for safe public delivery.

## Links

- [ADR index](../ARCHITECTURE_DECISIONS.md)
- [Catalog bounded context](../bounded-contexts/Catalog.md)
- [Multi-store consistency audit](../architecture/multi-store-consistency-audit.md)
- [Privacy classification](../privacy-classification.md)
- [Amazon S3 metadata semantics](https://docs.aws.amazon.com/AmazonS3/latest/userguide/UsingMetadata.html)
- [Amazon S3 ETag semantics](https://docs.aws.amazon.com/AmazonS3/latest/API/API_Object.html)
