using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class BrandingLogoPathTests
{
    private static readonly string ValidHash = new('a', 64);

    [Fact]
    public void A_lowercase_sha256_is_a_content_address()
    {
        // Act
        var valid = BrandingLogoPath.IsContentAddress(ValidHash);

        // Assert
        valid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("\n")]
    [InlineData(" ")]
    [InlineData("a")]
    public void A_hash_with_a_trailing_character_is_not_a_content_address(string suffix)
    {
        // Arrange
        var candidate = ValidHash + suffix;

        // Act
        var valid = BrandingLogoPath.IsContentAddress(candidate);

        // Assert
        valid.ShouldBeFalse();
    }

    [Fact]
    public void An_uppercase_hash_is_not_a_content_address()
    {
        // Act
        var valid = BrandingLogoPath.IsContentAddress(ValidHash.ToUpperInvariant());

        // Assert
        valid.ShouldBeFalse();
    }
}
