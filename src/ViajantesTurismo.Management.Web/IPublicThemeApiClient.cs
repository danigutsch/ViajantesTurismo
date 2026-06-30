using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Management.Web;

internal interface IPublicThemeApiClient
{
    Task<PublicThemeSettingsDto> GetTheme(CancellationToken ct);

    Task<PublicThemeSettingsDto> SaveTheme(PublicThemeSettingsDto request, CancellationToken ct);
}
