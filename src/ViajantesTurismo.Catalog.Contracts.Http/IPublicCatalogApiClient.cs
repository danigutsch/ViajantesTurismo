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
    Task<CatalogTourDto[]> GetPublishedTours(CancellationToken ct);

    /// <summary>
    /// Gets a published tour by slug.
    /// </summary>
    Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken ct);

    /// <summary>
    /// Gets public content by key and culture.
    /// </summary>
    Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct);

    /// <summary>
    /// Gets public theme settings.
    /// </summary>
    Task<PublicThemeSettingsDto> GetThemeSettings(CancellationToken ct);
}
