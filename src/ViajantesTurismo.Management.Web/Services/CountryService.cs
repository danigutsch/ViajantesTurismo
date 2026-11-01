using System.Text.Json;

namespace ViajantesTurismo.Management.Web.Services;

/// <summary>
/// Service for loading and managing country data.
/// </summary>
/// <remarks>
/// Registered as a singleton: the loaded list is shared across scopes. Concurrent first calls may each read the file once with equivalent results.
/// </remarks>
/// <param name="hostEnvironment">The web host environment.</param>
internal sealed class CountryService(IWebHostEnvironment hostEnvironment) : ICountryService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Maps common demonyms to country names.</summary>
    private static readonly Dictionary<string, string> DemonymToCountryName = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Brazilian", "Brazil" },
        { "American", "United States" },
        { "British", "United Kingdom" },
        { "German", "Germany" },
        { "French", "France" },
        { "Italian", "Italy" },
        { "Spanish", "Spain" },
        { "Portuguese", "Portugal" },
        { "Canadian", "Canada" },
        { "Mexican", "Mexico" }
    };

    private volatile CountryInfo[]? _countries;

    /// <summary>
    /// Gets the list of countries asynchronously.
    /// </summary>
    /// <returns>A list of country information.</returns>
    /// <remarks>Falls back to a few common countries, as used in tests, when the data file is missing or malformed.</remarks>
    public async Task<CountryInfo[]> GetCountries(CancellationToken ct)
    {
        if (_countries is not null)
        {
            return _countries;
        }

        try
        {
            var filePath = Path.Combine(hostEnvironment.WebRootPath, "data", "countries.json");
            var json = await File.ReadAllTextAsync(filePath, ct);
            var countriesDictionary = JsonSerializer.Deserialize<Dictionary<string, CountryData>>(json, JsonSerializerOptions);

            _countries = countriesDictionary?
                .Select(kvp => new CountryInfo
                {
                    Code = kvp.Key.ToUpperInvariant(),
                    Name = kvp.Value.Name
                })
                .OrderBy(countryInfo => countryInfo.Name)
                .ToArray() ?? [];

            return _countries;
        }
        catch (FileNotFoundException)
        {
            _countries = GetFallbackCountries();
            return _countries;
        }
        catch (JsonException)
        {
            _countries = GetFallbackCountries();
            return _countries;
        }
    }

    /// <summary>
    /// Normalizes a nationality/demonym to a country name.
    /// For example, "Brazilian" -> "Brazil", "American" -> "United States"
    /// </summary>
    public static string NormalizeNationality(string? nationality)
    {
        if (string.IsNullOrEmpty(nationality))
        {
            return nationality ?? string.Empty;
        }

        return DemonymToCountryName.GetValueOrDefault(nationality, nationality);
    }

    private static CountryInfo[] GetFallbackCountries()
    {
        return
        [
            new CountryInfo { Code = "BR", Name = "Brazil" },
            new CountryInfo { Code = "US", Name = "United States" },
            new CountryInfo { Code = "GB", Name = "United Kingdom" },
            new CountryInfo { Code = "DE", Name = "Germany" },
            new CountryInfo { Code = "FR", Name = "France" },
            new CountryInfo { Code = "IT", Name = "Italy" },
            new CountryInfo { Code = "ES", Name = "Spain" },
            new CountryInfo { Code = "PT", Name = "Portugal" },
            new CountryInfo { Code = "CA", Name = "Canada" },
            new CountryInfo { Code = "MX", Name = "Mexico" }
        ];
    }
}
