using System.Text.Json;
using SharedKernel.Testing.Assertions;
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
            Guid.CreateVersion7());

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
            Guid.CreateVersion7());
        var eventType = serializer.GetEventType(draftCreated);

        // Act
        var payloadJson = serializer.Serialize(draftCreated);
        var deserialized = serializer.Deserialize(eventType, payloadJson);

        // Assert
        var typedEvent = deserialized.ShouldBeOfType<CatalogTourDraftCreated>();
        typedEvent.ShouldBe(draftCreated);
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
            Guid.CreateVersion7());
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
}
