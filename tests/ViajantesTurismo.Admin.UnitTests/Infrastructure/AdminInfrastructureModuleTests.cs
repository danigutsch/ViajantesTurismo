using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
public sealed class AdminInfrastructureModuleTests
{
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
    public void Remove_unused_admin_idempotency_migration_refuses_unexpected_rows()
    {
        // Arrange
        var migrationType = typeof(AdminWriteDbContext).Assembly.GetType(
            "ViajantesTurismo.Admin.Infrastructure.Migrations.RemoveUnusedAdminIdempotencyKeys");

        // Act
        var migration = Activator.CreateInstance(migrationType.ShouldNotBeNull()).ShouldBeAssignableTo<Migration>();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var upMethod = migration.GetType().GetMethod("Up", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .ShouldNotBeNull();

        upMethod.Invoke(migration, [migrationBuilder]);
        var guard = migrationBuilder.Operations.OfType<SqlOperation>().ShouldHaveSingleItem();
        var droppedTables = migrationBuilder.Operations.OfType<DropTableOperation>()
            .Select(static operation => operation.Name)
            .ToArray();

        // Assert
        guard.Sql.ShouldContain("RAISE EXCEPTION", StringComparison.Ordinal);
        guard.Sql.ShouldContain("messaging.idempotency_keys", StringComparison.Ordinal);
        droppedTables.ShouldContain("idempotency_keys");
        droppedTables.ShouldNotContain("outbox_messages");
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
        var customerStore = services.CustomerStore;
        var documentStore = services.DocumentStore;
        var outbox = services.Outbox;
        var brandingApiClient = services.BrandingApiClient;
        var hostedServices = services.HostedServices;

        // Assert
        unitOfWork.ShouldBeOfType<AdminWriteDbContext>();
        queryService.ShouldBeOfType<QueryService>();
        tourStore.ShouldBeOfType<TourStore>();
        customerStore.ShouldBeOfType<CustomerStore>();
        documentStore.ShouldBeOfType<DocumentStore>();
        outbox.ShouldBeOfType<EfIntegrationEventOutbox<AdminWriteDbContext>>();
        brandingApiClient.ShouldBeOfType<FakeBrandingApiClient>();
        hostedServices.ShouldContain(service => service is DocumentDraftRetentionHostedService);
        hostedServices.ShouldContain(service => service is DocumentAuditRetentionHostedService);
        hostedServices.ShouldContain(service => (service is IntegrationEventOutboxRelayHostedService<AdminWriteDbContext>));
    }

    [Fact]
    public void Admin_command_handlers_are_resolved_as_scoped_direct_handlers()
    {
        // Arrange
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule();

        // Act
        var hasScopedLifetime = serviceProvider.CreateTourHandlerHasScopedLifetime();

        // Assert
        hasScopedLifetime.ShouldBeTrue();
    }

    [Fact]
    public void Explicit_openapi_generation_registration_omits_admin_background_workers()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithOpenApiBuildGenerationInfrastructureModule();

        // Act
        var hostedServices = services.HostedServices;

        // Assert
        hostedServices.ShouldNotContain(service => service is DocumentDraftRetentionHostedService);
        hostedServices.ShouldNotContain(service => service is DocumentAuditRetentionHostedService);
        hostedServices.ShouldNotContain(service => service is IntegrationEventOutboxRelayHostedService<AdminWriteDbContext>);
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
        outbox.ShouldBeOfType<EfIntegrationEventOutbox<AdminWriteDbContext>>();
        dispatcher.ShouldNotBeNull();
        hostedServices.ShouldNotContain(service => (service is IntegrationEventOutboxRelayHostedService<AdminWriteDbContext>));
    }
}
