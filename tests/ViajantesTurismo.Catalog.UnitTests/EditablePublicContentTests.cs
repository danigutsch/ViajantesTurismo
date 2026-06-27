using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;

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
        Assert.True(result.IsSuccess);
        Assert.Equal("HOME.HERO", result.Value.Key);
        Assert.Equal(PublicContentLanguage.EnUs, result.Value.SourceLanguage);
        Assert.Contains(enUs, result.Value.Variants);
        Assert.Contains(ptBr, result.Value.Variants);
        Assert.Equal(PublicContentPublicationState.Draft, result.Value.PublicationState);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(PublicContentPublicationState.ReviewRequired, result.Value.PublicationState);
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
        Assert.True(result.IsFailure);
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
        Assert.True(result.IsSuccess);
        Assert.Equal("HOME.HERO", result.Value.Key);
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
        Assert.True(result.IsFailure);
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
        Assert.True(result.IsFailure);
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
        Assert.True(result.IsFailure);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains(nameof(EditablePublicContent.Variants), result.ErrorDetails.ValidationErrors?.Keys ?? []);
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
        Assert.True(result.IsFailure);
        Assert.Equal(PublicContentPublicationState.Draft, content.PublicationState);
        Assert.Contains(content.Variants, variant => variant.Language == PublicContentLanguage.PtBr);
    }

    [Fact]
    public void Publish_marks_reviewed_content_as_published()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        var result = content.Publish();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PublicContentPublicationState.Published, content.PublicationState);
    }

    [Fact]
    public void Publish_rejects_content_that_still_requires_human_review()
    {
        // Arrange
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        var result = content.Publish();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PublicContentPublicationState.ReviewRequired, content.PublicationState);
    }

    [Fact]
    public async Task InMemory_store_gets_content_by_sanitized_key()
    {
        // Arrange
        var store = new InMemoryPublicContentStore();
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);
        await store.SaveContent(content, TestContext.Current.CancellationToken);

        // Act
        var saved = await store.GetContent($"  {content.Key}  ", TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(content, saved);
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
        Assert.True(result.IsFailure);
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
        Assert.True(result.IsFailure);
    }
}
