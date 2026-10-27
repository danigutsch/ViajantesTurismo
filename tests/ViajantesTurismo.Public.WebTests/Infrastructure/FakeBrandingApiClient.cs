using System.Text.Json;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

/// <summary>In-memory <see cref="IBrandingApiClient"/> fake with switches to simulate failures and bad responses.</summary>
internal sealed class FakeBrandingApiClient : IBrandingApiClient
{
    private BrandingSettingsDto branding = CreateDefaultBranding();

    /// <summary>When true, reads throw <see cref="HttpRequestException"/>.</summary>
    public bool FailRequests
    {
        get; set;
    }

    /// <summary>When true, reads throw <see cref="InvalidOperationException"/> to simulate an empty response.</summary>
    public bool ReturnEmptyResponse
    {
        get; set;
    }

    /// <summary>When true, reads throw <see cref="JsonException"/> to simulate malformed JSON.</summary>
    public bool ReturnMalformedResponse
    {
        get; set;
    }

    /// <summary>When true, reads throw <see cref="NotSupportedException"/> to simulate unsupported content.</summary>
    public bool ReturnUnsupportedResponse
    {
        get; set;
    }

    /// <summary>Replaces the branding returned by later reads.</summary>
    public void SetBranding(BrandingSettingsDto branding)
    {
        ArgumentNullException.ThrowIfNull(branding);
        this.branding = branding;
    }

    /// <summary>Returns the current branding, or throws according to the failure switches.</summary>
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

    /// <summary>Same as <see cref="GetPublicSettings"/>.</summary>
    public Task<BrandingSettingsDto> GetSettings(CancellationToken ct)
    {
        return GetPublicSettings(ct);
    }

    /// <summary>Stores <paramref name="request"/> as the current branding and returns it.</summary>
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
