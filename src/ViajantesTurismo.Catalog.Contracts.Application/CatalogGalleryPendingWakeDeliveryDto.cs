namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Contains pending outbox delivery metadata associated with a Gallery placement.</summary>
/// <param name="EventId">The durable queued envelope identifier.</param>
/// <param name="RecordedAt">When the wake entered the outbox.</param>
/// <param name="FailedAttempts">The number of failed attempts recorded by the outbox relay.</param>
/// <param name="LastAttemptAt">The last recorded delivery attempt, when available.</param>
/// <param name="NextAttemptAt">The next scheduled delivery attempt, when available.</param>
public sealed record CatalogGalleryPendingWakeDeliveryDto(
    Guid EventId, DateTimeOffset RecordedAt, int FailedAttempts,
    DateTimeOffset? LastAttemptAt, DateTimeOffset? NextAttemptAt);
