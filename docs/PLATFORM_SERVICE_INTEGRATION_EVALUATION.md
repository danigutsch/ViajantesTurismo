# Platform Service Integration Evaluation

This document records the current evaluation posture for Epic #903.

The purpose is to group candidate platform services before any package, AppHost resource,
container, or service wiring is adopted. No candidate service is adopted by this document.

## Scope

Epic #903 covers evaluation posture for these candidates:

- YARP reverse proxy edge gateway, tracked by #897.
- Local AI with Ollama, tracked by #893.
- Mailing and Mailpit local/test capture, tracked by #891.
- NoSQL data store opportunities, tracked by #892.
- Feature flags with flagd or OpenFeature-style providers, tracked by #896.
- Cross-cutting Aspire integration strategy, tracked by #898.

## Non-goals

This pass is documentation-only and test-only.

- Do not add `PackageReference` or central package versions for candidate services.
- Do not add AppHost resources, containers, executables, or project wiring for candidates.
- Do not add Docker Compose, deployment manifests, launch profiles, or seed configuration.
- Do not add YARP, Ollama, Mailpit, NoSQL, flagd, or OpenFeature application code.
- Do not add generic integration abstractions, registries, factories, wrappers, or caches.

## Current repository baseline

The current local runtime model is `src/ViajantesTurismo.AppHost`.

The AppHost currently orchestrates:

- PostgreSQL and PgWeb.
- Redis and RedisInsight.
- MigrationService.
- Admin.ApiService and Catalog.ApiService.
- IntegrationEventWorker.
- Management.Web and Public.Web.
- Opt-in `admin-performance-smoke`.
- Opt-in Grafana LGTM observability stack.

Production runtime options remain centralized in `docs/CONFIGURATION.md`. The repository
currently has no active feature flags, and the first flag must be added only with the feature
that consumes it.

## Options reviewed

| Candidate | Tracking issue | Current posture | Adoption preconditions |
| --- | --- | --- | --- |
| YARP gateway | #897 | Evaluate and defer. | A concrete routing, security-header, rate-limit, composition, or deployment need exists. |
| Ollama local AI | #893 | Evaluate and defer. | A bounded AI use case exists with privacy rules, quality evals, and failure behavior. |
| Mailing and Mailpit | #891 | Evaluate and defer. | A concrete mail workflow exists and dev/test mail cannot reach real recipients by default. |
| NoSQL store | #892 | Assess and likely defer unless a fit is proven. | A current data flow benefits clearly over relational persistence. |
| flagd/OpenFeature | #896 | Evaluate against native configuration first. | Multiple services need coordinated dynamic flags, auditing, or operator control. |
| Aspire strategy | #898 | Document conventions before adoption. | A candidate service is accepted and needs local orchestration. |

## Decision

Do not adopt any candidate platform service as part of Epic #903.

Candidate integrations stay evaluation-only until a separate adoption issue identifies a current
use case, implementation owner, test plan, operational model, and rollback or deferral rationale.

Prefer official Aspire integrations when a later adoption issue proves a candidate is needed. If
no official integration fits, document why a project resource, container resource, or custom
resource is the smallest safe option.

## Why deferred

Deferral is intentional:

1. No candidate has a mandatory current application workflow in this epic.
2. Unused services add package, container, configuration, security, and operations cost.
3. AppHost should stay an orchestration map, not a speculative platform catalog.
4. Service-specific security, privacy, validation, testing, and operations work belongs with the
   candidate issue that adopts the service.

Security, privacy, validation, testing, and operations implications must be documented before implementation.

## Adoption checklist

Before a later issue adopts a candidate service, it must document:

- the current use case and explicit non-goals
- why native framework or existing infrastructure is insufficient
- data classification, secret handling, and safe logging rules
- validation rules and failure behavior
- local Aspire integration choice and fallback rationale
- health, startup order, observability, and operations ownership
- focused tests for enabled, disabled, unavailable, and failure states where applicable
- documentation updates in the owning feature docs and AppHost README

## Revisit conditions

Revisit a candidate only when one of these becomes true:

- a product or operational workflow requires the service now
- an existing implementation creates repeated integration pain that the service would remove
- a candidate issue completes its research and recommends a minimal adoption path
- an upstream Aspire integration changes enough to reduce risk and complexity for a current need

## References

- `docs/CONFIGURATION.md`
- `docs/architecture/runtime-wiring-and-deployment.md`
- `src/ViajantesTurismo.AppHost/README.md`
- `src/ViajantesTurismo.AppHost/AppHost.cs`
