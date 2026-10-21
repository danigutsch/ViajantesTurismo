using Amazon.S3;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class S3MediaObjectStorageConfigurationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Uses_cloud_configuration_instead_of_local_section_when_both_exist(bool includeSeaweedFsSection)
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateS3Scenario(includeSeaweedFsSection);
        var store = scenario.ShouldResolve<IMediaObjectStore>();
        var request = new MediaObjectUploadRequest("pending/image.jpg", "image/jpeg", 123, TimeSpan.FromMinutes(5));

        // Act
        var ticket = await store.CreateUploadUrl(request, TestContext.Current.CancellationToken);
        var publicUri = store.GetPublicUri("tours/image 1.jpg");

        // Assert
        ticket.UploadUri.Host.ShouldBe("cloud-media.s3.eu-central-1.amazonaws.com");
        var query = Uri.UnescapeDataString(ticket.UploadUri.Query);
        query.ShouldContain("/eu-central-1/s3/aws4_request", StringComparison.Ordinal);
        publicUri.AbsoluteUri.ShouldBe("https://cdn.example.com/published/tours/image%201.jpg");
        scenario.ShouldResolveSingleton<IMediaObjectStore>();
    }

    [Fact]
    public void Binds_cloud_region_credentials_and_preprovisioned_bucket_policy()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateS3Scenario(includeSeaweedFsSection: true);

        // Act
        var options = scenario.ShouldResolve<IOptions<SeaweedFsMediaObjectStorageOptions>>().Value;
        var client = scenario.ShouldResolve<IAmazonS3>();

        // Assert
        options.AuthenticationRegion.ShouldBe("eu-central-1");
        options.ForcePathStyle.ShouldBeFalse();
        options.Bucket.ShouldBe("cloud-media");
        options.AutoProvisionBucket.ShouldBeFalse();
        options.AccessKey.ShouldBe("test-access");
        options.SecretKey.ShouldBe("test-secret");
        client.Config.ServiceURL.ShouldBe("https://s3.eu-central-1.amazonaws.com/");
    }

    [Fact]
    public void Rejects_missing_signing_region_before_creating_cloud_client()
    {
        // Arrange
        var options = SeaweedFsMediaObjectStorageOptionsTestFactory.CreateValidOptions();
        options.AuthenticationRegion = " ";
        var validator = new SeaweedFsMediaObjectStorageOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldBe("S3 authentication region must be provided.");
    }
}
