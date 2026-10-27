using System.Security.Cryptography;
using System.Text;
using PandaBear.InputNormalization;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Builds the branding data that decorates one immutable document revision.
/// </summary>
internal static class DocumentBrandingSnapshotFactory
{
    /// <summary>Builds snapshot values from the current branding projection, hashing them into a version.</summary>
    public static DocumentBrandingSnapshotValues FromProjection(BrandingProjectionState projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        return Create(
            projection.BrandName,
            projection.LogoUri?.OriginalString,
            projection.PrimaryColor,
            projection.AccentColor,
            projection.BackgroundColor,
            projection.TextColor,
            projection.HeadingFontFamily,
            projection.BodyFontFamily,
            projection.SourceRevision);
    }

    private static DocumentBrandingSnapshotValues Create(
        string brandName,
        string? rawLogoUri,
        string primaryColor,
        string accentColor,
        string backgroundColor,
        string textColor,
        string headingFontFamily,
        string bodyFontFamily,
        long sourceRevision)
    {
        var logoUri = rawLogoUri is { Length: <= DocumentLimits.MaxBrandingLogoUriLength } value ? value : null;
        var parsedLogoUri = WebAssetUriSanitizer.ToRootRelativeOrHttpsUri(logoUri, DocumentLimits.MaxBrandingLogoUriLength);
        var footerText = brandName;
        var source = string.Join("\n", brandName, parsedLogoUri?.OriginalString ?? string.Empty, primaryColor,
            accentColor, backgroundColor, textColor, headingFontFamily, bodyFontFamily, footerText);
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        return new DocumentBrandingSnapshotValues(
            version,
            brandName,
            parsedLogoUri,
            primaryColor,
            accentColor,
            backgroundColor,
            textColor,
            headingFontFamily,
            bodyFontFamily,
            footerText,
            sourceRevision);
    }
}
