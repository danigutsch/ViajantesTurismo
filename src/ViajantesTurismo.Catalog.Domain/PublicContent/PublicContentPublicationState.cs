namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>
/// Publication state for editable public website content.
/// </summary>
public enum PublicContentPublicationState
{
    /// <summary>
    /// No publication state has been selected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Content is editable and not publicly visible.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Content cannot be published until a human reviews it.
    /// </summary>
    ReviewRequired = 2,

    /// <summary>
    /// Content is approved for public rendering.
    /// </summary>
    Published = 3
}
