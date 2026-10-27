namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>
/// Defines Catalog API permission policies and provider-role mappings.
/// </summary>
internal static class CatalogAuthorization
{
    /// <summary>Name of the bearer authentication scheme.</summary>
    public const string BearerScheme = "CatalogBearer";
    /// <summary>Permission to read catalog data.</summary>
    public const string CatalogRead = "catalog.read";
    /// <summary>Permission to modify catalog data.</summary>
    public const string CatalogWrite = "catalog.write";
    /// <summary>Permission to publish catalog data.</summary>
    public const string CatalogPublish = "catalog.publish";
    /// <summary>Permission to use AI-assisted media features.</summary>
    public const string MediaAi = "media.ai";

    /// <summary>Permissions granted to each provider role, keyed by role name.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole
    {
        get;
    } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["Admin"] = [CatalogRead, CatalogWrite, CatalogPublish, MediaAi],
            ["Operator"] = [CatalogRead, CatalogWrite, MediaAi]
        };
}
