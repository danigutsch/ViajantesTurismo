using SharedKernel.ObjectStorage;
using SharedKernel.ObjectStorage.S3;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SharedKernel.Testing;
using SharedKernel.EventSourcing;
using SharedKernel.EventSourcing.Npgsql;
using SharedKernel.MalwareScanning;
using Npgsql;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.UnitTests.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
public sealed class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void Catalog_infrastructure_preserves_consumer_clock_registration()
    {
        // Arrange
        var clock = new FakeTimeProvider(new DateTimeOffset(2030, 2, 3, 14, 0, 0, TimeSpan.Zero));
        // Act
        using var scenario = CatalogInfrastructureTestServices.CreateScenario(services => services.AddSingleton<TimeProvider>(clock));

        // Assert
        scenario.ShouldResolve<TimeProvider>().ShouldBeSameAs(clock);
    }

    [Fact]
    public void Catalog_infrastructure_preserves_consumer_read_model_registration()
    {
        // Arrange
        var store = new StubCatalogTourReadModelStore();

        // Act
        using var scenario = CatalogInfrastructureTestServices.CreateScenario(services => services.AddSingleton<ICatalogTourReadModelStore>(store));

        // Assert
        scenario.ShouldResolve<ICatalogTourReadModelStore>().ShouldBeSameAs(store);
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void Catalog_worker_gates_sensitive_logging_for_catalog_and_transport_contexts(string environmentName, bool expected)
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateWorkerScenario(environmentName);

        // Act
        var catalogSensitiveLogging = scenario.IsSensitiveDataLoggingEnabled<CatalogDbContext>();
        var transportSensitiveLogging = scenario.IsSensitiveDataLoggingEnabled<CatalogIntegrationTransportDbContext>();

        // Assert
        catalogSensitiveLogging.ShouldBe(expected);
        transportSensitiveLogging.ShouldBe(expected);
    }

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
        scenario.ShouldResolveAs<ICatalogTourReadModelStore, EfCatalogTourReadModelStore>();
        scenario.ShouldResolveAs<IPublicMediaImageStore, EfPublicMediaImageStore>();
        scenario.ShouldResolveAs<IObjectStore, LocalMediaObjectStore>();
        scenario.ShouldResolveAs<IMediaUploadScanner, MalwareScannerMediaUploadScanner>();
        scenario.ShouldResolveAs<IMediaUploadValidator, MediaUploadValidator>();
        scenario.ShouldResolve<MediaImageUploadIntake>();
        scenario.ShouldResolve<MediaObjectReconciliationService>();
        scenario.ShouldResolveAs<IEventSerializer, CatalogEventSerializer>();
        scenario.ShouldResolveAs<IEventStore, PostgreSqlEventStore>();
        scenario.ShouldResolveAs<IProjectionCheckpointStore, PostgreSqlProjectionCheckpointStore>();
        scenario.ShouldResolveEnumerableItemAs<IProjection, CatalogTourReadModelProjection>();
        scenario.ShouldResolve<CatalogProjectionRunner>();
    }

    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public void AddCatalogInfrastructure_rejects_missing_durable_media_storage_in_production()
    {
        // Arrange
        Action action = CatalogInfrastructureTestServices.AddProductionInfrastructureWithoutMediaStorage;

        // Act
        var exception = action.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe(
            $"Catalog durable media object storage requires configuration section '{MediaObjectStorageConfiguration.SectionName}'.");
    }

    [Fact]
    public void AddCatalogInfrastructure_allows_explicitly_disabled_development_scanning()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateDevelopmentScenario();

        // Act

        // Assert
        scenario.ShouldResolveDbContextOptions<CatalogDbContext>();
        scenario.ShouldResolveAs<IMediaUploadScanner, MalwareScannerMediaUploadScanner>();
        scenario.ShouldResolve<IMalwareScanner>();
    }

    [Fact]
    public void AddCatalogInfrastructure_uses_clamav_when_development_configures_it()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateConfiguredDevelopmentScenario();

        // Assert
        scenario.ShouldResolveAs<IMediaUploadScanner, MalwareScannerMediaUploadScanner>();
    }

    [Fact]
    public void AddCatalogInfrastructure_uses_singleton_seaweedfs_store_when_storage_is_configured()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateSeaweedFsScenario();

        // Act

        // Assert
        scenario.ShouldResolveAs<IObjectStore, S3ObjectStore>();
        scenario.ShouldResolveSingleton<IObjectStore>();
    }

    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public void Seaweedfs_storage_registers_object_storage_tracing_and_aws_metrics()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateSeaweedFsScenario();
        _ = scenario.ShouldResolve<TracerProvider>();
        _ = scenario.ShouldResolve<MeterProvider>();
        using var activitySource = new ActivitySource(S3ObjectStoreTelemetry.Name);

        // Act
        using var activity = activitySource.StartActivity("object_storage.put", ActivityKind.Client);

        // Assert
        activity.ShouldNotBeNull();
    }

    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public void Catalog_persistence_registers_npgsql_tracing_and_metrics()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();
        _ = scenario.ShouldResolve<TracerProvider>();
        _ = scenario.ShouldResolve<MeterProvider>();
        using var activitySource = new ActivitySource("Npgsql");

        // Act
        using var activity = activitySource.StartActivity("postgresql.query", ActivityKind.Client);

        // Assert
        activity.ShouldNotBeNull();
    }

    [Fact]
    public void AddCatalogDatabaseInitialization_does_not_require_malware_scanner_configuration()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateSeedingScenario();

        // Act

        // Assert
        var catalogDbContext = scenario.ShouldResolve<CatalogDbContext>();
        scenario.ShouldResolveSingleton<NpgsqlDataSource>();
        var designTimeModel = catalogDbContext.GetService<IDesignTimeModel>().Model;
        var outboxEntity = designTimeModel.GetEntityTypes().SingleOrDefault(
            entity => entity.FindAnnotation("Relational:TableName")?.Value?.Equals("outbox_messages") == true);
        var transportEntity = designTimeModel.GetEntityTypes().SingleOrDefault(
            entity => entity.FindAnnotation("Relational:TableName")?.Value?.Equals("transport_messages") == true);
        var mappedOutboxEntity = outboxEntity
            ?? throw new InvalidOperationException("The integration-event outbox entity is not mapped.");
        var mappedTransportEntity = transportEntity
            ?? throw new InvalidOperationException("The integration-event transport entity is not mapped.");

        mappedOutboxEntity.FindAnnotation("Relational:Schema").ShouldNotBeNull().Value.ShouldBe("messaging");
        mappedTransportEntity.FindAnnotation("Relational:Schema").ShouldNotBeNull().Value.ShouldBe("messaging");
        mappedTransportEntity.IsTableExcludedFromMigrations().ShouldBeFalse();
    }
}
