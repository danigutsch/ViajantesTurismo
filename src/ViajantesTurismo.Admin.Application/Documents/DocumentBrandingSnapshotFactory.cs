using System.Security.Cryptography;
using System.Text;
using SharedKernel.Branding;
using SharedKernel.InputNormalization;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Captures the branding data that decorates one immutable document revision.
/// </summary>
internal static class DocumentBrandingSnapshotFactory
{
    public static async Task<DocumentBrandingSnapshotValues> Capture(IBrandingApiClient brandingApiClient, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(brandingApiClient);

        var settings = await brandingApiClient.GetPublicSettings(ct);
        var logoUri = settings.LogoUri is { Length: <= DocumentLimits.MaxBrandingLogoUriLength } value ? value : null;
        var parsedLogoUri = WebAssetUriSanitizer.ToRootRelativeOrHttpsUri(logoUri, DocumentLimits.MaxBrandingLogoUriLength);
        var footerText = settings.BrandName;
        var source = string.Join("\n", settings.BrandName, parsedLogoUri?.OriginalString ?? string.Empty, settings.PrimaryColor,
            settings.AccentColor, settings.BackgroundColor, settings.TextColor, settings.HeadingFontFamily, settings.BodyFontFamily, footerText);
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        return new DocumentBrandingSnapshotValues(
            version,
            settings.BrandName,
            parsedLogoUri,
            settings.PrimaryColor,
            settings.AccentColor,
            settings.BackgroundColor,
            settings.TextColor,
            settings.HeadingFontFamily,
            settings.BodyFontFamily,
            footerText);
    }
}
