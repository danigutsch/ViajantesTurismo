using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>Formats a stored ISO 3166-1 alpha-2 country code for display.</summary>
internal static class CountryFormatter
{
    /// <summary>Gets the English name of a country code, for example Brazil for BR.</summary>
    /// <param name="code">The stored country code.</param>
    /// <returns>The country name, or the text as given when it is not an assigned code.</returns>
    public static string FormatCountry(string? code)
    {
        var country = CountryCode.Create(code);
        return country.IsSuccess ? country.Value.EnglishName : code ?? string.Empty;
    }
}
