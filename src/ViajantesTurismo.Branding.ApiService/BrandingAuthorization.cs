namespace ViajantesTurismo.Branding.ApiService;

/// <summary>
/// Defines Branding API permission policies and provider-role mappings.
/// </summary>
internal static class BrandingAuthorization
{
    public const string BrandingRead = "branding.read";
    public const string BrandingWrite = "branding.write";

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["Admin"] = [BrandingRead, BrandingWrite],
            ["Operator"] = [BrandingRead, BrandingWrite]
        };
}
