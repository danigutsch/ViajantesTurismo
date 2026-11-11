using System.Globalization;
using Microsoft.Extensions.Configuration;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Infrastructure that already runs outside this AppHost, named by configuration. A configured dependency replaces the container the
/// AppHost would otherwise start for it; the projects receive the configured address instead. Nothing configured means every container
/// is started as usual.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>ConnectionStrings:admin-database</c>, <c>ConnectionStrings:catalog-database</c> and <c>ConnectionStrings:security-database</c>,
/// all three or none: no PostgreSQL server.</item>
/// <item><c>ConnectionStrings:cache</c>: no Redis.</item>
/// <item><see cref="IdentityProviderAuthorityKey"/>: no Keycloak; the APIs and Management Web use that authority.</item>
/// <item><c>AppHost:External:ClamAv:Host</c> and <c>:Port</c>: no ClamAV.</item>
/// <item><c>AppHost:External:SeaweedFs:Endpoint</c>, <c>:AccessKey</c>, <c>:SecretKey</c> and <c>:Bucket</c>: no SeaweedFS.</item>
/// <item><see cref="OtlpEndpointKey"/>: no collector gateway; every project exports OTLP over gRPC to that endpoint.</item>
/// </list>
/// The hosted-test fixture names the same settings <c>HostedTests:Authority</c>, <c>HostedTests:ClamAv:*</c>,
/// <c>HostedTests:SeaweedFs:*</c> and <c>HostedTests:OtlpEndpoint</c>; those names are read when the <c>AppHost:External</c> ones are absent.
/// With <see cref="SharedInfrastructureKey"/> set to <c>true</c>, every external setting is required.
/// </remarks>
internal static class ExternalInfrastructure
{
    /// <summary>The absolute URI of an external OpenID Connect authority (realm), such as <c>http://localhost:8080/realms/viajantes</c>.</summary>
    public const string IdentityProviderAuthorityKey = "AppHost:External:IdentityProvider:Authority";

    /// <summary>The host name of an external ClamAV daemon.</summary>
    public const string ClamAvHostKey = "AppHost:External:ClamAv:Host";

    /// <summary>The TCP port of an external ClamAV daemon.</summary>
    public const string ClamAvPortKey = "AppHost:External:ClamAv:Port";

    /// <summary>The S3 endpoint of an external SeaweedFS, such as <c>http://localhost:8333</c>.</summary>
    public const string SeaweedFsEndpointKey = "AppHost:External:SeaweedFs:Endpoint";

    /// <summary>The S3 access key of an external SeaweedFS.</summary>
    public const string SeaweedFsAccessKeyKey = "AppHost:External:SeaweedFs:AccessKey";

    /// <summary>The S3 secret key of an external SeaweedFS.</summary>
    public const string SeaweedFsSecretKeyKey = "AppHost:External:SeaweedFs:SecretKey";

    /// <summary>The media bucket in an external SeaweedFS.</summary>
    public const string SeaweedFsBucketKey = "AppHost:External:SeaweedFs:Bucket";

    /// <summary>The OTLP gRPC endpoint of an external collector, such as <c>http://localhost:4317</c>.</summary>
    public const string OtlpEndpointKey = "AppHost:External:Otlp:Endpoint";

    /// <summary>Requires every dependency to be external, as the hosted-test fixture does for its shared infrastructure.</summary>
    public const string SharedInfrastructureKey = "HostedTests:SharedInfrastructure";

    /// <summary>The hosted-test names of the external settings, read when the <c>AppHost:External</c> name is absent.</summary>
    private static readonly Dictionary<string, string> HostedTestNames = new(StringComparer.Ordinal)
    {
        [IdentityProviderAuthorityKey] = "HostedTests:Authority",
        [ClamAvHostKey] = "HostedTests:ClamAv:Host",
        [ClamAvPortKey] = "HostedTests:ClamAv:Port",
        [SeaweedFsEndpointKey] = "HostedTests:SeaweedFs:Endpoint",
        [SeaweedFsAccessKeyKey] = "HostedTests:SeaweedFs:AccessKey",
        [SeaweedFsSecretKeyKey] = "HostedTests:SeaweedFs:SecretKey",
        [SeaweedFsBucketKey] = "HostedTests:SeaweedFs:Bucket",
        [OtlpEndpointKey] = "HostedTests:OtlpEndpoint",
    };

    private static readonly string[] DatabaseNames = [ResourceNames.AdminDatabase, ResourceNames.CatalogDatabase, ResourceNames.SecurityDatabase];

    /// <summary>
    /// When <see cref="SharedInfrastructureKey"/> is <c>true</c>, checks that the databases, the identity provider, ClamAV, SeaweedFS and
    /// the OTLP endpoint are all configured, so a missing setting fails the model instead of starting a container.
    /// </summary>
    /// <exception cref="InvalidOperationException">A required setting is missing.</exception>
    public static void RequireAllWhenShared(IConfiguration configuration)
    {
        if (!string.Equals(configuration[SharedInfrastructureKey], bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var missing = HostedTestNames.Keys.Where(key => string.IsNullOrWhiteSpace(Value(configuration, key))).ToList();
        if (!HasDatabases(configuration))
        {
            missing.AddRange(DatabaseNames.Select(name => $"ConnectionStrings:{name}"));
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"{SharedInfrastructureKey} needs every external setting; missing: {string.Join(", ", missing)}.");
        }
    }

    /// <summary>Gets whether the three application databases are configured as connection strings.</summary>
    /// <exception cref="InvalidOperationException">Only some of the three are configured.</exception>
    public static bool HasDatabases(IConfiguration configuration)
    {
        var configured = DatabaseNames.Where(name => !string.IsNullOrWhiteSpace(configuration.GetConnectionString(name))).ToArray();
        if (configured.Length is 0)
        {
            return false;
        }

        return configured.Length == DatabaseNames.Length
            ? true
            : throw new InvalidOperationException(
                $"External databases need all of {string.Join(", ", DatabaseNames.Select(name => $"ConnectionStrings:{name}"))}; "
                + $"missing: {string.Join(", ", DatabaseNames.Except(configured, StringComparer.Ordinal))}.");
    }

    /// <summary>Gets whether the cache is configured as a connection string.</summary>
    public static bool HasCache(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration.GetConnectionString(ResourceNames.Cache));

    /// <summary>Gets the external OpenID Connect authority, or <see langword="null"/> when none is configured.</summary>
    /// <exception cref="InvalidOperationException">The value is not an absolute HTTP or HTTPS URI.</exception>
    public static Uri? IdentityProviderAuthority(IConfiguration configuration) => Endpoint(configuration, IdentityProviderAuthorityKey);

    /// <summary>Gets the external OTLP endpoint, or <see langword="null"/> when none is configured.</summary>
    /// <exception cref="InvalidOperationException">The value is not an absolute HTTP or HTTPS URI.</exception>
    public static Uri? OtlpEndpoint(IConfiguration configuration) => Endpoint(configuration, OtlpEndpointKey);

    /// <summary>Gets the external ClamAV daemon, or <see langword="null"/> when none is configured.</summary>
    /// <exception cref="InvalidOperationException">Only one of the two settings is configured, or the port is not a TCP port.</exception>
    public static (string Host, int Port)? ClamAv(IConfiguration configuration)
    {
        if (Required(configuration, ClamAvHostKey, ClamAvPortKey) is not [var host, var port])
        {
            return null;
        }

        return int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number is > 0 and <= 65535
            ? (host, number)
            : throw new InvalidOperationException($"{ClamAvPortKey} must be a TCP port.");
    }

    /// <summary>Gets the external SeaweedFS, or <see langword="null"/> when none is configured.</summary>
    /// <exception cref="InvalidOperationException">Only some of the four settings are configured, or the endpoint is not an absolute URI.</exception>
    public static (string Endpoint, string AccessKey, string SecretKey, string Bucket)? SeaweedFs(IConfiguration configuration)
    {
        if (Required(configuration, SeaweedFsEndpointKey, SeaweedFsAccessKeyKey, SeaweedFsSecretKeyKey, SeaweedFsBucketKey)
            is not [_, var accessKey, var secretKey, var bucket])
        {
            return null;
        }

        var endpoint = Endpoint(configuration, SeaweedFsEndpointKey)!;
        return (endpoint.GetLeftPart(UriPartial.Authority), accessKey, secretKey, bucket);
    }

    private static Uri? Endpoint(IConfiguration configuration, string key)
    {
        var value = Value(configuration, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : throw new InvalidOperationException($"{key} must be an absolute http or https URI.");
    }

    private static string? Value(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value ? value : HostedTestNames.TryGetValue(key, out var hosted) ? configuration[hosted] : null;

    private static string[]? Required(IConfiguration configuration, params string[] keys)
    {
        var values = keys.Select(key => Value(configuration, key) ?? string.Empty).ToArray();
        if (values.All(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        var missing = keys.Where((_, index) => string.IsNullOrWhiteSpace(values[index])).ToArray();
        return missing.Length is 0
            ? values
            : throw new InvalidOperationException($"External infrastructure needs {string.Join(", ", keys)}; missing: {string.Join(", ", missing)}.");
    }
}
