using Microsoft.AspNetCore.Http;
using Bunit;
using TourCard = ViajantesTurismo.Public.Web.Components.Shared.TourCard;

namespace ViajantesTurismo.Public.WebTests.Components.Shared;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestScope(TestScope.Component)]
public sealed class TourCardTests : BunitContext
{
    public TourCardTests() => PublicMediaComponentTestsHelpers.AddMediaRoutes(Services);

    [Fact]
    public void TourCard_renders_default_heading_link_and_first_image()
    {
        // Arrange
        var tour = PublicTourTestData.CreateSummary("camino norte", "Camino Norte", includeImage: true);
        var image = tour.Images.ShouldHaveSingleItem();

        // Act
        var cut = Render<TourCard>(parameters => parameters
            .Add(component => component.Tour, tour)
            .AddCascadingValue(PublicPageCulture.FromPath(new PathString("/en/group-bike-tours"))));

        // Assert
        cut.Find("article").ClassList.ShouldContain("site-card");
        var heading = cut.Find("h2 a");
        heading.TextContent.ShouldBe("Camino Norte");
        heading.GetAttribute("href").ShouldBe("/en/group-bike-tours/camino%20norte");
        cut.Find("p").TextContent.ShouldBe("Discover Camino Norte by bicycle.");
        cut.Find("img").GetAttribute("src").ShouldBe($"/catalog/media/{image.Id}/640/jpg");
        cut.Find("img").GetAttribute("alt").ShouldBe("Cyclists on the Camino");
        cut.Find("img").GetAttribute("loading").ShouldBe("lazy");
    }

    [Fact]
    public void TourCard_renders_level_three_heading_and_no_image_when_tour_has_no_images()
    {
        // Arrange
        var tour = PublicTourTestData.CreateSummary("andes/ride", "Andes Ride", includeImage: false);

        // Act
        var cut = Render<TourCard>(parameters => parameters
            .Add(component => component.Tour, tour)
            .Add(component => component.HeadingLevel, 3)
            .AddCascadingValue(PublicPageCulture.FromPath(new PathString("/pt/"))));

        // Assert
        cut.Find("h3 a").TextContent.ShouldBe("Andes Ride");
        cut.Find("h3 a").GetAttribute("href").ShouldBe("/pt/group-bike-tours/andes%2Fride");
        cut.FindAll("img").ShouldBeEmpty();
    }

    [Fact]
    public void TourCard_shows_the_cover_image_in_the_smallest_jpeg_at_least_640_wide()
    {
        // Arrange
        var coverImageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var tour = new TourSummaryDto
        {
            Title = "Camino Norte",
            Slug = "camino-norte",
            Summary = "Discover Camino Norte by bicycle.",
            Images =
            [
                new CatalogTourImageDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    AltText = "Gallery image",
                    SortOrder = 0,
                    IsCover = false,
                    ResponsiveVariants =
                    [
                        new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
                    ]
                },
                new CatalogTourImageDto
                {
                    Id = coverImageId,
                    AltText = "Cover image",
                    SortOrder = 10,
                    IsCover = true,
                    ResponsiveVariants =
                    [
                        new CatalogMediaImageVariantDto { Width = 320, Height = 213, ContentType = "image/avif", FileSizeBytes = 256 },
                        new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/webp", FileSizeBytes = 512 },
                        new CatalogMediaImageVariantDto { Width = 320, Height = 213, ContentType = "image/jpeg", FileSizeBytes = 512 },
                        new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 },
                        new CatalogMediaImageVariantDto { Width = 1280, Height = 853, ContentType = "image/jpeg", FileSizeBytes = 4096 }
                    ]
                }
            ],
            UpdatedAt = TimeProvider.System.GetUtcNow()
        };

        // Act
        var cut = Render<TourCard>(parameters => parameters
            .Add(component => component.Tour, tour)
            .AddCascadingValue(PublicPageCulture.FromPath(new PathString("/en/"))));

        // Assert
        cut.FindAll("source").ShouldBeEmpty();
        var image = cut.Find("img");
        image.GetAttribute("src").ShouldBe($"/catalog/media/{coverImageId}/640/jpg");
        image.GetAttribute("alt").ShouldBe("Cover image");
        image.GetAttribute("width").ShouldBe("640");
        image.GetAttribute("height").ShouldBe("427");
    }

    [Fact]
    public void TourCard_marks_a_cover_image_without_alternative_text_as_decorative()
    {
        // Arrange
        var tour = PublicTourTestData.CreateSummary("camino-norte", "Camino Norte", includeImage: true) with
        {
            Images = [PublicTourTestData.CreateImage(" ")]
        };

        // Act
        var cut = Render<TourCard>(parameters => parameters
            .Add(component => component.Tour, tour)
            .AddCascadingValue(PublicPageCulture.FromPath(new PathString("/en/"))));

        // Assert
        cut.Find("img").GetAttribute("alt").ShouldBe(string.Empty);
    }
}
