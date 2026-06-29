# Architecture boundaries and dependency flow

This page documents the current source-controlled dependency shape. Planned changes are listed
separately so diagrams can stay useful as repository structure evolves.

## Current bounded-context ownership

Admin owns booking and management workflows. Catalog owns public tour presentation. Admin publishes
integration events; Catalog consumes them and builds public read models.

```mermaid
flowchart LR
    admin[Admin bounded context]
    adminContracts[Admin.Contracts]
    integration[Integration events]
    catalog[Catalog bounded context]
    catalogContracts[Catalog.Contracts]
    publicRead[Public tour presentation]

    admin --> adminContracts
    admin --> integration
    integration --> catalog
    catalog --> catalogContracts
    catalog --> publicRead
```

Boundary rules:

- Admin business rules stay in `src/ViajantesTurismo.Admin.Domain`.
- Catalog presentation rules stay in `src/ViajantesTurismo.Catalog.Domain`.
- Cross-context interaction uses contracts and integration events, not direct domain references.
- Public web reads Catalog contracts; it does not reference Admin projects.

Related docs: [Admin bounded context](../bounded-contexts/Admin.md),
[Catalog bounded context](../bounded-contexts/Catalog.md), and
[events and messaging](../domain/EVENTS_AND_MESSAGING.md).

## Current project dependency map

```mermaid
flowchart TB
    subgraph Admin
        adminDomain[Admin.Domain]
        adminApp[Admin.Application]
        adminInfra[Admin.Infrastructure]
        adminApi[Admin.ApiService]
        adminContracts[Admin.Contracts]
    end

    subgraph Catalog
        catalogDomain[Catalog.Domain]
        catalogApp[Catalog.Application]
        catalogInfra[Catalog.Infrastructure]
        catalogApi[Catalog.ApiService]
        catalogContracts[Catalog.Contracts]
    end

    subgraph Hosts
        appHost[AppHost]
        migration[MigrationService]
        management[Management.Web]
        publicWeb[Public.Web]
    end

    subgraph Shared
        shared[SharedKernel.*]
        common[ViajantesTurismo.Common]
        resources[ViajantesTurismo.Resources]
        defaults[ServiceDefaults]
    end

    adminApi --> adminApp
    adminApi --> adminInfra
    adminApi --> adminContracts
    adminApp --> adminDomain
    adminInfra --> adminApp
    adminInfra --> adminDomain
    adminDomain --> adminContracts

    catalogApi --> catalogApp
    catalogApi --> catalogInfra
    catalogApi --> catalogContracts
    catalogApp --> catalogDomain
    catalogApp --> adminContracts
    catalogInfra --> catalogApp
    catalogInfra --> catalogDomain
    catalogDomain --> catalogContracts

    migration --> adminInfra
    migration --> catalogInfra
    management --> adminContracts
    management --> catalogContracts
    publicWeb --> catalogContracts
    appHost --> adminApi
    appHost --> catalogApi
    appHost --> migration
    appHost --> management
    appHost --> publicWeb

    adminDomain --> shared
    adminApp --> shared
    adminApi --> shared
    catalogDomain --> shared
    catalogApp --> shared
    catalogApi --> shared
    common --> shared
    defaults --> shared
    defaults --> resources
```

## Allowed and forbidden dependency directions

```mermaid
flowchart LR
    ui[Web frontends]
    api[API services]
    infra[Infrastructure]
    app[Application]
    domain[Domain]
    contracts[Contracts]
    shared[SharedKernel and Common]

    ui --> contracts
    api --> app
    api --> infra
    api --> contracts
    infra --> app
    infra --> domain
    app --> domain
    domain --> contracts
    domain --> shared
    app --> shared
    contracts --> shared
```

Forbidden directions:

- Domain must not reference API, infrastructure, web, AppHost, or migration projects.
- Application must not depend on API, web, AppHost, or migration projects.
- Infrastructure must not depend on API or web projects.
- Web frontends must not reference domain or infrastructure projects directly.
- Catalog must not call Admin domain or infrastructure code directly.
- SharedKernel modules must not depend on bounded-context projects.

## Current SharedKernel module map

```mermaid
flowchart TB
    results[SharedKernel.Results]
    resultsAspNet[SharedKernel.Results.AspNet]
    mediatorAbs[SharedKernel.Mediator.Abstractions]
    mediator[SharedKernel.Mediator]
    integration[SharedKernel.IntegrationEvents]
    cloudEvents[SharedKernel.IntegrationEvents.CloudEvents]
    domain[SharedKernel.Domain]
    domainEvents[SharedKernel.DomainEvents]
    eventSourcing[SharedKernel.EventSourcing]
    eventSourcingPg[SharedKernel.EventSourcing.PostgreSQL]
    building[SharedKernel.BuildingBlocks]
    idempotency[SharedKernel.Idempotency]
    observability[SharedKernel.Observability]
    openApi[SharedKernel.OpenApi]

    resultsAspNet --> results
    mediator --> mediatorAbs
    integration --> mediatorAbs
    cloudEvents --> integration
    domainEvents --> domain
    domainEvents --> mediatorAbs
    eventSourcingPg --> eventSourcing
    building --> results
```

Module rules:

- Put reusable domain primitives in `SharedKernel.Domain` or `SharedKernel.BuildingBlocks`.
- Put reusable contracts for dispatching in mediator or integration-event modules.
- Put provider-specific persistence in provider modules such as
  `SharedKernel.EventSourcing.PostgreSQL`.
- Keep analyzers, source generators, and code fixes in their current tool-specific modules.

Related decisions:

- [split SharedKernel domain and building blocks](../adr/20260621-split-sharedkernel-domain-and-building-blocks.md)
- provider-specific SharedKernel infrastructure modules in
  [Architecture decisions](../ARCHITECTURE_DECISIONS.md#architecture--layers)
- domain materialization and persistence boundaries in
  [Architecture decisions](../ARCHITECTURE_DECISIONS.md#architecture--layers)

## Planned improvements

- Add automated dependency-direction checks only after the stable rules above are accepted.
- Split new SharedKernel provider packages only when at least two real callers need the capability.
- Keep future diagrams in Mermaid Markdown unless a different source-controlled diagram format is
  already required by the owning docs.
