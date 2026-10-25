using PandaBear.Results;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class CatalogTourStateTests
{
    private static readonly string[] PresentationValues =
    [
        "Camino Norte",
        "camino-norte",
        "Ride the Camino Norte.",
        "A detailed customer-facing description.",
        "Day one: depart by bicycle.",
        "Camino Norte cycling tour",
        "Cycle the Camino Norte with Viajantes Turismo.",
    ];

    [Fact]
    public void An_empty_event_stream_rehydrates_to_an_unpublished_tour_with_empty_text()
    {
        // Act
        var tour = CatalogTour.Rehydrate([]);

        // Assert
        tour.Identifier.ShouldBeEmpty();
        tour.Title.ShouldBeEmpty();
        tour.Slug.ShouldBeEmpty();
        tour.Summary.ShouldBeEmpty();
        tour.Description.ShouldBeEmpty();
        tour.Itinerary.ShouldBeEmpty();
        tour.SeoTitle.ShouldBeEmpty();
        tour.SeoDescription.ShouldBeEmpty();
        tour.IsPublished.ShouldBeFalse();
    }

    [Theory]
    [InlineData("identifier")]
    [InlineData("title")]
    [InlineData("initialSlug")]
    public void CreateDraft_rejects_a_missing_text_argument(string parameterName)
    {
        // Arrange
        Action create = () => CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            parameterName == "identifier" ? null! : "TOUR-2026",
            parameterName == "title" ? null! : "Draft Tour",
            Guid.CreateVersion7(),
            parameterName == "initialSlug" ? null! : "tour-2026");

        // Act
        var exception = create.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe(parameterName);
    }

    [Theory]
    [InlineData("identifier", "")]
    [InlineData("identifier", "   ")]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    public void CreateDraft_rejects_a_blank_identifier_or_title(string parameterName, string blank)
    {
        // Arrange
        Action create = () => CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            parameterName == "identifier" ? blank : "TOUR-2026",
            parameterName == "title" ? blank : "Draft Tour",
            Guid.CreateVersion7(),
            "tour-2026");

        // Act
        var exception = create.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe(parameterName);
    }

    [Theory]
    [InlineData(0, "title")]
    [InlineData(1, "slug")]
    [InlineData(2, "summary")]
    [InlineData(3, "description")]
    [InlineData(4, "itinerary")]
    [InlineData(5, "seoTitle")]
    [InlineData(6, "seoDescription")]
    public void ChangePresentation_rejects_a_missing_value(int index, string parameterName)
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "TOUR-2026", "Draft Tour", Guid.CreateVersion7());
        string?[] values = [.. PresentationValues];
        values[index] = null;

        Action change = () => tour.ChangePresentation(
            values[0]!,
            values[1]!,
            values[2]!,
            values[3]!,
            values[4]!,
            values[5]!,
            values[6]!);

        // Act
        var exception = change.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe(parameterName);
    }

    [Fact]
    public void ChangePresentation_with_unchanged_values_adds_no_event()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "TOUR-2026", "Draft Tour", Guid.CreateVersion7());
        tour.ChangePresentation(
            PresentationValues[0],
            PresentationValues[1],
            PresentationValues[2],
            PresentationValues[3],
            PresentationValues[4],
            PresentationValues[5],
            PresentationValues[6]).IsSuccess.ShouldBeTrue();
        tour.ClearUncommittedEvents();

        // Act
        var result = tour.ChangePresentation(
            PresentationValues[0],
            PresentationValues[1],
            PresentationValues[2],
            PresentationValues[3],
            PresentationValues[4],
            PresentationValues[5],
            PresentationValues[6]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ChangePresentation_adds_an_event_when_only_one_value_changes(int index)
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "TOUR-2026", "Draft Tour", Guid.CreateVersion7());
        tour.ChangePresentation(
            PresentationValues[0],
            PresentationValues[1],
            PresentationValues[2],
            PresentationValues[3],
            PresentationValues[4],
            PresentationValues[5],
            PresentationValues[6]).IsSuccess.ShouldBeTrue();
        tour.ClearUncommittedEvents();
        string[] values = [.. PresentationValues];
        values[index] = index == 1 ? "another-slug" : values[index] + " changed";

        // Act
        var result = tour.ChangePresentation(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourPresentationChanged>();
    }

    [Theory]
    [InlineData("", "camino-norte")]
    [InlineData("   ", "camino-norte")]
    [InlineData("Camino Norte", "")]
    [InlineData("Camino Norte", "   ")]
    public void Publish_returns_an_invalid_result_when_the_persisted_title_or_slug_is_blank(string title, string slug)
    {
        // Arrange
        var tour = CatalogTour.Rehydrate(
        [
            new CatalogTourPresentationChanged(
                Guid.Empty,
                title,
                slug,
                "Ride the Camino Norte.",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty),
        ]);

        // Act
        var result = tour.Publish();

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        tour.IsPublished.ShouldBeFalse();
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Rehydrate_rejects_a_missing_event()
    {
        Action rehydrate = () => CatalogTour.Rehydrate([null!]);

        // Act and assert
        rehydrate.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData("presentation")]
    [InlineData("published")]
    [InlineData("unpublished")]
    public void Rehydrate_rejects_an_event_for_another_tour(string kind)
    {
        // Arrange
        var created = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7(),
            "tour-2026");
        var otherTourId = Guid.CreateVersion7();
        object foreign = kind switch
        {
            "presentation" => new CatalogTourPresentationChanged(
                otherTourId, "Title", "slug", "Summary", string.Empty, string.Empty, string.Empty, string.Empty),
            "published" => new CatalogTourPublished(otherTourId),
            _ => new CatalogTourUnpublished(otherTourId),
        };

        Action rehydrate = () => CatalogTour.Rehydrate([created, foreign]);

        // Act
        var exception = rehydrate.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("Catalog tour events must match the aggregate identifier.");
    }


    [Fact]
    public void CreateDraft_rejects_a_non_canonical_initial_slug()
    {
        // Arrange
        Action create = () => CatalogTour.CreateDraft(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "TOUR-2026",
            "Draft Tour",
            Guid.CreateVersion7(),
            "Not A Slug");

        // Act
        var exception = create.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe("initialSlug");
        exception.Message.ShouldStartWith("The initial Catalog tour slug must be canonical.", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, true, false, 0L, true)]
    [InlineData(false, false, false, 1L, true)]
    [InlineData(false, true, false, 1L, false)]
    [InlineData(false, false, false, 2L, false)]
    [InlineData(false, false, true, 3L, false)]
    public void CanEditGallery_depends_on_publication_authority_visibility_and_projection_version(
        bool canPublish,
        bool isPubliclyVisible,
        bool publishTour,
        long projectionVersion,
        bool expected)
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "TOUR-2026", "Draft Tour", Guid.CreateVersion7());
        if (publishTour)
        {
            tour.ChangePresentation(
                PresentationValues[0],
                PresentationValues[1],
                PresentationValues[2],
                PresentationValues[3],
                PresentationValues[4],
                PresentationValues[5],
                PresentationValues[6]).IsSuccess.ShouldBeTrue();
            tour.Publish().IsSuccess.ShouldBeTrue();
        }

        // Act
        var canEdit = tour.CanEditGallery(canPublish, isPubliclyVisible, projectionVersion);

        // Assert
        canEdit.ShouldBe(expected);
    }

    [Fact]
    public void Publish_is_idempotent_for_a_published_tour()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "TOUR-2026", "Draft Tour", Guid.CreateVersion7());
        tour.ChangePresentation(
            PresentationValues[0],
            PresentationValues[1],
            PresentationValues[2],
            PresentationValues[3],
            PresentationValues[4],
            PresentationValues[5],
            PresentationValues[6]).IsSuccess.ShouldBeTrue();
        tour.Publish().IsSuccess.ShouldBeTrue();
        tour.ClearUncommittedEvents();

        // Act
        var result = tour.Publish();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Unpublish_adds_no_event_for_an_unpublished_tour()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "TOUR-2026", "Draft Tour", Guid.CreateVersion7());
        tour.ClearUncommittedEvents();

        // Act
        tour.Unpublish();

        // Assert
        tour.GetUncommittedEvents().ShouldBeEmpty();
        tour.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public void Rehydrate_rejects_an_unknown_event_type()
    {
        // Arrange
        Action rehydrate = () => CatalogTour.Rehydrate([new object()]);

        // Act
        var exception = rehydrate.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("Catalog tour cannot apply event type 'System.Object'.");
    }
}
