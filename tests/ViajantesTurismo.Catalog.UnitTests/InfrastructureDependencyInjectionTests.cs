using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SharedKernel.Testing;
using SharedKernel.EventSourcing;
using SharedKernel.EventSourcing.Npgsql;
using SharedKernel.MalwareScanning;
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
        scenario.ShouldResolveAs<IMediaObjectStore, LocalMediaObjectStore>();
        scenario.ShouldResolveAs<IMediaUploadScanner, MalwareScannerMediaUploadScanner>();
        scenario.ShouldResolveAs<IMediaUploadValidator, MediaUploadValidator>();
        scenario.ShouldResolveAs<IEventSerializer, CatalogEventSerializer>();
        scenario.ShouldResolveAs<IEventStore, PostgreSqlEventStore>();
        scenario.ShouldResolveAs<IProjectionCheckpointStore, PostgreSqlProjectionCheckpointStore>();
        scenario.ShouldResolveEnumerableItemAs<IProjection, CatalogTourReadModelProjection>();
        scenario.ShouldResolve<CatalogProjectionRunner>();
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
        scenario.ShouldResolveAs<IMediaObjectStore, SeaweedFsMediaObjectStore>();
        scenario.ShouldResolveSingleton<IMediaObjectStore>();
    }

    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public void Seaweedfs_storage_registers_aws_tracing_and_metrics()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateSeaweedFsScenario();
        _ = scenario.ShouldResolve<TracerProvider>();
        _ = scenario.ShouldResolve<MeterProvider>();
        using var activitySource = new ActivitySource("AWSSDK.S3");

        // Act
        using var activity = activitySource.StartActivity("S3.PutObject", ActivityKind.Client);

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

        mappedOutboxEntity.FindAnnotation("Relational:Schema")?.Value.ShouldBe("messaging");
        mappedTransportEntity.FindAnnotation("Relational:Schema")?.Value.ShouldBe("messaging");
        mappedTransportEntity.IsTableExcludedFromMigrations().ShouldBeFalse();
    }
}
