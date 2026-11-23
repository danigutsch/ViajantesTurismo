namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Published tour details with the cover plus one window of remaining photos.</summary>
/// <param name="Tour">The published details with the bounded set of images.</param>
/// <param name="Page">The current one-based photo page.</param>
/// <param name="PageSize">The maximum number of thumbnails per page.</param>
/// <param name="Total">The number of eligible thumbnails, excluding the cover.</param>
public sealed record PublishedTourDetailsPageDto(TourDetailsDto Tour, int Page, int PageSize, int Total)
{
    /// <summary>Number of gallery thumbnails on each tour detail page.</summary>
    public const int PhotosPerPage = 6;
}
