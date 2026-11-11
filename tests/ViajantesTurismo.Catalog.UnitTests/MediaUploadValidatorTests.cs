using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaUploadValidatorTests
{
    [Fact]
    public void Validate_accepts_content_type_with_outer_whitespace()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.jpg",
            " image/jpeg ",
            3);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_rejects_spoofed_content_type()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.jpg",
            "image/png",
            3);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.FileName));
    }

    [Fact]
    public void Validate_rejects_oversized_uploads()
    {
        // Arrange
        var validator = new MediaUploadValidator(new MediaUploadValidationOptions { MaxLengthBytes = 2 });
        var request = new MediaUploadValidationRequest(
            "photo.jpg",
            "image/jpeg",
            3);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.Length));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_empty_or_negative_uploads(long length)
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.jpg",
            "image/jpeg",
            length);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.Length));
    }

    [Fact]
    public void Validate_rejects_unknown_content_type()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.gif",
            "image/gif",
            6);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.ContentType));
    }

    [Fact]
    public void Validate_rejects_extension_mismatch()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.png",
            "image/jpeg",
            3);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.FileName));
    }

    [Fact]
    public void Validate_rejects_null_request()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var method = typeof(MediaUploadValidator).GetMethod(nameof(MediaUploadValidator.Validate)).ShouldNotBeNull();

        // Act
        Action action = () => method.Invoke(validator, [null]);

        // Assert
        var exception = action.ShouldThrowInner<ArgumentNullException>();
        exception.ParamName.ShouldBe("request");
    }

    [Theory]
    [InlineData("../photo.jpg")]
    [InlineData("..\\photo.jpg")]
    [InlineData("dir/photo.jpg")]
    [InlineData("dir\\photo.jpg")]
    public void Validate_rejects_path_like_file_names(string fileName)
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            fileName,
            "image/jpeg",
            3);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.FileName));
    }
}
