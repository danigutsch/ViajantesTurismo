using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaUploadValidationOptionsValidatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_fails_when_max_length_is_not_positive(long maxLengthBytes)
    {
        // Arrange
        using var fixture = new CatalogMediaTestServices(options => options.MaxLengthBytes = maxLengthBytes);

        // Act
        Action action = () => _ = fixture.ValidationOptions;

        // Assert
        var exception = action.ShouldThrow<OptionsValidationException>();
        exception.Message.ShouldContain("Media upload maximum length must be greater than zero.", StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_fails_when_no_content_types_are_allowed()
    {
        // Arrange
        using var fixture = new CatalogMediaTestServices(options =>
            options.AllowedExtensionsByContentType = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

        // Act
        Action action = () => _ = fixture.ValidationOptions;

        // Assert
        var exception = action.ShouldThrow<OptionsValidationException>();
        exception.Message.ShouldContain("At least one media upload content type must be allowed.", StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_fails_when_decoded_width_exceeds_the_web_limit()
    {
        // Arrange
        using var fixture = new CatalogMediaTestServices(options =>
            options.MaxDecodedWidth = MediaUploadValidationOptions.DefaultMaxDecodedWidth + 1);

        // Act
        Action action = () => _ = fixture.ValidationOptions;

        // Assert
        var exception = action.ShouldThrow<OptionsValidationException>();
        exception.Message.ShouldContain("Media upload maximum decoded width must not exceed", StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_fails_when_decoded_height_exceeds_the_web_limit()
    {
        // Arrange
        using var fixture = new CatalogMediaTestServices(options =>
            options.MaxDecodedHeight = MediaUploadValidationOptions.DefaultMaxDecodedHeight + 1);

        // Act
        Action action = () => _ = fixture.ValidationOptions;

        // Assert
        var exception = action.ShouldThrow<OptionsValidationException>();
        exception.Message.ShouldContain("Media upload maximum decoded height must not exceed", StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_fails_when_decoded_pixel_count_exceeds_the_web_limit()
    {
        // Arrange
        using var fixture = new CatalogMediaTestServices(options =>
            options.MaxDecodedPixelCount = MediaUploadValidationOptions.DefaultMaxDecodedPixelCount + 1);

        // Act
        Action action = () => _ = fixture.ValidationOptions;

        // Assert
        var exception = action.ShouldThrow<OptionsValidationException>();
        exception.Message.ShouldContain("Media upload maximum decoded pixel count must not exceed", StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_succeeds_when_options_are_valid()
    {
        // Arrange
        using var fixture = new CatalogMediaTestServices();

        // Act
        var validator = fixture.Validator;

        // Assert
        validator.ShouldBeOfType<MediaUploadValidator>();
    }
}
