using Aspire.Hosting;
using Aspire.Hosting.Dotnet;
using Microsoft.Extensions.Configuration;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Creates model builders that resolve application project paths from the repository's AppHost directory.
/// </summary>
internal static class AppHostTestBuilder
{
    /// <summary>Creates a run-mode builder with AppHost configuration values, as <c>AppHostLaunch.Configuration</c> passes them.</summary>
    public static IDistributedApplicationBuilder Create(IReadOnlyDictionary<string, string?>? configuration = null)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            ProjectDirectory = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost"),
        });
        builder.Configuration.AddInMemoryCollection(configuration ?? new Dictionary<string, string?>());
        return builder;
    }

    /// <summary>Finds the project resource with the given name.</summary>
    public static DotnetProjectResource Project(IDistributedApplicationBuilder builder, string name) =>
        builder.Resources.OfType<DotnetProjectResource>().Single(project => string.Equals(project.Name, name, StringComparison.Ordinal));
}
