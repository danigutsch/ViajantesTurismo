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
        Assert.Equal("Camino Norte", heading.TextContent);
        Assert.Equal("/group-bike-tours/camino%20norte", heading.GetAttribute("href"));
        Assert.Equal("TOUR-2026", cut.Find("p").TextContent);
        Assert.Equal("https://cdn.example/camino.jpg", cut.Find("img").GetAttribute("src"));
        Assert.Equal("Cyclists on the Camino", cut.Find("img").GetAttribute("alt"));
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
        Assert.Equal("Andes Ride", cut.Find("h3 a").TextContent);
        Assert.Equal("/group-bike-tours/andes%2Fride", cut.Find("h3 a").GetAttribute("href"));
        Assert.Empty(cut.FindAll("img"));
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
        var source = cut.Find("source");
        Assert.Equal("https://cdn.example/cover-320.jpg 320w, https://cdn.example/cover-640.jpg 640w", source.GetAttribute("srcset"));
        Assert.Equal("https://cdn.example/cover.jpg", cut.Find("img").GetAttribute("src"));
        Assert.Equal("Cover image", cut.Find("img").GetAttribute("alt"));
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
        Assert.Equal(2, cut.FindAll("figure").Count);
        Assert.Equal(2, cut.FindAll("img[loading='lazy']").Count);
        var caption = Assert.Single(cut.FindAll("figcaption"));
        Assert.Equal("Mountain pass", caption.TextContent);
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
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-320.jpg"), Width = 320, Height = 213, ContentType = "image/jpeg", FileSizeBytes = 512 },
                    new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
                ]
            }
        };

        // Act
        var cut = Render<TourGallery>(parameters => parameters.Add(component => component.Images, images));

        // Assert
        var source = cut.Find("source");
        Assert.Equal("https://cdn.example/one-320.jpg 320w, https://cdn.example/one-640.jpg 640w", source.GetAttribute("srcset"));
        Assert.Equal("(min-width: 48rem) 50vw, 100vw", source.GetAttribute("sizes"));
    }

}
