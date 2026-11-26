using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

/// <summary>Verifies the application projection of validated Catalog tour presentation values.</summary>
[Trait(TestTraitNames.AreaName, TestAreas.Catalog)]
public sealed class CatalogTourPresentationUpdateTests
{
    [Fact]
    public void Create_projects_the_sanitized_domain_presentation()
    {
        // Arrange
        var expected = new CatalogTourPresentationUpdate(
            "Public Tour", "cafe-2026", "Summary", "Description", "Itinerary", "Title", "SEO description");

        // Act
        var result = CatalogTourPresentationUpdate.Create(
            "  Public Tour  ", "__Café  2026__", "  Summary  ", "  Description  ",
            "  Itinerary  ", "  Title  ", "  SEO description  ");

        // Assert
        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public void Create_preserves_the_domain_validation_details()
    {
        // Arrange
        const string? title = null;
        const string invalidSlug = "東京";
        var source = CatalogTourPresentation.Create(title, invalidSlug, null, null, null, null, null);
        var expected = source.ErrorDetails.ShouldNotBeNull();

        // Act
        var result = CatalogTourPresentationUpdate.Create(title, invalidSlug, null, null, null, null, null);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ErrorDetails.ShouldNotBeNull().ShouldBe(expected);
        expected.ValidationErrors.ShouldNotBeNull().Count.ShouldBe(2);
    }
}
