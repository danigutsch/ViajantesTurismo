namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>
/// Defines Branding API permission policies and provider-role mappings.
/// </summary>
internal static class BrandingAuthorization
{
    /// <summary>Permission required to read Branding management settings.</summary>
    public const string BrandingRead = "branding.read";
    /// <summary>Permission required to change Branding settings.</summary>
    public const string BrandingWrite = "branding.write";

    /// <summary>Permissions granted to each provider role.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole
    {
        get;
    } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["Admin"] = [BrandingRead, BrandingWrite],
            ["Operator"] = [BrandingRead, BrandingWrite]
        };
}
