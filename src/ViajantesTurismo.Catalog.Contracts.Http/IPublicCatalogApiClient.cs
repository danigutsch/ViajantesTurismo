using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client contract for public catalog endpoints.
/// </summary>
public interface IPublicCatalogApiClient
{
    /// <summary>
    /// Gets published tours.
    /// </summary>
    Task<HttpOutcome<TourSummaryDto[]>> GetPublishedTours(CancellationToken ct);

    /// <summary>
    /// Gets a published tour by slug.
    /// </summary>
    Task<HttpOutcome<TourDetailsDto>> GetPublishedTourBySlug(string slug, CancellationToken ct);

    /// <summary>Gets one page of the public gallery in the requested content language.</summary>
    Task<HttpOutcome<CatalogGalleryIndexDto>> ListPublishedAlbums(string culture, string? page, CancellationToken ct);

    /// <summary>Gets one page of photos from a public tour album.</summary>
    Task<HttpOutcome<CatalogGalleryAlbumDto>> GetPublishedAlbum(string slug, string culture, string? page, CancellationToken ct);

    /// <summary>
    /// Gets public content by key and culture.
    /// </summary>
    Task<HttpOutcome<PublicContentVariantDto>> GetPublicContent(string key, string? culture, CancellationToken ct);

    /// <summary>
    /// Opens public media content for streaming.
    /// </summary>
    /// <remarks>
    /// The caller must asynchronously dispose a non-null response after streaming completes.
    /// </remarks>
    Task<PublicMediaObjectResponse?> GetPublicMedia(Guid id, int width, string format, CancellationToken ct);

}
