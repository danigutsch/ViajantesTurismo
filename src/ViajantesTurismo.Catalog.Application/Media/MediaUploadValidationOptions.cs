namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Configures media upload validation limits.
/// </summary>
public sealed class MediaUploadValidationOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Catalog:MediaUploadValidation";

    /// <summary>
    /// Gets the default maximum upload length.
    /// </summary>
    public const long DefaultMaxLengthBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum upload length in bytes.
    /// </summary>
    public long MaxLengthBytes { get; set; } = DefaultMaxLengthBytes;

    /// <summary>
    /// Gets the allowed extensions keyed by content type.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> AllowedExtensionsByContentType { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/webp"] = [".webp"],
        ["image/avif"] = [".avif"]
    };
}
