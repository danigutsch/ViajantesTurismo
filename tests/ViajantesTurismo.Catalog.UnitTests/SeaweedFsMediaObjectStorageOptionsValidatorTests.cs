using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class SeaweedFsMediaObjectStorageOptionsValidatorTests
{
    [Theory]
    [InlineData("endpoint")]
    [InlineData("relative-endpoint")]
    [InlineData("unsupported-scheme")]
    [InlineData("bucket")]
    [InlineData("access-key")]
    [InlineData("secret-key")]
    [InlineData("timeout")]
    [InlineData("public-base-uri")]
    public void Validate_fails_for_invalid_required_storage_configuration(string invalidField)
    {
        // Arrange
        var options = SeaweedFsMediaObjectStorageOptionsTestFactory.CreateValidOptions();
        switch (invalidField)
        {
            case "endpoint":
                options.Endpoint = null;
                break;
            case "relative-endpoint":
                options.Endpoint = new Uri("seaweedfs", UriKind.Relative);
                break;
            case "unsupported-scheme":
                options.Endpoint = new Uri("ftp://seaweedfs:8333");
                break;
            case "bucket":
                options.Bucket = " ";
                break;
            case "access-key":
                options.AccessKey = " ";
                break;
            case "secret-key":
                options.SecretKey = " ";
                break;
            case "timeout":
                options.BucketProvisioningTimeout = TimeSpan.Zero;
                break;
            case "public-base-uri":
                options.PublicBaseUri = null!;
                break;
        }

        var validator = new SeaweedFsMediaObjectStorageOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBe(false);
    }

    [Fact]
    public void Validate_succeeds_for_complete_storage_configuration()
    {
        // Arrange
        var validator = new SeaweedFsMediaObjectStorageOptionsValidator();

        // Act
        var result = validator.Validate(null, SeaweedFsMediaObjectStorageOptionsTestFactory.CreateValidOptions());

        // Assert
        result.Succeeded.ShouldBe(true);
    }

    [Fact]
    public void Validate_fails_when_bucket_provisioning_timeout_exceeds_the_cancellation_token_limit()
    {
        // Arrange
        var options = SeaweedFsMediaObjectStorageOptionsTestFactory.CreateValidOptions();
        options.BucketProvisioningTimeout = TimeSpan.MaxValue;
        var validator = new SeaweedFsMediaObjectStorageOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBe(false);
    }
}
