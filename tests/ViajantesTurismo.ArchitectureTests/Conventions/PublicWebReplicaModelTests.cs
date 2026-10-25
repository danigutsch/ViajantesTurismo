using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Verifies replica configuration without starting application processes or containers.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class PublicWebReplicaModelTests
{
    [Theory]
    [InlineData(null, 1)]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    public void Only_public_web_uses_the_configured_replica_count(string? configuredCount, int expectedCount)
    {
        // Arrange
        string[] arguments = configuredCount is null
            ? [.. HostedProfile.System.ToArguments()]
            : [.. HostedProfile.System.ToArguments(), $"--Aspire:Replicas:PublicWeb={configuredCount}"];
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = arguments,
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
        });

        // Act
        builder.AddProductResources(HostedProfile.System);
        var publicWeb = builder.Resources.ShouldHaveSingleItem(resource => resource.Name == ResourceNames.PublicWebApp);
        var otherProjects = builder.Resources.OfType<ProjectResource>()
            .Where(resource => resource.Name != ResourceNames.PublicWebApp).ToArray();

        // Assert
        publicWeb.GetReplicaCount().ShouldBe(expectedCount);
        publicWeb.Annotations.OfType<HealthCheckAnnotation>().ShouldNotBeEmpty();
        var endpoints = publicWeb.Annotations.OfType<EndpointAnnotation>().ToArray();
        endpoints.ShouldNotBeEmpty();
        foreach (var endpoint in endpoints)
        {
            endpoint.IsExternal.ShouldBeTrue();
            endpoint.IsProxied.ShouldBeTrue();
            endpoint.TargetPort.ShouldBeNull();
        }

        var dependencies = publicWeb.Annotations.OfType<WaitAnnotation>()
            .Select(annotation => annotation.Resource.Name).ToArray();
        dependencies.ShouldContain(ResourceNames.CatalogApi);
        dependencies.ShouldNotContain(ResourceNames.BrandingApi);
        otherProjects.ShouldNotBeEmpty();
        foreach (var project in otherProjects)
        {
            project.GetReplicaCount().ShouldBe(1);
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("two")]
    [InlineData("2147483648")]
    public void Invalid_public_web_replica_counts_fail_during_model_creation(string configuredCount)
    {
        // Arrange
        string[] arguments = [.. HostedProfile.System.ToArguments(), $"--Aspire:Replicas:PublicWeb={configuredCount}"];

        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = arguments,
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
        });

        // Act
        Action createModel = () => builder.AddProductResources(HostedProfile.System);

        // Assert
        createModel.ShouldThrow<InvalidOperationException>();
    }
}
