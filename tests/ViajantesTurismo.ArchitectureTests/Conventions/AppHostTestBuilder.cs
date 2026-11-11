using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Creates AppHost model builders for this project, which references the AppHost as a library: the Aspire testing builder looks for an
/// AppHost entry assembly and finds none here, so <c>AppHostModel.Project</c>, which needs that builder, is replaced by <see cref="Project"/>.
/// </summary>
internal static class AppHostTestBuilder
{
    /// <summary>Creates a run-mode builder with AppHost configuration values, as <c>AppHostLaunch.Configuration</c> passes them.</summary>
    public static IDistributedApplicationBuilder Create(IReadOnlyDictionary<string, string?>? configuration = null)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost"),
        });
        builder.Configuration.AddInMemoryCollection(configuration ?? new Dictionary<string, string?>());
        return builder;
    }

    /// <summary>Finds the project resource with the given name.</summary>
    public static ProjectResource Project(IDistributedApplicationBuilder builder, string name) =>
        builder.Resources.OfType<ProjectResource>().Single(project => string.Equals(project.Name, name, StringComparison.Ordinal));
}
