namespace ViajantesTurismo.Admin.Web.Services;

/// <summary>
/// Provides access to country data.
/// </summary>
internal interface ICountryService
{
    /// <summary>
    /// Gets the list of countries asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of country information.</returns>
    Task<CountryInfo[]> GetCountries(CancellationToken ct);
}
