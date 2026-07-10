using ViajantesTurismo.Catalog.Contracts.Application;
namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client contract for public theme management endpoints.
/// </summary>
public interface IPublicThemeApiClient
{
    /// <summary>
    /// Gets theme settings.
    /// </summary>
    Task<PublicThemeSettingsDto> GetTheme(CancellationToken ct);

    /// <summary>
    /// Saves theme settings.
    /// </summary>
    Task<PublicThemeSettingsDto> SaveTheme(PublicThemeSettingsDto request, CancellationToken ct);
}
