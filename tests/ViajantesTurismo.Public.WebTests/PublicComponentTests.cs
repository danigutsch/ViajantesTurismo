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

}
