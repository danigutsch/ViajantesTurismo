using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Records that an original Catalog media image object was stored.
/// </summary>
/// <param name="EventId">The stable event identifier used for idempotent consumption.</param>
/// <param name="OccurredAt">The time the original media image was stored.</param>
/// <param name="MediaImageId">The public media image identifier.</param>
/// <param name="SourceObjectKey">The stored original object key.</param>
/// <param name="ProcessingVersion">The deterministic output version.</param>
public sealed record MediaImageOriginalStoredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MediaImageId,
    string SourceObjectKey,
    int ProcessingVersion) : IIntegrationEvent
{
    /// <summary>
    /// Gets the stable event type.
    /// </summary>
    public static string EventType => "catalog.media-image.original-stored";

    /// <summary>
    /// Gets the event contract version.
    /// </summary>
    public static int EventVersion => 1;
}
