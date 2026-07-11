using System.Globalization;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

public sealed class TourIntegrationEventMappingsTests
{
    [Fact]
    public void MapTourCreated_copies_domain_fields_and_preserves_metadata()
    {
        // Arrange
        var eventId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.Parse("2026-07-05T12:30:00+00:00", CultureInfo.InvariantCulture);
        var tourId = Guid.CreateVersion7();
        var domainEvent = new TourCreatedDomainEvent(tourId, "RIO-2026", "Rio de Janeiro");

        // Act
        var integrationEvent = TourIntegrationEventMappings.MapTourCreated(domainEvent, eventId, occurredAt);

        // Assert
        integrationEvent.EventId.ShouldBe(eventId);
        integrationEvent.OccurredAt.ShouldBe(occurredAt);
        integrationEvent.AdminTourId.ShouldBe(tourId);
        integrationEvent.Identifier.ShouldBe("RIO-2026");
        integrationEvent.Name.ShouldBe("Rio de Janeiro");
    }

    [Fact]
    public void MapTourCreated_rejects_null_domain_events()
    {
        // Act
        Action map = () => TourIntegrationEventMappings.MapTourCreated(null!, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var exception = map.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("domainEvent");
    }
}
