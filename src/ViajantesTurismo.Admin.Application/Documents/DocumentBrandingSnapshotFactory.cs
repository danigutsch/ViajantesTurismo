using System.Security.Cryptography;
using System.Text;
using SharedKernel.Branding;

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
        var source = string.Join("\n", settings.BrandName, settings.LogoUri ?? string.Empty, settings.PrimaryColor,
            settings.AccentColor, settings.BackgroundColor, settings.TextColor, settings.HeadingFontFamily, settings.BodyFontFamily);
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        var logoUri = Uri.TryCreate(settings.LogoUri, UriKind.RelativeOrAbsolute, out var parsedLogoUri) ? parsedLogoUri : null;
        return new DocumentBrandingSnapshotValues(version, settings.BrandName, logoUri);
    }
}
