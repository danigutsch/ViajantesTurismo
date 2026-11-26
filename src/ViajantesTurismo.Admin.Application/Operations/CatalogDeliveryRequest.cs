namespace ViajantesTurismo.Admin.Application.Operations;

/// <summary>Filters and continuation position for the application's Catalog delivery reader.</summary>
/// <param name="EventId">The exact envelope identifier, when supplied.</param>
/// <param name="EventType">The exact registered event type, when supplied.</param>
/// <param name="RecordedSince">The inclusive lower bound on the received timestamp.</param>
/// <param name="AfterRecordedAt">The timestamp from the preceding page's continuation.</param>
/// <param name="AfterStorageId">The storage identifier paired with the continuation timestamp.</param>
/// <param name="Limit">The validated number of rows to return.</param>
public sealed record CatalogDeliveryRequest(
    string? EventId, string? EventType, DateTimeOffset? RecordedSince,
    DateTimeOffset? AfterRecordedAt, Guid? AfterStorageId, int Limit);
