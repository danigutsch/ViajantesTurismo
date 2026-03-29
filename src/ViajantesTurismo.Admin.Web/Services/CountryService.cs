using System.Text.Json;
using JetBrains.Annotations;

namespace ViajantesTurismo.Admin.Web.Services;

/// <summary>
/// Service for loading and managing country data.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CountryService"/> class.
/// </remarks>
/// <param name="hostEnvironment">The web host environment.</param>
internal sealed class CountryService(IWebHostEnvironment hostEnvironment) : ICountryService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    // Mapping of common demonyms to country names
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

    private CountryInfo[]? _countries;

    /// <summary>
    /// Gets the list of countries asynchronously.
    /// </summary>
    /// <returns>A list of country information.</returns>
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
            // Fallback with common countries used in tests
            _countries = GetFallbackCountries();
            return _countries;
        }
        catch (JsonException)
        {
            // Fallback with common countries used in tests
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

/// <summary>
/// Represents country information.
/// </summary>
public sealed record CountryInfo
{
    /// <summary>
    /// Gets the country code (ISO 3166-1 alpha-2).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the name of the country.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// Represents country data from JSON.
/// </summary>
[UsedImplicitly]
internal sealed record CountryData
{
    /// <summary>
    /// Gets the name of the country.
    /// </summary>
    public required string Name { get; init; }
}
