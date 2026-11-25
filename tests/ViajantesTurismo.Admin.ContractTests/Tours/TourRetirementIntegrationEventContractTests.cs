using System.Text.Json;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.ContractCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.CapabilityName, SharedKernel.Testing.TestTraitValues.IntegrationEventTransport)]
public sealed class TourRetirementIntegrationEventContractTests
{
    [Fact]
    public void Admin_retirement_event_round_trips_with_only_operation_metadata()
    {
        // Arrange
        var retirementId = Guid.CreateVersion7();
        var message = new AdminTourRetiredIntegrationEvent(
            retirementId, new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero), Guid.CreateVersion7(), retirementId);
        var metadata = AdminIntegrationEventJsonContext.Default.AdminTourRetiredIntegrationEvent;

        // Act
        var json = JsonSerializer.SerializeToElement(message, metadata);
        var restored = json.Deserialize(metadata);
        var propertyNames = json.EnumerateObject().Select(property => property.Name).Order().ToArray();

        // Assert
        restored.ShouldBe(message);
        propertyNames.ShouldBe(["AdminTourId", "EventId", "OccurredAt", "RetirementId"]);
        AdminTourRetiredIntegrationEvent.EventType.ShouldBe("admin.tour.retired");
        AdminTourRetiredIntegrationEvent.EventVersion.ShouldBe(1);
    }

    [Fact]
    public void Catalog_retirement_acknowledgement_round_trips_with_only_operation_metadata()
    {
        // Arrange
        var message = new CatalogTourRetirementAppliedIntegrationEvent(
            Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 19, 10, 1, 0, TimeSpan.Zero), Guid.CreateVersion7(), Guid.CreateVersion7());
        var metadata = CatalogIntegrationEventJsonContext.Default.CatalogTourRetirementAppliedIntegrationEvent;

        // Act
        var json = JsonSerializer.SerializeToElement(message, metadata);
        var restored = json.Deserialize(metadata);
        var propertyNames = json.EnumerateObject().Select(property => property.Name).Order().ToArray();

        // Assert
        restored.ShouldBe(message);
        propertyNames.ShouldBe(["AdminTourId", "EventId", "OccurredAt", "RetirementId"]);
        CatalogTourRetirementAppliedIntegrationEvent.EventType.ShouldBe("catalog.tour.retirement-applied");
        CatalogTourRetirementAppliedIntegrationEvent.EventVersion.ShouldBe(1);
    }
}
