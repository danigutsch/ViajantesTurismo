using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
public sealed class PublicMediaImageProcessingTransitionTests
{
    [Fact]
    public void Completing_processing_makes_the_image_ready_with_the_processed_output()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 2048);
        var variant = new MediaImageResponsiveVariant("media/processed/v1/640-jpeg.jpg", 640, 427, "image/jpeg", 1024);

        // Act
        var result = image.CompleteProcessing(new MediaImageDimensions(1200, 800), [variant]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var completed = result.Value;
        completed.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Ready);
        completed.Dimensions.ShouldBe(new MediaImageDimensions(1200, 800));
        var completedVariant = completed.ResponsiveVariants.ShouldHaveSingleItem();
        completedVariant.ObjectKey.ShouldBe(variant.ObjectKey);
        completed.Id.ShouldBe(image.Id);
    }

    [Fact]
    public void Completing_processing_rejects_output_without_public_variants()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 2048);

        // Act
        var result = image.CompleteProcessing(new MediaImageDimensions(1200, 800), []);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors.ContainsKey(nameof(PublicMediaImage.ResponsiveVariants)).ShouldBeTrue();
    }

    [Fact]
    public void Failing_processing_marks_the_image_failed_without_public_variants_and_keeps_its_dimensions()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 2048);

        // Act
        var result = image.FailProcessing();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var failed = result.Value;
        failed.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Failed);
        failed.ResponsiveVariants.Count.ShouldBe(0);
        failed.Dimensions.ShouldBe(image.Dimensions);
        failed.Id.ShouldBe(image.Id);
    }

    [Fact]
    public void Failing_processing_is_rejected_while_existing_public_variants_are_retained()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);

        // Act
        var result = image.FailProcessing();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var validationErrors = result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull();
        validationErrors.ContainsKey(nameof(PublicMediaImage.ProcessingStatus)).ShouldBeTrue();
    }
}
