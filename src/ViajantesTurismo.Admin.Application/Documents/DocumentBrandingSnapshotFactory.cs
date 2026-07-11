using System.Security.Cryptography;
using System.Text;
using SharedKernel.Branding;
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
        var source = string.Join("\n", settings.BrandName, logoUri ?? string.Empty, settings.PrimaryColor,
            settings.AccentColor, settings.BackgroundColor, settings.TextColor, settings.HeadingFontFamily, settings.BodyFontFamily);
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        var parsedLogoUri = Uri.TryCreate(logoUri, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
        return new DocumentBrandingSnapshotValues(version, settings.BrandName, parsedLogoUri);
    }
}
