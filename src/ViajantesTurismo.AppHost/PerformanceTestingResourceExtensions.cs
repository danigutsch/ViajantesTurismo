using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds optional performance testing resources to the local Aspire model.
/// </summary>
internal static class PerformanceTestingResourceExtensions
{
    private const string EnablePerformanceTestsVariable = "VT_ASPIRE_ENABLE_PERFORMANCE_TESTS";
    private const string ApiBaseUrlVariable = "VT_API_BASE_URL";
    private const string ProfileVariable = "VT_K6_PROFILE";
    private const string TokenPath = "/realms/viajantes/protocol/openid-connect/token";
    private const string ClientId = "k6-smoke";

    private static readonly string[] OptionalK6Variables =
    [
        "VT_K6_VUS",
        "VT_K6_DURATION",
    ];

    /// <summary>
    /// Adds the Admin k6 smoke scenario as an opt-in container that starts on request.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminApi">The Admin API resource that the scenario targets.</param>
    /// <param name="identityProvider">The identity provider that issues the scenario token.</param>
    /// <remarks>
    /// Set <c>VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1</c> to include this resource. It stays opt-in so
    /// normal AppHost runs do not execute load tooling accidentally.
    /// </remarks>
    public static void AddAdminPerformanceSmoke(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithEndpoints> adminApi,
        IResourceBuilder<IResourceWithEndpoints> identityProvider)
    {
        AddAdminPerformanceSmoke(builder, adminApi, identityProvider, Environment.GetEnvironmentVariable);
    }

    /// <summary>
    /// Adds the Admin k6 smoke scenario when <paramref name="getEnvironmentVariable"/> enables it.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminApi">The Admin API resource that the scenario targets.</param>
    /// <param name="identityProvider">The identity provider that issues the scenario token.</param>
    /// <param name="getEnvironmentVariable">Reads an environment variable.</param>
    internal static void AddAdminPerformanceSmoke(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithEndpoints> adminApi,
        IResourceBuilder<IResourceWithEndpoints> identityProvider,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(adminApi);
        ArgumentNullException.ThrowIfNull(identityProvider);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        if (!IsEnabled(getEnvironmentVariable(EnablePerformanceTestsVariable)))
        {
            return;
        }

        var k6Directory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "tests", "performance"));
        var performanceSmoke = builder.AddK6Scenario(
                ResourceNames.AdminPerformanceSmoke,
                Path.Combine(k6Directory, "k6"),
                "scenarios/admin-smoke.js",
                Path.Combine(k6Directory, "results"))
            .WithEnvironment(ApiBaseUrlVariable, adminApi.GetEndpoint("http"))
            .WithEnvironment(ProfileVariable, GetEnvironmentOrDefault(getEnvironmentVariable, ProfileVariable, "smoke"))
            .WaitFor(adminApi)
            .WithK6ClientCredentials(identityProvider, TokenPath, ClientId, builder.GetK6ClientSecret());

        foreach (var variableName in OptionalK6Variables)
        {
            AddOptionalEnvironmentVariable(performanceSmoke, getEnvironmentVariable, variableName);
        }
    }

    private static void AddOptionalEnvironmentVariable<TResource>(
        IResourceBuilder<TResource> resourceBuilder,
        Func<string, string?> getEnvironmentVariable,
        string name)
        where TResource : IResourceWithEnvironment
    {
        var value = getEnvironmentVariable(name);

        if (!string.IsNullOrWhiteSpace(value))
        {
            resourceBuilder.WithEnvironment(name, value);
        }
    }

    private static bool IsEnabled(string? value)
    {
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetEnvironmentOrDefault(Func<string, string?> getEnvironmentVariable, string name, string defaultValue)
    {
        var value = getEnvironmentVariable(name);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
