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
            IsPublished = isPublished,
            Images = [],
            UpdatedAt = new DateTimeOffset(2026, 6, 25, 10, 30, 0, TimeSpan.Zero)
        };
    }
}
