using Microsoft.EntityFrameworkCore;
using SharedKernel.Idempotency;
using SharedKernel.Idempotency.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
public sealed class CatalogIntegrationEventTransportRegistrationTests
{
    [Fact]
    public void Catalog_api_infrastructure_does_not_start_the_admin_transport_consumer()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesTransportConsumer = scenario.ContainsHostedService<PostgreSqlIntegrationEventTransportConsumerHostedService<CatalogIntegrationTransportDbContext>>();

        // Assert
        includesTransportConsumer.ShouldBeFalse();
    }

    [Fact]
    public void Catalog_api_infrastructure_starts_the_catalog_outbox_relay()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<IntegrationEventOutboxRelayHostedService<CatalogDbContext>>();

        // Assert
        includesCatalogOutboxRelay.ShouldBeTrue();
    }

    [Fact]
    public void Catalog_composition_explicitly_registers_inbox_idempotency()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var idempotencyStore = scenario.ShouldResolve<IIdempotencyStore>();

        // Assert
        idempotencyStore.ShouldNotBeNull();
    }

    [Fact]
    public void Catalog_production_composition_maps_idempotency_keys_to_the_messaging_schema()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();
        var dbContext = scenario.ShouldResolve<CatalogDbContext>();

        // Act
        var entityType = dbContext.Model.FindEntityType(typeof(IdempotencyEntryEntity)).ShouldNotBeNull();
        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema();

        // Assert
        tableName.ShouldBe("idempotency_keys");
        schema.ShouldBe("messaging");
    }

    [Fact]
    public void Hosted_transport_mode_registers_the_admin_transport_consumer()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateApiHostedTransportScenario();

        // Act
        var includesTransportConsumer = scenario.ContainsHostedService<PostgreSqlIntegrationEventTransportConsumerHostedService<CatalogIntegrationTransportDbContext>>();

        // Assert
        includesTransportConsumer.ShouldBeTrue();
    }

    [Fact]
    public void Hosted_transport_mode_registers_the_admin_transport_context_options()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateApiHostedTransportScenario();

        // Act

        // Assert
        scenario.ShouldResolveDbContextOptions<CatalogIntegrationTransportDbContext>();
    }

    [Fact]
    public void Standalone_worker_registers_transport_consumer_without_catalog_outbox_relay()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateWorkerScenario();

        // Act
        var includesTransportConsumer = scenario.ContainsHostedService<PostgreSqlIntegrationEventTransportConsumerHostedService<CatalogIntegrationTransportDbContext>>();
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<IntegrationEventOutboxRelayHostedService<CatalogDbContext>>();

        // Assert
        includesTransportConsumer.ShouldBeTrue();
        includesCatalogOutboxRelay.ShouldBeFalse();
    }

    [Fact]
    public void Database_initialization_infrastructure_does_not_start_catalog_outbox_relay()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateSeedingScenario();

        // Act
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<IntegrationEventOutboxRelayHostedService<CatalogDbContext>>();

        // Assert
        includesCatalogOutboxRelay.ShouldBeFalse();
    }

    [Fact]
    public void Explicit_openapi_generation_registration_omits_catalog_outbox_relay()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateOpenApiBuildGenerationScenario();

        // Act
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<IntegrationEventOutboxRelayHostedService<CatalogDbContext>>();

        // Assert
        includesCatalogOutboxRelay.ShouldBeFalse();
    }

    [Fact]
    public void Database_initialization_infrastructure_does_not_start_catalog_projection_workers()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateSeedingScenario();

        // Act
        var includesProjectionWorker = scenario.ContainsHostedService<CatalogProjectionHostedService>();
        var includesReconciliationWorker = scenario.ContainsHostedService<MediaObjectReconciliationHostedService>();

        // Assert
        includesProjectionWorker.ShouldBeFalse();
        includesReconciliationWorker.ShouldBeFalse();
    }

    [Fact]
    public void Standalone_worker_registers_catalog_projection_hosted_service()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateWorkerScenario();

        // Act
        var includesProjectionWorker = scenario.ContainsHostedService<CatalogProjectionHostedService>();

        // Assert
        includesProjectionWorker.ShouldBeTrue();
    }

    [Fact]
    public void Standalone_worker_registers_media_object_reconciliation_hosted_service()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateWorkerScenario();

        // Act
        var includesReconciliationWorker = scenario.ContainsHostedService<MediaObjectReconciliationHostedService>();

        // Assert
        includesReconciliationWorker.ShouldBeTrue();
    }

    [Fact]
    public void Catalog_api_infrastructure_does_not_start_the_catalog_projection_worker()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesProjectionWorker = scenario.ContainsHostedService<CatalogProjectionHostedService>();

        // Assert
        includesProjectionWorker.ShouldBeFalse();
    }

    [Fact]
    public void Catalog_api_infrastructure_does_not_start_the_media_object_reconciliation_worker()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesReconciliationWorker = scenario.ContainsHostedService<MediaObjectReconciliationHostedService>();

        // Assert
        includesReconciliationWorker.ShouldBeFalse();
    }
}
