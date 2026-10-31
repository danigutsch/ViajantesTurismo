using System.Text.RegularExpressions;
using PandaBear.Branding;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Defines the immutable public route that serves a content-addressed Branding logo.</summary>
public static partial class BrandingLogoPath
{
    /// <summary>Route segment that precedes the content address.</summary>
    public const string Segment = "logo";

    /// <summary>Route parameter name of the content address.</summary>
    public const string HashParameter = "sha256";

    /// <summary>Length of a lowercase hexadecimal SHA-256.</summary>
    public const int HashLength = 64;

    private const string ApiVersionPrefix = "/api/v1";

    /// <summary>Gets the route template below the public Branding group.</summary>
    public static string Template => $"/{Segment}/{{{HashParameter}}}";

    /// <summary>Gets the route template of the management logo upload below the management Branding group.</summary>
    public static string ManagementTemplate => $"/{Segment}";

    /// <summary>Builds the app-generated root-relative URL of a logo.</summary>
    /// <param name="sha256">The lowercase hexadecimal content address.</param>
    /// <returns>The root-relative logo URL.</returns>
    public static string For(string sha256)
    {
        ArgumentNullException.ThrowIfNull(sha256);

        return $"{ApiVersionPrefix}/{BrandingRoutes.PublicSettingsPath}/{Segment}/{sha256}";
    }

    /// <summary>Returns whether the value is a lowercase hexadecimal SHA-256.</summary>
    /// <param name="value">The candidate content address.</param>
    /// <returns><see langword="true" /> when the value is well formed.</returns>
    public static bool IsContentAddress(string? value) => value is not null && ContentAddressPattern().IsMatch(value);

    [GeneratedRegex("^[0-9a-f]{64}\\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex ContentAddressPattern();
}
