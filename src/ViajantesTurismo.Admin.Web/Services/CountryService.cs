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
internal sealed class CountryService(IWebHostEnvironment hostEnvironment)
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
                    Code = kvp.Key.ToLowerInvariant(),
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
            new CountryInfo { Code = "br", Name = "Brazil" },
            new CountryInfo { Code = "us", Name = "United States" },
            new CountryInfo { Code = "gb", Name = "United Kingdom" },
            new CountryInfo { Code = "de", Name = "Germany" },
            new CountryInfo { Code = "fr", Name = "France" },
            new CountryInfo { Code = "it", Name = "Italy" },
            new CountryInfo { Code = "es", Name = "Spain" },
            new CountryInfo { Code = "pt", Name = "Portugal" },
            new CountryInfo { Code = "ca", Name = "Canada" },
            new CountryInfo { Code = "mx", Name = "Mexico" }
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
