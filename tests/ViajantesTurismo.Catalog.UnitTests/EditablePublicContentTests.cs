using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EditablePublicContentTests
{
    [Fact]
    public void Create_stores_both_supported_language_variants()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs, ptBr]);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (result.Value.Key).ShouldBe("HOME.HERO");
        (result.Value.SourceLanguage).ShouldBe(PublicContentLanguage.EnUs);
        (result.Value.Variants).ShouldContain(enUs);
        (result.Value.Variants).ShouldContain(ptBr);
        (result.Value.PublicationState).ShouldBe(PublicContentPublicationState.Draft);
    }

    [Fact]
    public void Create_marks_content_as_review_required_when_translation_needs_review()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: true);

        // Act
        var result = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs, ptBr]);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (result.Value.PublicationState).ShouldBe(PublicContentPublicationState.ReviewRequired);
    }

    [Fact]
    public void Create_rejects_empty_content_keys()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create(" ", PublicContentLanguage.EnUs, [enUs, ptBr]);

        // Assert
        (result.IsFailure).ShouldBeTrue();
    }

    [Fact]
    public void Create_normalizes_content_key_casing()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create("  Home.Hero  ", PublicContentLanguage.EnUs, [enUs, ptBr]);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (result.Value.Key).ShouldBe("HOME.HERO");
    }

    [Fact]
    public void Create_rejects_unsupported_source_language()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create("home.hero", PublicContentLanguage.None, [enUs, ptBr]);

        // Assert
        (result.IsFailure).ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_duplicate_language_variants()
    {
        // Arrange
        var wrongLanguageVariant = EditablePublicContentTestFactory.CreateVariant(
            PublicContentLanguage.PtBr,
            requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create(
            "home.hero",
            PublicContentLanguage.EnUs,
            [wrongLanguageVariant, ptBr]);

        // Assert
        (result.IsFailure).ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_missing_supported_language_variants()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create(
            "home.hero",
            PublicContentLanguage.EnUs,
            [enUs]);

        // Assert
        (result.IsFailure).ShouldBeTrue();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors?.Keys ?? []).ShouldContain(nameof(EditablePublicContent.Variants));
    }

    [Fact]
    public void ReplaceVariants_rejects_missing_supported_language_variants_without_changing_content()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: true);

        // Act
        var result = content.ReplaceVariants(PublicContentLanguage.EnUs, [enUs]);

        // Assert
        (result.IsFailure).ShouldBeTrue();
        (content.PublicationState).ShouldBe(PublicContentPublicationState.Draft);
        (content.Variants).ShouldContain(variant => variant.Language == PublicContentLanguage.PtBr);
    }

    [Fact]
    public void Publish_marks_reviewed_content_as_published()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var result = content.Publish();

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (content.PublicationState).ShouldBe(PublicContentPublicationState.Published);
    }

    [Fact]
    public void Publish_rejects_content_that_still_requires_human_review()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        var result = content.Publish();

        // Assert
        (result.IsFailure).ShouldBeTrue();
        (content.PublicationState).ShouldBe(PublicContentPublicationState.ReviewRequired);
    }

    [Fact]
    public void CanPublish_is_true_only_when_all_variants_are_reviewed()
    {
        // Arrange
        var reviewedContent = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);
        var reviewRequiredContent = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        var reviewedResult = reviewedContent.CanPublish;
        var reviewRequiredResult = reviewRequiredContent.CanPublish;

        // Assert
        reviewedResult.ShouldBe(true);
        reviewRequiredResult.ShouldBe(false);
    }

    [Fact]
    public void IsPubliclyVisible_is_true_only_after_publication()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var draftResult = content.IsPubliclyVisible;
        var publish = content.Publish();
        var publishedResult = content.IsPubliclyVisible;

        // Assert
        draftResult.ShouldBe(false);
        publish.IsSuccess.ShouldBe(true);
        publishedResult.ShouldBe(true);
    }

    [Fact]
    public void FindPublicVariant_returns_requested_approved_language()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var variant = content.FindPublicVariant(PublicContentLanguage.PtBr);

        // Assert
        variant.ShouldNotBeNull().Language.ShouldBe(PublicContentLanguage.PtBr);
    }

    [Fact]
    public void FindPublicVariant_falls_back_to_approved_english_when_requested_language_needs_review()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        var variant = content.FindPublicVariant(PublicContentLanguage.PtBr);

        // Assert
        variant.ShouldNotBeNull().Language.ShouldBe(PublicContentLanguage.EnUs);
    }

    [Fact]
    public void FindPublicVariant_returns_null_when_requested_english_needs_review()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.EnUs, requiresHumanReview: true);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentLanguage.PtBr, requiresHumanReview: false);
        var result = EditablePublicContent.Create("home.hero", PublicContentLanguage.PtBr, [enUs, ptBr]);
        result.IsSuccess.ShouldBe(true);

        // Act
        var variant = result.Value.FindPublicVariant(PublicContentLanguage.EnUs);

        // Assert
        variant.ShouldBeNull();
    }

    [Fact]
    public void Variant_rejects_required_text_that_exceeds_limits()
    {
        // Arrange
        var title = new string('t', ContractConstants.MaxNameLength + 1);

        // Act
        var result = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            title,
            "Body",
            null,
            null,
            null,
            requiresHumanReview: false);

        // Assert
        (result.IsFailure).ShouldBeTrue();
    }

    [Fact]
    public void Variant_rejects_unsupported_language()
    {
        // Act
        var result = PublicContentVariant.Create(
            PublicContentLanguage.None,
            "Welcome",
            "Discover cycling tours.",
            null,
            null,
            null,
            requiresHumanReview: false);

        // Assert
        (result.IsFailure).ShouldBeTrue();
    }
}
