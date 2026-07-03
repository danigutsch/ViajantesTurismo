using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PublicMediaImageTests
{
    [Fact]
    public void Has_public_variants_requires_ready_status_and_responsive_variants()
    {
        // Arrange
        var readyImage = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);
        var pendingImage = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var readyImageWithoutVariants = PublicMediaImageTestFactory.CreateReadyImageWithoutVariants(Guid.CreateVersion7());

        // Act
        var readyResult = readyImage.HasPublicVariants;
        var pendingResult = pendingImage.HasPublicVariants;
        var noVariantsResult = readyImageWithoutVariants.HasPublicVariants;

        // Assert
        readyResult.ShouldBe(true);
        pendingResult.ShouldBe(false);
        noVariantsResult.ShouldBe(false);
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
    public void Create_persists_sanitized_metadata_and_variant_text()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var imageId = Guid.CreateVersion7();
        var metadata = new PublicMediaImageMetadata
        {
            Id = imageId,
            SourceUri = new Uri("https://cdn.example/source.jpg"),
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
            new MediaImageResponsiveVariant(new Uri("https://cdn.example/one-640.jpg"), 640, 427, "  image/jpeg  ", 1024),
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
}
