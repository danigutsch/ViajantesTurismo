using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class ClamAvMediaUploadScannerOptionsValidatorTests
{
    [Fact]
    public void Validate_fails_when_timeout_exceeds_the_cancellation_token_limit()
    {
        // Arrange
        var options = new ClamAvMediaUploadScannerOptions { Timeout = TimeSpan.MaxValue };
        var validator = new ClamAvMediaUploadScannerOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBe(false);
    }

    [Fact]
    public void Validate_succeeds_when_timeout_is_supported_by_the_cancellation_token_source()
    {
        // Arrange
        var options = new ClamAvMediaUploadScannerOptions { Timeout = TimeSpan.FromDays(1) };
        var validator = new ClamAvMediaUploadScannerOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBe(true);
    }
}
