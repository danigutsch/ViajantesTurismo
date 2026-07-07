using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Requests AI-assisted draft accessibility text for a public media image.
/// </summary>
public sealed record PublicMediaImageAccessibilityDraftRequest
{
    /// <summary>
    /// Gets the draft language.
    /// </summary>
    [Required]
    public PublicContentLanguageDto Language { get; init; } = PublicContentLanguageDto.EnUs;

    /// <summary>
    /// Gets optional editorial context for the image purpose or tour placement.
    /// </summary>
    [StringLength(1_000)]
    public string? Context { get; init; }

    /// <summary>
    /// Gets the optional latitude supplied by trusted metadata.
    /// </summary>
    [Range(-90, 90)]
    public decimal? Latitude { get; init; }

    /// <summary>
    /// Gets the optional longitude supplied by trusted metadata.
    /// </summary>
    [Range(-180, 180)]
    public decimal? Longitude { get; init; }
}
