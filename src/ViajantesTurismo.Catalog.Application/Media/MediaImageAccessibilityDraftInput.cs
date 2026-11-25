using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Contains editor-supplied inputs for AI-assisted media image accessibility drafting.
/// </summary>
public sealed record MediaImageAccessibilityDraftInput
{
    /// <summary>
    /// Gets the requested draft culture, or <see langword="null"/> when the request named none.
    /// </summary>
    public required ContentCulture? Language
    {
        get; init;
    }

    /// <summary>
    /// Gets optional editorial context for the image purpose or placement.
    /// </summary>
    public string? Context
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional latitude supplied by trusted metadata.
    /// </summary>
    public decimal? Latitude
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional longitude supplied by trusted metadata.
    /// </summary>
    public decimal? Longitude
    {
        get; init;
    }
}
