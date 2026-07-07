using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing;
using SharedKernel.Testing.Assertions;
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
    public void Catalog_api_infrastructure_does_not_start_the_catalog_projection_worker()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act
        var includesProjectionWorker = scenario.ContainsHostedService<CatalogProjectionHostedService>();

        // Assert
        includesProjectionWorker.ShouldBeFalse();
    }
}
