using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.Web;

internal sealed class PublicThemeApiClient(HttpClient httpClient) : IPublicThemeApiClient
{
    public async Task<PublicThemeSettingsDto> GetTheme(CancellationToken ct)
    {
        var theme = await httpClient.GetFromJsonAsync<PublicThemeSettingsDto>("/catalog/public-theme", ct);
        return theme ?? throw new InvalidOperationException("Catalog API returned an empty theme response.");
    }

    public async Task<PublicThemeSettingsDto> SaveTheme(PublicThemeSettingsDto request, CancellationToken ct)
    {
        using var response = await httpClient.PutAsJsonAsync("/catalog/public-theme", request, ct);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);

        var theme = await response.Content.ReadFromJsonAsync<PublicThemeSettingsDto>(ct);
        return theme ?? throw new InvalidOperationException("Catalog API returned an empty theme response.");
    }
}
