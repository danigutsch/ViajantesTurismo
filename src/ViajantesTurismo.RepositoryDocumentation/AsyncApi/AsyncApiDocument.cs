using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

using PandaBear.Documentation;

using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.RepositoryDocumentation.AsyncApi;

/// <summary>Generates the AsyncAPI description of the application's durable integration events.</summary>
public static class AsyncApiDocument
{
    private const string IntegrationEventTypeSuffix = "IntegrationEvent";
    private const string GeneratorName = "ViajantesTurismo.AsyncApi.Tool";
    private const string ToolDirectory = $"scripts/{GeneratorName}";
    private const string SourcePath = "src/ViajantesTurismo.RepositoryDocumentation/AsyncApi/AsyncApiDocument.cs";
    private const string CanonicalArtifactPath = "docs/asyncapi.json";

    private static readonly AsyncApiEvent[] Events =
    [
        AsyncApiEvent.Describe(
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent,
            title: "Admin tour created",
            producer: "Admin",
            consumer: "Catalog",
            consumerName: IntegrationEventConsumerNames.Catalog,
            outbox: "AdminWriteDbContext",
            inbox: "CatalogDbContext",
            handler: "ViajantesTurismo.Catalog.Application.Tours.AdminTourCreatedIntegrationHandler",
            transportConsumer: "CatalogIntegrationTransportDbContext",
            domainEventMapping: "ViajantesTurismo.Admin.Application.Tours.TourIntegrationEventMappings.MapTourCreated"),
        AsyncApiEvent.Describe(
            AdminIntegrationEventJsonContext.Default.AdminTourRetiredIntegrationEvent,
            title: "Admin tour retired",
            producer: "Admin",
            consumer: "Catalog",
            consumerName: IntegrationEventConsumerNames.Catalog,
            outbox: "AdminWriteDbContext",
            inbox: "CatalogDbContext",
            handler: "ViajantesTurismo.Catalog.Application.Tours.AdminTourRetiredIntegrationHandler",
            transportConsumer: "CatalogIntegrationTransportDbContext",
            domainEventMapping: "ViajantesTurismo.Admin.Application.Tours.TourIntegrationEventMappings.MapTourRetired"),
        AsyncApiEvent.Describe(
            BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent,
            title: "Branding settings changed",
            producer: "Branding",
            consumer: "Admin",
            consumerName: IntegrationEventConsumerNames.Admin,
            outbox: "BrandingDbContext",
            inbox: "AdminWriteDbContext",
            handler: "ViajantesTurismo.Admin.IntegrationEventWorker.BrandingSettingsChangedIntegrationHandler",
            transportConsumer: "AdminBrandingTransportDbContext"),
        AsyncApiEvent.Describe(
            CatalogIntegrationEventJsonContext.Default.MediaImageOriginalStoredIntegrationEvent,
            title: "Catalog media image original stored",
            producer: "Catalog",
            consumer: "Catalog",
            consumerName: IntegrationEventConsumerNames.Catalog,
            outbox: "CatalogDbContext",
            inbox: "CatalogDbContext",
            handler: "ViajantesTurismo.Catalog.Application.Media.MediaImageOriginalStoredIntegrationHandler"),
        AsyncApiEvent.Describe(
            CatalogIntegrationEventJsonContext.Default.CatalogTourRetirementAppliedIntegrationEvent,
            title: "Catalog tour retirement applied",
            producer: "Catalog",
            consumer: "Admin",
            consumerName: IntegrationEventConsumerNames.Admin,
            outbox: "CatalogDbContext",
            inbox: "AdminWriteDbContext",
            handler: "ViajantesTurismo.Admin.IntegrationEventWorker.CatalogTourRetirementAppliedIntegrationHandler",
            transportConsumer: "AdminBrandingTransportDbContext"),
    ];

    private static readonly JsonSchemaExporterOptions PayloadOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = FormatPayloadProperty,
    };

    /// <summary>Creates deterministic, indented AsyncAPI JSON with a trailing newline.</summary>
    /// <returns>The JSON document as text.</returns>
    public static string Render() => PandaBear.Documentation.AsyncApiDocument.Render(
        "ViajantesTurismo integration events",
        "1.0.0",
        "Generated from the runtime JSON contracts and durable integration-event topology.",
        Events.Select(Message),
        PayloadOptions,
        new JsonObject
        {
            ["x-viajantes"] = new JsonObject
            {
                ["scope"] = "Current durable integration events only.",
                ["versioning"] = "Event type strings are stable names; eventVersion increments on breaking payload changes.",
                ["provenance"] = new JsonObject
                {
                    ["generator"] = GeneratorName,
                    ["source"] = SourcePath,
                    ["refreshCommand"] = $"dotnet run --project {ToolDirectory} -- generate {CanonicalArtifactPath}",
                    ["verifyCommand"] = $"dotnet run --project {ToolDirectory} -- verify {CanonicalArtifactPath}",
                },
            },
        });

    private static AsyncApiMessage Message(AsyncApiEvent contract)
    {
        var typeName = contract.Contract.Type.Name;
        var version = contract.EventVersion.ToString(CultureInfo.InvariantCulture);
        var messageId = $"{typeName[..^IntegrationEventTypeSuffix.Length]}V{version}";
        var route = contract.TransportConsumer is null ? "its in-process outbox relay" : $"the {contract.ConsumerName} consumer stream";
        var topology = new JsonObject
        {
            ["producerContext"] = contract.Producer,
            ["consumerContexts"] = new JsonArray(contract.Consumer),
            ["consumerNames"] = new JsonArray(contract.ConsumerName),
            ["channel"] = contract.EventType,
            ["sourceType"] = contract.Contract.Type.FullName,
            ["outboxOwner"] = contract.Outbox,
            ["inboxOwner"] = contract.Inbox,
            ["handler"] = contract.Handler,
        };
        if (contract.DomainEventMapping is not null)
        {
            topology.Add("domainEventMapping", contract.DomainEventMapping);
        }

        if (contract.TransportConsumer is not null)
        {
            topology.Add("transportConsumer", contract.TransportConsumer);
        }

        return new AsyncApiMessage(
            contract.EventType, messageId, $"{typeName}V{version}", contract.EventType,
            $"{contract.Title} v{version}", contract.Contract)
        {
            Send = new AsyncApiOperation($"publish{messageId}", $"{contract.Producer} publishes {contract.EventType} for {contract.Consumer}."),
            Receive = new AsyncApiOperation($"consume{messageId}", $"{contract.Consumer} consumes {contract.EventType} from {route}."),
            Headers = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = new JsonArray("eventType", "eventVersion"),
                ["properties"] = new JsonObject
                {
                    ["eventType"] = new JsonObject { ["const"] = contract.EventType },
                    ["eventVersion"] = new JsonObject { ["const"] = contract.EventVersion },
                },
            },
            Extensions = new JsonObject { ["x-viajantes"] = topology },
        };
    }

    private static JsonNode FormatPayloadProperty(JsonSchemaExporterContext context, JsonNode schema)
    {
        if (schema is not JsonObject property)
        {
            return schema;
        }

        if (context.TypeInfo.Type == typeof(int))
        {
            property["format"] = "int32";
        }
        else if (context.TypeInfo.Type == typeof(long))
        {
            property["format"] = "int64";
        }
        else if (context.TypeInfo.Type == typeof(Uri))
        {
            // Branding emits app-relative logo addresses and the Uri converter preserves them.
            property["format"] = "uri-reference";
        }

        if (context.Path.IsEmpty)
        {
            property["additionalProperties"] = false;
        }

        return schema;
    }
}
