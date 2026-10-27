using JetBrains.Annotations;

namespace ViajantesTurismo.Management.Web.Services;

/// <summary>
/// Represents country data from JSON.
/// </summary>
[UsedImplicitly]
internal sealed record CountryData
{
    /// <summary>
    /// Gets the name of the country.
    /// </summary>
    public required string Name
    {
        get; init;
    }
}
