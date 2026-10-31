namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Defines the size and dimension bounds of an uploaded Branding logo.</summary>
public static class BrandingLogoLimits
{
    /// <summary>Largest accepted upload in bytes.</summary>
    public const int MaxUploadBytes = 1024 * 1024;

    /// <summary>Largest accepted decoded width or height of an upload, in pixels.</summary>
    public const int MaxUploadEdgePixels = 2048;

    /// <summary>Largest accepted decoded pixel count of an upload.</summary>
    public const long MaxUploadPixelCount = 4_000_000;

    /// <summary>
    /// Largest re-encoded logo in bytes. Its base64 data URI stays under the 60,000 character limit of a document
    /// branding snapshot.
    /// </summary>
    public const int MaxEncodedBytes = 40 * 1024;

    /// <summary>Largest in-memory buffer for a non-seekable upload before it spools to a temporary file.</summary>
    internal const int SpoolMemoryThresholdBytes = 256 * 1024;
}
