using System.Globalization;

using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.ContractTests.Infrastructure;
using ViajantesTurismo.Catalog.Contracts.Media;
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
        var componentsStart = artifact.IndexOf("components:", StringComparison.Ordinal);
        var componentsMessagesStart = componentsStart >= 0
            ? artifact.IndexOf("messages:", componentsStart, StringComparison.Ordinal)
            : -1;
        var adminMessageStart = componentsMessagesStart >= 0
            ? artifact.IndexOf("AdminTourCreatedV1:", componentsMessagesStart, StringComparison.Ordinal)
            : -1;
        var mediaMessageStart = adminMessageStart >= 0
            ? artifact.IndexOf("MediaImageOriginalStoredV1:", adminMessageStart, StringComparison.Ordinal)
            : -1;
        var schemaStart = artifact.IndexOf("schemas:", StringComparison.Ordinal);
        var adminSchemaStart = artifact.IndexOf("AdminTourCreatedIntegrationEventV1:", StringComparison.Ordinal);
        var mediaSchemaStart = artifact.IndexOf("MediaImageOriginalStoredIntegrationEventV1:", StringComparison.Ordinal);

        componentsStart.ShouldBeGreaterThanOrEqualTo(0);
        componentsMessagesStart.ShouldBeGreaterThanOrEqualTo(0);
        adminMessageStart.ShouldBeGreaterThanOrEqualTo(0);
        mediaMessageStart.ShouldBeGreaterThanOrEqualTo(0);
        schemaStart.ShouldBeGreaterThanOrEqualTo(0);
        adminSchemaStart.ShouldBeGreaterThanOrEqualTo(0);
        mediaSchemaStart.ShouldBeGreaterThanOrEqualTo(0);
        componentsStart.ShouldBeLessThan(componentsMessagesStart);
        componentsMessagesStart.ShouldBeLessThan(adminMessageStart);
        adminMessageStart.ShouldBeLessThan(mediaMessageStart);
        mediaMessageStart.ShouldBeLessThan(schemaStart);
        schemaStart.ShouldBeLessThan(adminSchemaStart);
        adminSchemaStart.ShouldBeLessThan(mediaSchemaStart);

        var adminMessage = artifact[adminMessageStart..mediaMessageStart];
        var mediaMessage = artifact[mediaMessageStart..schemaStart];
        var adminSchema = artifact[adminSchemaStart..mediaSchemaStart];
        var mediaSchema = artifact[mediaSchemaStart..];

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

        adminMessage.ShouldContain($"const: {AdminTourCreatedIntegrationEvent.EventType}", StringComparison.Ordinal);
        adminMessage.ShouldContain($"const: {AdminTourCreatedIntegrationEvent.EventVersion.ToString(CultureInfo.InvariantCulture)}", StringComparison.Ordinal);
        adminMessage.ShouldContain("required:", StringComparison.Ordinal);
        adminMessage.ShouldContain("- eventType", StringComparison.Ordinal);
        adminMessage.ShouldContain("- eventVersion", StringComparison.Ordinal);
        adminMessage.ShouldContain("consumerNames:", StringComparison.Ordinal);
        adminMessage.ShouldContain($"- {IntegrationEventConsumerNames.Catalog}", StringComparison.Ordinal);
        adminSchema.ShouldContain("eventId:", StringComparison.Ordinal);
        adminSchema.ShouldContain("occurredAt:", StringComparison.Ordinal);
        adminSchema.ShouldContain("adminTourId:", StringComparison.Ordinal);
        adminSchema.ShouldContain("identifier:", StringComparison.Ordinal);
        adminSchema.ShouldContain("name:", StringComparison.Ordinal);

        mediaMessage.ShouldContain($"const: {MediaImageOriginalStoredIntegrationEvent.EventType}", StringComparison.Ordinal);
        mediaMessage.ShouldContain($"const: {MediaImageOriginalStoredIntegrationEvent.EventVersion.ToString(CultureInfo.InvariantCulture)}", StringComparison.Ordinal);
        mediaMessage.ShouldContain("required:", StringComparison.Ordinal);
        mediaMessage.ShouldContain("- eventType", StringComparison.Ordinal);
        mediaMessage.ShouldContain("- eventVersion", StringComparison.Ordinal);
        mediaMessage.ShouldContain("consumerNames:", StringComparison.Ordinal);
        mediaMessage.ShouldContain($"- {IntegrationEventConsumerNames.Catalog}", StringComparison.Ordinal);
        mediaSchema.ShouldContain("eventId:", StringComparison.Ordinal);
        mediaSchema.ShouldContain("occurredAt:", StringComparison.Ordinal);
        mediaSchema.ShouldContain("mediaImageId:", StringComparison.Ordinal);
        mediaSchema.ShouldContain("sourceObjectKey:", StringComparison.Ordinal);
        mediaSchema.ShouldContain("processingVersion:", StringComparison.Ordinal);
    }
}
