namespace ViajantesTurismo.Public.WebTests;

internal static class PublicComponentTestsHelpers
{
    public static CatalogTourDto CreateTour(string slug, string title, bool includeImage)
    {
        return new CatalogTourDto
        {
            Id = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = "TOUR-2026",
            Title = title,
            Slug = slug,
            IsPublished = true,
            Images = includeImage
                ?
                [
                    new CatalogTourImageDto
                    {
                        Uri = new Uri("https://cdn.example/camino.jpg"),
                        AltText = "Cyclists on the Camino",
                        Caption = "Camino caption"
                    }
                ]
                : [],
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
