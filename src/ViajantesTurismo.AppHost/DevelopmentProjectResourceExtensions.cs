namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds development project resources with random local endpoints.
/// </summary>
internal static class DevelopmentProjectResourceExtensions
{
    private const string AspNetCoreEnvironmentVariable = "ASPNETCORE_ENVIRONMENT";
    private const string DotNetEnvironmentVariable = "DOTNET_ENVIRONMENT";
    private const string DevelopmentEnvironment = "Development";
    private const string ContainerImageTagConfigurationKey = "VT_ASPIRE_CONTAINER_IMAGE_TAG";
    private const string ContainerRegistryConfigurationKey = "VT_ASPIRE_CONTAINER_REGISTRY";
    private const string DeploymentVersionConfigurationKey = "VT_ASPIRE_DEPLOYMENT_VERSION";
    private const string SourceRevisionConfigurationKey = "VT_ASPIRE_SOURCE_REVISION";
    private const string DeploymentVersionEnvironmentVariable = "VT_DEPLOYMENT_VERSION";
    private const string SourceRevisionEnvironmentVariable = "VT_SOURCE_REVISION";

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
        var project = builder.AddProject<TProject>(name, launchProfileName: null);
        if (!HasContainerImageTag(builder))
        {
            project.WithEnvironment(AspNetCoreEnvironmentVariable, DevelopmentEnvironment);
        }

        return project
            .WithReleasePublishing(builder)
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
        var project = builder.AddProject<TProject>(name, launchProfileName: null);
        if (!HasContainerImageTag(builder))
        {
            project.WithEnvironment(DotNetEnvironmentVariable, DevelopmentEnvironment);
        }

        return project.WithReleasePublishing(builder);
    }

    private static bool HasContainerImageTag(IDistributedApplicationBuilder builder)
    {
        return !string.IsNullOrWhiteSpace(builder.Configuration[ContainerImageTagConfigurationKey]);
    }

    private static IResourceBuilder<ProjectResource> WithReleasePublishing(
        this IResourceBuilder<ProjectResource> project,
        IDistributedApplicationBuilder builder)
    {
        var imageTag = builder.Configuration[ContainerImageTagConfigurationKey];
        if (string.IsNullOrWhiteSpace(imageTag))
        {
            return project;
        }

        var registry = builder.Configuration[ContainerRegistryConfigurationKey];
        var deploymentVersion = builder.Configuration[DeploymentVersionConfigurationKey];
        var sourceRevision = builder.Configuration[SourceRevisionConfigurationKey];

        if (!string.IsNullOrWhiteSpace(deploymentVersion))
        {
            project.WithEnvironment(DeploymentVersionEnvironmentVariable, deploymentVersion);
        }

        if (!string.IsNullOrWhiteSpace(sourceRevision))
        {
            project.WithEnvironment(SourceRevisionEnvironmentVariable, sourceRevision);
        }

        return project.PublishAsDockerFile(container =>
        {
            container.WithImageTag(imageTag);

            if (!string.IsNullOrWhiteSpace(registry))
            {
                container.WithImageRegistry(registry);
            }
        });
    }
}
