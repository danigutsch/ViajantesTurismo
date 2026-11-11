using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogTourSlugTests
{
    [Fact]
    public void TryNormalize_transliterates_diacritics_and_collapses_separators()
    {
        // Act
        var normalized = CatalogTourSlug.TryNormalize("__Café  2026__", out var slug);

        // Assert
        normalized.ShouldBeTrue();
        slug.ShouldBe("cafe-2026");
    }

    [Fact]
    public void TryNormalize_rejects_non_ascii_scripts_without_returning_a_partial_slug()
    {
        // Act
        var normalized = CatalogTourSlug.TryNormalize("東京", out var slug);

        // Assert
        normalized.ShouldBeFalse();
        slug.ShouldBeEmpty();
    }

    [Fact]
    public void TryNormalize_rejects_a_lone_surrogate_instead_of_throwing()
    {
        // Arrange
        const string value = "tour-\uD800-2026";

        // Act
        var normalized = CatalogTourSlug.TryNormalize(value, out var slug);

        // Assert
        normalized.ShouldBeFalse();
        slug.ShouldBeEmpty();
    }

    [Fact]
    public void TryNormalize_rejects_a_slug_longer_than_the_catalog_limit_without_returning_it()
    {
        // Arrange
        var value = new string('a', CatalogDomainLimits.MaxSlugLength + 1);

        // Act
        var normalized = CatalogTourSlug.TryNormalize(value, out var slug);

        // Assert
        normalized.ShouldBeFalse();
        slug.ShouldBeEmpty();
    }
}
