using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class LocalMediaObjectStorageOptionsValidatorTests
{
    [Fact]
    public void Validate_fails_when_public_base_uri_is_null()
    {
        // Arrange
        var options = new LocalMediaObjectStorageOptions();
        var property = typeof(LocalMediaObjectStorageOptions).GetProperty(nameof(LocalMediaObjectStorageOptions.PublicBaseUri));
        var validator = new LocalMediaObjectStorageOptionsValidator();
        property.ShouldNotBeNull();
        property.SetValue(options, null);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBe(false);
    }

    [Fact]
    public void Validate_fails_when_root_path_is_blank()
    {
        // Arrange
        var options = new LocalMediaObjectStorageOptions { RootPath = " " };
        var validator = new LocalMediaObjectStorageOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBe(false);
    }

    [Fact]
    public void Validate_succeeds_when_options_are_valid()
    {
        // Arrange
        var options = new LocalMediaObjectStorageOptions();
        var validator = new LocalMediaObjectStorageOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBe(true);
    }
}
