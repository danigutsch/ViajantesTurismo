namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

/// <summary>Test data helpers for the catalog index page tests.</summary>
internal static class IndexPageTestsHelpers
{
    /// <summary>Builds a catalog tour DTO with derived text fields and the given publish state.</summary>
    public static CatalogTourDto CreateTour(string identifier, string title, string slug, bool isPublished)
    {
        return new CatalogTourDto
        {
            Id = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = identifier,
            Title = title,
            Slug = slug,
            Summary = $"Summary for {title}",
            Description = $"Description for {title}",
            Itinerary = $"Itinerary for {title}",
            SeoTitle = $"{title} SEO",
            SeoDescription = $"SEO description for {title}",
            IsPublished = isPublished,
            Version = 1,
            Images = [],
            UpdatedAt = new DateTimeOffset(2026, 6, 25, 10, 30, 0, TimeSpan.Zero)
        };
    }
}
