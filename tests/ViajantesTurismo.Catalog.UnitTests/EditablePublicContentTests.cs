using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EditablePublicContentTests
{
    [Fact]
    public void Create_stores_both_supported_language_variants()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create("home.hero", PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Key.ShouldBe("HOME.HERO");
        result.Value.SourceLanguage.ShouldBe(PublicContentCultures.EnglishUnitedStates);
        result.Value.Variants.ShouldContain(enUs);
        result.Value.Variants.ShouldContain(ptBr);
        result.Value.PublicationState.ShouldBe(PublicContentPublicationState.Draft);
    }

    [Fact]
    public void Create_marks_content_as_review_required_when_translation_needs_review()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: true);

        // Act
        var result = EditablePublicContent.Create("home.hero", PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.PublicationState.ShouldBe(PublicContentPublicationState.ReviewRequired);
    }

    [Fact]
    public void Create_rejects_empty_content_keys()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create(" ", PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_normalizes_content_key_casing()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create("  Home.Hero  ", PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Key.ShouldBe("HOME.HERO");
    }

    [Fact]
    public void Create_rejects_unsupported_source_language()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create("home.hero", null, [enUs, ptBr]);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_duplicate_language_variants()
    {
        // Arrange
        var wrongLanguageVariant = EditablePublicContentTestFactory.CreateVariant(
            PublicContentCultures.PortugueseBrazil,
            requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create(
            "home.hero",
            PublicContentCultures.EnglishUnitedStates,
            [wrongLanguageVariant, ptBr]);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_missing_supported_language_variants()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create(
            "home.hero",
            PublicContentCultures.EnglishUnitedStates,
            [enUs]);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull();
        var validationKeys = result.ErrorDetails.ValidationErrors?.Keys ?? [];
        validationKeys.ShouldContain(nameof(EditablePublicContent.Variants));
    }

    [Fact]
    public void ReplaceVariants_rejects_missing_supported_language_variants_without_changing_content()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: true);

        // Act
        var result = content.ReplaceVariants(PublicContentCultures.EnglishUnitedStates, [enUs]);

        // Assert
        result.IsFailure.ShouldBeTrue();
        content.PublicationState.ShouldBe(PublicContentPublicationState.Draft);
        content.Variants.ShouldContain(variant => variant.Language == PublicContentCultures.PortugueseBrazil);
    }

    [Fact]
    public void Publish_marks_reviewed_content_as_published()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var result = content.Publish();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        content.PublicationState.ShouldBe(PublicContentPublicationState.Published);
    }

    [Fact]
    public void Publish_rejects_content_that_still_requires_human_review()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        var result = content.Publish();

        // Assert
        result.IsFailure.ShouldBeTrue();
        content.PublicationState.ShouldBe(PublicContentPublicationState.ReviewRequired);
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
    public void TryResolvePublicVariant_returns_requested_approved_culture()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var found = content.TryResolvePublicVariant(PublicContentCultures.PortugueseBrazil, out var variant);

        // Assert
        found.ShouldBeTrue();
        variant.ShouldNotBeNull().Language.ShouldBe(PublicContentCultures.PortugueseBrazil);
    }

    [Fact]
    public void TryResolvePublicVariant_resolves_a_neutral_or_regional_request_to_the_same_language()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var found = content.TryResolvePublicVariant(ContentCulture.Restore("pt-PT"), out var variant);

        // Assert
        found.ShouldBeTrue();
        variant.ShouldNotBeNull().Language.ShouldBe(PublicContentCultures.PortugueseBrazil);
    }

    [Fact]
    public void TryResolvePublicVariant_falls_back_to_approved_english_when_requested_language_needs_review()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        var found = content.TryResolvePublicVariant(PublicContentCultures.PortugueseBrazil, out var variant);

        // Assert
        found.ShouldBeTrue();
        variant.ShouldNotBeNull().Language.ShouldBe(PublicContentCultures.EnglishUnitedStates);
    }

    [Fact]
    public void TryResolvePublicVariant_falls_back_to_approved_english_for_an_unknown_culture()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var found = content.TryResolvePublicVariant(ContentCulture.Restore("fr-FR"), out var variant);

        // Assert
        found.ShouldBeTrue();
        variant.ShouldNotBeNull().Language.ShouldBe(PublicContentCultures.EnglishUnitedStates);
    }

    [Fact]
    public void TryResolvePublicVariant_finds_nothing_when_requested_english_needs_review()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: true);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);
        var result = EditablePublicContent.Create("home.hero", PublicContentCultures.PortugueseBrazil, [enUs, ptBr]);
        result.IsSuccess.ShouldBe(true);

        // Act
        var found = result.Value.TryResolvePublicVariant(PublicContentCultures.EnglishUnitedStates, out var variant);

        // Assert
        found.ShouldBeFalse();
        variant.ShouldBeNull();
    }

    [Fact]
    public void Variant_rejects_required_text_that_exceeds_limits()
    {
        // Arrange
        var title = new string('t', ContractConstants.MaxNameLength + 1);

        // Act
        var result = PublicContentVariant.Create(
            PublicContentCultures.EnglishUnitedStates,
            title,
            "Body",
            null,
            null,
            null,
            requiresHumanReview: false);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Variant_rejects_unsupported_language()
    {
        // Act
        var result = PublicContentVariant.Create(
            ContentCulture.Restore("fr-FR"),
            "Welcome",
            "Discover cycling tours.",
            null,
            null,
            null,
            requiresHumanReview: false);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
    [Fact]
    public void Create_rejects_missing_variants()
    {
        Action create = () => EditablePublicContent.Create("home.hero", PublicContentCultures.EnglishUnitedStates, null!);

        // Act
        var exception = create.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("variants");
    }

    [Fact]
    public void Create_accepts_a_key_at_the_maximum_length_and_rejects_one_character_more()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);

        // Act
        var atLimit = EditablePublicContent.Create(new string('k', CatalogDomainLimits.MaxDefaultLength), PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]);
        var overLimit = EditablePublicContent.Create(new string('k', CatalogDomainLimits.MaxDefaultLength + 1), PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]);

        // Assert
        atLimit.IsSuccess.ShouldBeTrue();
        overLimit.IsFailure.ShouldBeTrue();
        var validationErrors = overLimit.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["key"].ShouldHaveSingleItem().ShouldBe($"Key cannot exceed {CatalogDomainLimits.MaxDefaultLength} characters.");
    }

    [Fact]
    public void Create_reports_each_duplicate_language_exactly_once()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: false);
        var otherPtBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: true);

        // Act
        var result = EditablePublicContent.Create("home.hero", PublicContentCultures.EnglishUnitedStates, [enUs, ptBr, otherPtBr]);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["Variants"].ShouldHaveSingleItem().ShouldBe("Variants contains a duplicate language variant.");
    }

    [Fact]
    public void Create_reports_only_the_missing_language()
    {
        // Arrange
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);

        // Act
        var result = EditablePublicContent.Create("home.hero", PublicContentCultures.EnglishUnitedStates, [enUs]);

        // Assert
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors["Variants"].ShouldHaveSingleItem().ShouldBe("Variants is missing a supported language variant.");
        result.ErrorDetails.Detail.ShouldContain(PublicContentCultures.PortugueseBrazil.Value, StringComparison.Ordinal);
        result.ErrorDetails.Detail.ShouldNotContain(PublicContentCultures.EnglishUnitedStates.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void PublishIfReady_publishes_content_when_every_variant_is_approved()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var result = content.PublishIfReady();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        content.PublicationState.ShouldBe(PublicContentPublicationState.Published);
    }

    [Fact]
    public void PublishIfReady_leaves_content_in_review_when_a_variant_needs_review()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        var result = content.PublishIfReady();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        content.PublicationState.ShouldBe(PublicContentPublicationState.ReviewRequired);
    }

    [Fact]
    public void ReplaceVariants_rejects_missing_variants()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        Action replace = () => content.ReplaceVariants(PublicContentCultures.EnglishUnitedStates, null!);

        // Act
        var exception = replace.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("variants");
    }

    [Fact]
    public void ReplaceVariants_rejects_an_unsupported_source_language_without_changing_content()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: true);

        // Act
        var result = content.ReplaceVariants(ContentCulture.Restore("fr-FR"), [enUs, ptBr]);

        // Assert
        result.IsFailure.ShouldBeTrue();
        content.SourceLanguage.ShouldBe(PublicContentCultures.EnglishUnitedStates);
        content.PublicationState.ShouldBe(PublicContentPublicationState.Draft);
    }

    [Fact]
    public void ReplaceVariants_stores_exactly_the_new_variants_and_recomputes_the_publication_state()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: false);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: true);

        // Act
        var result = content.ReplaceVariants(PublicContentCultures.PortugueseBrazil, [enUs, ptBr]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        content.SourceLanguage.ShouldBe(PublicContentCultures.PortugueseBrazil);
        content.Variants.Count.ShouldBe(2);
        content.Variants.ShouldContain(ptBr);
        content.PublicationState.ShouldBe(PublicContentPublicationState.ReviewRequired);
    }

    [Fact]
    public void ReplaceWith_rejects_missing_replacement_content()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        Action replace = () => content.ReplaceWith(null!);

        // Act
        var exception = replace.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("replacement");
    }

    [Fact]
    public void TryResolvePublicVariant_finds_nothing_when_neither_the_requested_language_nor_english_is_approved()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);
        var enUs = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.EnglishUnitedStates, requiresHumanReview: true);
        var ptBr = EditablePublicContentTestFactory.CreateVariant(PublicContentCultures.PortugueseBrazil, requiresHumanReview: true);
        content.ReplaceVariants(PublicContentCultures.EnglishUnitedStates, [enUs, ptBr]).IsSuccess.ShouldBeTrue();

        // Act
        var found = content.TryResolvePublicVariant(PublicContentCultures.PortugueseBrazil, out var variant);

        // Assert
        found.ShouldBeFalse();
        variant.ShouldBeNull();
    }
}
