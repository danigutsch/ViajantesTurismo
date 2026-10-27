using Microsoft.Extensions.DependencyInjection;
using PandaBear.Domain;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Owns a service provider that exposes a single <see cref="IDomainEventDispatcher"/> for tests.
/// </summary>
/// <param name="serviceProvider">The built provider owned and disposed by this wrapper.</param>
internal sealed class DomainEventDispatcherServiceProvider(ServiceProvider serviceProvider) : IDisposable
{
    /// <summary>
    /// Gets the owned service provider.
    /// </summary>
    public IServiceProvider ServiceProvider => serviceProvider;

    /// <summary>
    /// Builds a provider that resolves <paramref name="dispatcher"/> as a singleton.
    /// </summary>
    public static DomainEventDispatcherServiceProvider Create(IDomainEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);

        return new DomainEventDispatcherServiceProvider(services.BuildServiceProvider());
    }

    /// <summary>
    /// Disposes the owned service provider.
    /// </summary>
    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
