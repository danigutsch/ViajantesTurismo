using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AsyncApi.Tool;

/// <summary>Generates the AsyncAPI description of the application's durable integration events.</summary>
internal static class AsyncApiDocument
{
    private const string JsonContentType = "application/json";
    private const string IntegrationEventTypeSuffix = "IntegrationEvent";
    private const string GeneratorName = "ViajantesTurismo.AsyncApi.Tool";
    private const string ToolDirectory = $"scripts/{GeneratorName}";
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

    private static readonly JsonSerializerOptions DocumentOptions = new() { WriteIndented = true };
    private static readonly JsonSchemaExporterOptions PayloadOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = FormatPayloadProperty,
    };

    /// <summary>Creates deterministic, indented AsyncAPI JSON with a trailing newline.</summary>
    /// <returns>The JSON document as text.</returns>
    internal static string Render()
    {
        var channels = new JsonObject();
        var operations = new JsonObject();
        var messages = new JsonObject();
        var schemas = new JsonObject();
        foreach (var contract in Events.OrderBy(contract => contract.EventType, StringComparer.Ordinal))
        {
            var typeName = contract.Contract.Type.Name;
            var version = contract.EventVersion.ToString(CultureInfo.InvariantCulture);
            var messageId = $"{typeName[..^IntegrationEventTypeSuffix.Length]}V{version}";
            var schemaId = $"{typeName}V{version}";
            var channelMessageName = JsonNamingPolicy.CamelCase.ConvertName(messageId);
            var channelPointer = $"#/channels/{PointerSegment(contract.EventType)}";
            var channelMessagePointer = $"{channelPointer}/messages/{PointerSegment(channelMessageName)}";
            channels.Add(contract.EventType, new JsonObject
            {
                ["address"] = contract.EventType,
                ["messages"] = new JsonObject { [channelMessageName] = Reference($"#/components/messages/{messageId}") },
            });
            operations.Add($"publish{messageId}", CreateOperation("send", channelPointer, channelMessagePointer,
                $"{contract.Producer} publishes {contract.EventType} for {contract.Consumer}."));
            var route = contract.TransportConsumer is null ? "its in-process outbox relay" : $"the {contract.ConsumerName} consumer stream";
            operations.Add($"consume{messageId}", CreateOperation("receive", channelPointer, channelMessagePointer,
                $"{contract.Consumer} consumes {contract.EventType} from {route}."));
            messages.Add(messageId, CreateMessage(contract, schemaId, version));
            schemas.Add(schemaId, CreatePayload(contract));
        }

        var root = new JsonObject
        {
            ["asyncapi"] = "3.0.0",
            ["info"] = new JsonObject
            {
                ["title"] = "ViajantesTurismo integration events",
                ["version"] = "1.0.0",
                ["description"] = "Generated from the runtime JSON contracts and durable integration-event topology.",
            },
            ["defaultContentType"] = JsonContentType,
            ["channels"] = channels,
            ["operations"] = operations,
            ["components"] = new JsonObject { ["messages"] = messages, ["schemas"] = schemas },
            ["x-viajantes"] = new JsonObject
            {
                ["scope"] = "Current durable integration events only.",
                ["versioning"] = "Event type strings are stable names; eventVersion increments on breaking payload changes.",
                ["provenance"] = new JsonObject
                {
                    ["generator"] = GeneratorName,
                    ["source"] = $"{ToolDirectory}/AsyncApiDocument.cs",
                    ["refreshCommand"] = $"dotnet run --project {ToolDirectory} -- generate {CanonicalArtifactPath}",
                    ["verifyCommand"] = $"dotnet run --project {ToolDirectory} -- verify {CanonicalArtifactPath}",
                },
            },
        };
        return root.ToJsonString(DocumentOptions) + "\n";
    }

    private static JsonObject CreateOperation(string action, string channel, string message, string summary)
        => new()
        {
            ["action"] = action,
            ["channel"] = Reference(channel),
            ["messages"] = new JsonArray(Reference(message)),
            ["summary"] = summary,
        };

    private static JsonObject CreateMessage(AsyncApiEvent contract, string schemaId, string version)
    {
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

        return new JsonObject
        {
            ["name"] = contract.EventType,
            ["title"] = $"{contract.Title} v{version}",
            ["contentType"] = JsonContentType,
            ["headers"] = new JsonObject
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
            ["payload"] = Reference($"#/components/schemas/{schemaId}"),
            ["x-viajantes"] = topology,
        };
    }

    private static JsonObject CreatePayload(AsyncApiEvent contract)
    {
        var payload = contract.Contract.GetJsonSchemaAsNode(PayloadOptions).AsObject();
        // Describe the producer's emitted object.
        payload["additionalProperties"] = false;
        return payload;
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

        return schema;
    }

    private static string PointerSegment(string value)
        => value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    private static JsonObject Reference(string pointer) => new() { ["$ref"] = pointer };
}
