using System.Text.Json;
using System.Text.Json.Nodes;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogEventSerializerTests
{
    [Fact]
    public void GetEventType_returns_stable_name_for_catalog_draft_created()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7(),
            "andes-2026");

        // Act
        var eventType = serializer.GetEventType(draftCreated);

        // Assert
        eventType.ShouldBe("catalog.tours.draft-created.v1");
    }

    [Fact]
    public void Serialize_round_trips_catalog_draft_created_payload()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7(),
            "andes-2026");
        var eventType = serializer.GetEventType(draftCreated);

        // Act
        var payloadJson = serializer.Serialize(draftCreated);
        var deserialized = serializer.Deserialize(eventType, payloadJson);

        // Assert
        var typedEvent = deserialized.ShouldBeOfType<CatalogTourDraftCreated>();
        typedEvent.ShouldBe(draftCreated);
    }

    [Fact]
    public void Deserialize_rejects_catalog_draft_created_without_an_initial_slug()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7(),
            "andes-2026");
        var payload = JsonNode.Parse(serializer.Serialize(draftCreated)).ShouldNotBeNull().AsObject();
        payload.Remove(nameof(CatalogTourDraftCreated.InitialSlug)).ShouldBeTrue();

        // Act
        Action deserialize = () => serializer.Deserialize(
            serializer.GetEventType(draftCreated),
            payload.ToJsonString());

        // Assert
        deserialize.ShouldThrow<JsonException>();
    }

    [Fact]
    public void Serialize_round_trips_catalog_presentation_and_publication_events()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();
        var catalogTourId = Guid.CreateVersion7();
        (object DomainEvent, string EventType)[] events =
        [
            (new CatalogTourPresentationChanged(
                catalogTourId,
                "Camino Norte",
                "camino-norte",
                "Tour summary",
                "Tour description",
                "Tour itinerary",
                "Tour SEO title",
                "Tour SEO description"), "catalog.tours.presentation-changed.v1"),
            (new CatalogTourPublished(catalogTourId), "catalog.tours.published.v1"),
            (new CatalogTourUnpublished(catalogTourId), "catalog.tours.unpublished.v1")
        ];

        // Act & assert
        foreach (var (domainEvent, expectedEventType) in events)
        {
            var eventType = serializer.GetEventType(domainEvent);
            var payload = serializer.Serialize(domainEvent);
            var deserialized = serializer.Deserialize(eventType, payload);

            eventType.ShouldBe(expectedEventType);
            deserialized.ShouldBe(domainEvent);
        }
    }

    [Fact]
    public void Deserialize_rejects_unknown_event_type()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();

        // Act
        Action deserialize = () => serializer.Deserialize("catalog.unknown.v1", "{}");

        // Assert
        var exception = deserialize.ShouldThrow<NotSupportedException>();
        exception.Message.ShouldContain("catalog.unknown.v1", StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_rejects_invalid_json_for_registered_event_type()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7(),
            "andes-2026");
        var eventType = serializer.GetEventType(draftCreated);

        // Act
        Action deserialize = () => serializer.Deserialize(eventType, "{");

        // Assert
        deserialize.ShouldThrow<JsonException>();
    }

    [Fact]
    public void Serialize_rejects_unregistered_event_payload()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();

        // Act
        Action serialize = () => serializer.Serialize(new object());

        // Assert
        var exception = serialize.ShouldThrow<NotSupportedException>();
        exception.Message.ShouldContain("System.Object", StringComparison.Ordinal);
    }

    [Fact]
    public void GetEventType_rejects_unregistered_event_payload()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();

        // Act
        Action getEventType = () => serializer.GetEventType(new object());

        // Assert
        var exception = getEventType.ShouldThrow<NotSupportedException>();
        exception.Message.ShouldContain("System.Object", StringComparison.Ordinal);
    }
}
