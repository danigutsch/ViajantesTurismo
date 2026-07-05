using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents;
using SharedKernel.IntegrationEvents;
using SharedKernel.Testing;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Infrastructure;
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

        exception.Message.ShouldContain(nameof(IIntegrationEventOutbox), StringComparison.Ordinal);
    }

    [Fact]
    public void AddIntegrationEventOutboxModule_composes_generated_domain_dispatching_dependencies()
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
    public void AddIntegrationEventOutboxModule_preserves_existing_outbox_registration()
    {
        var outbox = new CapturingIntegrationEventOutbox(new FakeUnitOfWork());
        using var serviceProvider = AdminInfrastructureModuleTestServices.CreateWithOutbox(outbox);

        var registeredOutbox = serviceProvider.GetRequiredService<IIntegrationEventOutbox>();

        registeredOutbox.ShouldBeSameAs(outbox);
    }
}
