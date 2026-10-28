using PandaBear.Results;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PublicContentVariantTests
{
    [Fact]
    public void Create_accepts_every_text_at_its_maximum_length()
    {
        // Act
        var result = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            new string('t', CatalogDomainLimits.MaxNameLength),
            new string('b', CatalogDomainLimits.MaxBodyLength),
            new string('s', CatalogDomainLimits.MaxNameLength),
            new string('m', CatalogDomainLimits.MaxCaptionLength),
            new string('h', CatalogDomainLimits.MaxCaptionLength),
            requiresHumanReview: false);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Title", CatalogDomainLimits.MaxNameLength)]
    [InlineData("Body", CatalogDomainLimits.MaxBodyLength)]
    [InlineData("SeoTitle", CatalogDomainLimits.MaxNameLength)]
    [InlineData("MetaDescription", CatalogDomainLimits.MaxCaptionLength)]
    [InlineData("ShareSummary", CatalogDomainLimits.MaxCaptionLength)]
    public void Create_rejects_text_one_character_over_its_maximum_length(string field, int maxLength)
    {
        // Arrange
        var tooLong = new string('x', maxLength + 1);

        // Act
        var result = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            field == "Title" ? tooLong : "Welcome",
            field == "Body" ? tooLong : "Discover cycling tours.",
            field == "SeoTitle" ? tooLong : null,
            field == "MetaDescription" ? tooLong : null,
            field == "ShareSummary" ? tooLong : null,
            requiresHumanReview: false);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors.Keys.ShouldBe([field]);
        validationErrors[field].ShouldHaveSingleItem().ShouldBe($"{field} cannot exceed {maxLength} characters.");
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("Body")]
    public void Create_rejects_blank_required_text(string field)
    {
        // Act
        var result = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            field == "Title" ? "   " : "Welcome",
            field == "Body" ? "   " : "Discover cycling tours.",
            null,
            null,
            null,
            requiresHumanReview: false);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors[field].ShouldHaveSingleItem().ShouldBe($"{field} is required.");
    }

    [Fact]
    public void Variants_that_differ_only_in_language_are_not_equal()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: false);
        var sameEnUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);

        // Assert
        enUs.ShouldNotBe(ptBr);
        enUs.ShouldBe(sameEnUs);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("body")]
    [InlineData("seoTitle")]
    [InlineData("metaDescription")]
    [InlineData("shareSummary")]
    [InlineData("requiresHumanReview")]
    public void Variants_that_differ_in_one_value_are_not_equal(string changed)
    {
        // Arrange
        var original = PublicContentVariant.Create(
            PublicContentLanguage.EnUs, "Title", "Body", "SeoTitle", "MetaDescription", "ShareSummary", requiresHumanReview: false);
        var different = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            changed == "title" ? "Other" : "Title",
            changed == "body" ? "Other" : "Body",
            changed == "seoTitle" ? "Other" : "SeoTitle",
            changed == "metaDescription" ? "Other" : "MetaDescription",
            changed == "shareSummary" ? "Other" : "ShareSummary",
            requiresHumanReview: changed == "requiresHumanReview");

        // Assert
        original.Value.ShouldNotBe(different.Value);
    }
}
