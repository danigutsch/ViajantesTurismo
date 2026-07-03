using SharedKernel.Testing.Assertions;

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
        var readyResult = readyImage.HasPublicVariants();
        var pendingResult = pendingImage.HasPublicVariants();
        var noVariantsResult = readyImageWithoutVariants.HasPublicVariants();

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
}
