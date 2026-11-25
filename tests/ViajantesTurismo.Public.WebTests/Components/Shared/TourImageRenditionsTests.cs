using Bunit;
using SharedKernel.Blazor.Site;

namespace ViajantesTurismo.Public.WebTests.Components.Shared;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestScope(TestScope.Component)]
public sealed class TourImageRenditionsTests : BunitContext
{
    public TourImageRenditionsTests() => PublicMediaComponentTestsHelpers.AddMediaRoutes(Services);

    [Fact]
    public void Gallery_renders_captions_only_when_present()
    {
        // Arrange
        var images = new[]
        {
            PublicTourTestData.CreateImage("First image", "Mountain pass"),
            PublicTourTestData.CreateImage("Second image", "   ")
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicMediaComponentTestsHelpers.Gallery(Services, images)));

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
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicMediaComponentTestsHelpers.Gallery(Services, images)));

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
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicMediaComponentTestsHelpers.Gallery(Services, images)));

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
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicMediaComponentTestsHelpers.Gallery(Services, images)));

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
        var cut = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicMediaComponentTestsHelpers.Gallery(Services, images)));

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
        Action renderGallery = () => _ = Render<ImageGrid>(parameters => parameters.Add(component => component.Images, PublicMediaComponentTestsHelpers.Gallery(Services, [image])));

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
            PublicTourTestData.CreateImage("First image"),
            PublicTourTestData.CreateImage("Second image")
        };

        // Act
        var cut = Render<ImageGrid>(parameters => parameters
            .Add(component => component.Images, PublicMediaComponentTestsHelpers.Gallery(Services, images))
            .Add(component => component.PrioritizeFirstImage, true));

        // Assert
        var imageElements = cut.FindAll("img");
        imageElements[0].GetAttribute("loading").ShouldBe("eager");
        imageElements[0].GetAttribute("fetchpriority").ShouldBe("high");
        imageElements[1].GetAttribute("loading").ShouldBe("lazy");
        imageElements[1].GetAttribute("fetchpriority").ShouldBeNull();
    }
}
