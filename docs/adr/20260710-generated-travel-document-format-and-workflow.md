# ADR-036: Generated Travel Document Format and Workflow

**Status**: Accepted — 2026-07-10

## Context

ViajantesTurismo needs generated travel documents for customer-facing and internal staff workflows.
The documents combine booking, customer, tour, pricing, payment-state, and operational data. That data
can contain personal data and sometimes sensitive personal data, so generation must preserve source
ownership, authorization, audit, retention, and privacy boundaries.

Staff also need to review and correct customer-facing text before a document becomes final. The final
customer artifact should be an accessible PDF, but a PDF is not a safe editing or canonical state
format.

The first implementation should be a small vertical slice. It should not choose a broad rendering
framework, workflow engine, or storage abstraction before one document path proves the behavior.

## Decision

Use a structured, versioned document draft as the editable intermediate format. Generate PDFs only
from approved drafts. Treat finalized PDFs as immutable derived artifacts.

### Document source boundaries

- Booking, customer, tour, pricing, and payment data remain owned by their existing sources.
- Document generation reads source data through the owning application or contract boundary.
- Document drafts store source references and version/hash signals instead of becoming a source of
  truth for booking, customer, tour, pricing, or payment records.
- Staff document edits apply only to document-specific fields. They do not update source records.
- Source data corrections happen in the owning source first, then document regeneration creates a new
  draft or revision.

### Editable intermediate format

The intermediate draft stores:

- document type and audience
- template identifier and template version
- source references and source version/hash signals
- ordered sections with stable field identifiers
- generated values and approved staff overrides
- field editability and privacy classification
- review state, reviewer metadata, timestamps, and revision number

The intermediate draft is not arbitrary HTML, Markdown, DOCX, or binary PDF. A future rich-text field
may be added only when a specific document type needs it and sanitizer plus accessibility tests exist.

### Workflow

Document revisions use explicit states:

1. `DraftGenerated`
2. `InReview`
3. `ChangesRequested`
4. `Approved`
5. `Finalized`
6. `Superseded`
7. `Voided`

Finalization is explicit and requires an approved draft. A finalized PDF cannot be edited in place.
Corrections or source/template changes create a new draft revision. Older finalized revisions remain
available for audit unless retention, legal hold, or voiding policy says otherwise.

### PDF generation and accessibility

The PDF renderer is selected later by the first implementation slice. The chosen renderer must support
accessible, selectable-text PDFs and deterministic output for the same finalized draft, template
version, and renderer version.

Generated PDFs must support semantic headings, reading order, document language, title metadata based
only on document type and an opaque reference, meaningful link text, adequate contrast, and
non-color-only status communication. PDF metadata, object names, and file paths must not contain
personal data.

### Storage, audit, and retention

Document records and artifact metadata belong outside booking/customer/tour aggregates. Artifacts use
non-PII object names and mediated or signed short-TTL access paths.

Audit events record operation metadata: opaque or pseudonymous actor identifier, document identifier,
revision, operation, outcome, timestamp, reason code when needed, and correlation identifier. Audit
events do not include document body, rendered excerpts, customer notes, payment details, binary
content, names, email addresses, phone numbers, external booking references, or other personal data.

Draft retention should be short. Finalized artifact retention follows the business legal/accounting
policy and must support legal hold before deletion.

### Privacy and security

- No personal data in logs, telemetry tags, metric dimensions, URL paths, object names, exception
  messages, or audit payload bodies.
- No request/response body logging for generation, preview, finalization, or download.
- Authorization is required for draft generation, editing, preview, approval, finalization,
  regeneration, download, voiding, and audit reads. Authorization evaluates actor role, document
  audience, operation, and booking/document scope.
- Manual staff edits are treated as personal data unless classified otherwise.
- Generated documents are not sent to third-party services without a separate privacy and processor
  review.

## Consequences

### Positive

- Staff can review and edit safe document fields before finalization.
- PDFs remain stable customer artifacts instead of mutable source state.
- Source ownership stays with existing booking, customer, tour, pricing, and payment boundaries.
- Regeneration can compare recorded source signals with current source data.
- Audit and telemetry can prove actions without leaking document contents.
- The first vertical slice can validate renderer, storage, review, and privacy behavior before more
  document types are added.

### Negative

- A draft schema and migration path are required before the first PDF renderer is useful.
- Manual overrides need conflict handling when source data or templates change.
- Accessibility requirements constrain renderer choice and may require more testing than a simple
  print-to-PDF path.
- Finalized immutable artifacts require explicit supersede/void workflows for corrections.

## Alternatives Considered

### Edit the PDF directly

Rejected. PDF editing makes validation, privacy classification, review-state enforcement, and
regeneration conflict detection harder.

### Store arbitrary HTML or Markdown as the canonical draft

Rejected for the first slice. Arbitrary markup increases sanitizer, accessibility, styling, and data
leakage risks before there is a proven need for rich free-form content.

### Generate final PDFs directly from source data with no review draft

Rejected. Staff need a manual review/finalization step, and finalized documents must show which source
signals and template version were reviewed.

### Adopt a workflow engine or rendering framework now

Rejected. The current need is one vertical slice. Choose the smallest renderer and state transitions
that satisfy tests and accessibility requirements; revisit broader tooling after at least two document
types prove shared needs.

## Links

- [ADR Index](../ARCHITECTURE_DECISIONS.md)
- [Generated travel documents](../generated-travel-documents.md)
- [Privacy classification and redaction](../privacy-classification.md)
