using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging.Abstractions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class SeaweedFsResourceTests
{
    private const string ResourceNameSuffixConfigurationKey = "DcpPublisher:ResourceNameSuffix";

    [Fact]
    public async Task AddSeaweedFs_uses_name_scoped_data_volume_by_default()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var seaweedFs = builder.AddSeaweedFs("seaweed", "media");
        var image = seaweedFs.Resource.Annotations
            .OfType<ContainerImageAnnotation>()
            .ShouldHaveSingleItem();
        var volume = seaweedFs.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .ShouldHaveSingleItem();
        var environmentConfiguration = await ExecutionConfigurationBuilder
            .Create(seaweedFs.Resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(
                new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
                NullLogger.Instance,
                TestContext.Current.CancellationToken);
        var bucketEnvironment = environmentConfiguration.EnvironmentVariablesWithUnprocessed
            .Where(variable => variable.Key == "S3_BUCKET")
            .ShouldHaveSingleItem();

        // Assert
        image.Registry.ShouldBe("docker.io");
        image.Image.ShouldBe("chrislusf/seaweedfs");
        image.SHA256.ShouldBe("c7d6c721b30ae711db766bbbfd40192776e263d4e51e22f57baef7bef93c12c6");
        volume.Source.ShouldBe("seaweed-data");
        volume.Target.ShouldBe("/data");
        volume.Type.ShouldBe(ContainerMountType.Volume);
        seaweedFs.Resource.BucketParameter.Name.ShouldBe("seaweed-bucket");
        bucketEnvironment.Value.Unprocessed.ShouldBeSameAs(seaweedFs.Resource.BucketParameter);
        bucketEnvironment.Value.Processed.ShouldBe("media");
        seaweedFs.Resource.Annotations
            .OfType<ContainerLifetimeAnnotation>()
            .ShouldBeEmpty();
    }

    [Fact]
    public void AddSeaweedFs_scopes_data_volume_with_dcp_resource_name_suffix()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration[ResourceNameSuffixConfigurationKey] = "test";

        // Act
        var seaweedFs = builder.AddSeaweedFs("seaweed", "media");
        var volume = seaweedFs.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .ShouldHaveSingleItem();

        // Assert
        volume.Source.ShouldBe("seaweed-test-data");
    }

    [Fact]
    public void AddSeaweedFs_uses_default_data_volume_when_dcp_resource_name_suffix_is_whitespace()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration[ResourceNameSuffixConfigurationKey] = " ";

        // Act
        var seaweedFs = builder.AddSeaweedFs("seaweed", "media");
        var volume = seaweedFs.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .ShouldHaveSingleItem();

        // Assert
        volume.Source.ShouldBe("seaweed-data");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddSeaweedFs_rejects_blank_bucket_defaults(string bucketDefault)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var action = () => builder.AddSeaweedFs("seaweed", bucketDefault);

        // Assert
        var exception = action.ShouldThrow<ArgumentException>();
        exception.ParamName.ShouldBe("bucketDefault");
    }

    [Fact]
    public void AddSeaweedFs_rejects_a_null_bucket_default()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var action = () => builder.AddSeaweedFs("seaweed", null!);

        // Assert
        var exception = action.ShouldThrow<ArgumentNullException>();
        exception.ParamName.ShouldBe("bucketDefault");
    }
}
