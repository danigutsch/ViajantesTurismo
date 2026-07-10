using System.Text.Json;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal sealed class FakeBrandingApiClient : IBrandingApiClient
{
    private BrandingSettingsDto branding = CreateDefaultBranding();

    public bool FailRequests { get; set; }

    public bool ReturnEmptyResponse { get; set; }

    public bool ReturnMalformedResponse { get; set; }

    public bool ReturnUnsupportedResponse { get; set; }

    public void SetBranding(BrandingSettingsDto branding)
    {
        ArgumentNullException.ThrowIfNull(branding);
        this.branding = branding;
    }

    public Task<BrandingSettingsDto> GetPublicSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (FailRequests)
        {
            throw new HttpRequestException("Branding unavailable.");
        }

        if (ReturnEmptyResponse)
        {
            throw new InvalidOperationException("Branding returned an empty response.");
        }

        ThrowIfBrandingContentShouldFail();
        return Task.FromResult(branding);
    }

    public Task<BrandingSettingsDto> GetSettings(CancellationToken ct)
    {
        return GetPublicSettings(ct);
    }

    public Task<BrandingSettingsDto> SaveSettings(BrandingSettingsDto request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        branding = request;
        return Task.FromResult(request);
    }

    private void ThrowIfBrandingContentShouldFail()
    {
        if (ReturnMalformedResponse)
        {
            throw new JsonException("Branding returned malformed JSON.");
        }

        if (ReturnUnsupportedResponse)
        {
            throw new NotSupportedException("Branding returned unsupported content.");
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
