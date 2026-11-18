using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Provides the country catalog for an isolated customer form.</summary>
internal sealed class FakeCountryBrowserService : ICountryService
{
    /// <inheritdoc />
    public Task<CountryInfo[]> GetCountries(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CountryInfo[] countries = [new() { Code = "BR", Name = "Brazil" }, new() { Code = "PT", Name = "Portugal" }];
        return Task.FromResult(countries);
    }
}
