# Generated travel documents

This plan defines the generated travel document scope.

## Goal

Generate travel documents from existing booking, customer, tour, pricing, payment, and operational
source data, allow staff to review and finalize the content, and produce accessible PDFs for
customers and internal operations.

The first implementation must stay small: one document type, one vertical slice, no new rendering
framework until tests prove the required behavior.

## Use cases

The epic covers several generated travel document categories. The first implementation should prove
one path without blocking later document types.

| Document type | Audience | First-slice status | Notes |
| --- | --- | --- | --- |
| Customer-facing confirmation packet | Customer | In first slice | Smallest customer-visible path that exercises draft, review, PDF generation, storage, audit, and regeneration. |
| Travel summary | Customer | Later slice | Can reuse the confirmation workflow once itinerary and localization needs are proven. |
| Itinerary | Customer or staff | Later slice | Needs richer day-by-day tour content and localization review before PDF rendering. |
| Voucher | Customer or supplier | Later slice | Needs supplier-facing privacy rules and redemption/audit policy. |
| Contract | Customer and staff | Later slice | Needs legal text ownership, version approval, and stronger retention rules. |
| Proposal | Customer | Later slice | Needs expiration, pricing validity, and conversion-to-booking rules. |
| Invoice or receipt | Customer and finance staff | Later slice | Needs accounting ownership, payment/tax policy, and retention confirmation. |
| Internal operations packet | Staff | Later slice | Needs staff-only audience rules and stricter operational-data authorization. |

### Customer-facing confirmation packet

Purpose: send a finalized travel packet to the customer after the booking is accepted and the staff
member has reviewed the generated content.

Expected content:

- booking reference and tour identity
- customer and companion names when required by the document
- tour dates, itinerary summary, meeting point, and included services
- payment summary that avoids exposing sensitive payment details
- operational notes that are approved for customer visibility
- cancellation, change, and support contact instructions

### Internal operations packet

Purpose: give staff a printable or downloadable trip-support packet for manual operations.

Expected content:

- booking reference, tour identity, and customer/companion roster
- logistics notes needed by staff
- non-sensitive payment state summary
- manual-review notes and completion state

Internal packets may include more personal data than customer-facing packets, but only when the
staff role is authorized and the purpose is operationally necessary.

### Regenerated corrected packet

Purpose: regenerate a draft after source data changes or after a template update.

Expected behavior:

- existing finalized PDFs remain immutable audit records
- regeneration creates a new draft or revision
- previous manual edits are carried forward only when the field still matches the new template and
  remains safe for the document audience
- staff must review before the corrected packet is finalized

## Source-data boundaries

Generated documents read from existing authoritative records. They must not become the source of
truth for bookings, customers, tours, pricing, payments, or operational data.

| Source data | Boundary | Rule |
| --- | --- | --- |
| Booking data | Admin booking model | Read booking identity, status, selected tour, customer link, companion link, price snapshot, and classified note fields approved for the selected document audience. Do not update booking fields through document editing. |
| Customer data | Admin customer model | Read only fields required by the selected document audience. Do not copy unnecessary personal data into drafts. |
| Tour data | Admin/Catalog-owned tour data exposed through existing contracts | Read public itinerary and operational fields through the owning boundary. Do not query another context's database directly. |
| Pricing and payment state | Admin pricing/payment model | Use booking price snapshots and coarse payment state. Do not include payment secrets, card data, or processor payloads. |
| Discount data | Admin pricing/discount model | Read discount labels or totals only when required for customer-facing explanation or finance traceability. Do not expose internal approval notes or rules. |
| Localization data | Localization resources or content owner | Render in the requested supported language with safe fallback rules. Track language and template version; do not auto-publish machine-translated legal or customer text without human review. |
| Branding data | Branding/public presentation owner | Apply approved logo, color, typography, contact, and legal footer values through the owning contract. Do not duplicate branding settings into source records. |
| Staff-entered document edits | Document draft | Store only approved document-specific text. Do not write edits back to booking, customer, tour, or payment records. |
| Template metadata | Document template record or deployed template version | Record template identifier and version used for draft and PDF generation. |

Document generation should record source identifiers and version signals, not duplicate whole source
records unless a finalized document needs an immutable customer-facing copy of specific text.

## Editable intermediate format

ADR-036 chooses a structured, versioned, editable document draft as the intermediate format. The PDF
is a generated output, not the editing surface or canonical document state.

The intermediate draft should contain:

- document type and audience
- template identifier and template version
- source record references and source version/hash signals
- ordered sections with stable field identifiers
- generated field values
- staff overrides for fields that are explicitly editable
- review state, reviewer identity, timestamps, and finalization state

Do not store the draft as arbitrary HTML, Markdown, DOCX, or a binary PDF. Rich text can be added
later only for fields that have a proven business need and a sanitizer/accessibility test plan.

## PDF generation requirements

PDF generation is a derived rendering step from a reviewed draft.

Minimum requirements:

- deterministic output for the same finalized draft, template version, and renderer version
- stable file name and metadata policy that avoids personal data in paths or object names
- selectable text instead of image-only pages
- embedded or declared document language
- document title built from document type and an opaque reference only, with no personal data
- semantic heading structure and reading order
- tagged tables or list structure where tables/lists are rendered
- link text that is meaningful outside visual context
- sufficient color contrast and no color-only status indicators
- page numbers and repeated headers/footers that do not confuse screen-reader order
- PDF metadata that records document type, template version, and generated timestamp without PII
- automated tests for generated HTML or renderer input, plus at least one snapshot or approval test
  for the PDF-generation boundary when a renderer is selected

The first slice currently emits a deterministic, semantic HTML artifact payload with an opaque `.html`
object name. The renderer is isolated from draft state so a PDF renderer can replace this payload without
changing review, finalization, retention, or revision behavior. No PDF library is bundled until a renderer
is selected and its accessibility boundary is tested. Additional templates should wait until the first path
proves storage, audit, review, and regeneration behavior.

## Manual review and finalization workflow

Documents move through explicit states:

1. `DraftGenerated`: source data and template produced an editable draft.
2. `InReview`: staff opened the draft for review.
3. `ChangesRequested`: staff identified source data or document edits that must be corrected.
4. `Approved`: staff confirmed the content is ready to render.
5. `Finalized`: an artifact was generated and sealed for distribution or operations.
6. `Superseded`: a newer finalized revision replaces this one for future use.
7. `Voided`: staff invalidated the finalized artifact because it must not be used.

Rules:

- Only authorized staff can generate, edit, approve, finalize, download, void, or regenerate.
- Authorization must evaluate the actor role, document audience, operation, and booking/document scope.
- Review must show the source-data timestamp/version used by the draft.
- Staff edits must be limited to fields declared editable for the document type.
- High-risk source data changes require updating the source record first, then regenerating.
- Finalization must be explicit; opening or saving a draft cannot finalize an artifact.
- Finalized artifacts are immutable. Corrections create a new revision.
- Download and view events are audited without logging document content.

## Storage, retention, audit, and regeneration

### Storage

Store document records separately from booking/customer/tour aggregates. A document record should
reference source records by opaque identifiers and store only the document-specific draft and
artifact metadata needed for review, finalization, retention, and audit.

PDF artifacts should use non-PII object names. Store display names separately and build them only for
authorized UI responses.

### Retention

Retention must follow the business retention policy for travel and accounting records. Until that
policy is explicit, use the smallest practical retention for drafts and a longer retention only for
finalized documents that have operational, contractual, legal, or accounting value.

Drafts that were never finalized should be purgeable after a short period. Finalized artifacts should
support legal hold and supervised deletion.

### Audit

Audit events should capture:

- operation name
- opaque or pseudonymous actor identifier
- document identifier and revision
- opaque source booking/tour identifiers when necessary for traceability
- timestamp
- outcome
- reason code for voiding, regenerating, or superseding
- correlation identifier

Audit events must not include document body, customer notes, personal data, payment details, binary
artifact content, rendered text excerpts, names, email addresses, phone numbers, or external booking
references.

### Regeneration

Regeneration compares the draft's recorded source signals with current source signals and classifies
the result:

- no source change: regenerate the same output only when renderer/template metadata changed
- safe source change: create a new draft revision and preserve compatible staff overrides
- conflicting source change: create a new draft revision and require staff review of conflicting
  fields
- template change: create a new draft revision and require staff review before finalization

## Privacy and security rules

- Do not put personal data in logs, telemetry tags, metric dimensions, URL paths, file paths, object
  names, queue names, exception messages, or audit payload bodies.
- Do not log request/response bodies for document generation, preview, finalization, or download.
- Classify all document fields by audience and privacy level before rendering.
- Enforce authorization on every draft, preview, finalization, artifact, and audit-read endpoint.
- Use mediated downloads or signed short-TTL URLs; never expose public storage URLs, and never log
  storage URLs.
- Encrypt persisted documents and artifacts using the platform's normal storage encryption.
- Do not send generated documents to third-party services unless a separate privacy and processor
  review approves that integration.
- Redact or omit sensitive personal data unless the document type has a documented operational need.
- Treat manual staff edits as potentially containing personal data.

See also [Privacy classification and redaction](privacy-classification.md).

## First vertical slice

Implement one customer-facing booking confirmation packet.

Acceptance target:

- staff can generate a draft from one booking
- draft captures template version and source signals
- staff can edit only approved document fields
- staff can approve and finalize
- finalization creates one deterministic accessible HTML artifact payload
- finalized artifact is immutable
- regeneration after a source change creates a new draft revision
- audit records are emitted without document content or PII
- logs and telemetry contain only operation names, outcomes, and opaque identifiers

Recommended first slice boundaries:

- Document type: customer-facing booking confirmation packet.
- Audience: customer.
- Source: one booking with linked customer, optional companion, tour summary, and payment state.
- Editable fields: customer-facing greeting, optional trip note, and support contact text.
- Non-editable fields: booking reference, tour dates, price summary, and payment state.
- Output: one finalized deterministic HTML artifact payload; PDF rendering follows renderer selection.

## Implementation backlog

The items below remain follow-ups for work beyond this first implementation slice.

### 1. Define document draft model and privacy classification

- Add a document draft model with document type, audience, template version, source references,
  section fields, editable flags, review state, and revision number.
- Classify every field as operational data, personal data, sensitive personal data, or secret.
- Add tests that prevent unclassified fields from being rendered.

Acceptance criteria:

- Drafts cannot contain unclassified fields.
- Editable and non-editable fields are explicit.
- Source identifiers are opaque and do not include personal data.

### 2. Generate booking confirmation draft

- Add a use case that builds a customer-facing booking confirmation draft from one booking.
- Record source version/hash signals and template version.
- Exclude payment secrets and unnecessary personal data.

Acceptance criteria:

- Generated draft includes required customer-facing fields.
- Draft generation fails safely when required source data is missing.
- Logs contain no personal data.

### 3. Add manual review and editable-field update path

- Add review-state transitions and field-edit validation.
- Permit edits only to the document type's approved editable fields.
- Require reviewer identity and timestamp for approval.

Acceptance criteria:

- Non-editable field updates are rejected.
- Approval cannot happen from an invalid state.
- Audit events contain operation metadata only.

### 4. Render accessible PDF artifact

- Select the smallest renderer that can meet accessibility and deterministic-output requirements.
- Render the finalized draft to PDF.
- Add tests for renderer input and at least one stable boundary check for the artifact path.

Acceptance criteria:

- Output PDF is selectable text, not image-only.
- Renderer metadata includes template and renderer version without PII.
- File/object names contain no personal data.

### 5. Store finalized artifact and enforce download authorization

- Persist artifact metadata and binary storage reference.
- Enforce role, audience, purpose, and booking/document-scope authorization for preview,
  finalization, and download.
- Use mediated downloads or signed short-TTL URLs instead of direct public storage URLs.

Acceptance criteria:

- Unauthorized users cannot access drafts or PDFs.
- Finalized artifact is immutable.
- Download/view events are audited without content.

### 6. Implement regeneration and superseding

- Compare stored source signals with current source signals.
- Create a new draft revision for source or template changes.
- Mark older finalized revisions as superseded only after a newer revision is finalized.

Acceptance criteria:

- Regeneration never mutates a finalized PDF.
- Conflicting source changes require manual review.
- Superseded and voided states remain auditable.

### 7. Add retention and purge controls

- Add retention metadata for drafts and finalized artifacts.
- Add safe purge path for expired unfinalized drafts.
- Add legal-hold support before deleting finalized artifacts.

Acceptance criteria:

- Expired drafts can be purged without removing source records.
- Finalized artifacts are not deleted while under legal hold.
- Purge audit events contain no document content.

## Open questions

- Exact legal/accounting retention period for finalized travel documents.
- Which staff roles may view internal operations packets versus customer-facing packets.
- Whether final PDFs require PDF/A conformance in addition to tagged accessibility.
- Whether document delivery will be download-only or also sent by email in a later epic.
