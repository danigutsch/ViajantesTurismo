using System.Globalization;
using System.Text.Json;

using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.ContractTests.Infrastructure;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that the canonical AsyncAPI artifact stays aligned with current integration-event contracts.
/// </summary>
public sealed class AsyncApiContractArtifactTests
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.AsyncApiSurface)]
    public void Canonical_async_api_artifact_matches_current_integration_event_contracts()
    {
        // Arrange
        var artifact = AsyncApiContractArtifact.Read();
        using var document = JsonDocument.Parse(artifact);
        var root = document.RootElement;
        var info = root.GetProperty("info");
        var channels = root.GetProperty("channels");
        var operations = root.GetProperty("operations");
        var components = root.GetProperty("components");
        var messages = components.GetProperty("messages");
        var schemas = components.GetProperty("schemas");
        var adminMessage = messages.GetProperty("AdminTourCreatedV1");
        var mediaMessage = messages.GetProperty("MediaImageOriginalStoredV1");
        var adminSchema = schemas.GetProperty("AdminTourCreatedIntegrationEventV1");
        var mediaSchema = schemas.GetProperty("MediaImageOriginalStoredIntegrationEventV1");
        var adminHeaders = adminMessage.GetProperty("headers");
        var mediaHeaders = mediaMessage.GetProperty("headers");
        var adminMetadata = adminMessage.GetProperty("x-viajantes");
        var mediaMetadata = mediaMessage.GetProperty("x-viajantes");
        var adminConsumerContexts = adminMetadata.GetProperty("consumerContexts").EnumerateArray().Select(item => item.GetString()).ToArray();
        var adminConsumerNames = adminMetadata.GetProperty("consumerNames").EnumerateArray().Select(item => item.GetString()).ToArray();
        var mediaConsumerContexts = mediaMetadata.GetProperty("consumerContexts").EnumerateArray().Select(item => item.GetString()).ToArray();
        var mediaConsumerNames = mediaMetadata.GetProperty("consumerNames").EnumerateArray().Select(item => item.GetString()).ToArray();

        // Act
        var expectedContractTerms = new[]
        {
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion.ToString(CultureInfo.InvariantCulture),
            "eventId",
            "occurredAt",
            "adminTourId",
            "identifier",
            "name",
            MediaImageOriginalStoredIntegrationEvent.EventType,
            MediaImageOriginalStoredIntegrationEvent.EventVersion.ToString(CultureInfo.InvariantCulture),
            "eventId",
            "occurredAt",
            "mediaImageId",
            "sourceObjectKey",
            "processingVersion",
            IntegrationEventConsumerNames.Catalog,
            "producerContext",
            "consumerContexts",
            "channel",
            "sourceType",
            "domainEventMapping",
            "outboxOwner",
            "inboxOwner",
            "handler",
        };

        // Assert
        foreach (var expectedTerm in expectedContractTerms)
        {
            artifact.ShouldContain(expectedTerm, StringComparison.Ordinal);
        }

        root.TryGetProperty("asyncapi", out _).ShouldBeTrue();
        artifact.ShouldNotContain("\t");
        root.GetProperty("asyncapi").GetString().ShouldBe("3.0.0");
        info.GetProperty("version").GetString().ShouldBe("1.0.0");
        channels.GetProperty(AdminTourCreatedIntegrationEvent.EventType).GetProperty("address").GetString().ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        channels.GetProperty(MediaImageOriginalStoredIntegrationEvent.EventType).GetProperty("address").GetString().ShouldBe(MediaImageOriginalStoredIntegrationEvent.EventType);
        operations.GetProperty("publishAdminTourCreatedV1").GetProperty("action").GetString().ShouldBe("send");
        operations.GetProperty("consumeAdminTourCreatedV1").GetProperty("action").GetString().ShouldBe("receive");
        operations.GetProperty("publishMediaImageOriginalStoredV1").GetProperty("action").GetString().ShouldBe("send");
        operations.GetProperty("consumeMediaImageOriginalStoredV1").GetProperty("action").GetString().ShouldBe("receive");

        adminMessage.GetProperty("name").GetString().ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        adminMessage.GetProperty("payload").GetProperty("$ref").GetString().ShouldBe("#/components/schemas/AdminTourCreatedIntegrationEventV1");
        adminHeaders.GetProperty("properties").GetProperty("eventType").GetProperty("const").GetString().ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        adminHeaders.GetProperty("properties").GetProperty("eventVersion").GetProperty("const").GetInt32().ShouldBe(AdminTourCreatedIntegrationEvent.EventVersion);
        adminHeaders.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ShouldContain("eventType");
        adminHeaders.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ShouldContain("eventVersion");
        adminMetadata.GetProperty("producerContext").GetString().ShouldBe("Admin");
        adminConsumerContexts.Length.ShouldBe(1);
        adminConsumerContexts.ShouldContain("Catalog");
        adminConsumerNames.Length.ShouldBe(1);
        adminConsumerNames.ShouldContain(IntegrationEventConsumerNames.Catalog);
        adminMetadata.GetProperty("channel").GetString().ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        adminMetadata.GetProperty("sourceType").GetString().ShouldBe(typeof(AdminTourCreatedIntegrationEvent).FullName);
        adminMetadata.GetProperty("domainEventMapping").GetString().ShouldBe("ViajantesTurismo.Admin.Application.Tours.TourIntegrationEventMappings.MapTourCreated");
        adminMetadata.GetProperty("outboxOwner").GetString().ShouldBe("AdminWriteDbContext");
        adminMetadata.GetProperty("inboxOwner").GetString().ShouldBe("CatalogIntegrationTransportDbContext");
        adminMetadata.GetProperty("handler").GetString().ShouldBe("ViajantesTurismo.Catalog.Application.Tours.AdminTourCreatedIntegrationHandler");
        adminSchema.GetProperty("properties").TryGetProperty("eventId", out _).ShouldBeTrue();
        adminSchema.GetProperty("properties").TryGetProperty("occurredAt", out _).ShouldBeTrue();
        adminSchema.GetProperty("properties").TryGetProperty("adminTourId", out _).ShouldBeTrue();
        adminSchema.GetProperty("properties").TryGetProperty("identifier", out _).ShouldBeTrue();
        adminSchema.GetProperty("properties").TryGetProperty("name", out _).ShouldBeTrue();

        mediaMessage.GetProperty("name").GetString().ShouldBe(MediaImageOriginalStoredIntegrationEvent.EventType);
        mediaMessage.GetProperty("payload").GetProperty("$ref").GetString().ShouldBe("#/components/schemas/MediaImageOriginalStoredIntegrationEventV1");
        mediaHeaders.GetProperty("properties").GetProperty("eventType").GetProperty("const").GetString().ShouldBe(MediaImageOriginalStoredIntegrationEvent.EventType);
        mediaHeaders.GetProperty("properties").GetProperty("eventVersion").GetProperty("const").GetInt32().ShouldBe(MediaImageOriginalStoredIntegrationEvent.EventVersion);
        mediaHeaders.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ShouldContain("eventType");
        mediaHeaders.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ShouldContain("eventVersion");
        mediaMetadata.GetProperty("producerContext").GetString().ShouldBe("Catalog");
        mediaConsumerContexts.Length.ShouldBe(1);
        mediaConsumerContexts.ShouldContain("Catalog");
        mediaConsumerNames.Length.ShouldBe(1);
        mediaConsumerNames.ShouldContain(IntegrationEventConsumerNames.Catalog);
        mediaMetadata.GetProperty("channel").GetString().ShouldBe(MediaImageOriginalStoredIntegrationEvent.EventType);
        mediaMetadata.GetProperty("sourceType").GetString().ShouldBe(typeof(MediaImageOriginalStoredIntegrationEvent).FullName);
        mediaMetadata.TryGetProperty("domainEventMapping", out _).ShouldBeFalse();
        mediaMetadata.GetProperty("outboxOwner").GetString().ShouldBe("CatalogDbContext");
        mediaMetadata.GetProperty("inboxOwner").GetString().ShouldBe("CatalogIntegrationTransportDbContext");
        mediaMetadata.GetProperty("handler").GetString().ShouldBe("ViajantesTurismo.Catalog.Application.Media.MediaImageOriginalStoredIntegrationHandler");
        mediaSchema.GetProperty("properties").TryGetProperty("eventId", out _).ShouldBeTrue();
        mediaSchema.GetProperty("properties").TryGetProperty("occurredAt", out _).ShouldBeTrue();
        mediaSchema.GetProperty("properties").TryGetProperty("mediaImageId", out _).ShouldBeTrue();
        mediaSchema.GetProperty("properties").TryGetProperty("sourceObjectKey", out _).ShouldBeTrue();
        mediaSchema.GetProperty("properties").TryGetProperty("processingVersion", out _).ShouldBeTrue();
    }
}
