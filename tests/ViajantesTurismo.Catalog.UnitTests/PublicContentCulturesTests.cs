using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PublicContentCulturesTests
{
    [Theory]
    [InlineData(PublicContentLanguage.EnUs, "en-US")]
    [InlineData(PublicContentLanguage.PtBr, "pt-BR")]
    public void Maps_each_supported_language_to_its_culture(PublicContentLanguage language, string expected)
    {
        // Act
        var culture = PublicContentCultures.ToCulture(language);

        // Assert
        culture.ShouldNotBeNull().Value.ShouldBe(expected);
        PublicContentCultures.Supported.Contains(culture).ShouldBeTrue();
    }

    [Fact]
    public void Maps_no_language_to_no_culture()
    {
        // Act
        var culture = PublicContentCultures.ToCulture(PublicContentLanguage.None);

        // Assert
        culture.ShouldBeNull();
    }

    [Theory]
    [InlineData(PublicContentLanguage.EnUs, "en-US")]
    [InlineData(PublicContentLanguage.PtBr, "pt-BR")]
    [InlineData(PublicContentLanguage.None, "und")]
    [InlineData((PublicContentLanguage)99, "und")]
    public void ToLanguageTag_names_the_culture_or_undetermined(PublicContentLanguage language, string expected)
    {
        // Act
        var tag = PublicContentCultures.ToLanguageTag(language);

        // Assert
        tag.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, PublicContentLanguage.EnUs)]
    [InlineData("", PublicContentLanguage.EnUs)]
    [InlineData("  ", PublicContentLanguage.EnUs)]
    [InlineData("en", PublicContentLanguage.EnUs)]
    [InlineData("EN-us", PublicContentLanguage.EnUs)]
    [InlineData("en-GB", PublicContentLanguage.EnUs)]
    [InlineData("pt", PublicContentLanguage.PtBr)]
    [InlineData(" pt-br ", PublicContentLanguage.PtBr)]
    [InlineData("pt-PT", PublicContentLanguage.PtBr)]
    public void Resolve_maps_requested_culture_text_to_a_supported_language(string? requested, PublicContentLanguage expected)
    {
        // Act
        var language = PublicContentCultures.Resolve(requested);

        // Assert
        language.ShouldBe(expected);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("ZZ")]
    [InlineData("en_US")]
    [InlineData("not a culture")]
    public void Resolve_returns_no_language_for_an_unsupported_culture(string requested)
    {
        // Act
        var language = PublicContentCultures.Resolve(requested);

        // Assert
        language.ShouldBe(PublicContentLanguage.None);
    }
}
