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
| YARP | Evaluate official `Aspire.Hosting.Yarp` for the edge. Compare it with an ASP.NET Core project resource when the edge needs application-owned middleware or routes. [Aspire gallery](https://aspire.dev/integrations/gallery/) |
| Ollama | Evaluate `CommunityToolkit.Aspire.Hosting.Ollama` and its client integration in an opt-in local profile. Keep model selection, prompts, and data eligibility with the owning application. [Ollama integration](https://aspire.dev/integrations/ai/ollama/ollama-get-started/) |
| FusionCache and backing cache | Keep FusionCache configuration in the consuming runtime. The existing official Redis resource can provide a local distributed cache and backplane when the evaluation warrants them. [Redis integration](https://aspire.dev/integrations/caching/redis/redis-get-started/), [FusionCache setup](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/StepByStep.md) |
| NoSQL | Compare the current PostgreSQL model and its projections first. If a concrete flow needs a document store, evaluate the official MongoDB and Azure Cosmos DB integrations against that flow's consistency, operations, and privacy needs. [Aspire gallery](https://aspire.dev/integrations/gallery/) |
| Keycloak | Local development already uses a digest-pinned container with realm import and generated development credentials. Compare official `Aspire.Hosting.Keycloak` against those contracts and readiness behavior before switching. Production supplies its identity-provider configuration. [Aspire gallery](https://aspire.dev/integrations/gallery/) |
| Mailpit | Use `CommunityToolkit.Aspire.Hosting.MailPit` for local and fixture-owned SMTP capture if a mail workflow is approved. Test settings send only to the capture resource. [Aspire gallery](https://aspire.dev/integrations/gallery/) |
| flagd | Evaluate `CommunityToolkit.Aspire.Hosting.Flagd` for local evaluation and the OpenFeature .NET provider in the consuming application. The feature owner defines fallback values and evaluates flags at the use site. [Aspire gallery](https://aspire.dev/integrations/gallery/), [.NET flagd provider](https://flagd.dev/providers/dotnet/) |

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
