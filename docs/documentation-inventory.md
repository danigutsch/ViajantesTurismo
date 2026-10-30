# Documentation ownership and consolidation inventory

Owner: repository maintainers. Reviewed October 20, 2026.
Scope: maintained root, `docs/`, application READMEs, and test-suite guides. Generated output and
historical ADRs are classified separately. This is a curated disposition inventory; the
[documentation index](README.md) remains the canonical navigation map.

## Contents

- [Source-of-truth families](#source-of-truth-families)
- [Overlap and disposition](#overlap-and-disposition)
- [Reviewed follow-up boundaries](#reviewed-follow-up-boundaries)
- [Maintenance](#maintenance)

## Source-of-truth families

| Family | Canonical source | Disposition for secondary material |
| --- | --- | --- |
| Contributor setup and tools | [Root README](../README.md) and [Contributing](../CONTRIBUTING.md) | Keep quick commands in README; link workflow/recovery details to Contributing. Keep OS/devcontainer specifics in their scoped guides |
| Coding, validation, and exceptions | [Coding guidelines](CODING_GUIDELINES.md), [Domain validation](DOMAIN_VALIDATION.md), [Rich domain model](domain/RICH_DOMAIN_MODEL.md), [Exception guide](EXCEPTION_DOCUMENTATION.md) | Keep the coding guide's short rules and link detailed examples; keep domain encapsulation and behavior-placement rules in the rich domain model guide; avoid copying exception-contract policy into each project README |
| Test taxonomy and seams | [Tests index](../tests/README.md), [Test guidelines](TEST_GUIDELINES.md), [Fixture seam](../tests/TEST_FIXTURE_SEAM.md), [Test helper inventory](testing/TEST_HELPER_INVENTORY.md) | Keep suite READMEs for commands, ownership, and prerequisites. Link common fixture/parallelism rules instead of repeating them |
| API and async contracts | [Request shapes](API_REQUEST_SHAPES.md), [Client boundaries](API_CLIENT_BOUNDARIES.md), [Async contracts](ASYNC_CONTRACTS.md) | Keep OpenAPI/AsyncAPI as generated or maintained machine contracts; client READMEs explain generation rather than restating schema |
| Current architecture and migration | [Architecture index](architecture/README.md), [Flows](architecture/FLOWS.md), [Slice plan](vertical-slice-migration-plan.md) | Keep current implementation separate from proposed target; ADRs preserve decisions and supersession rather than duplicate runtime inventory |
| CI and dependencies | [CI index](ci/overview.md), [Quality gates](ci/quality-gates.md), [Contributing](../CONTRIBUTING.md#dependency-graph-and-lock-file-maintenance) | Keep CODE_QUALITY as tool invocation/reference; link CI severity, warning exceptions, and pin review to their canonical CI guides |
| Community and support | [Support](../SUPPORT.md), [Security disclosure](../SECURITY.md), [FOSS compliance](FOSS_COMPLIANCE.md#community-and-support-docs) | Keep public questions separate from private vulnerability reporting; community conduct remains explicitly deferred pending a maintained policy |
| Privacy and security | [Privacy classification](privacy-classification.md), [Threat model](security/threat-model.md), [Security baseline](security/security-baseline.md), [Error handling and OWASP alignment](security/error-handling-and-owasp.md) | Keep operational safeguards near workflows; do not invent a second legal or sensitive-field catalogue in feature docs |
| Generated travel documents | [Travel document specification](generated-travel-documents.md) and [ADR-036](adr/20260710-generated-travel-document-format-and-workflow.md) | Keep use cases/current-vs-target delivery in the specification; keep format rationale in the ADR and retention rules in the retention policy |
| Operations and retention | [Production readiness](operations/production-readiness.md), [Retention policy](operations/document-retention-and-legal-hold.md) | Keep policy status explicit; the policy is not legal advice. Link runbooks from the operations entrypoint |
| Generation and documentation governance | [Governance](DOCUMENTATION_GOVERNANCE.md), [Diagram inventory](architecture/generated-diagram-roadmap.md) | Keep source/generator/refresh ownership with outputs; manual narrative lives outside generated markers |
| Measurements and research | [Stream baselines](file-and-stream-benchmark-baselines.md), [Metrics proposal](architecture/coupling-and-cohesion-metrics.md), [Diagnostics proposal](ci/test-diagnostics.md) | Keep measured results separate from proposed collection. Do not repeat numeric results without version/input context |
| SharedKernel library guidance | Existing packaging and package READMEs | Keep as referenced library-owned material; this application documentation PR neither consolidates nor edits those sources |

## Overlap and disposition

| Inspected overlap | Recommendation | Action in this review |
| --- | --- | --- |
| README, Contributing, CODE_QUALITY, and CI workflow guide repeat lock refresh entrypoints | Keep short invocations; make Contributing authoritative for triggers, rebase recovery, and version/lock review | Align links with the existing dependency-graph command and the new pin-review path; do not duplicate recovery instructions |
| Root/docs/architecture indexes list related pages | Keep each index at its own depth; root routes by contributor task, docs owns the canonical map, architecture owns detailed diagrams | Add missing root task navigation and document-generation/research links at their owning level |
| ADR-036 and generated-travel-documents overlap format/workflow | Keep ADR decision and alternatives; keep operational requirements and implementation status in the feature specification | Link them explicitly and add status/evidence so future requirements cannot masquerade as shipped behavior |
| Generated diagram roadmap and governance both mention refresh policy | Keep the roadmap's per-output source inventory and governance's review rules | Link refresh commands directly from generated pages; avoid another generator or copied diagram |
| Existing Admin-only migration plan and current layer rules | Keep the replacement application plan merged previously; the project-merging proposal is superseded | Verify the new plan preserves Domain/Application/Infrastructure/API ownership; do not resurrect old project-combination instructions |
| Suite READMEs, test guidelines, and CI artifact instructions | Keep suite prerequisites and local commands; keep artifact/correlation design in the CI diagnostics guide | Add one canonical diagnostics link rather than another logging policy per test project |

No whole page is recommended for deletion solely because it shares a subject with an ADR or index.
The inspected overlaps have distinct audiences or historical value. Future stale duplicates should
be merged into the named owner with a redirect/link update after verifying inbound links. Do not
rewrite unrelated historical decisions or delete operational instructions during navigation cleanup.

## Reviewed follow-up boundaries

Review considered existing exception guidance, FOSS community/support documentation, file/stream
intake policy, generated document use-case planning, and manual review/finalization work before
suggesting anything new. Exception guidance and governance already have canonical homes. Support
routing lives in SUPPORT.md; SECURITY.md owns private disclosure, and FOSS compliance explicitly
defers a conduct policy. Preserve that decision instead of creating a conflicting conduct page. Stream
benchmark results are not a complete intake policy; leave that implementation-independent policy
work with its existing owner rather than treating navigation as completion.

Document format/use-case planning can complete with the linked specification and ADR. Staff
review/finalization implementation remains a separate feature: permissions, identity, change-request
reasons, stale-source checks, edit constraints, and transition tests must actually pass before that
work closes. Reorganizing documentation does not change the retention policy.
No duplicate follow-up issue is required by this inventory.

## Maintenance

When a maintained page changes substantially, check its canonical owner, implementation/plan status,
links, and generator boundaries. Review this inventory on the next documentation reorganization;
do not update its reviewed date without rechecking evidence. Run existing link, Markdown,
generated-drift, and conformance checks. Add no new inventory automation until a real consumer
needs machine-readable ownership beyond the current conformance facts.
