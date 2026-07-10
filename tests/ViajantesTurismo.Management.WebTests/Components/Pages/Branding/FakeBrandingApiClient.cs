using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Branding;

internal sealed class FakeBrandingApiClient : IBrandingApiClient
{
    public BrandingSettingsDto Branding { get; set; } = CreateDefaultBranding();

    public bool ThrowOnGetSettings { get; set; }

    public bool ReturnEmptyGetResponse { get; set; }

    public bool ReturnEmptySaveResponse { get; set; }

    public ContractValidationException? ValidationException { get; set; }

    public BrandingSettingsDto? SavedBranding { get; private set; }

    public Task<BrandingSettingsDto> GetPublicSettings(CancellationToken ct)
    {
        return GetSettings(ct);
    }

    public Task<BrandingSettingsDto> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ThrowIfGetShouldFail();
        return Task.FromResult(Branding);
    }

    public Task<BrandingSettingsDto> SaveSettings(BrandingSettingsDto request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            throw ValidationException;
        }

        if (ReturnEmptySaveResponse)
        {
            throw new InvalidOperationException("Branding API returned an empty settings response.");
        }

        SavedBranding = request;
        Branding = request;
        return Task.FromResult(request);
    }

    private void ThrowIfGetShouldFail()
    {
        if (ThrowOnGetSettings)
        {
            throw new HttpRequestException("Branding settings unavailable.");
        }

        if (ReturnEmptyGetResponse)
        {
            throw new InvalidOperationException("Branding API returned an empty settings response.");
        }
    }

    private static BrandingSettingsDto CreateDefaultBranding()
    {
        return new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            LogoUri = string.Empty,
            PrimaryColor = "#0F766E",
            AccentColor = "#F97316",
            BackgroundColor = "#FFFBF5",
            TextColor = "#1F2937",
            HeadingFontFamily = "Georgia",
            BodyFontFamily = "system-ui"
        };
    }
}
