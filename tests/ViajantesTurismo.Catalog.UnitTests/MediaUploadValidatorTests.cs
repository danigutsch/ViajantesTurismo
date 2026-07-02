using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaUploadValidatorTests
{
    [Theory]
    [InlineData("photo.jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF })]
    [InlineData("photo.png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    [InlineData("photo.webp", "image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 })]
    [InlineData("photo.avif", "image/avif", new byte[] { 0x00, 0x00, 0x00, 0x00, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66 })]
    public void Validate_accepts_allowed_upload_signatures(string fileName, string contentType, byte[] headerBytes)
    {
        // Arrange
        headerBytes.ShouldNotBeNull();
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            fileName,
            contentType,
            headerBytes.Length,
            headerBytes);

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_accepts_content_type_with_outer_whitespace()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.jpg",
            " image/jpeg ",
            3,
            new byte[] { 0xFF, 0xD8, 0xFF });

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
            3,
            new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.FileName));
        errors.Keys.ShouldContain(nameof(request.HeaderBytes));
    }

    [Fact]
    public void Validate_rejects_oversized_uploads()
    {
        // Arrange
        var validator = new MediaUploadValidator(new MediaUploadValidationOptions { MaxLengthBytes = 2 });
        var request = new MediaUploadValidationRequest(
            "photo.jpg",
            "image/jpeg",
            3,
            new byte[] { 0xFF, 0xD8, 0xFF });

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
            length,
            new byte[] { 0xFF, 0xD8, 0xFF });

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
            6,
            "GIF89a"u8.ToArray());

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.ContentType));
        errors.Keys.ShouldContain(nameof(request.HeaderBytes));
    }

    [Fact]
    public void Validate_rejects_extension_mismatch()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.png",
            "image/jpeg",
            3,
            new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.FileName));
    }

    [Fact]
    public void Validate_rejects_incomplete_webp_header()
    {
        // Arrange
        var validator = new MediaUploadValidator();
        var request = new MediaUploadValidationRequest(
            "photo.webp",
            "image/webp",
            4,
            "RIFF"u8.ToArray());

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.HeaderBytes));
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
            3,
            new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var errors = validator.Validate(request);

        // Assert
        errors.Keys.ShouldContain(nameof(request.FileName));
    }
}
