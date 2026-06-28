using EditPresentation = ViajantesTurismo.Management.Web.Components.Pages.Catalog.EditPresentation;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

public sealed class EditPresentationPageTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public EditPresentationPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
    }

    [Fact]
    public void Saves_catalog_tour_presentation_fields()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Draft Tour", "draft-tour", isPublished: false);
        catalogApi.Tours = [tour];

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Draft Tour", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#title").Change("Published Tour");
        cut.Find("#slug").Change("published-tour");
        cut.Find("#isPublished").Change(true);
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("Catalog presentation updated", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var updated = Assert.Single(catalogApi.Tours);
        Assert.Equal("Published Tour", updated.Title);
        Assert.Equal("published-tour", updated.Slug);
        Assert.True(updated.IsPublished);
    }
}
