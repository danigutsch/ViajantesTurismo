namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Contains retained delivery metadata for a Gallery wake.</summary>
/// <param name="RecordedAt">When the wake was queued.</param>
/// <param name="PublishedAt">When the outbox relay recorded successful delivery, when available.</param>
/// <param name="FailedAttempts">The number of failed delivery attempts recorded by the outbox relay.</param>
/// <param name="LastAttemptAt">The last recorded delivery attempt, when available.</param>
/// <param name="NextAttemptAt">The next scheduled delivery attempt, when available.</param>
public sealed record CatalogGalleryOriginalWakeDeliveryDto(
    DateTimeOffset RecordedAt, DateTimeOffset? PublishedAt, int FailedAttempts,
    DateTimeOffset? LastAttemptAt, DateTimeOffset? NextAttemptAt);
