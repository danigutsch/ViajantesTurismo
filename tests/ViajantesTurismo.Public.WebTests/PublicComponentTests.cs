using Bunit;
using TourCard = ViajantesTurismo.Public.Web.Components.Shared.TourCard;
using TourGallery = ViajantesTurismo.Public.Web.Components.Shared.TourGallery;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class PublicComponentTests : BunitContext
{
    [Fact]
    public void TourCard_renders_default_heading_link_and_first_image()
    {
        // Arrange
        var tour = PublicComponentTestsHelpers.CreateTour("camino norte", "Camino Norte", includeImage: true);

        // Act
        var cut = Render<TourCard>(parameters => parameters.Add(component => component.Tour, tour));

        // Assert
        var heading = cut.Find("h2 a");
        heading.TextContent.ShouldBe("Camino Norte");
        heading.GetAttribute("href").ShouldBe("/group-bike-tours/camino%20norte");
        cut.Find("p").TextContent.ShouldBe("TOUR-2026");
        cut.Find("img").GetAttribute("src").ShouldBe("https://cdn.example/camino.jpg");
        cut.Find("img").GetAttribute("alt").ShouldBe("Cyclists on the Camino");
    }

    [Fact]
    public void TourCard_renders_level_three_heading_and_no_image_when_tour_has_no_images()
    {
        // Arrange
        var tour = PublicComponentTestsHelpers.CreateTour("andes/ride", "Andes Ride", includeImage: false);

        // Act
        var cut = Render<TourCard>(parameters => parameters
            .Add(component => component.Tour, tour)
            .Add(component => component.HeadingLevel, 3));

        // Assert
        cut.Find("h3 a").TextContent.ShouldBe("Andes Ride");
        cut.Find("h3 a").GetAttribute("href").ShouldBe("/group-bike-tours/andes%2Fride");
        cut.FindAll("img").ShouldBeEmpty();
    }

    [Fact]
    public void TourCard_renders_cover_image_and_responsive_source()
    {
        // Arrange
        var tour = new CatalogTourDto
        {
            Id = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = "TOUR-2026",
            Title = "Camino Norte",
            Slug = "camino-norte",
            IsPublished = true,
            Images =
            [
                new CatalogTourImageDto
                {
                    Uri = new Uri("https://cdn.example/gallery.jpg"),
                    AltText = "Gallery image",
                    SortOrder = 0,
                    IsCover = false
                },
                new CatalogTourImageDto
                {
                    Uri = new Uri("https://cdn.example/cover.jpg"),
                    AltText = "Cover image",
                    SortOrder = 10,
                    IsCover = true,
                    ResponsiveVariants =
                    [
                        new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/cover-320.avif"), Width = 320, Height = 213, ContentType = "image/avif", FileSizeBytes = 256 },
                        new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/cover-640.webp"), Width = 640, Height = 427, ContentType = "image/webp", FileSizeBytes = 512 },
                        new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/cover-320.jpg"), Width = 320, Height = 213, ContentType = "image/jpeg", FileSizeBytes = 512 },
                        new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/cover-640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
                    ]
                }
            ],
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var cut = Render<TourCard>(parameters => parameters.Add(component => component.Tour, tour));

        // Assert
        var sources = cut.FindAll("source");
        sources[0].GetAttribute("type").ShouldBe("image/avif");
        sources[0].GetAttribute("srcset").ShouldBe("https://cdn.example/cover-320.avif 320w");
        sources[1].GetAttribute("type").ShouldBe("image/webp");
        sources[1].GetAttribute("srcset").ShouldBe("https://cdn.example/cover-640.webp 640w");
        sources[2].GetAttribute("type").ShouldBe("image/jpeg");
        sources[2].GetAttribute("srcset").ShouldBe("https://cdn.example/cover-320.jpg 320w, https://cdn.example/cover-640.jpg 640w");
        cut.Find("img").GetAttribute("src").ShouldBe("https://cdn.example/cover-640.jpg");
        cut.Find("img").GetAttribute("alt").ShouldBe("Cover image");
        cut.Find("img").GetAttribute("width").ShouldBe("640");
        cut.Find("img").GetAttribute("height").ShouldBe("427");
    }

    [Fact]
    public void TourGallery_renders_captions_only_when_present()
    {
        // Arrange
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Uri = new Uri("https://cdn.example/one.jpg"),
                AltText = "First image",
                Caption = "Mountain pass"
            },
            new CatalogTourImageDto
            {
                Uri = new Uri("https://cdn.example/two.jpg"),
                AltText = "Second image",
                Caption = "   "
            }
        };

        // Act
        var cut = Render<TourGallery>(parameters => parameters.Add(component => component.Images, images));

        // Assert
        cut.FindAll("figure").Count.ShouldBe(2);
        cut.FindAll("img[loading='lazy']").Count.ShouldBe(2);
        var caption = cut.FindAll("figcaption").ShouldHaveSingleItem();
        caption.TextContent.ShouldBe("Mountain pass");
    }

    [Fact]
    public void TourGallery_renders_responsive_sources_when_variants_are_present()
    {
        // Arrange
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Uri = new Uri("https://cdn.example/one.jpg"),
                AltText = "First image",
                ResponsiveVariants =
                [
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-320.avif"), Width = 320, Height = 213, ContentType = "image/avif", FileSizeBytes = 256 },
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.webp"), Width = 640, Height = 427, ContentType = "image/webp", FileSizeBytes = 512 },
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-320.jpg"), Width = 320, Height = 213, ContentType = "image/jpeg", FileSizeBytes = 512 },
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
                ]
            }
        };

        // Act
        var cut = Render<TourGallery>(parameters => parameters.Add(component => component.Images, images));

        // Assert
        var sources = cut.FindAll("source");
        sources[0].GetAttribute("type").ShouldBe("image/avif");
        sources[0].GetAttribute("srcset").ShouldBe("https://cdn.example/one-320.avif 320w");
        sources[1].GetAttribute("type").ShouldBe("image/webp");
        sources[1].GetAttribute("srcset").ShouldBe("https://cdn.example/one-640.webp 640w");
        sources[2].GetAttribute("type").ShouldBe("image/jpeg");
        sources[2].GetAttribute("srcset").ShouldBe("https://cdn.example/one-320.jpg 320w, https://cdn.example/one-640.jpg 640w");
        sources[2].GetAttribute("sizes").ShouldBe("(min-width: 48rem) 50vw, 100vw");
        cut.Find("img").GetAttribute("src").ShouldBe("https://cdn.example/one-640.jpg");
        cut.Find("img").GetAttribute("width").ShouldBe("640");
        cut.Find("img").GetAttribute("height").ShouldBe("427");
    }

    [Fact]
    public void TourGallery_keeps_original_image_as_fallback_when_no_jpeg_or_png_variant_exists()
    {
        // Arrange
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Uri = new Uri("https://cdn.example/original.jpg"),
                AltText = "First image",
                ResponsiveVariants =
                [
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-320.avif"), Width = 320, Height = 213, ContentType = "image/avif", FileSizeBytes = 256 },
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.webp"), Width = 640, Height = 427, ContentType = "image/webp", FileSizeBytes = 512 }
                ]
            }
        };

        // Act
        var cut = Render<TourGallery>(parameters => parameters.Add(component => component.Images, images));

        // Assert
        cut.Find("img").GetAttribute("src").ShouldBe("https://cdn.example/original.jpg");
        cut.Find("img").GetAttribute("width").ShouldBeNull();
        cut.Find("img").GetAttribute("height").ShouldBeNull();
    }

    [Fact]
    public void TourGallery_can_prioritize_the_first_image()
    {
        // Arrange
        var images = new[]
        {
            new CatalogTourImageDto
            {
                Uri = new Uri("https://cdn.example/one.jpg"),
                AltText = "First image"
            },
            new CatalogTourImageDto
            {
                Uri = new Uri("https://cdn.example/two.jpg"),
                AltText = "Second image"
            }
        };

        // Act
        var cut = Render<TourGallery>(parameters => parameters
            .Add(component => component.Images, images)
            .Add(component => component.PrioritizeFirstImage, true));

        // Assert
        var imageElements = cut.FindAll("img");
        imageElements[0].GetAttribute("loading").ShouldBe("eager");
        imageElements[0].GetAttribute("fetchpriority").ShouldBe("high");
        imageElements[1].GetAttribute("loading").ShouldBe("lazy");
        imageElements[1].GetAttribute("fetchpriority").ShouldBeNull();
    }

}
