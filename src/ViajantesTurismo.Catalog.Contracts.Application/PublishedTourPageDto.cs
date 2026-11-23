namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>One page of visible tours, with at most one eligible cover photo per card.</summary>
/// <param name="Page">The current one-based page number.</param>
/// <param name="PageSize">The maximum number of cards per page.</param>
/// <param name="Total">The number of visible tours.</param>
/// <param name="Tours">The tour cards in stable order.</param>
public sealed record PublishedTourPageDto(int Page, int PageSize, int Total, IReadOnlyList<TourSummaryDto> Tours)
{
    /// <summary>Number of tours on a page of the public tours index.</summary>
    public const int IndexPageSize = 12;

    /// <summary>Number of tours in the Home preview.</summary>
    public const int HomePreviewSize = 6;
}
