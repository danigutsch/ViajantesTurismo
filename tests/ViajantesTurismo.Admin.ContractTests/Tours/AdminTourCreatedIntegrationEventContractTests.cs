using System.Globalization;
using System.Text.Json;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using TestTraits = ViajantesTurismo.Admin.ContractTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
public sealed class AdminTourCreatedIntegrationEventContractTests
{
    [Fact]
    public void Admin_tour_created_event_round_trips_with_source_generated_metadata()
    {
        // Arrange
        var eventId = Guid.Parse("0197d1f1-91f2-7d8f-9b36-5bfe87084fe0", CultureInfo.InvariantCulture);
        var occurredAt = DateTimeOffset.Parse("2026-07-05T12:30:00+00:00", CultureInfo.InvariantCulture);
        var adminTourId = Guid.Parse("0197d1f1-a17b-72e1-8545-b7dd7ed23912", CultureInfo.InvariantCulture);
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            eventId,
            occurredAt,
            adminTourId,
            "RIO-2026",
            "Rio de Janeiro");

        // Act
        var json = JsonSerializer.Serialize(
            integrationEvent,
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent);
        var roundTrippedEvent = JsonSerializer.Deserialize(
            json,
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent);

        // Assert
        var contractEvent = roundTrippedEvent.ShouldNotBeNull();
        AdminTourCreatedIntegrationEvent.EventType.ShouldBe("admin.tour.created");
        AdminTourCreatedIntegrationEvent.EventVersion.ShouldBe(1);
        contractEvent.EventId.ShouldBe(eventId);
        contractEvent.OccurredAt.ShouldBe(occurredAt);
        contractEvent.AdminTourId.ShouldBe(adminTourId);
        contractEvent.Identifier.ShouldBe("RIO-2026");
        contractEvent.Name.ShouldBe("Rio de Janeiro");
    }
}
