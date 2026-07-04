using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class DomainEventDispatcherServiceProvider(ServiceProvider serviceProvider) : IDisposable
{
    public IServiceProvider ServiceProvider => serviceProvider;

    public static DomainEventDispatcherServiceProvider Create(IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);

        return new DomainEventDispatcherServiceProvider(services.BuildServiceProvider());
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
