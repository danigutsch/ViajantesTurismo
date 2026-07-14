namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>
/// Defines Catalog API permission policies and provider-role mappings.
/// </summary>
internal static class CatalogAuthorization
{
    public const string CatalogRead = "catalog.read";
    public const string CatalogWrite = "catalog.write";
    public const string MediaAi = "media.ai";

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["Admin"] = [CatalogRead, CatalogWrite, MediaAi],
            ["Operator"] = [CatalogRead, CatalogWrite, MediaAi]
        };
}
