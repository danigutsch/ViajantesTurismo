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
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
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
