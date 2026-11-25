using ViajantesTurismo.Admin.Infrastructure.Customers;
using ViajantesTurismo.Admin.Infrastructure.Tours;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(TestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
public sealed class AdminInfrastructureModuleTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void AddInfrastructure_gates_sensitive_logging_for_admin_contexts(string environmentName, bool expected)
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule(environmentName);

        // Act
        var writeSensitiveLogging = services.IsSensitiveDataLoggingEnabled<AdminWriteDbContext>();
        var readSensitiveLogging = services.IsSensitiveDataLoggingEnabled<AdminReadDbContext>();

        // Assert
        writeSensitiveLogging.ShouldBe(expected);
        readSensitiveLogging.ShouldBe(expected);
    }

    [Fact]
    [TestFocus(TestFocus.Security)]
    public void AddInfrastructure_registers_npgsql_tracing_and_metrics()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule();

        // Act
        var sourceEnabled = services.IsActivitySourceEnabled("Npgsql");
        var hasMeterProvider = services.HasMeterProvider();

        // Assert
        sourceEnabled.ShouldBeTrue();
        hasMeterProvider.ShouldBeTrue();
    }

    [Fact]
    public void AddInfrastructure_configures_a_retrying_execution_strategy_for_the_write_context()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule();

        // Act
        var strategy = services.WriteContext.Database.CreateExecutionStrategy();

        // Assert
        strategy.ShouldBeOfType<NpgsqlRetryingExecutionStrategy>();
        strategy.RetriesOnFailure.ShouldBeTrue();
    }

    [Fact]
    public void AddInfrastructure_shares_the_write_context_with_the_transaction_behavior()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule();

        // Act
        var transactionContext = services.TransactionContext;

        // Assert
        transactionContext.ShouldBeSameAs(services.WriteContext);
    }

    [Fact]
    public void AddApplication_requires_an_integration_event_outbox_to_resolve_domain_dispatching()
    {
        using var services = AdminInfrastructureModuleTestServices.CreateWithoutOutbox();
        Func<object?> resolveDispatcher = () => services.Dispatcher;

        var exception = resolveDispatcher.ShouldThrow<InvalidOperationException>();

        exception.Message.ShouldContain(nameof(IDomainEventIntegrationEventOutbox), StringComparison.Ordinal);
    }

    [Fact]
    public void AddIntegrationEventOutbox_composes_generated_domain_dispatching_dependencies()
    {
        using var services = AdminInfrastructureModuleTestServices.CreateWithOutboxModule();

        var dispatcher = services.Dispatcher;

        dispatcher.ShouldNotBeNull();
    }

    [Fact]
    public void Admin_write_context_resolves_with_composed_modules()
    {
        using var services = AdminInfrastructureModuleTestServices.CreateWithWriteContext();

        var dbContext = services.WriteContext;

        dbContext.ShouldNotBeNull();
    }

    [Fact]
    public void AddIntegrationEventOutbox_preserves_existing_outbox_registration()
    {
        var outbox = new CapturingIntegrationEventOutbox(new FakeUnitOfWork());
        using var services = AdminInfrastructureModuleTestServices.CreateWithOutbox(outbox);

        var registeredOutbox = services.Outbox;

        registeredOutbox.ShouldBeSameAs(outbox);
    }

    [Fact]
    public void AddIntegrationEventOutbox_does_not_register_inbox_idempotency()
    {
        // Arrange
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithOutboxModule();

        // Act
        var idempotencyStore = serviceProvider.IdempotencyStore;
        var configurations = serviceProvider.DbContextConfigurations
            .Select(static configuration => configuration.GetType().Name)
            .ToArray();

        // Assert
        idempotencyStore.ShouldBeNull();
        configurations.ShouldNotContain("IdempotencyDbContextConfiguration`1");
    }

    [Fact]
    public void Admin_write_context_model_does_not_include_an_inbox_table()
    {
        // Arrange
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithWriteContext();
        var dbContext = serviceProvider.WriteContext;

        // Act
        var entityTypeNames = dbContext.Model.GetEntityTypes()
            .Select(static entityType => entityType.ClrType.Name)
            .ToArray();

        // Assert
        entityTypeNames.ShouldNotContain("IdempotencyEntryEntity");
    }

    [Fact]
    public void AddInfrastructure_registers_admin_runtime_services()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule();

        // Act
        var unitOfWork = services.UnitOfWork;
        var queryService = services.QueryService;
        var tourStore = services.TourStore;
        var tourCommandStore = services.TourCommandStore;
        var customerStore = services.CustomerStore;
        var documentStore = services.DocumentStore;
        var outbox = services.Outbox;
        var idempotencyStore = services.IdempotencyStore;
        var brandingProjectionStore = services.BrandingProjectionStore;
        var hostedServices = services.HostedServices;

        // Assert
        unitOfWork.ShouldBeOfType<AdminWriteDbContext>();
        queryService.ShouldBeOfType<QueryService>();
        tourStore.ShouldBeOfType<TourStore>();
        tourCommandStore.ShouldBeOfType<TourCommandStore>();
        customerStore.ShouldBeOfType<CustomerStore>();
        documentStore.ShouldBeOfType<DocumentStore>();
        outbox.ShouldNotBeNull();
        idempotencyStore.ShouldNotBeNull();
        brandingProjectionStore.ShouldBeOfType<EfBrandingProjectionStore>();
        hostedServices.OfType<DurableDocumentMaintenanceHostedService>().ShouldHaveSingleItem();
        services.HasOutboxRelay.ShouldBeTrue();
    }

    [Fact]
    public void Admin_stores_are_resolved_with_scoped_lifetime()
    {
        // Arrange
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule();

        // Act
        var hasScopedLifetime = serviceProvider.TourStoreHasScopedLifetime();

        // Assert
        hasScopedLifetime.ShouldBeTrue();
    }

    [Fact]
    public void AddInfrastructure_preserves_supplied_tour_stores()
    {
        // Arrange
        var suppliedStore = new FakeTourStore();
        using var services = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule(tourStore: suppliedStore);

        // Act
        var aggregateStore = services.TourStore;
        var commandStore = services.TourCommandStore;

        // Assert
        aggregateStore.ShouldBeSameAs(suppliedStore);
        commandStore.ShouldBeSameAs(suppliedStore);
    }

    [Fact]
    public void AddInfrastructure_preserves_a_supplied_customer_store()
    {
        // Arrange
        var suppliedStore = new FakeCustomerStore();
        using var services = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule(customerStore: suppliedStore);

        // Act
        var customerStore = services.CustomerStore;

        // Assert
        customerStore.ShouldBeSameAs(suppliedStore);
    }

    [Fact]
    public void Tour_persistence_requires_the_admin_write_context()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithTourPersistenceOnly();
        Action resolveCommandStore = () => _ = services.TourCommandStore;

        // Act
        var error = resolveCommandStore.ShouldThrow<InvalidOperationException>();

        // Assert
        error.Message.ShouldContain(nameof(AdminWriteDbContext), StringComparison.Ordinal);
    }

    [Fact]
    public void Explicit_OpenApi_generation_registration_omits_admin_background_workers()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithOpenApiBuildGenerationInfrastructureModule();

        // Act
        var hostedServices = services.HostedServices;

        // Assert
        hostedServices.ShouldNotContain(service => service is DurableDocumentMaintenanceHostedService);
        services.HasOutboxRelay.ShouldBeFalse();
    }

    [Fact]
    public void AddAdminDatabaseInitialization_registers_development_data_initializer_without_outbox_relay()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithDatabaseInitializationModule();

        // Act
        var initializer = services.Initializer;
        var outbox = services.Outbox;
        var dispatcher = services.Dispatcher;
        var hostedServices = services.HostedServices;

        // Assert
        initializer.ShouldNotBeNull();
        outbox.ShouldNotBeNull();
        dispatcher.ShouldNotBeNull();
        hostedServices.ShouldNotContain(service => service.GetType().IsGenericType
            && service.GetType().GetGenericTypeDefinition().Name == "IntegrationEventOutboxRelayHostedService`1"
            && service.GetType().GenericTypeArguments.ShouldHaveSingleItem() == typeof(AdminWriteDbContext));
    }
}
