using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task Publish_mode_management_web_receives_client_and_token_exchange_settings_but_no_local_authority()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
        });
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.System);
        var managementWeb = builder.Resources.Where(resource => resource.Name == ResourceNames.WebApp).ShouldHaveSingleItem();

        // Act
        var configuration = await ExecutionConfigurationBuilder.Create(managementWeb)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(builder.ExecutionContext, NullLogger.Instance, TestContext.Current.CancellationToken);
        var environment = configuration.EnvironmentVariables.Select(static variable => variable.Key).ToArray();

        // Assert
        environment.ShouldContain("Authentication__ClientId");
        environment.ShouldContain("Authentication__ClientSecret");
        environment.ShouldContain("Authentication__TokenExchange__Enabled");
        environment.ShouldContain("Authentication__TokenExchange__Provider");
        environment.ShouldNotContain("Authentication__Authority");
        environment.ShouldNotContain("Authentication__Issuer");
        environment.ShouldNotContain("Authentication__AllowHttpDevelopmentAuthority");
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
