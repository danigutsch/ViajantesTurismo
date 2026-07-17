using ViajantesTurismo.Catalog.Contracts.Application;

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
    public const long DefaultMaxLengthBytes = ContractConstants.MaxMediaUploadBytes;

    /// <summary>
    /// Gets the default maximum decoded image width.
    /// </summary>
    public const int DefaultMaxDecodedWidth = 8_000;

    /// <summary>
    /// Gets the default maximum decoded image height.
    /// </summary>
    public const int DefaultMaxDecodedHeight = 8_000;

    /// <summary>
    /// Gets the default maximum decoded image pixel count.
    /// </summary>
    public const long DefaultMaxDecodedPixelCount = 40_000_000;

    /// <summary>
    /// Gets or sets the maximum upload length in bytes.
    /// </summary>
    public long MaxLengthBytes { get; set; } = DefaultMaxLengthBytes;

    /// <summary>
    /// Gets or sets the maximum decoded image width in pixels.
    /// </summary>
    public int MaxDecodedWidth { get; set; } = DefaultMaxDecodedWidth;

    /// <summary>
    /// Gets or sets the maximum decoded image height in pixels.
    /// </summary>
    public int MaxDecodedHeight { get; set; } = DefaultMaxDecodedHeight;

    /// <summary>
    /// Gets or sets the maximum decoded image pixel count.
    /// </summary>
    public long MaxDecodedPixelCount { get; set; } = DefaultMaxDecodedPixelCount;

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
