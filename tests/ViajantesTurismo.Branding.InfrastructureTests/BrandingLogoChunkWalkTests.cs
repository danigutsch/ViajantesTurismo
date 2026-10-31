using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class BrandingLogoChunkWalkTests
{
    private const int PngSignature = LogoImageBytes.PngSignatureLength;
    private const int PngAfterIhdr = PngSignature + LogoImageBytes.PngIhdrChunkLength;
    private const int PngHeaderAfterIhdr = PngAfterIhdr + 8;
    private const int WebPChunkStart = LogoImageBytes.WebPRiffHeaderLength;
    private const int WebPAfterVp8X = WebPChunkStart + 8 + 10;
    private const int WebPAfterVp8 = WebPAfterVp8X + 8 + 16;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Every_png_truncation_is_decided_deterministically_without_throwing(bool animated)
    {
        // Arrange
        var png = LogoImageBytes.HandBuiltPng(animated);

        for (var cut = 0; cut <= png.Length; cut++)
        {
            // Act
            using var stream = new MemoryStream(png, 0, cut);
            var refused = BrandingLogoIntake.HasAnimationControlChunk(stream);

            // Assert
            var onlyCleanBoundariesAreStill = cut is PngSignature or PngAfterIhdr;
            var pastTheDecisiveHeader = cut >= PngHeaderAfterIhdr;
            var expected = animated ? pastTheDecisiveHeader || !onlyCleanBoundariesAreStill : !pastTheDecisiveHeader && !onlyCleanBoundariesAreStill;
            refused.ShouldBe(expected, $"cut at {cut}");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Every_webp_truncation_is_decided_deterministically_without_throwing(bool animationFlag)
    {
        // Arrange
        var webp = LogoImageBytes.HandBuiltWebP(animationFlag);

        for (var cut = 0; cut <= webp.Length; cut++)
        {
            // Act
            using var stream = new MemoryStream(webp, 0, cut);
            var refused = BrandingLogoIntake.HasWebPAnimation(stream);

            // Assert
            var cleanBoundary = cut is WebPChunkStart or WebPAfterVp8X or WebPAfterVp8;
            var expected = animationFlag
                ? cut != WebPChunkStart
                : !cleanBoundary;
            refused.ShouldBe(expected, $"cut at {cut}");
        }
    }

    [Fact]
    public void A_complete_still_png_and_webp_are_not_refused()
    {
        // Arrange
        using var png = new MemoryStream(LogoImageBytes.HandBuiltPng(animated: false));
        using var webp = new MemoryStream(LogoImageBytes.HandBuiltWebP(animationFlag: false));

        // Act
        var pngRefused = BrandingLogoIntake.HasAnimationControlChunk(png);
        var webpRefused = BrandingLogoIntake.HasWebPAnimation(webp);

        // Assert
        pngRefused.ShouldBeFalse();
        webpRefused.ShouldBeFalse();
    }
}
