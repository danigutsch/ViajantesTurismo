using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PublicMediaImageTests
{
    [Fact]
    public void Has_public_variants_requires_ready_status_and_responsive_variants()
    {
        // Arrange
        var readyImage = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);
        var pendingImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);

        // Act
        var readyResult = readyImage.HasPublicVariants;
        var pendingResult = pendingImage.HasPublicVariants;

        // Assert
        readyResult.ShouldBe(true);
        pendingResult.ShouldBe(false);
    }

    [Fact]
    public void Has_public_variants_for_processing_version_requires_matching_ready_variants()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateReadyImageForProcessingVersion(Guid.CreateVersion7(), 2);

        // Act
        var matchingVersionResult = image.HasPublicVariantsForProcessingVersion(2);
        var differentVersionResult = image.HasPublicVariantsForProcessingVersion(1);

        // Assert
        matchingVersionResult.ShouldBe(true);
        differentVersionResult.ShouldBe(false);
    }

    [Fact]
    public void Has_public_variants_for_processing_version_ignores_query_and_fragment_matches()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        var image = PublicMediaImageTestFactory.CreateReadyImageWithVariantUri(
            imageId,
            new Uri($"https://cdn.example/media/{imageId:N}/v1/640-jpeg.jpg?next=/v2/#/v2/"));

        // Act
        var result = image.HasPublicVariantsForProcessingVersion(2);

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public void Should_process_original_requires_missing_or_outdated_public_variants()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateReadyImageForProcessingVersion(Guid.CreateVersion7(), 2);
        var pendingImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);

        // Act
        var currentVersionResult = image.ShouldProcessOriginal(2);
        var outdatedVersionResult = image.ShouldProcessOriginal(3);
        var pendingResult = pendingImage.ShouldProcessOriginal(2);

        // Assert
        currentVersionResult.ShouldBe(false);
        outdatedVersionResult.ShouldBe(true);
        pendingResult.ShouldBe(true);
    }

    [Fact]
    public void Can_retain_public_variants_after_processing_failure_requires_existing_public_variants()
    {
        // Arrange
        var readyImage = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);
        var pendingImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);

        // Act
        var readyResult = readyImage.CanRetainPublicVariantsAfterProcessingFailure;
        var pendingResult = pendingImage.CanRetainPublicVariantsAfterProcessingFailure;

        // Assert
        readyResult.ShouldBe(true);
        pendingResult.ShouldBe(false);
    }

    [Fact]
    public void Ai_generated_accessibility_text_requires_human_review_before_publication()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);

        // Act
        var result = image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "Cyclists riding near a mountain trail", "Mountain cycling tour");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        image.RequiresHumanReview.ShouldBeTrue();
        image.IsAiGenerated.ShouldBeTrue();
        image.HasPublicVariants.ShouldBeFalse();
        var text = image.AccessibilityTexts.ShouldHaveSingleItem();
        text.RequiresHumanReview.ShouldBeTrue();
        text.IsAiGenerated.ShouldBeTrue();
    }

    [Fact]
    public void Reviewed_accessibility_text_allows_publication()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);
        image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "Cyclists riding near a mountain trail", null).IsSuccess.ShouldBeTrue();

        // Act
        var result = image.SetReviewedAccessibilityText(PublicContentLanguage.EnUs, "Cyclists riding near a mountain trail", null, isDecorative: false);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        image.RequiresHumanReview.ShouldBeFalse();
        image.IsAiGenerated.ShouldBeFalse();
        image.HasPublicVariants.ShouldBeTrue();
    }

    [Fact]
    public void Decorative_images_can_publish_without_alt_text_after_review()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);

        // Act
        var result = image.SetReviewedAccessibilityText(PublicContentLanguage.EnUs, null, null, isDecorative: true);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        image.IsDecorative.ShouldBeTrue();
        image.AltText.ShouldBe(string.Empty);
        image.HasPublicVariants.ShouldBeTrue();
    }

    [Fact]
    public void Reviewed_non_decorative_images_require_alt_text()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);

        // Act
        var result = image.SetReviewedAccessibilityText(PublicContentLanguage.EnUs, null, null, isDecorative: false);

        // Assert
        result.IsFailure.ShouldBeTrue();
        image.HasPublicVariants.ShouldBeTrue();
    }

    [Fact]
    public void Ai_draft_accessibility_text_requires_alt_text()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);

        // Act
        var result = image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, string.Empty, null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        image.HasPublicVariants.ShouldBeTrue();
    }

    [Fact]
    public void Ai_draft_accessibility_text_cannot_mark_images_decorative()
    {
        // Arrange
        const PublicContentLanguage language = PublicContentLanguage.EnUs;

        // Act
        var result = PublicMediaImageAccessibilityText.Create(
            language,
            altText: null,
            caption: null,
            isDecorative: true,
            requiresHumanReview: true,
            isAiGenerated: true);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Manual_draft_accessibility_text_requires_human_review_without_ai_flag()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);

        // Act
        var result = image.SetDraftAccessibilityText(PublicContentLanguage.EnUs, "Editor draft image description", "Editor draft caption");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        image.RequiresHumanReview.ShouldBeTrue();
        image.IsAiGenerated.ShouldBeFalse();
        image.HasPublicVariants.ShouldBeFalse();
        var text = image.AccessibilityTexts.Single(accessibilityText => accessibilityText.Language == PublicContentLanguage.EnUs);
        text.RequiresHumanReview.ShouldBeTrue();
        text.IsAiGenerated.ShouldBeFalse();
    }

    [Fact]
    public void Pt_br_ai_draft_does_not_replace_default_public_accessibility_text()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, altText: "Reviewed default alt");

        // Act
        var result = image.SetAiDraftAccessibilityText(PublicContentLanguage.PtBr, "Rascunho em português", null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        image.AltText.ShouldBe("Reviewed default alt");
        image.RequiresHumanReview.ShouldBeFalse();
        image.IsAiGenerated.ShouldBeFalse();
        image.HasPublicVariants.ShouldBeTrue();
    }

    [Fact]
    public void Accessibility_text_can_be_localized_independently_for_review()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);

        // Act
        var result = image.SetAiDraftAccessibilityText(PublicContentLanguage.PtBr, "Ciclistas em uma trilha de montanha", "Passeio de bicicleta");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        image.AccessibilityTexts.Count.ShouldBe(2);
        var localized = image.AccessibilityTexts.Single(text => text.Language == PublicContentLanguage.PtBr);
        localized.RequiresHumanReview.ShouldBeTrue();
        localized.IsAiGenerated.ShouldBeTrue();
    }

    [Fact]
    public void Gallery_placement_exposes_cover_and_display_order_for_current_view()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 3, true);

        // Act
        var isCover = image.IsCover;
        var displayOrder = image.DisplayOrder;

        // Assert
        isCover.ShouldBe(true);
        displayOrder.ShouldBe(3);
    }

    [Fact]
    public void Tour_placement_finds_cover_and_display_order_for_requested_tour()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var image = PublicMediaImageTestFactory.CreateImage(tourId, 2, false);

        // Act
        var belongsToTour = image.BelongsToTour(tourId);
        var missingTour = image.BelongsToTour(Guid.CreateVersion7());
        var isCover = image.IsCoverForTour(tourId);
        var displayOrder = image.GetDisplayOrderForTour(tourId);

        // Assert
        belongsToTour.ShouldBe(true);
        missingTour.ShouldBe(false);
        isCover.ShouldBe(false);
        displayOrder.ShouldBe(2);
    }

    [Fact]
    public void Tour_placement_returns_missing_outcome_for_unlinked_tour()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 2, false);
        var missingTourId = Guid.CreateVersion7();

        // Act
        var found = image.TryGetTourLink(missingTourId, out var link);
        var isCover = image.IsCoverForTour(missingTourId);
        var displayOrder = image.GetDisplayOrderForTour(missingTourId);

        // Assert
        found.ShouldBe(false);
        link.ShouldBeNull();
        isCover.ShouldBe(false);
        displayOrder.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void Create_persists_sanitized_metadata_and_variant_text()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var imageId = Guid.CreateVersion7();
        var metadata = new PublicMediaImageMetadata
        {
            Id = imageId,
            SourceObjectKey = "media/source.jpg",
            Checksum = "  sha256:\u0001abc  ",
            ContentType = "  image/jpeg  ",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensions(1200, 800),
            ProcessingStatus = MediaImageProcessingStatus.Ready,
            AltText = "  Cyclists\u0002 in   mountains  ",
            Caption = "  Caption\u0003 text  ",
            Attribution = "  Attribution\u0004 text  ",
            Copyright = "  Copyright\u0005 text  ",
        };
        var variants = new[]
        {
            new MediaImageResponsiveVariant("  media/one-640.jpg  ", 640, 427, "  image/jpeg  ", 1024),
        };
        var tags = new[] { "  mountain  " };
        var tourLinks = new[] { new MediaImageTourLink(tourId, 0, true) };

        // Act
        var result = PublicMediaImage.Create(metadata, variants, tags, tourLinks);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var image = result.Value;
        image.Checksum.ShouldBe("sha256:abc");
        image.ContentType.ShouldBe("image/jpeg");
        image.AltText.ShouldBe("Cyclists in mountains");
        image.Caption.ShouldBe("Caption text");
        image.Attribution.ShouldBe("Attribution text");
        image.Copyright.ShouldBe("Copyright text");
        image.ResponsiveVariants[0].ContentType.ShouldBe("image/jpeg");
        image.Tags.ShouldContain("mountain");
    }

    [Fact]
    public void Create_rejects_ready_images_without_public_variants()
    {
        // Arrange
        var metadata = new PublicMediaImageMetadata
        {
            Id = Guid.CreateVersion7(),
            SourceObjectKey = "media/source.jpg",
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensions(1200, 800),
            ProcessingStatus = MediaImageProcessingStatus.Ready,
            AltText = "Cyclists in the mountains",
        };
        var tourLinks = new[] { new MediaImageTourLink(Guid.CreateVersion7(), 0, true) };

        // Act
        var result = PublicMediaImage.Create(metadata, [], ["mountain"], tourLinks);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var errorDetails = result.ErrorDetails ?? throw new InvalidOperationException("Expected validation error details.");
        var validationErrors = errorDetails.ValidationErrors ?? throw new InvalidOperationException("Expected validation errors.");
        validationErrors.ContainsKey(nameof(PublicMediaImage.ResponsiveVariants)).ShouldBe(true);
    }

    [Theory]
    [InlineData("/media/source.jpg")]
    [InlineData("media//source.jpg")]
    [InlineData("media/./source.jpg")]
    [InlineData("media/../source.jpg")]
    [InlineData("C:/media/source.jpg")]
    public void Create_rejects_invalid_source_object_keys(string sourceObjectKey)
    {
        // Arrange
        var metadata = new PublicMediaImageMetadata
        {
            Id = Guid.CreateVersion7(),
            SourceObjectKey = sourceObjectKey,
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensions(1200, 800),
            ProcessingStatus = MediaImageProcessingStatus.Pending,
            AltText = "Cyclists in the mountains",
        };
        var tourLinks = new[] { new MediaImageTourLink(Guid.CreateVersion7(), 0, true) };

        // Act
        var result = PublicMediaImage.Create(metadata, [], ["mountain"], tourLinks);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors.ContainsKey(nameof(PublicMediaImageMetadata.SourceObjectKey)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("/media/source.jpg")]
    [InlineData("media//source.jpg")]
    [InlineData("media/./source.jpg")]
    [InlineData("media/../source.jpg")]
    [InlineData("C:/media/source.jpg")]
    public void Create_rejects_invalid_responsive_variant_object_keys(string objectKey)
    {
        // Arrange
        var metadata = new PublicMediaImageMetadata
        {
            Id = Guid.CreateVersion7(),
            SourceObjectKey = "media/source.jpg",
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensions(1200, 800),
            ProcessingStatus = MediaImageProcessingStatus.Ready,
            AltText = "Cyclists in the mountains",
        };
        var variants = new[] { new MediaImageResponsiveVariant(objectKey, 640, 427, "image/jpeg", 1024) };
        var tourLinks = new[] { new MediaImageTourLink(Guid.CreateVersion7(), 0, true) };

        // Act
        var result = PublicMediaImage.Create(metadata, variants, ["mountain"], tourLinks);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors.ContainsKey(nameof(PublicMediaImage.ResponsiveVariants)).ShouldBeTrue();
    }

    [Fact]
    public void With_processing_result_rejects_ready_images_without_public_variants()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 2048);

        // Act
        var result = image.WithProcessingResult(
            new MediaImageDimensions(1200, 800),
            MediaImageProcessingStatus.Ready,
            []);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var errorDetails = result.ErrorDetails ?? throw new InvalidOperationException("Expected validation error details.");
        var validationErrors = errorDetails.ValidationErrors ?? throw new InvalidOperationException("Expected validation errors.");
        validationErrors.ContainsKey(nameof(PublicMediaImage.ResponsiveVariants)).ShouldBe(true);
    }
}
