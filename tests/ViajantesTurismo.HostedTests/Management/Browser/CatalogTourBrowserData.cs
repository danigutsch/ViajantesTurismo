using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>Builds the Catalog tour the browser journeys edit.</summary>
internal static class CatalogTourBrowserData
{
    /// <summary>Creates a valid draft tour.</summary>
    public static CatalogTourDto DraftTour() => new()
    {
        Id = Guid.CreateVersion7(),
        AdminTourId = Guid.CreateVersion7(),
        Identifier = "TOUR-1",
        Title = "Draft Tour",
        Slug = "draft-tour",
        Summary = "Summary",
        Description = "Description",
        Itinerary = "Itinerary",
        SeoTitle = "Draft Tour SEO",
        SeoDescription = "SEO description",
        IsPublished = false,
        Version = 1,
        Images = [],
        UpdatedAt = new DateTimeOffset(2026, 6, 25, 10, 30, 0, TimeSpan.Zero)
    };
}
