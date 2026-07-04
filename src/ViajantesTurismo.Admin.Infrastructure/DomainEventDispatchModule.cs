using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ViajantesTurismo.Admin.Infrastructure;

internal static class DomainEventDispatchModule
{
    public static IServiceCollection AddDomainEventDispatchModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<DispatchDomainEventsSaveChangesInterceptor>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAdminWriteDbContextModule, DomainEventDispatchDbContextModule>());

        return services;
    }
}
