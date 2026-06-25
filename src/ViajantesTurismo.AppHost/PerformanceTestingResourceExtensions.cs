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
    private const string ResultsDirectoryVariable = "VT_K6_RESULTS_DIR";

    private static readonly string[] OptionalK6Variables =
    [
        "VT_K6_VUS",
        "VT_K6_DURATION",
        "VT_K6_USE_DOCKER",
        "VT_K6_DOCKER_IMAGE",
    ];

    /// <summary>
    /// Adds the Admin k6 smoke scenario as an opt-in executable resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="adminApi">The Admin API resource that the scenario targets.</param>
    /// <remarks>
    /// Set <c>VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1</c> to include this resource. It stays opt-in so
    /// normal AppHost runs do not execute load tooling accidentally.
    /// </remarks>
    public static void AddAdminPerformanceSmoke(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> adminApi)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(adminApi);

        if (!IsEnabled(Environment.GetEnvironmentVariable(EnablePerformanceTestsVariable)))
        {
            return;
        }

        var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        var command = OperatingSystem.IsWindows() ? "pwsh" : "bash";
        var commandArguments = OperatingSystem.IsWindows()
            ? new[] { "-NoProfile", "-File", "scripts/run-admin-performance-smoke.ps1" }
            : new[] { "scripts/run-admin-performance-smoke.sh" };

        var performanceSmoke = builder.AddExecutable(ResourceNames.AdminPerformanceSmoke, command, repositoryRoot, commandArguments)
            .WithEnvironment(ApiBaseUrlVariable, adminApi.GetEndpoint("http"))
            .WithEnvironment(ProfileVariable, GetEnvironmentOrDefault(ProfileVariable, "smoke"))
            .WithEnvironment(ResultsDirectoryVariable, GetEnvironmentOrDefault(ResultsDirectoryVariable, "tests/performance/results"))
            .WaitFor(adminApi);

        foreach (var variableName in OptionalK6Variables)
        {
            AddOptionalEnvironmentVariable(performanceSmoke, variableName);
        }
    }

    private static void AddOptionalEnvironmentVariable<TResource>(
        IResourceBuilder<TResource> resourceBuilder,
        string name)
        where TResource : IResourceWithEnvironment
    {
        var value = Environment.GetEnvironmentVariable(name);

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

    private static string GetEnvironmentOrDefault(string name, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
