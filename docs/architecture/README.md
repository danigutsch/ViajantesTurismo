# Architecture Overview

This page links the current implementation shape to the longer-lived ADR and domain docs. Diagrams
show current repository structure unless a section is explicitly marked as planned.

## Current runtime resources

The Aspire AppHost is the source of truth for local runtime wiring.

```mermaid
flowchart LR
    postgres[(PostgreSQL)]
    redis[(Redis)]
    migration[MigrationService]
    adminApi[Admin.ApiService]
    catalogApi[Catalog.ApiService]
    management[Management.Web]
    publicWeb[Public.Web]

    migration --> postgres
    adminApi --> postgres
    adminApi --> migration
    catalogApi --> postgres
    catalogApi --> migration
    management --> redis
    management --> adminApi
    management --> catalogApi
    publicWeb --> catalogApi
```

Source: `src/ViajantesTurismo.AppHost/AppHost.cs`.

## Current project boundary map

```mermaid
flowchart TB
    adminDomain[Admin.Domain]
    adminApp[Admin.Application]
    adminInfra[Admin.Infrastructure]
    adminApi[Admin.ApiService]
    catalogDomain[Catalog.Domain]
    catalogApp[Catalog.Application]
    catalogInfra[Catalog.Infrastructure]
    catalogApi[Catalog.ApiService]
    management[Management.Web]
    publicWeb[Public.Web]
    shared[SharedKernel.*]

    adminApp --> adminDomain
    adminInfra --> adminApp
    adminApi --> adminApp
    adminApi --> adminInfra
    catalogApp --> catalogDomain
    catalogInfra --> catalogApp
    catalogApi --> catalogApp
    catalogApi --> catalogInfra
    management --> adminApi
    management --> catalogApi
    publicWeb --> catalogApi
    adminDomain --> shared
    adminApp --> shared
    catalogDomain --> shared
    catalogApp --> shared
```

Keep business rules in domain projects. Keep provider-specific persistence and external adapters in
bounded-context infrastructure unless ADR-027's split threshold justifies a reusable
`SharedKernel.<Capability>.<Provider>` adapter package.

## Current async content workflow

```mermaid
sequenceDiagram
    participant Admin as Admin context
    participant Dispatcher as Integration event dispatcher
    participant Catalog as Catalog consumer
    participant Store as PostgreSQL event store
    participant Projection as Catalog projection
    participant Public as Public.Web

    Admin->>Dispatcher: Published tour integration event
    Dispatcher->>Catalog: Handle integration event
    Catalog->>Store: Append Catalog event
    Catalog->>Projection: Process projection batch
    Public->>Projection: Read published tour presentation
```

See [Events and messaging](../domain/EVENTS_AND_MESSAGING.md) and Catalog ADRs in
[Architecture decisions](../ARCHITECTURE_DECISIONS.md#architecture--layers).

## Domain references

- [Admin bounded context](../bounded-contexts/Admin.md)
- [Catalog bounded context](../bounded-contexts/Catalog.md)
- [Domain aggregates](../domain/AGGREGATES.md)
- [Glossary](../domain/GLOSSARY.md)

## Planned or evolving areas

- Public website content variants and localization are tracked separately from core Admin CRUD.
- Media/gallery management is planned under public-web media issues.
- Adapter package splits should follow ADR-027's capability-first naming, dependency-direction, and
  split-threshold rules.
