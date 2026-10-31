using ImageMagick;
using PandaBear.ImageProcessing;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Documents what the shared image processor does with animated input; the Catalog pipeline accepts animated uploads.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
public sealed class MediaAnimatedImageProcessingTests
{
    [Theory]
    [InlineData(ImageOutputFormat.WebP)]
    [InlineData(ImageOutputFormat.Jpeg)]
    [InlineData(ImageOutputFormat.Avif)]
    public void Process_turns_an_animated_webp_into_a_single_frame_variant(ImageOutputFormat format)
    {
        // Arrange
        var animated = CatalogTestImages.CreateAnimatedWebP(64, 32);
        using (var source = new MagickImageCollection(animated))
        {
            source.Count.ShouldBe(2);
        }

        // Act
        var result = MagickImageProcessor.Process(
            new ImageProcessingRequest(
                new MemoryStream(animated),
                [new ImageVariantRequest("v", format, 32, 80, 32)],
                ImageProcessingLimits.WebDefault),
            TestContext.Current.CancellationToken);

        // Assert
        using var frames = new MagickImageCollection(result.Variants.ShouldHaveSingleItem().Content.ToArray());
        frames.Count.ShouldBe(1);
    }
}

