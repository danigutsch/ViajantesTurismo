# Documentation governance

This page defines the documentation model for maintained repository guidance. It keeps durable
documents navigable and reviewable without forcing every Markdown file into one template.

## Contents

- [Goals and non-goals](#goals-and-non-goals)
- [Required sections by document type](#required-sections-by-document-type)
- [Composable schema blocks](#composable-schema-blocks)
- [Authorship, generation, and provenance](#authorship-generation-and-provenance)
- [Composition and incremental adoption](#composition-and-incremental-adoption)
- [Table of contents policy](#table-of-contents-policy)
- [Validation and tooling](#validation-and-tooling)
- [Ownership and review](#ownership-and-review)
- [Related guidance](#related-guidance)

## Goals and non-goals

### Goals

- Define small, reusable documentation blocks that describe durable repository guidance.
- Keep metadata source-agnostic so documents do not depend on GitHub-specific fields or workflows.
- Support different document types through composition instead of a global Markdown template.
- Adopt the model incrementally as documents are created or substantively changed.
- Keep future validation on the repository's .NET local-tool path.

### Non-goals

- Do not require front matter, a table of contents, or every block for every Markdown file.
- Do not rewrite existing documentation solely to fit this model.
- Do not replace the established ADR, Markdown lint, link validation, or generated-diagram policies.
- Do not add a shell, Python, npm, or transient helper solely for documentation schema validation.

Machine-readable standards guarded by the documentation conformance check:

<!-- doc-fact:documentation-governance:start -->
- current-standard: `generated-manual-provenance-explicit`
- current-standard: `required-sections-by-document-type`
- current-standard: `small-focused-docs-exempt`
<!-- doc-fact:documentation-governance:end -->

## Required sections by document type

Classify maintained documentation by its primary purpose. A document may compose more than one
block when that improves its reviewability.

<!-- doc-content:required-sections-checklist:start -->

| Class | Typical repository locations | Required blocks | Explicit exemption or note |
| --- | --- | --- | --- |
| Standard or reference | `docs/CODING_GUIDELINES.md`, `docs/CONFIGURATION.md`, `docs/TEST_GUIDELINES.md` | Common metadata when durable ownership matters; link policy; optional inventory | TOC policy applies. |
| Decision record | `docs/adr/*.md` | Durable decision; links | Existing ADR conventions remain canonical. Short ADRs are TOC-exempt. |
| Operational workflow | `docs/operations/`, contributor workflows, runbooks | Workflow; link policy; optional common metadata | Include validation and rollback only when the workflow changes state. |
| Index or inventory | `docs/README.md`, architecture maps, generated-diagram roadmap | Inventory; link policy; optional common metadata | Landing pages are TOC-optional. Generated outputs retain their generator ownership. |
| Short how-to or release note | Focused README sections, changelog entries, concise guides | Link policy only when references need governance | Exempt from metadata and TOC requirements unless the document grows into a reference. |

<!-- doc-content:required-sections-checklist:end -->

## Composable schema blocks

These blocks are a documentation model, not a required literal syntax. A document can express a
block through headings, prose, front matter, or a future machine-readable companion file. The
fields intentionally avoid GitHub-specific identifiers.

### Common metadata

Use this block when a document needs durable ownership or review context:

- Title or stable document identity.
- Owner or maintaining group.
- Status, such as draft, active, deprecated, or superseded.
- Last-reviewed date when review cadence matters.
- Related durable records, such as an ADR, standard, or specification.

### Durable decision

Use for ADR-style records and other choices with lasting consequences:

- Context.
- Decision.
- Consequences.
- Alternatives when they materially explain the choice.
- Status and links to superseding or related records.

### Operational workflow

Use when contributors or operators need to perform a repeatable action:

- Purpose and applicability.
- Preconditions and authorized command path.
- Validation signals.
- Rollback or recovery when the workflow changes a durable state.

### Index or inventory

Use for navigational maps, owned lists, and generated-document inventories:

- Scope of the inventory.
- Listed items or links.
- Owner or update trigger.
- Whether entries are curated, generated, or deferred.

### Link policy

Use wherever references form part of the document's contract:

- Prefer repository-relative links for maintained repository content.
- Link to durable external records only when they are needed to understand or operate the document.
- Keep generated-document sources and generated outputs linked through their existing ownership path.
- Rely on the existing link validation path for local links and durable-reference policy.

## Authorship, generation, and provenance

State ownership where a reader could otherwise mistake generated content for manually maintained
guidance:

<!-- doc-content:provenance-expectations:start -->

- Manually maintained documents treat their tracked Markdown as the source and are edited with the
  code or workflow they describe.
- Generated documents identify the owning generator and source or configuration. Do not edit their
  generated blocks by hand; refresh them through the documented .NET tool command.
- Hybrid documents keep manual prose outside explicit generated markers. The generator owns only
  the marked blocks, while reviewers own the surrounding explanation.
- Curated inventories identify the repository source used to verify each claim, even when the tool
  cannot derive the claim automatically.
- Imported or adapted material records its durable source and any applicable attribution or license
  evidence. Unverifiable copied guidance is not accepted as repository provenance.

<!-- doc-content:provenance-expectations:end -->

## Composition and incremental adoption

New or substantively revised durable documents should select the smallest useful set of blocks.
For example, an ADR composes durable decision and link policy; a runbook composes workflow and
link policy; an architecture map composes inventory and link policy.

<!-- doc-content:small-doc-exemption:start -->

Existing documents adopt the model incrementally. The classification above records the current
path-level intent; authors do not need to add metadata or rewrite content until a document has a
real maintenance need. An explicit exemption remains valid while the document stays short,
generated, or narrowly scoped.

<!-- doc-content:small-doc-exemption:end -->

## Table of contents policy

### When a table of contents is needed

- Require a TOC for long standards and reference documents when navigation would otherwise depend
  on scanning headings.
- Recommend a TOC for documents with four or more top-level sections or a deep heading hierarchy.
- Keep a TOC optional for short how-tos, README landing pages, changelog entries, and short ADRs.

### Maintenance model

Prefer a generated or tool-verified TOC when an existing repository tool supports the document.
The current `SharedKernel.Documentation.Tool` owns generated architecture-document outputs; it does
not yet generate general Markdown TOCs. Do not add a second tool or script for this policy.

Until a supported generator exists, maintain a required or recommended TOC in the same change that
changes its headings. Existing Markdown and link validation remain the check path for formatting,
links, and anchors; a future .NET tool may validate the documentation model and TOC drift when a
concrete machine-readable source is justified.

### Initial candidates

The following high-value reference documents meet the TOC threshold and should gain or retain a
TOC when next substantively revised:

- `docs/CODE_QUALITY.md`
- `docs/CONFIGURATION.md`
- `docs/CODING_GUIDELINES.md`
- `docs/DOMAIN_VALIDATION.md`
- `docs/TEST_GUIDELINES.md`

## Validation and tooling

The current repository checks remain authoritative for their distinct responsibilities:

- `bash scripts/lint-all.sh` checks maintained documentation quality and generated-document drift.
- `scripts/lint-links.py` validates local links and durable external-reference policy through the
  repository wrapper.
- `SharedKernel.Documentation.Tool` owns its configured generated-document outputs.

`SharedKernel.Documentation.Tool check` validates the small set of machine-readable governance and
architecture facts that have concrete consumers. Keep broader schema enforcement deferred until a
new durable fact has a real consumer; extend the existing .NET tool rather than adding another
script or tool.

## Ownership and review

Document owners choose the smallest composition that preserves review context and navigation.
Reviewers should reject duplicated repository-wide guidance, unsupported generated claims, and
new validation tooling without a concrete source, consumer, and .NET local-tool boundary.

## Related guidance

- [Documentation map](README.md)
- [Architecture decision conventions](ARCHITECTURE_DECISIONS.md)
- [Generated diagram roadmap](architecture/generated-diagram-roadmap.md)
- [Code quality](CODE_QUALITY.md)
- [Local tool security](local-tool-security.md)
