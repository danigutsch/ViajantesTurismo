using System.Text.Json;
using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class CatalogEventSerializer : IEventSerializer
{
    private const string CatalogTourDraftCreatedEventType = "catalog.tours.draft-created.v1";

    public string GetEventType(object eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        return eventData switch
        {
            CatalogTourDraftCreated => CatalogTourDraftCreatedEventType,
            _ => throw new NotSupportedException($"Catalog event type '{eventData.GetType().FullName}' is not registered for durable serialization.")
        };
    }

    public string Serialize(object eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        return eventData switch
        {
            CatalogTourDraftCreated draftCreated => JsonSerializer.Serialize(draftCreated),
            _ => throw new NotSupportedException($"Catalog event type '{eventData.GetType().FullName}' is not registered for durable serialization.")
        };
    }

    public object Deserialize(string eventType, string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return eventType switch
        {
            CatalogTourDraftCreatedEventType => JsonSerializer.Deserialize<CatalogTourDraftCreated>(payloadJson)
                ?? throw new InvalidOperationException($"Catalog event payload for '{eventType}' was empty."),
            _ => throw new NotSupportedException($"Catalog event type '{eventType}' is not registered for durable deserialization.")
        };
    }
}
