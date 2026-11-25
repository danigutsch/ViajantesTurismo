namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Catalog metadata for a published tour's sitemap URLs.</summary>
/// <param name="Slug">The public tour's canonical URL segment.</param>
/// <param name="UpdatedAt">The latest change to the publication projection.</param>
public sealed record PublishedTourSitemapEntryDto(string Slug, DateTimeOffset UpdatedAt)
{
    /// <summary>The maximum number of entries Catalog accepts in one sitemap read.</summary>
    public const int MaximumCount = 50_000;
}
