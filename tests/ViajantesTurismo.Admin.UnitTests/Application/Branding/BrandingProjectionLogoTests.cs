using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CategoryName, Infrastructure.TestTraits.CoreBehaviorCategory)]
public sealed class BrandingProjectionLogoTests
{
    [Fact]
    public void A_logo_prints_its_address_type_and_size_but_not_its_bytes()
    {
        // Arrange
        var logo = new BrandingProjectionLogo(DocumentLogoTestData.Sha256, "image/webp", new byte[40 * 1024]);

        // Act
        var text = logo.ToString();

        // Assert
        text.ShouldContain(DocumentLogoTestData.Sha256, StringComparison.Ordinal);
        text.ShouldContain("ContentLength = 40960", StringComparison.Ordinal);
        text.Length.ShouldBeLessThan(300);
    }

    [Fact]
    public void A_projection_state_does_not_print_the_logo_bytes()
    {
        // Arrange
        var state = BrandingProjectionState.From(BrandingProjectionTestData.CreateEventWithLogo(1, DocumentLogoTestData.Content));

        // Act
        var text = state.ToString();

        // Assert
        text.ShouldContain("ContentLength = ", StringComparison.Ordinal);
        text.ShouldNotContain("System.Byte[]", StringComparison.Ordinal);
    }

    [Fact]
    public void Logos_with_equal_bytes_in_different_buffers_are_equal()
    {
        // Arrange
        var first = DocumentLogoTestData.LogoFor([1, 2, 3]);
        var second = DocumentLogoTestData.LogoFor([1, 2, 3]);

        // Act
        var equal = first.Equals(second);

        // Assert
        equal.ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Logos_with_the_same_address_but_different_bytes_are_not_equal()
    {
        // Arrange
        var first = new BrandingProjectionLogo(DocumentLogoTestData.Sha256, "image/webp", new byte[] { 1, 2, 3 });
        var second = new BrandingProjectionLogo(DocumentLogoTestData.Sha256, "image/webp", new byte[] { 1, 2, 4 });

        // Act
        var equal = first.Equals(second);

        // Assert
        equal.ShouldBeFalse();
    }
}
