namespace ViajantesTurismo.Public.WebTests;

/// <summary>Builders for DTOs used by public component tests.</summary>
internal static class PublicComponentTestsHelpers
{
    /// <summary>Builds a tour summary, with one sample image when <paramref name="includeImage"/> is true.</summary>
    public static TourSummaryDto CreateTour(string slug, string title, bool includeImage)
    {
        return new TourSummaryDto
        {
            Title = title,
            Slug = slug,
            Summary = $"Discover {title} by bicycle.",
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
            UpdatedAt = TimeProvider.System.GetUtcNow()
        };
    }

    /// <summary>Builds a catalog image with a single 640x427 JPEG variant.</summary>
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
