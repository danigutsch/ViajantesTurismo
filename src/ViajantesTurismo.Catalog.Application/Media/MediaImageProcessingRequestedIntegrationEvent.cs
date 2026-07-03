using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Requests asynchronous processing for a stored Catalog media image.
/// </summary>
/// <param name="EventId">The stable event identifier used for idempotent consumption.</param>
/// <param name="OccurredAt">The time the processing request was created.</param>
/// <param name="MediaImageId">The public media image identifier.</param>
/// <param name="SourceObjectKey">The stored original object key.</param>
/// <param name="ProcessingVersion">The deterministic output version.</param>
public sealed record MediaImageProcessingRequestedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MediaImageId,
    string SourceObjectKey,
    int ProcessingVersion) : IIntegrationEvent
{
    /// <summary>
    /// Gets the stable event type.
    /// </summary>
    public static string EventType => "catalog.media-image.processing-requested";

    /// <summary>
    /// Gets the event contract version.
    /// </summary>
    public static int EventVersion => 1;
}
