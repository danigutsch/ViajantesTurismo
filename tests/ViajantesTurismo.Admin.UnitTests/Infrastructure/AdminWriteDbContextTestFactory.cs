using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents;
using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class AdminWriteDbContextTestFactory
{
    public static AdminWriteDbContext Create(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AdminWriteDbContext(options, [new TestInterceptorDbContextConfiguration(interceptors)]);
    }

    public static AdminWriteDbContextTestScope CreateWithDomainEventDispatcher(
        IDomainEventDispatcher dispatcher,
        params IInterceptor[] additionalInterceptors)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        services.AddSingleton<DispatchDomainEventsSaveChangesInterceptor>();
        services.AddDbContext<AdminWriteDbContext>((provider, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            options.AddInterceptors(provider.GetRequiredService<DispatchDomainEventsSaveChangesInterceptor>());
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
