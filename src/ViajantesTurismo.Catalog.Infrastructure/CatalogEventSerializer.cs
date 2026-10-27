using System.Text.Json;
using PandaBear.EventSourcing;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Serializes Catalog tour events to and from their durable JSON payloads and versioned event type names.</summary>
internal sealed class CatalogEventSerializer : IEventSerializer
{
    private const string CatalogTourDraftCreatedEventType = "catalog.tours.draft-created.v1";
    private const string CatalogTourPresentationChangedEventType = "catalog.tours.presentation-changed.v1";
    private const string CatalogTourPublishedEventType = "catalog.tours.published.v1";
    private const string CatalogTourUnpublishedEventType = "catalog.tours.unpublished.v1";

    /// <summary>Returns the durable event type name for a registered Catalog event.</summary>
    public string GetEventType(object eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        return eventData switch
        {
            CatalogTourDraftCreated => CatalogTourDraftCreatedEventType,
            CatalogTourPresentationChanged => CatalogTourPresentationChangedEventType,
            CatalogTourPublished => CatalogTourPublishedEventType,
            CatalogTourUnpublished => CatalogTourUnpublishedEventType,
            _ => throw new NotSupportedException($"Catalog event type '{eventData.GetType().FullName}' is not registered for durable serialization.")
        };
    }

    /// <summary>Serializes a registered Catalog event to JSON.</summary>
    public string Serialize(object eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        return eventData switch
        {
            CatalogTourDraftCreated draftCreated => JsonSerializer.Serialize(draftCreated),
            CatalogTourPresentationChanged presentationChanged => JsonSerializer.Serialize(presentationChanged),
            CatalogTourPublished published => JsonSerializer.Serialize(published),
            CatalogTourUnpublished unpublished => JsonSerializer.Serialize(unpublished),
            _ => throw new NotSupportedException($"Catalog event type '{eventData.GetType().FullName}' is not registered for durable serialization.")
        };
    }

    /// <summary>Deserializes a Catalog event from its durable event type name and JSON payload.</summary>
    public object Deserialize(string eventType, string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return eventType switch
        {
            CatalogTourDraftCreatedEventType => DeserializeDraftCreated(payloadJson),
            CatalogTourPresentationChangedEventType => JsonSerializer.Deserialize<CatalogTourPresentationChanged>(payloadJson)
                ?? throw new InvalidOperationException($"Catalog event payload for '{eventType}' was empty."),
            CatalogTourPublishedEventType => JsonSerializer.Deserialize<CatalogTourPublished>(payloadJson)
                ?? throw new InvalidOperationException($"Catalog event payload for '{eventType}' was empty."),
            CatalogTourUnpublishedEventType => JsonSerializer.Deserialize<CatalogTourUnpublished>(payloadJson)
                ?? throw new InvalidOperationException($"Catalog event payload for '{eventType}' was empty."),
            _ => throw new NotSupportedException($"Catalog event type '{eventType}' is not registered for durable deserialization.")
        };
    }

    private static CatalogTourDraftCreated DeserializeDraftCreated(string payloadJson)
    {
        var draftCreated = JsonSerializer.Deserialize<CatalogTourDraftCreated>(payloadJson)
            ?? throw new InvalidOperationException($"Catalog event payload for '{CatalogTourDraftCreatedEventType}' was empty.");
        if (!CatalogTourSlug.IsCanonical(draftCreated.InitialSlug))
        {
            throw new JsonException("Catalog tour creation events require a canonical initial slug.");
        }

        return draftCreated;
    }
}
