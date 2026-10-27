namespace ViajantesTurismo.Management.Web.Services;

/// <summary>
/// Represents country information.
/// </summary>
public sealed record CountryInfo
{
    /// <summary>
    /// Gets the country code (ISO 3166-1 alpha-2).
    /// </summary>
    public required string Code
    {
        get; init;
    }

    /// <summary>
    /// Gets the name of the country.
    /// </summary>
    public required string Name
    {
        get; init;
    }
}
