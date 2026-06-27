using Index = ViajantesTurismo.Management.Web.Components.Pages.Catalog.Index;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

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
        Assert.Contains("Catalog Tours", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("TOUR-1", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("first-public-tour", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Published", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Draft", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("2026-06-25", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("No Catalog tours are available yet", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_error_when_catalog_api_fails()
    {
        // Arrange
        catalogApi.ThrowOnGetTours = true;

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("could not be loaded", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.Contains("Catalog tours could not be loaded", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_paginator_when_more_than_ten_catalog_tours_exist()
    {
        // Arrange
        catalogApi.Tours = Enumerable.Range(1, 11)
            .Select(index => IndexPageTestsHelpers.CreateTour($"TOUR-{index:00}", $"Tour {index:00}", $"tour-{index:00}", isPublished: index % 2 == 0))
            .ToArray();

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("TOUR-01", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("pagination", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

}
