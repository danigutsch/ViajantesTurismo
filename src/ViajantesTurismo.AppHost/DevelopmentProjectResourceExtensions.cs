using Aspire.Hosting.Dotnet;
using SharedKernel.AspNetCore;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds development project resources with random local endpoints.
/// </summary>
internal static class DevelopmentProjectResourceExtensions
{
    private const string DevelopmentEnvironment = "Development";
    private const string ContainerImageTagConfigurationKey = "VT_ASPIRE_CONTAINER_IMAGE_TAG";
    private const string ContainerRegistryConfigurationKey = "VT_ASPIRE_CONTAINER_REGISTRY";
    private const string DeploymentVersionConfigurationKey = "VT_ASPIRE_DEPLOYMENT_VERSION";
    private const string SourceRevisionConfigurationKey = "VT_ASPIRE_SOURCE_REVISION";
    private const string DeploymentVersionEnvironmentVariable = "VT_DEPLOYMENT_VERSION";
    private const string SourceRevisionEnvironmentVariable = "VT_SOURCE_REVISION";

    /// <summary>
    /// Adds an ASP.NET Core project without launch profile endpoints. It runs in the AppHost's environment and has a health check on
    /// <c>/health</c>, which the SharedKernel service defaults map in every environment.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name.</param>
    /// <param name="projectPath">The project file relative to the AppHost.</param>
    /// <returns>The configured project resource.</returns>
    public static IResourceBuilder<DotnetProjectResource> AddDevelopmentAspNetCoreProject(
        this IDistributedApplicationBuilder builder,
        string name,
        string projectPath)
    {
        var project = builder.AddDotnetProject(name, projectPath, static options => options.ExcludeLaunchProfile = true)
            .WithReleasePublishing(builder)
            .WithHttpEndpoint()
            .WithHttpsEndpoint()
            .WithHttpHealthCheckInAppHostEnvironment();

        if (RunsInHttpMaintenance(builder))
        {
            project.WithEnvironment(HttpMaintenanceConfiguration.EnabledEnvironmentVariable, "true");
        }

        return project;
    }

    /// <summary>Waits for a local API process during maintenance so that the dependent web page can report its own status.</summary>
    public static IResourceBuilder<DotnetProjectResource> WaitForHttpDependency(
        this IResourceBuilder<DotnetProjectResource> project, IResourceBuilder<DotnetProjectResource> dependency) =>
        RunsInHttpMaintenance(project.ApplicationBuilder) ? project.WaitForStart(dependency) : project.WaitFor(dependency);

    private static bool RunsInHttpMaintenance(IDistributedApplicationBuilder builder) =>
        builder.ExecutionContext.IsRunMode &&
        HttpMaintenanceConfiguration.IsEnabled(builder.Configuration[HttpMaintenanceConfiguration.EnabledKey]);

    /// <summary>
    /// Adds a .NET project without launch profile endpoints.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name.</param>
    /// <param name="projectPath">The project file relative to the AppHost.</param>
    /// <returns>The configured project resource.</returns>
    public static IResourceBuilder<DotnetProjectResource> AddDevelopmentDotNetProject(
        this IDistributedApplicationBuilder builder,
        string name,
        string projectPath)
    {
        var project = builder.AddDotnetProject(name, projectPath, static options => options.ExcludeLaunchProfile = true);
        if (builder.ExecutionContext.IsRunMode)
        {
            project.WithEnvironment(HostEnvironmentVariableNames.DotNetEnvironment, DevelopmentEnvironment);
        }

        return project.WithReleasePublishing(builder);
    }

    private static IResourceBuilder<DotnetProjectResource> WithReleasePublishing(
        this IResourceBuilder<DotnetProjectResource> project,
        IDistributedApplicationBuilder builder)
    {
        if (builder.ExecutionContext.IsRunMode)
        {
            return project;
        }

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
