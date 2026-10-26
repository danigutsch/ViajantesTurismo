using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostVolumeTests
{
    [Fact]
    public void Development_apphost_keeps_the_named_volumes_of_clamav_and_seaweedfs()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Full);
        var clamAv = builder.Resources.OfType<ClamAvResource>().ShouldHaveSingleItem();
        var seaweedFs = builder.Resources.OfType<SeaweedFsResource>().ShouldHaveSingleItem();
        var clamAvMounts = clamAv.Annotations.OfType<ContainerMountAnnotation>().ToArray();
        var seaweedFsMounts = seaweedFs.Annotations.OfType<ContainerMountAnnotation>().ToArray();

        // Assert
        clamAvMounts.ShouldHaveSingleItem().Source.ShouldBe("clamav-definitions");
        seaweedFsMounts.ShouldHaveSingleItem().Source.ShouldBe("seaweedfs-data");
    }
}
