using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.Idempotency;
using PandaBear.Messaging;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
public sealed class CatalogIntegrationEventTransportRegistrationTests
{
    [Fact]
    public async Task Catalog_relay_routes_retirement_completion_to_admin_and_keeps_media_local()
    {
        // Arrange
        var localPublisher = new CapturingCatalogOutboxPublisher();
        using var scenario = CatalogInfrastructureTestServices.CreateScenario(
            services => services.AddSingleton<IEventEnvelopePublisher>(localPublisher));
        var retirementId = Guid.CreateVersion7();
        var now = TimeProvider.System.GetUtcNow();
        var retirement = new CatalogTourRetirementAppliedIntegrationEvent(retirementId, now, Guid.CreateVersion7(), retirementId);
        var media = new MediaImageOriginalStoredIntegrationEvent(Guid.CreateVersion7(), now, Guid.CreateVersion7(), "media/original", 1);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var retirementTargets = await scenario.RouteOutboxEvent(retirement, ct);
        var mediaTargets = await scenario.RouteOutboxEvent(media, ct);

        // Assert
        retirementTargets.ShouldBe([IntegrationEventConsumerNames.Admin]);
        mediaTargets.ShouldBeEmpty();
        localPublisher.EventTypes.ShouldBe([MediaImageOriginalStoredIntegrationEvent.EventType]);
    }

    [Fact]
    public void Catalog_API_infrastructure_does_not_start_the_admin_transport_consumer()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesTransportConsumer = scenario.ContainsHostedService<CatalogIntegrationTransportDbContext>("PostgreSqlIntegrationEventTransportConsumerHostedService`1");

        // Assert
        includesTransportConsumer.ShouldBeFalse();
    }

    [Fact]
    public void Catalog_API_infrastructure_starts_the_catalog_outbox_relay()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<CatalogDbContext>("IntegrationEventOutboxRelayHostedService`1");

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
        var entityType = dbContext.Model.GetEntityTypes().ShouldHaveSingleItem(entity => entity.GetTableName() == "idempotency_keys");
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
        var includesTransportConsumer = scenario.ContainsHostedService<CatalogIntegrationTransportDbContext>("PostgreSqlIntegrationEventTransportConsumerHostedService`1");

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
        var includesTransportConsumer = scenario.ContainsHostedService<CatalogIntegrationTransportDbContext>("PostgreSqlIntegrationEventTransportConsumerHostedService`1");
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<CatalogDbContext>("IntegrationEventOutboxRelayHostedService`1");

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
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<CatalogDbContext>("IntegrationEventOutboxRelayHostedService`1");

        // Assert
        includesCatalogOutboxRelay.ShouldBeFalse();
    }

    [Fact]
    public void Explicit_OpenApi_generation_registration_omits_catalog_outbox_relay()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateOpenApiBuildGenerationScenario();

        // Act
        var includesCatalogOutboxRelay = scenario.ContainsHostedService<CatalogDbContext>("IntegrationEventOutboxRelayHostedService`1");

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
    public void Catalog_API_infrastructure_does_not_start_the_catalog_projection_worker()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesProjectionWorker = scenario.ContainsHostedService<CatalogProjectionHostedService>();

        // Assert
        includesProjectionWorker.ShouldBeFalse();
    }

    [Fact]
    public void Catalog_API_infrastructure_does_not_start_the_media_object_reconciliation_worker()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesReconciliationWorker = scenario.ContainsHostedService<MediaObjectReconciliationHostedService>();

        // Assert
        includesReconciliationWorker.ShouldBeFalse();
    }
}
