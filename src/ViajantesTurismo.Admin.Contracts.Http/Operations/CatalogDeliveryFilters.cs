namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Filters one bounded page of Catalog delivery metadata.</summary>
/// <param name="EventId">The exact event identifier, when supplied.</param>
/// <param name="EventType">The exact registered event type, when supplied.</param>
/// <param name="RecordedSince">The inclusive UTC lower bound, when supplied.</param>
/// <param name="AfterRecordedAt">The timestamp of the previous continuation, when paging.</param>
/// <param name="AfterStorageId">The storage identifier of the previous continuation, when paging.</param>
public sealed record CatalogDeliveryFilters(
    string? EventId = null,
    string? EventType = null,
    DateTimeOffset? RecordedSince = null,
    DateTimeOffset? AfterRecordedAt = null,
    Guid? AfterStorageId = null);
