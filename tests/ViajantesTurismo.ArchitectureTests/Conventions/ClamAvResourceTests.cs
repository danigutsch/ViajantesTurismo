using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging.Abstractions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class ClamAvResourceTests
{
    [Fact]
    public async Task AddClamAv_creates_a_private_typed_resource_with_pinned_image_and_health_check()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var clamAv = builder.AddClamAv("clamav");
        var image = clamAv.Resource.Annotations.OfType<ContainerImageAnnotation>().ShouldHaveSingleItem();
        var endpoint = clamAv.Resource.Annotations.OfType<EndpointAnnotation>().ShouldHaveSingleItem();
        var healthCheck = clamAv.Resource.Annotations.OfType<HealthCheckAnnotation>().ShouldHaveSingleItem();
        var mounts = clamAv.Resource.Annotations.OfType<ContainerMountAnnotation>().ToArray();
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "clamav", 3310, "3310");
        var destination = builder.AddResource(new ContainerResource("consumer")).WithClamAvReference(clamAv);
        var environmentConfiguration = await ExecutionConfigurationBuilder
            .Create(destination.Resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(
                new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
                NullLogger.Instance,
                TestContext.Current.CancellationToken);

        // Assert
        clamAv.Resource.ShouldBeOfType<ClamAvResource>();
        image.Registry.ShouldBe("docker.io");
        image.Image.ShouldBe("clamav/clamav");
        image.SHA256.ShouldBe("6f4a9e7d616ffc8d1070200fe35ac860735fdd522161a1043f94856e6ee13c28");
        endpoint.Name.ShouldBe(ClamAvResource.TcpEndpointName);
        endpoint.TargetPort.ShouldBe(3310);
        endpoint.UriScheme.ShouldBe("tcp");
        endpoint.IsExternal.ShouldBeFalse();
        mounts.ShouldBeEmpty();
        healthCheck.Key.ShouldBe("clamav-tcp-ping");
        clamAv.Resource.Annotations.OfType<ContainerLifetimeAnnotation>().ShouldBeEmpty();
        environmentConfiguration.EnvironmentVariablesWithUnprocessed
            .Where(variable => variable.Key == "MalwareScanning__ClamAv__Host")
            .ShouldHaveSingleItem();
        environmentConfiguration.EnvironmentVariablesWithUnprocessed
            .Where(variable => variable.Key == "MalwareScanning__ClamAv__Port")
            .ShouldHaveSingleItem();
    }

    [Fact]
    public async Task WithFreshClam_false_disables_the_update_daemon()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        var clamAv = builder.AddClamAv("clamav").WithFreshClam(false);

        // Act
        var environmentConfiguration = await ExecutionConfigurationBuilder
            .Create(clamAv.Resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(
                new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
                NullLogger.Instance,
                TestContext.Current.CancellationToken);
        var freshClamSetting = environmentConfiguration.EnvironmentVariablesWithUnprocessed
            .Where(variable => variable.Key == "CLAMAV_NO_FRESHCLAMD")
            .ShouldHaveSingleItem();

        // Assert
        freshClamSetting.Value.Processed.ShouldBe("true");
    }
}
