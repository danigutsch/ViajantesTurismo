using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.ScopeName, SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope)]
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
}
