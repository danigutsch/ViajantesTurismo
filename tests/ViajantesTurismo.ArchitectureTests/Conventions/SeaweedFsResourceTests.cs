using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class SeaweedFsResourceTests
{
    [Fact]
    public void AddSeaweedFs_uses_name_scoped_data_volume_by_default()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var seaweedFs = builder.AddSeaweedFs("seaweed");
        var volume = seaweedFs.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .ShouldHaveSingleItem();

        // Assert
        volume.Source.ShouldBe("seaweed-data");
        volume.Target.ShouldBe("/data");
        volume.Type.ShouldBe(ContainerMountType.Volume);
        seaweedFs.Resource.Annotations
            .OfType<ContainerLifetimeAnnotation>()
            .ShouldBeEmpty();
    }

    [Fact]
    public void AddSeaweedFs_scopes_data_volume_with_dcp_resource_name_suffix()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration["DcpPublisher:ResourceNameSuffix"] = "test";

        // Act
        var seaweedFs = builder.AddSeaweedFs("seaweed");
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
        builder.Configuration["DcpPublisher:ResourceNameSuffix"] = " ";

        // Act
        var seaweedFs = builder.AddSeaweedFs("seaweed");
        var volume = seaweedFs.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .ShouldHaveSingleItem();

        // Assert
        volume.Source.ShouldBe("seaweed-data");
    }
}
