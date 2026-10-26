using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static class PerformanceTestingResourceTestsHelpers
{
    internal static string? Enabled(string name) => name == "VT_ASPIRE_ENABLE_PERFORMANCE_TESTS" ? "1" : null;

    internal static (IResourceBuilder<IResourceWithEndpoints> Api, IResourceBuilder<IResourceWithEndpoints> Identity) AddDependencies(
        IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<IResourceWithEndpoints> api = builder.AddContainer("api", "example/api").WithHttpEndpoint(targetPort: 8080, name: "http");
        IResourceBuilder<IResourceWithEndpoints> identity = builder.AddContainer("identity-provider", "example/idp").WithHttpEndpoint(targetPort: 8080, name: "http");
        return (api, identity);
    }
}
