namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal static class IndexPageTestsHelpers
{
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
