using PandaBear.BuildingBlocks;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PublicContentCulturesTests
{
    [Fact]
    public void Supported_lists_english_and_brazilian_portuguese_with_english_as_the_default()
    {
        // Act
        var supported = PublicContentCultures.Supported;

        // Assert
        supported.Cultures.Select(culture => culture.Value).ShouldBe(["en-US", "pt-BR"]);
        supported.Default.ShouldBe(PublicContentCultures.EnglishUnitedStates);
        PublicContentCultures.PortugueseBrazil.Value.ShouldBe("pt-BR");
    }

    [Theory]
    [InlineData("en-US", true)]
    [InlineData("pt-BR", true)]
    [InlineData("pt", false)]
    [InlineData("fr-FR", false)]
    public void IsSupported_accepts_only_an_exact_supported_culture(string culture, bool expected)
    {
        // Act
        var supported = PublicContentCultures.IsSupported(ContentCulture.Restore(culture));

        // Assert
        supported.ShouldBe(expected);
    }

    [Fact]
    public void IsSupported_rejects_no_culture()
    {
        // Act
        var supported = PublicContentCultures.IsSupported(null);

        // Assert
        supported.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null, "en-US")]
    [InlineData("", "en-US")]
    [InlineData("  ", "en-US")]
    [InlineData("en", "en-US")]
    [InlineData("EN-us", "en-US")]
    [InlineData("en-GB", "en-US")]
    [InlineData("pt", "pt-BR")]
    [InlineData(" pt-br ", "pt-BR")]
    [InlineData("pt-PT", "pt-BR")]
    public void Resolve_maps_requested_culture_text_to_a_supported_culture(string? requested, string expected)
    {
        // Act
        var culture = PublicContentCultures.Resolve(requested);

        // Assert
        culture.ShouldNotBeNull().Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("ZZ")]
    [InlineData("en_US")]
    [InlineData("not a culture")]
    public void Resolve_returns_no_culture_for_an_unsupported_culture(string requested)
    {
        // Act
        var culture = PublicContentCultures.Resolve(requested);

        // Assert
        culture.ShouldBeNull();
    }

    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData(" pt-br ", "pt-BR")]
    [InlineData("fr-FR", "fr-FR")]
    public void Parse_reads_any_language_tag_in_canonical_casing(string tag, string expected)
    {
        // Act
        var culture = PublicContentCultures.Parse(tag);

        // Assert
        culture.ShouldNotBeNull().Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("en-")]
    [InlineData("en_US")]
    public void Parse_returns_no_culture_for_text_that_is_not_a_language_tag(string? tag)
    {
        // Act
        var culture = PublicContentCultures.Parse(tag);

        // Assert
        culture.ShouldBeNull();
    }

    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("fr-FR", "und")]
    [InlineData(null, "und")]
    public void ToLanguageTag_names_a_supported_culture_or_undetermined(string? culture, string expected)
    {
        // Act
        var tag = PublicContentCultures.ToLanguageTag(culture is null ? null : ContentCulture.Restore(culture));

        // Assert
        tag.ShouldBe(expected);
    }
}
