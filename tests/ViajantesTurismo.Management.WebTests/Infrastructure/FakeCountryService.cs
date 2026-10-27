namespace ViajantesTurismo.Management.WebTests.Infrastructure;

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

    private readonly Task<CountryInfo[]> countries;

    /// <summary>Creates the fake with the default list of ten countries.</summary>
    public FakeCountryService()
        : this(Task.FromResult(DefaultCountries.ToArray()))
    {
    }

    /// <summary>Creates the fake returning the given pending or completed countries task.</summary>
    public FakeCountryService(Task<CountryInfo[]> countries)
    {
        ArgumentNullException.ThrowIfNull(countries);

        this.countries = countries;
    }

    /// <inheritdoc />
    public Task<CountryInfo[]> GetCountries(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return countries.WaitAsync(ct);
    }
}
