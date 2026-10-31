using PandaBear.InputNormalization;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Lets only a valid raster data URI reach the renderer; root-relative and external addresses never do.</summary>
internal static class RasterDataLogo
{
    /// <summary>Returns the logo when it is a valid PNG, JPEG or WebP data URI; otherwise null.</summary>
    public static Uri? From(Uri? logoUri) =>
        logoUri is not null
        && WebAssetUriSanitizer.NormalizeRasterDataImage(logoUri.OriginalString, DocumentLimits.MaxBrandingLogoUriLength) is not null
            ? logoUri
            : null;
}
