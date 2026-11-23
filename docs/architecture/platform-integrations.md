# Aspire platform integration strategy

AppHost composes resources, configuration, health checks, and startup order. The consuming
application owns provider registration, defaults, and behavior. Deployment supplies production
endpoints and credentials and applies the approved network and data topology.

## Choosing an integration

1. Start with the official hosting and client integrations. Check their compatibility with the
   repository's pinned Aspire AppHost SDK and verify their configuration, lifecycle, and health
   support against the service's needs. Hosting and client packages can be selected independently.
   [Aspire integrations](https://aspire.dev/integrations/)
2. Consider a community or pinned SharedKernel integration when it supplies a maintained resource
   model and a compatible version. Inspect its image provenance, capabilities, and maintenance
   requirements. Keep the application's provider policy in ViajantesTurismo.
   [Aspire integration gallery](https://aspire.dev/integrations/gallery/)
3. Use the standard project, container, or external-connection resource for a simple dependency.
   Add a focused AppHost extension if a capability needs typed configuration or repeated lifecycle
   wiring. Share that extension through a pinned candidate when multiple consumers can use the
   same resource behavior independently of application names, policy, and deployment targets.
   [SharedKernel adoption workflow](../sharedkernel/development.md#adopt-a-library-change)

## Integration inventory

| Service | Available integration and evaluation path |
| --- | --- |
| YARP | Defer an application-owned edge until the deployment target supplies a routing requirement. [Edge routing decision](#edge-routing-decision) |
| Ollama | Evaluate `CommunityToolkit.Aspire.Hosting.Ollama` and its client integration in an opt-in local profile. Keep model selection, prompts, and data eligibility with the owning application. [Ollama integration](https://aspire.dev/integrations/ai/ollama/ollama-get-started/) |
| FusionCache and backing cache | Keep FusionCache configuration in the consuming runtime. The existing official Redis resource can provide a local distributed cache and backplane when the evaluation warrants them. [Redis integration](https://aspire.dev/integrations/caching/redis/redis-get-started/), [FusionCache setup](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/StepByStep.md) |
| NoSQL | Compare the current PostgreSQL model and its projections first. If a concrete flow needs a document store, evaluate the official MongoDB and Azure Cosmos DB integrations against that flow's consistency, operations, and privacy needs. [Aspire gallery](https://aspire.dev/integrations/gallery/) |
| Keycloak | Local development already uses a digest-pinned container with realm import and generated development credentials. Compare official `Aspire.Hosting.Keycloak` against those contracts and readiness behavior before switching. Production supplies its identity-provider configuration. [Aspire gallery](https://aspire.dev/integrations/gallery/) |
| Mailpit | Use `CommunityToolkit.Aspire.Hosting.MailPit` for local and fixture-owned SMTP capture if a mail workflow is approved. Test settings send only to the capture resource. [Aspire gallery](https://aspire.dev/integrations/gallery/) |
| flagd | Evaluate `CommunityToolkit.Aspire.Hosting.Flagd` for local evaluation and the OpenFeature .NET provider in the consuming application. The feature owner defines fallback values and evaluates flags at the use site. [Aspire gallery](https://aspire.dev/integrations/gallery/), [.NET flagd provider](https://flagd.dev/providers/dotnet/) |

## Feature flag decision

Keep ASP.NET Core configuration for the current deployment and rollout needs. Revisit flagd when deployed
services need the same change without a redeploy and operators need a controlled, audited way to make it.

| Use case | Configuration and fallback | When flagd would help |
| --- | --- | --- |
| Planned maintenance | The deployment owner would control a validated environment setting. Use normal service mode as the default when the switch is introduced, with the deployment traffic and readiness model governing service recovery. | Consider centralized evaluation only if a later topology requires coordinated changes and defines safe behavior during a provider outage. |
| Preview a new optional Management workflow | The feature owner adds one `FeatureFlags:Enable<Workflow>` option at the owning edge. A missing setting or unavailable provider keeps the current workflow in use. | Target cohorts only when the rollout has a named audience, metric, and removal date. |

Authorization, audit, retention, and data-integrity rules stay in their owning code. Operators follow
the [configuration policy](../CONFIGURATION.md#feature-flag-governance) for ownership, the initial value, and the
retirement trigger. The introducing PR documents the value and affected service, verifies both paths,
and records how to roll back. Operators change the value through the approved environment configuration,
check the resulting service behavior, and remove the setting and old path after the rollout ends.

If a later feature qualifies, `CommunityToolkit.Aspire.Hosting.Flagd` supplies a local AppHost resource,
health checks, and client connection properties. The .NET consumer owns key mapping, timeout, and a
safe explicit default. An environment that cannot run the Aspire resource can point the same provider
at its approved external endpoint. The owner must check TLS, health, change history, and outage behavior
before using that endpoint. [Aspire flagd setup](https://aspire.dev/integrations/devtools/flagd/flagd-get-started/),
[OpenFeature evaluation behavior](https://openfeature.dev/specification/sections/flag-evaluation/)

A disposable .NET 11 probe exercised flagd `0.19.0`, OpenFeature `2.14.1`, and
`OpenFeature.Providers.Flagd` `0.8.0`. The server returned `true` and `false` for configured variants,
and the client returned its `false` default for both a missing key and an unreachable provider. Its
initial restore exposed a moderate `System.Formats.Cbor` advisory. The probe restored and built after
pinning `8.0.2`, the patched version in that line. [Advisory](https://github.com/advisories/GHSA-vmv3-wvjp-wqpj)

## Composition and validation

- Use resource references to supply discovery and connection configuration, and keep service
  addresses and credentials in AppHost parameters or the deployment environment. Store secret
  parameters through the existing secret mechanisms. Preserve digest-pinned local images.
  [Configuration standards](../CONFIGURATION.md)
- Register liveness and readiness checks in the owning resource. Use `WaitFor` for a dependency's
  readiness and `WaitForCompletion` for jobs such as migration initialization. Keep backend and
  infrastructure endpoints internal. Put development companions behind explicit local profiles.
  [AppHost resources](../../src/ViajantesTurismo.AppHost/README.md)
- Document each adopted resource's profiles, local startup, configuration, persistent volumes, and
  health checks in the AppHost guide. Smoke-test the affected profile through the pinned Aspire CLI.
  Exercise the supported fixture profile and check the published model for development-only
  resources and credentials before enabling production wiring.
  [AppHost CI smoke evidence](https://github.com/danigutsch/ViajantesTurismo/actions/runs/45019870472/job/137200452961)

## Edge routing decision

Keep AppHost's direct frontend endpoints for local development. The production target will determine
whether platform ingress can fulfill the same routing requirements. Defer an application-owned YARP
gateway until a deployment needs a route policy, release strategy, or destination change that platform
ingress cannot supply. [Deployment and promotion model](../operations/production-readiness.md#deployment-and-promotion-model)

| Traffic | Destination and boundary |
| --- | --- |
| Public site, tours, galleries, sitemap, and rendition requests | Public Web on its own host name. Public Web owns visitor admission and service-to-service propagation to Catalog. |
| Staff pages, sign-in and sign-out, and private previews | Management Web on its own host name. Keep the OIDC callback origin and cookie boundary with that service. |
| Admin, Catalog, and Branding API calls | The private endpoints used by the intended frontend and worker clients. Keep the existing credentials, service discovery, API permissions, and request limits. |
| Liveness and readiness probes | The owning resource's `/alive` and `/health` endpoints. Supply the selected ingress or orchestrator with only the required health reachability. |

Per-service auth, CORS, security headers, and limits stay with their current owners. Any new edge
must retain the trusted-hop and forwarding depth policy for each consumer. Keep visit quotas and
deployment admission separate so an added proxy cannot change visitor isolation. Gateway business
logic and consolidation of the two frontend host names are outside this decision.
[Forwarding boundaries](../operations/public-visitor-deployment-validation.md#forwarding-boundaries)

If the selected deployment needs an application-owned edge, first trial the matching official
`Aspire.Hosting.Yarp` integration with AppHost references and health checks. An ASP.NET Core project
resource using `Yarp.ReverseProxy` is the supported alternative for application-owned middleware.
External ingress destinations are deployment configuration. A trial must cover forwarding from each
frontend hostname, OIDC redirects, private API reachability, failed downstream health, and visitor
isolation before enabling any new route. [Aspire YARP integration](https://learn.microsoft.com/en-us/dotnet/aspire/proxies/yarp-integration)

## Data-store evaluation

Keep PostgreSQL as the authoritative database for the current application workflows. The Catalog
read model is the closest document-store candidate. It already projects its append-only events into
queryable public records and can rebuild those records from the existing store. The current workload
has no measured query or write bottleneck that warrants moving the projection to another database.
[Data ownership and trust boundaries](data-ownership.md) ·
[Catalog event-sourcing decision](../adr/20260621-event-source-catalog-tour-presentation.md)

| Data flow | Candidate and decision |
| --- | --- |
| Admin booking, customer and payment writes | Document aggregates could group some fields, but the application also needs relational constraints, concurrent decisions and transactional outbox writes. Keep the current schema and EF transaction boundaries. |
| Catalog events and public reads | Documents could hold a localized read model under one key. Keep PostgreSQL events and projections while query tuning, indexing and rebuilds meet the workload. Reconsider a separate read store after a measured bottleneck and a demonstrated replay and freshness budget. |
| Branding, media and generated documents | Keep authoritative metadata and lifecycle checks in their owning relational stores. The existing object store holds media and document bodies. A third copy would add retention, recovery and reconciliation work. |
| Messaging and Management security | Keep inbox, outbox and processing state in the established transactional boundaries. Keep ticket and data-protection state in the restricted Management security store. Redis continues to serve cache use cases. |

MongoDB provides single-document atomic writes and supports distributed transactions when a workflow
needs several documents. Cosmos DB transactional batches tie atomicity to one logical partition.
Either option would require a new data model, a replay or migration procedure, tested recovery and
provider-specific consistency checks. A future projection trial should use synthetic public data
and an official Aspire integration. Before enabling production traffic, prove access and retention
controls, backup and restore, and rebuild or cutover from the authoritative store.
[MongoDB transactions](https://www.mongodb.com/docs/manual/core/transactions/) ·
[Cosmos DB transactional batches](https://learn.microsoft.com/en-us/azure/cosmos-db/transactional-batch) ·
[Governed PostgreSQL migrations](../adr/20261118-governed-postgresql-migrations.md) ·
[Privacy classification](../privacy-classification.md)

## Application email evaluation

Defer application email until a workflow owner approves a message, its trigger and its recipients.
Customer email currently serves as contact data and supports validation and import matching.
Management identity is supplied by the OIDC provider. Operational alerts use the observability
contact-point policy. Booking confirmations and document delivery would need a separate product
decision on recipients, content and delivery semantics before adding an application mail boundary.
[Admin bounded context](../bounded-contexts/Admin.md) ·
[OIDC and BFF authorization](../adr/20260711-provider-neutral-oidc-bff-authorization.md) ·
[OpenTelemetry operations](../OPEN_TELEMETRY.md)

For the first approved mail workflow, the owning context should define the template and allowed
recipient sources, with an explicit dispatch trigger and delivery status. Use Mailpit in the local
and fixture profiles as a capture-only endpoint. Override SMTP settings in those profiles, keep
relay disabled, and assert recipient, subject and expected body through the capture API using
synthetic test data. Keep addresses, message bodies and attachments out of logs. Production
provider selection and credentials belong to deployment configuration.
[Aspire integration gallery](https://aspire.dev/integrations/gallery/) ·
[Mailpit configuration](https://mailpit.axllent.org/docs/configuration/)
