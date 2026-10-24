using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>Registers the applier that keeps the local Branding projection current.</summary>
public static class BrandingProjectionDependencyInjection
{
    /// <summary>
    /// Adds the Branding projection applier; the host supplies <see cref="IBrandingProjectionStore" /> and the consumer
    /// delivery registration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAdminBrandingProjection(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<BrandingProjectionApplier>();

        return services;
    }
}
