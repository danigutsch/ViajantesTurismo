namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>EF row holding the projected Catalog tour read model.</summary>
internal sealed class CatalogTourReadModelEntity
{
    /// <summary>Gets or sets the Catalog tour identifier.</summary>
    public Guid CatalogTourId
    {
        get; set;
    }

    /// <summary>Gets or sets the Admin tour identifier this tour is projected from.</summary>
    public Guid AdminTourId
    {
        get; set;
    }

    /// <summary>Gets or sets the tour identifier shown in Admin.</summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>Gets or sets the tour title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the public tour slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the short public summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets the tour description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the tour itinerary.</summary>
    public string Itinerary { get; set; } = string.Empty;

    /// <summary>Gets or sets the SEO title.</summary>
    public string SeoTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the SEO description.</summary>
    public string SeoDescription { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the tour is flagged as published.</summary>
    public bool IsPublished
    {
        get; set;
    }

    /// <summary>Gets or sets the event stream version; 1 means only the draft was created.</summary>
    public long StreamVersion { get; set; } = 1;

    /// <summary>Gets or sets the stream position of the last applied presentation change.</summary>
    public long PresentationPosition
    {
        get; set;
    }

    /// <summary>Gets or sets the stream position of the last applied publication change.</summary>
    public long PublicationPosition
    {
        get; set;
    }

    /// <summary>Gets or sets the highest stream position applied to this row.</summary>
    public long Position
    {
        get; set;
    }

    /// <summary>Gets or sets when the projection last changed, as recorded by the events.</summary>
    public DateTimeOffset UpdatedAt
    {
        get; set;
    }
}
