using System.Globalization;
using System.Text.Json;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class AdminIntegrationEventSerializerTests
{
    [Fact]
    public void Serialize_writes_admin_tour_created_payload_fields()
    {
        // Arrange
        var serializer = new AdminIntegrationEventSerializer();
        var eventId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.Parse("2026-07-05T12:30:00+00:00", CultureInfo.InvariantCulture);
        var tourId = Guid.CreateVersion7();
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            eventId,
            occurredAt,
            tourId,
            "RIO-2026",
            "Rio de Janeiro");

        // Act
        var json = serializer.Serialize(integrationEvent);

        // Assert
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty(nameof(AdminTourCreatedIntegrationEvent.EventId)).GetGuid().ShouldBe(eventId);
        root.GetProperty(nameof(AdminTourCreatedIntegrationEvent.OccurredAt)).GetDateTimeOffset().ShouldBe(occurredAt);
        root.GetProperty(nameof(AdminTourCreatedIntegrationEvent.AdminTourId)).GetGuid().ShouldBe(tourId);
        root.GetProperty(nameof(AdminTourCreatedIntegrationEvent.Identifier)).GetString().ShouldBe("RIO-2026");
        root.GetProperty(nameof(AdminTourCreatedIntegrationEvent.Name)).GetString().ShouldBe("Rio de Janeiro");
    }

    [Fact]
    public void Serialize_reports_runtime_type_for_unknown_events()
    {
        // Arrange
        var serializer = new AdminIntegrationEventSerializer();
        var integrationEvent = new UnknownAdminIntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        // Act
        Action serialize = () => serializer.Serialize(integrationEvent);

        var exception = serialize.ShouldThrow<NotSupportedException>();

        // Assert
        var eventTypeName = typeof(UnknownAdminIntegrationEvent).FullName.ShouldNotBeNull();
        exception.Message.ShouldContain(eventTypeName, StringComparison.Ordinal);
    }
}
