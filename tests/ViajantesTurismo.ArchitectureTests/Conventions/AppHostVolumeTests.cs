using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostVolumeTests
{
    [Fact]
    public void Development_apphost_keeps_only_the_clamav_definitions_cache_by_default()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Full);
        var clamAv = builder.Resources.OfType<ClamAvResource>().ShouldHaveSingleItem();
        var seaweedFs = builder.Resources.OfType<SeaweedFsResource>().ShouldHaveSingleItem();
        var postgres = builder.Resources.OfType<PostgresServerResource>().ShouldHaveSingleItem();

        // Assert
        clamAv.Annotations.OfType<ContainerMountAnnotation>().ShouldHaveSingleItem().Source.ShouldBe("clamav-definitions");
        seaweedFs.Annotations.OfType<ContainerMountAnnotation>().ShouldBeEmpty();
        postgres.Annotations.OfType<ContainerMountAnnotation>().ShouldBeEmpty();
        AppHostVolumeTestsHelpers.IsPersistent(seaweedFs).ShouldBeFalse();
        AppHostVolumeTestsHelpers.IsPersistent(postgres).ShouldBeFalse();
        builder.Resources.OfType<ParameterResource>().ShouldNotContain(parameter => parameter.Name == "database-server-password");
    }

    [Fact]
    public void Persistent_data_opt_in_keeps_postgresql_and_seaweedfs_in_volumes_with_a_stable_password()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration["AppHost:PersistentData"] = "true";

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Full);
        var seaweedFs = builder.Resources.OfType<SeaweedFsResource>().ShouldHaveSingleItem();
        var postgres = builder.Resources.OfType<PostgresServerResource>().ShouldHaveSingleItem();
        var redis = builder.Resources.OfType<RedisResource>().ShouldHaveSingleItem();

        // Assert
        seaweedFs.Annotations.OfType<ContainerMountAnnotation>().ShouldHaveSingleItem().Type.ShouldBe(ContainerMountType.Volume);
        postgres.Annotations.OfType<ContainerMountAnnotation>().ShouldHaveSingleItem().Type.ShouldBe(ContainerMountType.Volume);
        AppHostVolumeTestsHelpers.IsPersistent(seaweedFs).ShouldBeTrue();
        AppHostVolumeTestsHelpers.IsPersistent(postgres).ShouldBeTrue();
        AppHostVolumeTestsHelpers.IsPersistent(redis).ShouldBeTrue();
        redis.Annotations.OfType<ContainerMountAnnotation>().ShouldBeEmpty();
        postgres.PasswordParameter.Name.ShouldBe("database-server-password");
    }

    [Fact]
    public void Realm_import_directory_comes_from_configuration()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        var directory = Path.Combine(Path.GetTempPath(), "realm-import");
        builder.Configuration[AppHost.AppHostResourceExtensions.RealmImportDirectoryConfigurationKey] = directory;
        var clientSecret = builder.AddParameter("client-secret", secret: true);

        // Act
        var identity = AppHost.AppHostResourceExtensions.AddRunModeIdentityProvider(builder, clientSecret).ShouldNotBeNull();

        // Assert
        identity.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .ShouldHaveSingleItem(mount => mount.Target == "/opt/keycloak/data/import")
            .Source.ShouldBe(directory);
    }
}
