using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client contract for public catalog endpoints.
/// </summary>
public interface IPublicCatalogApiClient
{
    /// <summary>Gets a bounded page of visible tours and at most one eligible cover photo per card.</summary>
    Task<HttpOutcome<PublishedTourPageDto>> GetPublishedTourPage(string? page, int pageSize, CancellationToken ct);

    /// <summary>Gets a bounded list of visible tour slugs and modification times for the sitemap.</summary>
    Task<HttpOutcome<PublishedTourSitemapEntryDto[]>> GetPublishedTourSitemapEntries(int limit, CancellationToken ct);

    /// <summary>Gets the published tour's details, selected cover and a bounded gallery photo window.</summary>
    Task<HttpOutcome<PublishedTourDetailsPageDto>> GetPublishedTourDetailsPage(string slug, string? page, CancellationToken ct);

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
