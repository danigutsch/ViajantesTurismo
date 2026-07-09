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
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, "asyncapi")]
    public void Canonical_async_api_artifact_matches_current_integration_event_contracts()
    {
        // Arrange
        var artifact = AsyncApiContractArtifact.Read();

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
    }
}
