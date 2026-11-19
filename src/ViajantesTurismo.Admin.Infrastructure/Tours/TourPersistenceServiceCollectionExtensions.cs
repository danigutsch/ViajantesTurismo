using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Tours;

/// <summary>Registers persistence for the Tour aggregate and its bookings.</summary>
internal static class TourPersistenceServiceCollectionExtensions
{
    /// <summary>Adds the scoped aggregate store and the transaction-owned command store.</summary>
    /// <param name="services">The Admin infrastructure service collection.</param>
    public static void AddTourPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ITourStore, TourStore>();
        services.TryAddScoped<ITourCommandStore, TourCommandStore>();
    }
}
