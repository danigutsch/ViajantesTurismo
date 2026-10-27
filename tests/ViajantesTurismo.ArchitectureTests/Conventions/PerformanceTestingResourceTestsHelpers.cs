using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for tests of the AppHost performance testing resources.</summary>
internal static class PerformanceTestingResourceTestsHelpers
{
    /// <summary>Environment reader stub that enables only VT_ASPIRE_ENABLE_PERFORMANCE_TESTS and returns null for every other name.</summary>
    internal static string? Enabled(string name) => name == "VT_ASPIRE_ENABLE_PERFORMANCE_TESTS" ? "1" : null;

    /// <summary>Adds fake api and identity-provider container resources to the builder and returns their builders.</summary>
    internal static (IResourceBuilder<IResourceWithEndpoints> Api, IResourceBuilder<IResourceWithEndpoints> Identity) AddDependencies(
        IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<IResourceWithEndpoints> api = builder.AddContainer("api", "example/api").WithHttpEndpoint(targetPort: 8080, name: "http");
        IResourceBuilder<IResourceWithEndpoints> identity = builder.AddContainer("identity-provider", "example/idp").WithHttpEndpoint(targetPort: 8080, name: "http");
        return (api, identity);
    }
}
