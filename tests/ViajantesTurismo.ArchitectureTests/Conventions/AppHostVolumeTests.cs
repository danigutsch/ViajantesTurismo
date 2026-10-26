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

        // Assert
        clamAv.Annotations.OfType<ContainerMountAnnotation>().ShouldHaveSingleItem().Source.ShouldBe("clamav-definitions");
        seaweedFs.Annotations.OfType<ContainerMountAnnotation>().ShouldHaveSingleItem().Source.ShouldBe("seaweedfs-data");
    }
}
