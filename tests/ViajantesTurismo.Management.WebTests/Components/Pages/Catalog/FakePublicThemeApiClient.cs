using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Exceptions;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakePublicThemeApiClient : IPublicThemeApiClient
{
    public PublicThemeSettingsDto Theme { get; set; } = PublicThemeSettingsDefaults.CreateDto();

    public bool ThrowOnGetTheme { get; set; }

    public bool ReturnEmptyGetResponse { get; set; }

    public bool ReturnEmptySaveResponse { get; set; }

    public ApiValidationException? ValidationException { get; set; }

    public PublicThemeSettingsDto? SavedTheme { get; private set; }

    public Task<PublicThemeSettingsDto> GetTheme(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ThrowIfGetShouldFail();
        return Task.FromResult(Theme);
    }

    public Task<PublicThemeSettingsDto> SaveTheme(PublicThemeSettingsDto request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            throw ValidationException;
        }

        if (ReturnEmptySaveResponse)
        {
            throw new InvalidOperationException("Catalog API returned an empty theme response.");
        }

        SavedTheme = request;
        Theme = request;
        return Task.FromResult(request);
    }

    private void ThrowIfGetShouldFail()
    {
        if (ThrowOnGetTheme)
        {
            throw new HttpRequestException("Public theme unavailable.");
        }

        if (ReturnEmptyGetResponse)
        {
            throw new InvalidOperationException("Catalog API returned an empty theme response.");
        }
    }
}
