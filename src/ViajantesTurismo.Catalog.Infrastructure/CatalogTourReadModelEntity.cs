namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class CatalogTourReadModelEntity
{
    public Guid CatalogTourId { get; set; }

    public Guid AdminTourId { get; set; }

    public string Identifier { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public long Position { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
