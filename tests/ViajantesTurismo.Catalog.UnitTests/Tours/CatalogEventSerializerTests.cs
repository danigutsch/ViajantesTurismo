using System.Text.Json;
using System.Text.Json.Nodes;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

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
    public void Deserialize_rejects_invalid_JSON_for_registered_event_type()
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

    [Fact]
    public void Gallery_change_payload_replays_as_a_catalog_tour_event()
    {
        // Arrange
        var serializer = new CatalogEventSerializer();
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        var firstPhoto = Guid.CreateVersion7();
        var coverPhoto = Guid.CreateVersion7();
        var payload = JsonSerializer.Serialize(new
        {
            CatalogTourId = tour.Id,
            PhotoIds = new[] { firstPhoto, coverPhoto },
            CoverId = coverPhoto
        });

        // Act
        var galleryEvent = serializer.Deserialize("catalog.tours.gallery-changed.v1", payload);
        var rebuilt = CatalogTour.Rehydrate([.. tour.GetUncommittedEvents(), galleryEvent]);

        // Assert
        serializer.GetEventType(galleryEvent).ShouldBe("catalog.tours.gallery-changed.v1");
        galleryEvent.ShouldBeOfType<CatalogTourGalleryChanged>().LegacyBootstrapThroughSequence.ShouldBeNull();
        var roundTripped = JsonNode.Parse(serializer.Serialize(galleryEvent)).ShouldNotBeNull().AsObject();
        roundTripped["CatalogTourId"].ShouldNotBeNull().GetValue<Guid>().ShouldBe(tour.Id);
        roundTripped["PhotoIds"].ShouldNotBeNull().AsArray().Select(photo => photo.ShouldNotBeNull().GetValue<Guid>()).ShouldBe([firstPhoto, coverPhoto]);
        roundTripped["CoverId"].ShouldNotBeNull().GetValue<Guid>().ShouldBe(coverPhoto);
        rebuilt.Version.ShouldBe(tour.Version + 1);
    }

    [Theory]
    [InlineData("tour")]
    [InlineData("photo")]
    [InlineData("duplicates")]
    [InlineData("cover")]
    [InlineData("negative bootstrap watermark")]
    [InlineData("bootstrap intent acknowledgement")]
    public void Gallery_change_payload_rejects_invalid_identity_and_placement(string invalidPart)
    {
        // Arrange
        var serializer = new CatalogEventSerializer();
        var photo = Guid.CreateVersion7();
        Guid[] ordered = invalidPart switch
        {
            "photo" => [Guid.Empty],
            "duplicates" => [photo, photo],
            _ => [photo]
        };
        var recordsIntent = invalidPart == "bootstrap intent acknowledgement";
        var payload = JsonSerializer.Serialize(new
        {
            CatalogTourId = invalidPart == "tour" ? Guid.Empty : Guid.CreateVersion7(),
            PhotoIds = ordered,
            CoverId = invalidPart == "cover" ? Guid.CreateVersion7() : (Guid?)null,
            IntentId = recordsIntent ? Guid.CreateVersion7() : (Guid?)null,
            IntentSequence = recordsIntent ? 1 : (long?)null,
            LegacyBootstrapThroughSequence = recordsIntent ? 1 : invalidPart == "negative bootstrap watermark" ? -1 : (long?)null
        });

        // Act
        Action deserialize = () => serializer.Deserialize("catalog.tours.gallery-changed.v1", payload);

        // Assert
        deserialize.ShouldThrow<ArgumentException>();
    }
}
