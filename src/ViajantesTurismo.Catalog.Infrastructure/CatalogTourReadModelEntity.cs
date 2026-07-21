namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class CatalogTourReadModelEntity
{
    public Guid CatalogTourId { get; set; }

    public Guid AdminTourId { get; set; }

    public string Identifier { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Itinerary { get; set; } = string.Empty;

    public string SeoTitle { get; set; } = string.Empty;

    public string SeoDescription { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public long StreamVersion { get; set; } = 1;

    public long PresentationPosition { get; set; }

    public long PublicationPosition { get; set; }

    public long Position { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
