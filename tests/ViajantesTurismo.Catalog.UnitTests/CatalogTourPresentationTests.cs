using PandaBear.Results;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogTourPresentationTests
{
    [Fact]
    public void Presentation_and_publication_events_rebuild_current_state()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7());
        var draftCreated = tour.GetUncommittedEvents().ShouldHaveSingleItem();
        tour.ClearUncommittedEvents();

        // Act
        var changed = tour.ChangePresentation(
            "Camino Norte",
            "camino-norte",
            "Ride the Camino Norte.",
            "A detailed customer-facing description.",
            "Day one: depart by bicycle.",
            "Camino Norte cycling tour",
            "Cycle the Camino Norte with Viajantes Turismo.");
        var published = tour.Publish();
        tour.Unpublish();
        var transitions = tour.GetUncommittedEvents();
        var rebuilt = CatalogTour.Rehydrate([draftCreated, .. transitions]);

        // Assert
        changed.IsSuccess.ShouldBeTrue();
        published.IsSuccess.ShouldBeTrue();
        transitions.ShouldMatchCollection(
            domainEvent => domainEvent.ShouldBeOfType<CatalogTourPresentationChanged>(),
            domainEvent => domainEvent.ShouldBeOfType<CatalogTourPublished>(),
            domainEvent => domainEvent.ShouldBeOfType<CatalogTourUnpublished>());
        rebuilt.Title.ShouldBe("Camino Norte");
        rebuilt.Slug.ShouldBe("camino-norte");
        rebuilt.Summary.ShouldBe("Ride the Camino Norte.");
        rebuilt.Description.ShouldBe("A detailed customer-facing description.");
        rebuilt.Itinerary.ShouldBe("Day one: depart by bicycle.");
        rebuilt.SeoTitle.ShouldBe("Camino Norte cycling tour");
        rebuilt.SeoDescription.ShouldBe("Cycle the Camino Norte with Viajantes Turismo.");
        rebuilt.IsPublished.ShouldBeFalse();
        rebuilt.Version.ShouldBe(4);
    }

    [Fact]
    public void Publish_returns_an_invalid_result_for_a_tour_without_a_summary()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7());
        tour.ClearUncommittedEvents();

        // Act
        var result = tour.Publish();

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["Summary"].ShouldHaveSingleItem()
            .ShouldBe("Catalog tours require a title, summary, and slug before publication.");
        tour.IsPublished.ShouldBeFalse();
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void ChangePresentation_returns_a_conflict_while_published_without_changing_state_or_events()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7());
        _ = tour.ChangePresentation(
            "Published Tour",
            "published-tour",
            "Published summary.",
            "Published description.",
            "Published itinerary.",
            "Published SEO title",
            "Published SEO description.");
        _ = tour.Publish();
        tour.ClearUncommittedEvents();
        var version = tour.Version;

        // Act
        var result = tour.ChangePresentation(
            "Changed Tour",
            "changed-tour",
            string.Empty,
            "Changed description.",
            "Changed itinerary.",
            "Changed SEO title",
            "Changed SEO description.");

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail
            .ShouldBe("Published tours must be unpublished before their presentation can change.");
        tour.Title.ShouldBe("Published Tour");
        tour.Slug.ShouldBe("published-tour");
        tour.Summary.ShouldBe("Published summary.");
        tour.IsPublished.ShouldBeTrue();
        tour.Version.ShouldBe(version);
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("public/tour")]
    [InlineData("public?tour")]
    [InlineData("   ")]
    public void ChangePresentation_returns_an_invalid_result_for_an_unsafe_slug(string slug)
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7());
        tour.ClearUncommittedEvents();

        // Act
        var result = tour.ChangePresentation("Camino Norte", slug, "Summary.", "", "", "", "");

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["Slug"].ShouldHaveSingleItem().ShouldBe("Slug must be a safe URL path segment.");
        tour.Slug.ShouldBe("tour-2026");
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void ChangePresentation_returns_an_invalid_result_for_an_over_long_title_and_seo_title()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7());
        tour.ClearUncommittedEvents();
        var tooLong = new string('t', CatalogDomainLimits.MaxNameLength + 1);

        // Act
        var result = tour.ChangePresentation(tooLong, "camino-norte", "Summary.", "", "", tooLong, "");

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["Title"].ShouldHaveSingleItem()
            .ShouldBe($"Title cannot exceed {CatalogDomainLimits.MaxNameLength} characters.");
        validationErrors["SeoTitle"].ShouldHaveSingleItem()
            .ShouldBe($"SeoTitle cannot exceed {CatalogDomainLimits.MaxNameLength} characters.");
        tour.Title.ShouldBe("Draft Tour");
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void ChangePresentation_returns_an_invalid_result_for_a_blank_title_or_over_long_body_text()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7());
        tour.ClearUncommittedEvents();
        var tooLong = new string('b', CatalogDomainLimits.MaxBodyLength + 1);

        // Act
        var result = tour.ChangePresentation("   ", "camino-norte", tooLong, tooLong, tooLong, "", tooLong);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["Title"].ShouldHaveSingleItem().ShouldBe("Title is required.");
        validationErrors.Keys.ShouldContain("Summary");
        validationErrors.Keys.ShouldContain("Description");
        validationErrors.Keys.ShouldContain("Itinerary");
        validationErrors.Keys.ShouldContain("SeoDescription");
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void ChangePresentation_normalizes_a_customer_entered_slug_and_sanitizes_text()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7());
        tour.ClearUncommittedEvents();

        // Act
        var result = tour.ChangePresentation("  Camino Norte  ", "  Camino Norte  ", "Summary.", "", "", "", "");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.Title.ShouldBe("Camino Norte");
        tour.Slug.ShouldBe("camino-norte");
    }

    [Fact]
    public void Rehydrate_uses_the_persisted_id_fallback_for_an_identifier_with_a_slash()
    {
        // Arrange
        var created = new CatalogTourDraftCreated(
            Guid.Parse("019bfcc8-3815-7a68-a515-35a13c1cd7b8"),
            Guid.Parse("019bfcc8-52c3-7cbc-b9f7-a4a3a0a957c2"),
            "TOUR/2026",
            "Fallback Tour",
            Guid.Parse("019bfcc8-6930-757d-84f8-afc8feac4497"),
            "tour-019bfcc838157a68a51535a13c1cd7b8");

        // Act
        var first = CatalogTour.Rehydrate([created]);
        var second = CatalogTour.Rehydrate([created]);

        // Assert
        first.Slug.ShouldBe(second.Slug);
        CatalogTourSlug.IsCanonical(first.Slug).ShouldBeTrue();
        first.Slug.ShouldNotContain("/", StringComparison.Ordinal);
    }

    [Fact]
    public void Rehydrate_preserves_distinct_persisted_id_fallbacks_for_identifiers_that_normalize_equally()
    {
        // Arrange
        var firstId = Guid.Parse("019bfcc8-3815-7a68-a515-35a13c1cd7b8");
        var secondId = Guid.Parse("019bfcc8-52c3-7cbc-b9f7-a4a3a0a957c2");
        var firstCreated = new CatalogTourDraftCreated(
            firstId,
            Guid.CreateVersion7(),
            "TOUR-1",
            "First Tour",
            Guid.CreateVersion7(),
            $"tour-{firstId:N}");
        var secondCreated = new CatalogTourDraftCreated(
            secondId,
            Guid.CreateVersion7(),
            "TOUR_1",
            "Second Tour",
            Guid.CreateVersion7(),
            $"tour-{secondId:N}");

        // Act
        var first = CatalogTour.Rehydrate([firstCreated]);
        var second = CatalogTour.Rehydrate([secondCreated]);

        // Assert
        first.Slug.ShouldBe($"tour-{firstId:N}");
        second.Slug.ShouldBe($"tour-{secondId:N}");
        first.Slug.ShouldNotBe(second.Slug);
    }
}
