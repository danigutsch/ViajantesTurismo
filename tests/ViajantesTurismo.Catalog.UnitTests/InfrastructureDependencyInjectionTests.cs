using SharedKernel.Testing;
using SharedKernel.EventSourcing;
using SharedKernel.EventSourcing.Npgsql;
using Npgsql;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Projections;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
public sealed class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddCatalogInfrastructure_registers_catalog_services()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act

        // Assert
        scenario.ShouldResolve<CatalogDbContext>();
        scenario.ShouldResolveSingleton<NpgsqlDataSource>();
        scenario.ShouldResolveAs<IPublicContentStore, EfPublicContentStore>();
        scenario.ShouldResolveAs<IMediaObjectStore, LocalMediaObjectStore>();
        scenario.ShouldResolveAs<IMediaUploadScanner, ClamAvMediaUploadScanner>();
        scenario.ShouldResolveAs<IMediaUploadValidator, MediaUploadValidator>();
        scenario.ShouldResolveAs<IEventSerializer, CatalogEventSerializer>();
        scenario.ShouldResolveAs<IEventStore, PostgreSqlEventStore>();
        scenario.ShouldResolveAs<IProjectionCheckpointStore, PostgreSqlProjectionCheckpointStore>();
        scenario.ShouldResolveEnumerableItemAs<IProjection, CatalogTourReadModelProjection>();
        scenario.ShouldResolve<CatalogProjectionRunner>();
    }

    [Fact]
    public void AddCatalogInfrastructure_configures_development_catalog_options()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateDevelopmentScenario();

        // Act

        // Assert
        scenario.ShouldResolveDbContextOptions<CatalogDbContext>();
        scenario.ShouldResolveAs<IMediaUploadScanner, NoOpMediaUploadScanner>();
    }

    [Fact]
    public void AddCatalogInfrastructure_uses_clamav_when_development_configures_it()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateConfiguredDevelopmentScenario();

        // Assert
        scenario.ShouldResolveAs<IMediaUploadScanner, ClamAvMediaUploadScanner>();
    }

    [Fact]
    public void AddCatalogInfrastructure_uses_singleton_seaweedfs_store_when_storage_is_configured()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateSeaweedFsScenario();

        // Act

        // Assert
        scenario.ShouldResolveAs<IMediaObjectStore, SeaweedFsMediaObjectStore>();
        scenario.ShouldResolveSingleton<IMediaObjectStore>();
    }
}
