using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents;
using SharedKernel.DomainEvents.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class AdminWriteDbContextTestFactory
{
    public static AdminWriteDbContextTestScope CreateWithDomainEventDispatcher(
        IDomainEventDispatcher dispatcher,
        params IInterceptor[] additionalInterceptors)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContext<AdminWriteDbContext>((provider, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
            options.AddInterceptors(additionalInterceptors);
        });

        var provider = services.BuildServiceProvider();
        try
        {
            return new AdminWriteDbContextTestScope(provider);
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

}
