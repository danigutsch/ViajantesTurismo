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
    Task<TourSummaryDto[]> GetPublishedTours(CancellationToken ct);

    /// <summary>
    /// Gets a published tour by slug.
    /// </summary>
    Task<TourDetailsDto?> GetPublishedTourBySlug(string slug, CancellationToken ct);

    /// <summary>
    /// Gets public content by key and culture.
    /// </summary>
    Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct);

    /// <summary>
    /// Opens public media content for streaming.
    /// </summary>
    /// <remarks>
    /// The caller must asynchronously dispose a non-null response after streaming completes.
    /// </remarks>
    Task<PublicMediaObjectResponse?> GetPublicMedia(Guid id, int width, string format, CancellationToken ct);

}
