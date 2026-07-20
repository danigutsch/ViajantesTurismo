using EditPresentation = ViajantesTurismo.Management.Web.Components.Pages.Catalog.EditPresentation;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
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
        cut.Find("#summary").Change("A concise tour summary.");
        cut.Find("#description").Change("A detailed tour description.");
        cut.Find("#itinerary").Change("Day one: depart by bicycle.");
        cut.Find("#seoTitle").Change("Published Tour SEO");
        cut.Find("#seoDescription").Change("Search description for Published Tour.");
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("Catalog presentation updated", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var updated = catalogApi.Tours.ShouldHaveSingleItem();
        updated.Title.ShouldBe("Published Tour");
        updated.Slug.ShouldBe("published-tour");
        updated.Summary.ShouldBe("A concise tour summary.");
        updated.Description.ShouldBe("A detailed tour description.");
        updated.Itinerary.ShouldBe("Day one: depart by bicycle.");
        updated.SeoTitle.ShouldBe("Published Tour SEO");
        updated.SeoDescription.ShouldBe("Search description for Published Tour.");
        updated.IsPublished.ShouldBeFalse();
        catalogApi.LastPresentationRequest.ShouldNotBeNull();
        catalogApi.LastPresentationRequest.ExpectedVersion.ShouldBe(1);
        cut.FindAll("#isPublished").ShouldBeEmpty();
    }

    [Fact]
    public void Publishes_a_draft_with_an_explicit_action()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Draft Tour", "draft-tour", isPublished: false);
        catalogApi.Tours = [tour];

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Status: Draft", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#publish").Click();
        cut.WaitForState(() => cut.Markup.Contains("Catalog tour published", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("#publication-status").TextContent.ShouldContain("Published", StringComparison.Ordinal);
        cut.FindAll("#publish").ShouldBeEmpty();
        cut.Find("#unpublish").ShouldNotBeNull();
        catalogApi.LastPublicationState.ShouldBeTrue();
        catalogApi.LastPublicationRequest.ShouldNotBeNull();
        catalogApi.LastPublicationRequest.ExpectedVersion.ShouldBe(1);
    }

    [Fact]
    public void Publish_is_disabled_while_presentation_edits_are_unsaved()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Draft Tour", "draft-tour", isPublished: false);
        catalogApi.Tours = [tour];

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Status: Draft", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#summary").Change("An unsaved summary change.");

        // Assert
        var publishButton = cut.Find("#publish");
        publishButton.HasAttribute("disabled").ShouldBeTrue();
        publishButton.GetAttribute("aria-describedby").ShouldContain("publish-requirement", StringComparison.Ordinal);
        cut.Find("#publish-requirement").TextContent.ShouldContain("Save presentation changes", StringComparison.Ordinal);
        catalogApi.LastPublicationRequest.ShouldBeNull();
    }

    [Fact]
    public void Unpublishes_a_published_tour_with_an_explicit_action()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Published Tour", "published-tour", isPublished: true);
        catalogApi.Tours = [tour];

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Status: Published", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#unpublish").Click();
        cut.WaitForState(() => cut.Markup.Contains("Catalog tour unpublished", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("#publication-status").TextContent.ShouldContain("Draft", StringComparison.Ordinal);
        cut.FindAll("#unpublish").ShouldBeEmpty();
        cut.Find("#publish").ShouldNotBeNull();
        catalogApi.LastPublicationState.ShouldBeFalse();
        catalogApi.LastPublicationRequest.ShouldNotBeNull();
        catalogApi.LastPublicationRequest.ExpectedVersion.ShouldBe(1);
    }

    [Fact]
    public void Published_tour_presentation_fields_are_disabled_until_unpublished()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Published Tour", "published-tour", isPublished: true);
        catalogApi.Tours = [tour];

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Status: Published", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("#title").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("#slug").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("#summary").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
        cut.Markup.ShouldContain("Unpublish this tour before editing", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_not_found_when_catalog_tour_is_missing()
    {
        // Arrange
        catalogApi.Tours = [];

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.Markup.Contains("Catalog tour not found", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Catalog tour not found", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_load_error_when_catalog_api_fails()
    {
        // Arrange
        catalogApi.ThrowOnGetTours = true;

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.Markup.Contains("could not be loaded", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Catalog tour presentation could not be loaded", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_fallback_error_when_validation_has_no_messages()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Draft Tour", "draft-tour", isPublished: false);
        catalogApi.Tours = [tour];
        catalogApi.ValidationException = new ContractValidationException("Validation problem response body was not JSON.");

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Draft Tour", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("Catalog tour presentation could not be saved", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("Catalog tour presentation could not be saved", StringComparison.Ordinal);
    }

    [Fact]
    public void Conflict_prompts_reload_and_preserves_unsaved_presentation()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Draft Tour", "draft-tour", isPublished: false);
        catalogApi.Tours = [tour];
        catalogApi.ThrowConflictOnUpdate = true;

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Draft Tour", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#title").Change("My unsaved title");
        cut.Find("#summary").Change("My unsaved summary");
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("reload", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("reload", StringComparison.OrdinalIgnoreCase);
        cut.Find("#title").GetAttribute("value").ShouldBe("My unsaved title");
        cut.Find("#summary").GetAttribute("value").ShouldBe("My unsaved summary");
        catalogApi.LastPresentationRequest.ShouldNotBeNull();
    }

    [Fact]
    public void Accepted_update_prompts_reload_and_preserves_unsaved_presentation()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Draft Tour", "draft-tour", isPublished: false);
        catalogApi.Tours = [tour];
        catalogApi.ThrowAcceptedOnUpdate = true;

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Draft Tour", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#title").Change("Pending title");
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("accepted", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find(".alert-success").TextContent.ShouldContain("reload", StringComparison.OrdinalIgnoreCase);
        cut.Find("#title").GetAttribute("value").ShouldBe("Pending title");
    }

    [Fact]
    public void Accepted_publication_does_not_advance_local_status_or_version()
    {
        // Arrange
        var tour = IndexPageTestsHelpers.CreateTour("TOUR-1", "Draft Tour", "draft-tour", isPublished: false);
        catalogApi.Tours = [tour];
        catalogApi.ThrowAcceptedOnPublication = true;

        // Act
        var cut = Render<EditPresentation>(parameters => parameters.Add(component => component.Id, tour.Id));
        cut.WaitForState(() => cut.Markup.Contains("Status: Draft", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Publish", StringComparison.Ordinal)).Click();
        cut.WaitForState(() => cut.Markup.Contains("accepted", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Status: Draft", StringComparison.Ordinal);
        cut.Find(".alert-success").TextContent.ShouldContain("reload", StringComparison.OrdinalIgnoreCase);
        catalogApi.LastPublicationRequest.ShouldNotBeNull();
        catalogApi.LastPublicationRequest.ExpectedVersion.ShouldBe(tour.Version);
    }
}
