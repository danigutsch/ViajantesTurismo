using Microsoft.AspNetCore.Hosting;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.WebTests.Services;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Resolves the registered country service from separate scopes with scope and build validation enabled.</summary>
internal sealed class CountryServiceRegistrationHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>Registers the country service over a stub web host environment rooted at <paramref name="contentRoot"/>.</summary>
    /// <param name="contentRoot">Directory that holds the countries data file.</param>
    public CountryServiceRegistrationHost(string contentRoot)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(new StubWebHostEnvironment(contentRoot));
        services.AddCountryService();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <summary>Loads the country list through a fresh scope.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The country list returned by the service resolved in that scope.</returns>
    public async Task<CountryInfo[]> GetCountriesFromNewScope(CancellationToken ct)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICountryService>().GetCountries(ct);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
