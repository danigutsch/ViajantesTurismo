namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Public metadata for a media image used by Catalog tours.
/// </summary>
public sealed class PublicMediaImage
{
    private readonly List<MediaImageResponsiveVariant> _responsiveVariants = [];
    private readonly List<MediaImageTourLink> _tourLinks = [];
    private readonly List<string> _tags = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicMediaImage"/> class.
    /// </summary>
    /// <param name="Id">The stable media image identifier.</param>
    /// <param name="SourceUri">The original source URI.</param>
    /// <param name="Checksum">The content checksum supplied by the media source.</param>
    /// <param name="ContentType">The source media content type.</param>
    /// <param name="FileSizeBytes">The source file size in bytes.</param>
    /// <param name="Dimensions">The source image dimensions.</param>
    /// <param name="ProcessingStatus">The external processing state.</param>
    /// <param name="ResponsiveVariants">The public responsive renditions.</param>
    /// <param name="Tags">The editorial tags for discovery and grouping.</param>
    /// <param name="TourLinks">The tour gallery placements.</param>
    /// <param name="AltText">The accessible image description.</param>
    /// <param name="Caption">The optional public caption.</param>
    /// <param name="Attribution">The optional attribution text.</param>
    /// <param name="Copyright">The optional copyright notice.</param>
    public PublicMediaImage(
        Guid Id,
        Uri SourceUri,
        string Checksum,
        string ContentType,
        long FileSizeBytes,
        MediaImageDimensions Dimensions,
        MediaImageProcessingStatus ProcessingStatus,
        IReadOnlyList<MediaImageResponsiveVariant> ResponsiveVariants,
        IReadOnlyList<string> Tags,
        IReadOnlyList<MediaImageTourLink> TourLinks,
        string AltText,
        string? Caption,
        string? Attribution,
        string? Copyright)
    {
        this.Id = Id;
        this.SourceUri = SourceUri;
        this.Checksum = Checksum;
        this.ContentType = ContentType;
        this.FileSizeBytes = FileSizeBytes;
        this.Dimensions = Dimensions;
        this.ProcessingStatus = ProcessingStatus;
        _responsiveVariants = [.. ResponsiveVariants];
        _tags.AddRange(Tags);
        _tourLinks = [.. TourLinks];
        this.AltText = AltText;
        this.Caption = Caption;
        this.Attribution = Attribution;
        this.Copyright = Copyright;
    }

    private PublicMediaImage()
    {
        SourceUri = null!;
        Checksum = string.Empty;
        ContentType = string.Empty;
        Dimensions = null!;
        AltText = string.Empty;
    }

    /// <summary>
    /// Gets the stable media image identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the original source URI.
    /// </summary>
    public Uri SourceUri { get; private set; }

    /// <summary>
    /// Gets the content checksum supplied by the media source.
    /// </summary>
    public string Checksum { get; private set; }

    /// <summary>
    /// Gets the source media content type.
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// Gets the source file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Gets the source image dimensions.
    /// </summary>
    public MediaImageDimensions Dimensions { get; private set; }

    /// <summary>
    /// Gets the external processing state.
    /// </summary>
    public MediaImageProcessingStatus ProcessingStatus { get; private set; }

    /// <summary>
    /// Gets the public responsive renditions.
    /// </summary>
    public IReadOnlyList<MediaImageResponsiveVariant> ResponsiveVariants => _responsiveVariants.AsReadOnly();

    /// <summary>
    /// Gets the editorial tags for discovery and grouping.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Gets the tour gallery placements.
    /// </summary>
    public IReadOnlyList<MediaImageTourLink> TourLinks => _tourLinks.AsReadOnly();

    /// <summary>
    /// Gets the accessible image description.
    /// </summary>
    public string AltText { get; private set; }

    /// <summary>
    /// Gets the optional public caption.
    /// </summary>
    public string? Caption { get; private set; }

    /// <summary>
    /// Gets the optional attribution text.
    /// </summary>
    public string? Attribution { get; private set; }

    /// <summary>
    /// Gets the optional copyright notice.
    /// </summary>
    public string? Copyright { get; private set; }
}
