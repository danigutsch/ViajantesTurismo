namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds development project resources with random local endpoints.
/// </summary>
internal static class DevelopmentProjectResourceExtensions
{
    private const string AspNetCoreEnvironmentVariable = "ASPNETCORE_ENVIRONMENT";
    private const string DotNetEnvironmentVariable = "DOTNET_ENVIRONMENT";
    private const string DevelopmentEnvironment = "Development";

    /// <summary>
    /// Adds an ASP.NET Core project without launch profile endpoints.
    /// </summary>
    /// <typeparam name="TProject">The Aspire project metadata type.</typeparam>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name.</param>
    /// <returns>The configured project resource.</returns>
    public static IResourceBuilder<ProjectResource> AddDevelopmentAspNetCoreProject<TProject>(
        this IDistributedApplicationBuilder builder,
        string name)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>(name, launchProfileName: null)
            .WithEnvironment(AspNetCoreEnvironmentVariable, DevelopmentEnvironment)
            .WithHttpEndpoint()
            .WithHttpsEndpoint();
    }

    /// <summary>
    /// Adds a .NET project without launch profile endpoints.
    /// </summary>
    /// <typeparam name="TProject">The Aspire project metadata type.</typeparam>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name.</param>
    /// <returns>The configured project resource.</returns>
    public static IResourceBuilder<ProjectResource> AddDevelopmentDotNetProject<TProject>(
        this IDistributedApplicationBuilder builder,
        string name)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>(name, launchProfileName: null)
            .WithEnvironment(DotNetEnvironmentVariable, DevelopmentEnvironment);
    }
}
