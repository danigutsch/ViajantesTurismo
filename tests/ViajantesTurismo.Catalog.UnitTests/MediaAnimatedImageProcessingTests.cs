using ImageMagick;
using PandaBear.ImageProcessing;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Documents what the shared image processor does with animated input; the Catalog pipeline accepts animated uploads.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
public sealed class MediaAnimatedImageProcessingTests
{
    [Theory]
    [InlineData("webp", ImageOutputFormat.WebP)]
    [InlineData("webp", ImageOutputFormat.Jpeg)]
    [InlineData("webp", ImageOutputFormat.Avif)]
    [InlineData("apng", ImageOutputFormat.WebP)]
    [InlineData("apng", ImageOutputFormat.Jpeg)]
    [InlineData("apng", ImageOutputFormat.Avif)]
    public void Process_turns_an_animated_input_into_a_single_frame_variant(string input, ImageOutputFormat format)
    {
        // Arrange
        var animated = input == "webp" ? CatalogTestImages.CreateAnimatedWebP(64, 32) : CatalogTestImages.CreateAnimatedPng(64, 32);
        if (input == "webp")
        {
            using var source = new MagickImageCollection(animated);
            source.Count.ShouldBe(2);
        }
        else
        {
            System.Text.Encoding.Latin1.GetString(animated).ShouldContain("acTL", StringComparison.Ordinal);
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

