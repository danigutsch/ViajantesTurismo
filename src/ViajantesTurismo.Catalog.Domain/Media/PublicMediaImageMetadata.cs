namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Scalar public metadata for a media image used by Catalog tours.
/// </summary>
public sealed class PublicMediaImageMetadata
{
    /// <summary>
    /// Gets or initializes the stable media image identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the original source URI.
    /// </summary>
    public required Uri SourceUri { get; init; }

    /// <summary>
    /// Gets or initializes the content checksum supplied by the media source.
    /// </summary>
    public required string Checksum { get; init; }

    /// <summary>
    /// Gets or initializes the source media content type.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets or initializes the source file size in bytes.
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Gets or initializes the source image dimensions.
    /// </summary>
    public required MediaImageDimensions Dimensions { get; init; }

    /// <summary>
    /// Gets or initializes the external processing state.
    /// </summary>
    public required MediaImageProcessingStatus ProcessingStatus { get; init; }

    /// <summary>
    /// Gets or initializes the accessible image description.
    /// </summary>
    public required string AltText { get; init; }

    /// <summary>
    /// Gets or initializes the optional public caption.
    /// </summary>
    public string? Caption { get; init; }

    /// <summary>
    /// Gets or initializes the optional attribution text.
    /// </summary>
    public string? Attribution { get; init; }

    /// <summary>
    /// Gets or initializes the optional copyright notice.
    /// </summary>
    public string? Copyright { get; init; }
}
