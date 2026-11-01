using Aspire.Hosting;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Verifies the publish-mode AppHost model without starting application processes or containers.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostPublishModelTests
{
    [Fact]
    public void Publish_mode_model_omits_the_local_identity_provider()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.System);

        // Assert
        builder.ExecutionContext.IsPublishMode.ShouldBeTrue();
        builder.Resources.ShouldNotContain(resource => resource.Name == ResourceNames.IdentityProvider);
        builder.Resources.ShouldNotContain(resource => resource.Name == ResourceNames.IdentityProviderK6ClientSecret);
        builder.Resources.ShouldContain(resource => resource.Name == ResourceNames.PublicWebApp);
    }

    [Fact]
    public void Run_mode_model_adds_the_local_identity_provider()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.System);

        // Assert
        builder.ExecutionContext.IsRunMode.ShouldBeTrue();
        builder.Resources.ShouldContain(resource => resource.Name == ResourceNames.IdentityProvider);
    }
}
