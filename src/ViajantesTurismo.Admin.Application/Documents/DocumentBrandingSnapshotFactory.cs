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
            projection.Logo,
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
        BrandingProjectionLogo? logo,
        string primaryColor,
        string accentColor,
        string backgroundColor,
        string textColor,
        string headingFontFamily,
        string bodyFontFamily,
        long sourceRevision)
    {
        var parsedLogoUri = ToDataUri(logo);
        var footerText = brandName;
        var source = string.Join("\n", brandName, parsedLogoUri is null ? string.Empty : logo?.Sha256 ?? string.Empty, primaryColor,
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

    private static Uri? ToDataUri(BrandingProjectionLogo? logo)
    {
        if (logo is null)
        {
            return null;
        }

        var dataUri = $"data:{logo.ContentType};base64,{Convert.ToBase64String(logo.Content.Span)}";
        return WebAssetUriSanitizer.NormalizeRasterDataImage(dataUri, DocumentLimits.MaxBrandingLogoUriLength) is { } valid
            ? new Uri(valid, UriKind.Absolute)
            : null;
    }
}
