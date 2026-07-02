using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Testing.Assertions;
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
        using var provider = CatalogMediaTestServices.CreateProvider(options => options.MaxLengthBytes = maxLengthBytes);

        // Act
        Action action = () => _ = provider.GetRequiredService<IOptions<MediaUploadValidationOptions>>().Value;

        // Assert
        var exception = action.ShouldThrow<OptionsValidationException>();
        exception.Message.ShouldContain("Media upload maximum length must be greater than zero.");
    }

    [Fact]
    public void Validate_fails_when_no_content_types_are_allowed()
    {
        // Arrange
        using var provider = CatalogMediaTestServices.CreateProvider(options =>
            options.AllowedExtensionsByContentType = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

        // Act
        Action action = () => _ = provider.GetRequiredService<IOptions<MediaUploadValidationOptions>>().Value;

        // Assert
        var exception = action.ShouldThrow<OptionsValidationException>();
        exception.Message.ShouldContain("At least one media upload content type must be allowed.");
    }

    [Fact]
    public void Validate_succeeds_when_options_are_valid()
    {
        // Arrange
        using var provider = CatalogMediaTestServices.CreateProvider();

        // Act
        var validator = provider.GetRequiredService<IMediaUploadValidator>();

        // Assert
        validator.ShouldBeOfType<MediaUploadValidator>();
    }
}
