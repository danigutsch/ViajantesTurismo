using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.DomainEvents;
using SharedKernel.IntegrationEvents;
using SharedKernel.Testing;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Application;
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
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();
        using var serviceProvider = builder.Services.BuildServiceProvider();
        Action resolveDispatcher = () => serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        var exception = resolveDispatcher.ShouldThrow<InvalidOperationException>();

        exception.Message.ShouldContain(nameof(IIntegrationEventOutbox), StringComparison.Ordinal);
    }

    [Fact]
    public void AddIntegrationEventOutboxModule_composes_generated_domain_dispatching_dependencies()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();
        builder.Services.AddDbContext<AdminWriteDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        builder.Services.AddIntegrationEventOutboxModule();
        using var serviceProvider = builder.Services.BuildServiceProvider();

        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        dispatcher.ShouldNotBeNull();
    }

    [Fact]
    public void AddIntegrationEventOutboxModule_preserves_existing_outbox_registration()
    {
        var services = new ServiceCollection();
        var outbox = new CapturingIntegrationEventOutbox(new FakeUnitOfWork());
        services.AddSingleton<IIntegrationEventOutbox>(outbox);
        services.AddIntegrationEventOutboxModule();
        using var serviceProvider = services.BuildServiceProvider();

        var registeredOutbox = serviceProvider.GetRequiredService<IIntegrationEventOutbox>();

        registeredOutbox.ShouldBeSameAs(outbox);
    }
}
