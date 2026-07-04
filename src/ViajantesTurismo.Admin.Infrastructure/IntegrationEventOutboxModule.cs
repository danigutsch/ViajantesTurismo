using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.Infrastructure;

internal static class IntegrationEventOutboxModule
{
    public static IServiceCollection AddIntegrationEventOutboxModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IIntegrationEventOutbox, EfIntegrationEventOutbox>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAdminWriteDbContextModule, IntegrationEventOutboxDbContextModule>());

        return services;
    }
}
