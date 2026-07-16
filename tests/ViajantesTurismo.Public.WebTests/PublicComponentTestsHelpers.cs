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
                        Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        AltText = "Cyclists on the Camino",
                        Caption = "Camino caption",
                        ResponsiveVariants =
                        [
                            new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
                        ]
                    }
                ]
                : [],
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static CatalogTourImageDto CreateImage(string altText, string? caption = null)
    {
        return new CatalogTourImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = altText,
            Caption = caption,
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
            ]
        };
    }
}
