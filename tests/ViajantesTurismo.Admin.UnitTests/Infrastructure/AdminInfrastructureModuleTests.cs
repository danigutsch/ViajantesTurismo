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
    public void AddAdminSeeding_registers_seeder_without_outbox_relay()
    {
        // Arrange
        using var services = AdminInfrastructureModuleTestServices.CreateWithSeedingModule();

        // Act
        var seeder = services.Seeder;
        var outbox = services.Outbox;
        var dispatcher = services.Dispatcher;
        var hostedServices = services.HostedServices;

        // Assert
        seeder.ShouldNotBeNull();
        outbox.ShouldBeOfType<EfIntegrationEventOutbox<AdminWriteDbContext>>();
        dispatcher.ShouldNotBeNull();
        hostedServices.ShouldNotContain(service => (service is IntegrationEventOutboxRelayHostedService<AdminWriteDbContext>));
    }
}
