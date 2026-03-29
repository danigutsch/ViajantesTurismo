namespace ViajantesTurismo.Admin.WebTests.Infrastructure;

/// <summary>
/// In-memory fake that returns countries synchronously, avoiding real file I/O.
/// </summary>
internal sealed class FakeCountryService : ICountryService
{
    private static readonly CountryInfo[] DefaultCountries =
    [
        new() { Code = "BR", Name = "Brazil" },
        new() { Code = "CA", Name = "Canada" },
        new() { Code = "FR", Name = "France" },
        new() { Code = "DE", Name = "Germany" },
        new() { Code = "IT", Name = "Italy" },
        new() { Code = "MX", Name = "Mexico" },
        new() { Code = "PT", Name = "Portugal" },
        new() { Code = "ES", Name = "Spain" },
        new() { Code = "GB", Name = "United Kingdom" },
        new() { Code = "US", Name = "United States" }
    ];

    /// <inheritdoc />
    public Task<CountryInfo[]> GetCountries(CancellationToken ct)
    {
        return Task.FromResult(DefaultCountries.ToArray());
    }
}
