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
        var parsedLogoUri = ToSafeLogoUri(logoUri);
        var source = string.Join("\n", settings.BrandName, parsedLogoUri?.OriginalString ?? string.Empty, settings.PrimaryColor,
            settings.AccentColor, settings.BackgroundColor, settings.TextColor, settings.HeadingFontFamily, settings.BodyFontFamily);
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        return new DocumentBrandingSnapshotValues(version, settings.BrandName, parsedLogoUri);
    }

    private static Uri? ToSafeLogoUri(string? logoUri)
    {
        if (!Uri.TryCreate(logoUri, UriKind.RelativeOrAbsolute, out var uri))
        {
            return null;
        }

        if (uri.IsAbsoluteUri)
        {
            return uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) ? uri : null;
        }

        var value = uri.OriginalString;
        return value.StartsWith('/')
            && !value.StartsWith("//", StringComparison.Ordinal)
            && !value.Contains('\\', StringComparison.Ordinal)
            && !value.Any(static character => char.IsWhiteSpace(character) || char.IsControl(character))
            ? uri
            : null;
    }
}
