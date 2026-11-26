namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Summarizes the retained duplicate observations of the Catalog consumer.</summary>
/// <param name="EntriesWithDuplicates">The number of distinct events with a duplicate observation.</param>
/// <param name="DuplicateCount">The total number of retained completed duplicate deliveries.</param>
/// <param name="LastObservedAt">The UTC time of the most recent observation, when present.</param>
public sealed record GetCatalogInboxHealthDto(long EntriesWithDuplicates, long DuplicateCount, DateTimeOffset? LastObservedAt)
{
    /// <summary>Gets the Catalog inbox consumer used to scope these retained observations.</summary>
    public string? ConsumerName
    {
        get; init;
    }
}
