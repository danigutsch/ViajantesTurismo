using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.Management.Web;

/// <summary>Registers the country reference-data service.</summary>
internal static class CountryServiceCollectionExtensions
{
    /// <summary>Adds <see cref="ICountryService"/> so the loaded country list is shared across scopes.</summary>
    public static IServiceCollection AddCountryService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton<ICountryService, CountryService>();
    }
}
