# Documentation governance

This page defines the documentation model for maintained repository guidance. It keeps durable
documents navigable and reviewable without forcing every Markdown file into one template.

## Contents

- [Goals and non-goals](#goals-and-non-goals)
- [Document classification](#document-classification)
- [Composable schema blocks](#composable-schema-blocks)
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

## Document classification

Classify maintained documentation by its primary purpose. A document may compose more than one
block when that improves its reviewability.

| Class | Typical repository locations | Expected blocks | Explicit exemption or note |
| --- | --- | --- | --- |
| Standard or reference | `docs/CODING_GUIDELINES.md`, `docs/CONFIGURATION.md`, `docs/TEST_GUIDELINES.md` | Common metadata when durable ownership matters; link policy; optional inventory | TOC policy applies. |
| Decision record | `docs/adr/*.md` | Durable decision; links | Existing ADR conventions remain canonical. Short ADRs are TOC-exempt. |
| Operational workflow | `docs/operations/`, contributor workflows, runbooks | Workflow; link policy; optional common metadata | Include validation and rollback only when the workflow changes state. |
| Index or inventory | `docs/README.md`, architecture maps, generated-diagram roadmap | Inventory; link policy; optional common metadata | Landing pages are TOC-optional. Generated outputs retain their generator ownership. |
| Short how-to or release note | Focused README sections, changelog entries, concise guides | Link policy only when references need governance | Exempt from metadata and TOC requirements unless the document grows into a reference. |

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

## Composition and incremental adoption

New or substantively revised durable documents should select the smallest useful set of blocks.
For example, an ADR composes durable decision and link policy; a runbook composes workflow and
link policy; an architecture map composes inventory and link policy.

Existing documents adopt the model incrementally. The classification above records the current
path-level intent; authors do not need to add metadata or rewrite content until a document has a
real maintenance need. An explicit exemption remains valid while the document stays short,
generated, or narrowly scoped.

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

Documentation schema validation is deferred until a concrete machine-readable documentation source
has a real consumer. That future capability must use the existing .NET local-tool model, extending
`SharedKernel.RepoConfig.Tool` or `SharedKernel.Documentation.Tool` only when the boundary is clear.

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
