using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging.Abstractions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class SeaweedFsResourceTests
{
    [Fact]
    public async Task AddSeaweedFs_creates_a_pinned_image_without_a_volume()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var seaweedFs = builder.AddSeaweedFs("seaweed", "media");
        var image = seaweedFs.Resource.Annotations
            .OfType<ContainerImageAnnotation>()
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
        seaweedFs.Resource.Annotations.OfType<ContainerMountAnnotation>().ShouldBeEmpty();
        seaweedFs.Resource.BucketParameter.Name.ShouldBe("seaweed-bucket");
        bucketEnvironment.Value.Unprocessed.ShouldBeSameAs(seaweedFs.Resource.BucketParameter);
        bucketEnvironment.Value.Processed.ShouldBe("media");
        seaweedFs.Resource.Annotations
            .OfType<ContainerLifetimeAnnotation>()
            .ShouldBeEmpty();
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
