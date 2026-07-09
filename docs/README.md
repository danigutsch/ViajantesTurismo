# Documentation

Start here when looking for repository-wide guidance.

## Source-of-truth map

Use this map before adding or changing repository-wide guidance. Keep the canonical page focused, and link to it from
specialized docs instead of repeating the same policy.

| Topic | Canonical source | Related detail |
| --- | --- | --- |
| Setup and tooling | [README](../README.md#getting-started) | [Local tool security](local-tool-security.md), [Dev containers](DEVCONTAINERS.md) |
| Coding standards | [Coding guidelines](CODING_GUIDELINES.md) | [.editorconfig](../.editorconfig), [Code quality](CODE_QUALITY.md) |
| Testing | [Test guidelines](TEST_GUIDELINES.md) | [Tests README](../tests/README.md), [BDD guide](../tests/BDD_GUIDE.md) |
| Architecture and ADRs | [Architecture overview](architecture/README.md) and [Architecture decisions](ARCHITECTURE_DECISIONS.md) | [Bounded contexts](bounded-contexts/Admin.md), [Domain aggregates](domain/AGGREGATES.md) |
| Domain validation | [Domain validation](DOMAIN_VALIDATION.md) | [Domain aggregates](domain/AGGREGATES.md), [Glossary](domain/GLOSSARY.md) |
| API and client boundaries | [API client boundaries](API_CLIENT_BOUNDARIES.md) | [API compatibility](API_COMPATIBILITY.md), [API versioning](API_VERSIONING.md) |
| Async integration contracts | [Async contracts](ASYNC_CONTRACTS.md) | [AsyncAPI contract](asyncapi.yaml), [Events and messaging](domain/EVENTS_AND_MESSAGING.md) |
| Configuration and feature flags | [Configuration standards](CONFIGURATION.md) | [OpenTelemetry custom telemetry](OPEN_TELEMETRY.md) |
| CI, release, and contribution workflow | [Contributing](../CONTRIBUTING.md) | [CI overview](ci/overview.md), [CI governance](ci/governance.md), [Pull request template](pull_request_template.md) |
| Production operations | [Production readiness and operations](operations/production-readiness.md) | [Runtime wiring and deployment mapping](architecture/runtime-wiring-and-deployment.md) |

Deprecated docs: none identified.

Centralized guidance:

- Commit-message policy: [Contributing](../CONTRIBUTING.md#commit-messages).
- Test taxonomy: [Tests README](../tests/README.md).
- BDD/Gherkin guidance: [BDD guide](../tests/BDD_GUIDE.md).
- Generic related-documentation lists: this map.
- Scoped architecture/domain indexes own their detailed child links.

Mark more pages here only after proving they repeat a canonical source or point contributors to
outdated behavior.

## Architecture

- [Architecture overview](architecture/README.md) - system, runtime, project, and async-flow maps.
- [System architecture diagram](architecture/system-overview.md) - top-level system map with trust boundaries.
- [Diagram guidance](architecture/diagram-guidance.md) - diagram type selection and generation policy.
- [Architecture decisions](ARCHITECTURE_DECISIONS.md) - ADR index and decision history.
- [Domain validation](DOMAIN_VALIDATION.md) - validation patterns and links to domain-specific details.

## Engineering guidance

- [Coding guidelines](CODING_GUIDELINES.md)
- [Code quality](CODE_QUALITY.md)
- [Test guidelines](TEST_GUIDELINES.md)
- [Configuration standards](CONFIGURATION.md)
- [API versioning](API_VERSIONING.md)
- [Async contracts](ASYNC_CONTRACTS.md)
- [AsyncAPI contract](asyncapi.yaml)
- [OpenTelemetry custom telemetry](OPEN_TELEMETRY.md)
- [Mutation testing evaluation](MUTATION_TESTING.md)
- [Dev containers](DEVCONTAINERS.md)
- [Configurable model source generation](MODEL_SOURCE_GENERATION.md)

## Maintenance notes

- [Production readiness and operations](operations/production-readiness.md)
- [Analyzer hardening roadmap](ANALYZER_HARDENING_ROADMAP.md)
- [SharedKernel observability/runtime grouping](SHAREDKERNEL_OBSERVABILITY_RUNTIME_GROUPING.md)
- [Vertical slice migration plan](vertical-slice-migration-plan.md)
- [Local tool security](local-tool-security.md)
- [Line endings](LINE_ENDINGS.md)
