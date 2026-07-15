using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.DomainEvents;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
public sealed class AdminInfrastructureModuleTests
{
    [Fact]
    public void AddApplication_requires_an_integration_event_outbox_to_resolve_domain_dispatching()
    {
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithoutOutbox();
        Action resolveDispatcher = () => serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        var exception = resolveDispatcher.ShouldThrow<InvalidOperationException>();

        exception.Message.ShouldContain(nameof(IDomainEventIntegrationEventOutbox), StringComparison.Ordinal);
    }

    [Fact]
    public void AddIntegrationEventOutbox_composes_generated_domain_dispatching_dependencies()
    {
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithOutboxModule();

        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        dispatcher.ShouldNotBeNull();
    }

    [Fact]
    public void Admin_write_context_resolves_with_composed_modules()
    {
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithWriteContext();

        var dbContext = serviceProvider.GetRequiredService<AdminWriteDbContext>();

        dbContext.ShouldNotBeNull();
    }

    [Fact]
    public void AddIntegrationEventOutbox_preserves_existing_outbox_registration()
    {
        var outbox = new CapturingIntegrationEventOutbox(new FakeUnitOfWork());
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithOutbox(outbox);

        var registeredOutbox = serviceProvider.GetRequiredService<IIntegrationEventOutbox>();

        registeredOutbox.ShouldBeSameAs(outbox);
    }

    [Fact]
    public void AddInfrastructure_registers_admin_runtime_services()
    {
        // Arrange
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithInfrastructureModule();

        // Act
        var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
        var queryService = serviceProvider.GetRequiredService<IQueryService>();
        var tourStore = serviceProvider.GetRequiredService<ITourStore>();
        var customerStore = serviceProvider.GetRequiredService<ICustomerStore>();
        var documentStore = serviceProvider.GetRequiredService<IDocumentStore>();
        var outbox = serviceProvider.GetRequiredService<IIntegrationEventOutbox>();
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();

        // Assert
        unitOfWork.ShouldBeOfType<AdminWriteDbContext>();
        queryService.ShouldBeOfType<QueryService>();
        tourStore.ShouldBeOfType<TourStore>();
        customerStore.ShouldBeOfType<CustomerStore>();
        documentStore.ShouldBeOfType<DocumentStore>();
        outbox.ShouldBeOfType<EfIntegrationEventOutbox<AdminWriteDbContext>>();
        hostedServices.ShouldContain(service => service is DocumentDraftRetentionHostedService);
        hostedServices.ShouldContain(service => (service is IntegrationEventOutboxRelayHostedService<AdminWriteDbContext>));
    }

    [Fact]
    public void Explicit_openapi_generation_registration_omits_admin_background_workers()
    {
        // Arrange
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithOpenApiBuildGenerationInfrastructureModule();

        // Act
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();

        // Assert
        hostedServices.ShouldNotContain(service => service is DocumentDraftRetentionHostedService);
        hostedServices.ShouldNotContain(service => service is IntegrationEventOutboxRelayHostedService<AdminWriteDbContext>);
    }

    [Fact]
    public void AddAdminSeeding_registers_seeder_without_outbox_relay()
    {
        // Arrange
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithSeedingModule();

        // Act
        var seeder = serviceProvider.GetRequiredService<Seeder>();
        var outbox = serviceProvider.GetRequiredService<IIntegrationEventOutbox>();
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();

        // Assert
        seeder.ShouldNotBeNull();
        outbox.ShouldBeOfType<EfIntegrationEventOutbox<AdminWriteDbContext>>();
        hostedServices.ShouldNotContain(service => (service is IntegrationEventOutboxRelayHostedService<AdminWriteDbContext>));
    }
}
