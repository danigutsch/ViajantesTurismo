using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Bunit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.Blazor.Site;
using TourCard = ViajantesTurismo.Public.Web.Components.Shared.TourCard;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[TestScope(TestScope.Component)]
public sealed class PublicComponentTests : BunitContext
{
    public PublicComponentTests()
    {
        Services.AddRouting();
        Services.AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource(
            new RouteEndpoint(_ => Task.CompletedTask,
                RoutePatternFactory.Parse("/catalog/media/{id}/{width}/{format}"), 0,
                new EndpointMetadataCollection(new EndpointNameMetadata(PublicWebEndpoints.PublicMediaByRenditionEndpointName)), "media")));
    }

    [Fact]
    public void TourCard_renders_default_heading_link_and_first_image()
    {
        // Arrange
        var tour = PublicComponentTestsHelpers.CreateTour("camino norte", "Camino Norte", includeImage: true);
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
        var tour = PublicComponentTestsHelpers.CreateTour("andes/ride", "Andes Ride", includeImage: false);

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
        var tour = PublicComponentTestsHelpers.CreateTour("camino-norte", "Camino Norte", includeImage: true) with
        {
            Images = [PublicComponentTestsHelpers.CreateImage(" ")]
        };

        // Act
        var cut = Render<TourCard>(parameters => parameters
            .Add(component => component.Tour, tour)
            .AddCascadingValue(PublicPageCulture.FromPath(new PathString("/en/"))));

        // Assert
        cut.Find("img").GetAttribute("alt").ShouldBe(string.Empty);
    }

    [Fact]
    public void Gallery_renders_captions_only_when_present()
    {
        // Arrange
        var images = new[]
        {
            PublicComponentTestsHelpers.CreateImage("First image", "Mountain pass"),
            PublicComponentTestsHelpers.CreateImage("Second image", "   ")
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicComponentTestsHelpers.Gallery(Services, images)));

        // Assert
        cut.FindAll("figure").Count.ShouldBe(2);
        cut.FindAll("img[loading='lazy']").Count.ShouldBe(2);
        var caption = cut.FindAll("figcaption").ShouldHaveSingleItem();
        caption.TextContent.ShouldBe("Mountain pass");
    }

    [Fact]
    public void Gallery_renders_responsive_sources_when_variants_are_present()
    {
        // Arrange
        var imageId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Id = imageId,
                AltText = "First image",
                ResponsiveVariants =
                [
                    new CatalogMediaImageVariantDto { Width = 320, Height = 213, ContentType = "image/avif", FileSizeBytes = 256 },
                    new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/webp", FileSizeBytes = 512 },
                    new CatalogMediaImageVariantDto { Width = 320, Height = 213, ContentType = "image/jpeg", FileSizeBytes = 512 },
                    new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
                ]
            }
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicComponentTestsHelpers.Gallery(Services, images)));

        // Assert
        var sources = cut.FindAll("source");
        sources[0].GetAttribute("type").ShouldBe("image/avif");
        sources[0].GetAttribute("srcset").ShouldBe($"/catalog/media/{imageId}/320/avif 320w");
        sources[1].GetAttribute("type").ShouldBe("image/webp");
        sources[1].GetAttribute("srcset").ShouldBe($"/catalog/media/{imageId}/640/webp 640w");
        sources[2].GetAttribute("type").ShouldBe("image/jpeg");
        sources[2].GetAttribute("srcset").ShouldBe($"/catalog/media/{imageId}/320/jpg 320w, /catalog/media/{imageId}/640/jpg 640w");
        sources[2].GetAttribute("sizes").ShouldBe("(min-width: 48rem) 50vw, 100vw");
        cut.Find("img").GetAttribute("src").ShouldBe($"/catalog/media/{imageId}/640/jpg");
        cut.Find("img").GetAttribute("width").ShouldBe("640");
        cut.Find("img").GetAttribute("height").ShouldBe("427");
    }

    [Fact]
    public void Gallery_builds_local_media_links_from_the_image_ID()
    {
        // Arrange
        var imageId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Id = imageId,
                AltText = "First image",
                ResponsiveVariants =
                [
                    new CatalogMediaImageVariantDto
                    {
                        Width = 640,
                        Height = 427,
                        ContentType = "image/jpeg",
                        FileSizeBytes = 1024
                    }
                ]
            }
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicComponentTestsHelpers.Gallery(Services, images)));

        // Assert
        cut.Find("source").GetAttribute("srcset").ShouldBe("/catalog/media/33333333-3333-3333-3333-333333333333/640/jpg 640w");
        cut.Find("img").GetAttribute("src").ShouldBe("/catalog/media/33333333-3333-3333-3333-333333333333/640/jpg");
    }

    [Fact]
    public void Gallery_uses_the_largest_available_variant_when_no_jpeg_or_png_variant_exists()
    {
        // Arrange
        var imageId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Id = imageId,
                AltText = "First image",
                ResponsiveVariants =
                [
                    new CatalogMediaImageVariantDto { Width = 320, Height = 213, ContentType = "image/avif", FileSizeBytes = 256 },
                    new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = "image/webp", FileSizeBytes = 512 }
                ]
            }
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicComponentTestsHelpers.Gallery(Services, images)));

        // Assert
        cut.Find("img").GetAttribute("src").ShouldBe($"/catalog/media/{imageId}/640/webp");
        cut.Find("img").GetAttribute("width").ShouldBe("640");
        cut.Find("img").GetAttribute("height").ShouldBe("427");
    }

    [Fact]
    public void Gallery_trims_responsive_variant_content_types()
    {
        // Arrange
        var imageId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Id = imageId,
                AltText = "First image",
                ResponsiveVariants =
                [
                    new CatalogMediaImageVariantDto { Width = 320, Height = 213, ContentType = " image/avif ", FileSizeBytes = 256 },
                    new CatalogMediaImageVariantDto { Width = 640, Height = 427, ContentType = " image/jpeg ", FileSizeBytes = 512 }
                ]
            }
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicComponentTestsHelpers.Gallery(Services, images)));

        // Assert
        cut.Find("source").GetAttribute("type").ShouldBe("image/avif");
        cut.Find("source").GetAttribute("srcset").ShouldBe($"/catalog/media/{imageId}/320/avif 320w");
        cut.Find("img").GetAttribute("src").ShouldBe($"/catalog/media/{imageId}/640/jpg");
    }

    [Fact]
    public void Gallery_rejects_images_without_a_supported_responsive_variant()
    {
        // Arrange
        var image = new CatalogTourImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Unsupported image",
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto
                {
                    Width = 640,
                    Height = 427,
                    ContentType = "image/gif",
                    FileSizeBytes = 640
                }
            ]
        };
        Action renderGallery = () => _ = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicComponentTestsHelpers.Gallery(Services, [image])));

        // Act
        var exception = renderGallery.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("A responsive tour image requires a supported media variant.");
    }

    [Fact]
    public void Gallery_can_prioritize_the_first_image()
    {
        // Arrange
        var images = new[]
        {
            PublicComponentTestsHelpers.CreateImage("First image"),
            PublicComponentTestsHelpers.CreateImage("Second image")
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters
            .Add(component => component.Images, PublicComponentTestsHelpers.Gallery(Services, images))
            .Add(component => component.PrioritizeFirstImage, true));

        // Assert
        var imageElements = cut.FindAll("img");
        imageElements[0].GetAttribute("loading").ShouldBe("eager");
        imageElements[0].GetAttribute("fetchpriority").ShouldBe("high");
        imageElements[1].GetAttribute("loading").ShouldBe("lazy");
        imageElements[1].GetAttribute("fetchpriority").ShouldBeNull();
    }
}
