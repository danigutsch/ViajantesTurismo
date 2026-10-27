namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>Converts API resource locations into UI routes.</summary>
internal static class ApiLocationNormalizer
{
    private const string ApiVersionPrefix = "/api/v1";

    /// <summary>Maps an API Location URI to a UI route by stripping the API version prefix, or returns the fallback path when the location is missing or protocol-relative.</summary>
    internal static string ToUiRoute(Uri? location, string fallbackPath, string versionedResourcePathPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(versionedResourcePathPrefix);

        if (!versionedResourcePathPrefix.StartsWith(ApiVersionPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The resource prefix must start with '{ApiVersionPrefix}'.",
                nameof(versionedResourcePathPrefix));
        }

        if (location is null)
        {
            return fallbackPath;
        }

        var localPath = location.IsAbsoluteUri
            ? location.PathAndQuery
            : location.OriginalString;

        if (localPath.StartsWith("//", StringComparison.Ordinal))
        {
            return fallbackPath;
        }

        if (!localPath.StartsWith('/'))
        {
            localPath = $"/{localPath}";
        }

        return IsExactPathOrSubpath(localPath, versionedResourcePathPrefix)
            ? localPath[ApiVersionPrefix.Length..]
            : localPath;
    }

    private static bool IsExactPathOrSubpath(string path, string prefix) =>
        path.StartsWith(prefix, StringComparison.Ordinal)
        && (path.Length == prefix.Length || (path.Length > prefix.Length && path[prefix.Length] == '/'));
}
