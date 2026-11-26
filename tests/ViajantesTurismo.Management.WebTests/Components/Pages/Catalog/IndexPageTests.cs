using SharedKernel.Testing;
using Index = ViajantesTurismo.Management.Web.Components.Pages.Catalog.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[TestScope(TestScope.Component)]
public sealed class IndexPageTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public IndexPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
    }

    [Fact]
    public void Renders_loaded_catalog_tours_with_status_and_updated_date()
    {
        // Arrange
        catalogApi.Tours =
        [
            IndexPageTestsHelpers.CreateTour("TOUR-1", "First Public Tour", "first-public-tour", isPublished: true),
            IndexPageTestsHelpers.CreateTour("TOUR-2", "Second Draft Tour", "second-draft-tour", isPublished: false)
        ];

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("First Public Tour", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Catalog Tours", StringComparison.Ordinal);
        cut.Markup.ShouldContain("TOUR-1", StringComparison.Ordinal);
        cut.Markup.ShouldContain("first-public-tour", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Published", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Draft", StringComparison.Ordinal);
        cut.Markup.ShouldContain("2026-06-25", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Edit presentation", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_empty_state_when_no_catalog_tours_exist()
    {
        // Arrange
        catalogApi.Tours = [];

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("No Catalog tours", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("No Catalog tours are available yet", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_error_when_catalog_API_fails()
    {
        // Arrange
        catalogApi.ThrowOnGetTours = true;

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("could not be loaded", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var alert = cut.Find(".alert-danger");
        alert.TextContent.ShouldContain("Catalog tours could not be loaded", StringComparison.Ordinal);
    }

    [Fact]
    public void Requests_one_page_when_the_catalog_has_eight_hundred_tours()
    {
        // Arrange
        catalogApi.Tours = [.. Enumerable.Range(1, 800).Select(index => IndexPageTestsHelpers.CreateTour($"TOUR-{index:000}", $"Tour {index:000}", $"tour-{index:000}", isPublished: index % 2 == 0))];

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("TOUR-010", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("pagination", StringComparison.OrdinalIgnoreCase);
        cut.Markup.ShouldNotContain("TOUR-011", StringComparison.Ordinal);
        catalogApi.TourPageRequests.ShouldHaveSingleItem().ShouldBe((1, CatalogTourListSort.Title, false));
    }

    [Fact]
    public void Requests_only_the_page_and_sort_from_a_saved_catalog_URL()
    {
        // Arrange
        catalogApi.Tours =
        [
            .. Enumerable.Range(1, 20).Select(index => IndexPageTestsHelpers.CreateTour(
                $"TOUR-{index:000}", $"Tour {index:000}", $"tour-{index:000}", isPublished: true))
        ];
        this.GetNavigationManager().NavigateTo("/catalog/tours?page=2&sort=Identifier&direction=desc");

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("TOUR-011", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        catalogApi.TourPageRequests.ShouldHaveSingleItem().ShouldBe((2, CatalogTourListSort.Identifier, true));
        cut.Markup.ShouldNotContain("TOUR-001", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0")]
    public void An_invalid_catalog_page_parameter_loads_the_default_page(string page)
    {
        // Arrange
        catalogApi.Tours = [IndexPageTestsHelpers.CreateTour("TOUR-001", "One tour", "one-tour", isPublished: true)];
        this.GetNavigationManager().NavigateTo($"/catalog/tours?page={page}");

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("TOUR-001", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        catalogApi.TourPageRequests.ShouldHaveSingleItem().ShouldBe((1, CatalogTourListSort.Title, false));
    }

}
