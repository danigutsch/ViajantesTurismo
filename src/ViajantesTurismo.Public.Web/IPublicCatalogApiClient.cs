using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Public.Web;

internal interface IPublicCatalogApiClient
{
    Task<CatalogTourDto[]> GetPublishedTours(CancellationToken ct);

    Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken ct);

    Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct);

    Task<PublicThemeSettingsDto> GetThemeSettings(CancellationToken ct);
}
