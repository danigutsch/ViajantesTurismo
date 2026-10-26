using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.PerformanceTestingResourceTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class PerformanceTestingResourceTests
{
    private const string PinnedDigest = "8cd78f9d0de5f50bc8821cceecf356d5d9e839e6611c226a3fcf13c591080fbd";

    [Fact]
    public void Admin_smoke_stays_out_of_the_model_unless_enabled()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        var (api, identity) = AddDependencies(builder);

        // Act
        builder.AddAdminPerformanceSmoke(api, identity, static _ => null);

        // Assert
        builder.Resources.OfType<K6Resource>().ShouldBeEmpty();
    }

    [Fact]
    public void Admin_smoke_is_an_explicit_start_k6_container_pinned_by_digest()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        var (api, identity) = AddDependencies(builder);
        _ = builder.AddLocalSecret(ResourceNames.IdentityProviderK6ClientSecret);

        // Act
        builder.AddAdminPerformanceSmoke(api, identity, Enabled);

        // Assert
        var k6 = builder.Resources.OfType<K6Resource>().ShouldHaveSingleItem();
        k6.Name.ShouldBe(ResourceNames.AdminPerformanceSmoke);
        k6.Annotations.OfType<ContainerImageAnnotation>().ShouldHaveSingleItem().SHA256.ShouldBe(PinnedDigest);
        k6.Annotations.OfType<ExplicitStartupAnnotation>().Count().ShouldBe(1);
        k6.Annotations.OfType<WaitAnnotation>().Select(static wait => wait.Resource.Name).Order(StringComparer.Ordinal).ToArray()
            .ShouldBe(["api", "identity-provider"]);
    }

    [Fact]
    public async Task Admin_smoke_authenticates_with_the_k6_client_and_forwards_the_profile()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        var (api, identity) = AddDependencies(builder);
        _ = builder.AddLocalSecret(ResourceNames.IdentityProviderK6ClientSecret);
        Dictionary<string, string?> environment = new(StringComparer.Ordinal)
        {
            ["VT_ASPIRE_ENABLE_PERFORMANCE_TESTS"] = "1",
            ["VT_K6_PROFILE"] = "average-load",
            ["VT_K6_VUS"] = "3",
        };

        // Act
        builder.AddAdminPerformanceSmoke(api, identity, name => environment.GetValueOrDefault(name));

        // Assert
        var k6 = builder.Resources.OfType<K6Resource>().ShouldHaveSingleItem();
        var variables = new Dictionary<string, object>(StringComparer.Ordinal);
        var context = new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), k6, variables);
        foreach (var annotation in k6.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        variables["SK_K6_CLIENT_ID"].ShouldBe("k6-smoke");
        variables["VT_K6_PROFILE"].ShouldBe("average-load");
        variables["VT_K6_VUS"].ShouldBe("3");
        variables.ContainsKey("VT_K6_DURATION").ShouldBeFalse();
        variables["SK_K6_CLIENT_SECRET"].ShouldBeOfType<ParameterResource>().Name.ShouldBe(ResourceNames.IdentityProviderK6ClientSecret);
    }

    [Fact]
    public void The_file_upload_scenario_alone_remains_in_the_k6_tool_configuration()
    {
        // Arrange
        var text = File.ReadAllText(Path.Combine(GetRepositoryRoot(), ".config", "sharedkernel-k6.json"));

        // Act
        var json = System.Text.Json.Nodes.JsonNode.Parse(text)!;
        var scenarios = json["scenarios"]!.AsArray().Select(static scenario => scenario!["name"]!.GetValue<string>()).ToArray();

        // Assert
        scenarios.ShouldBe(["file-upload-scan"]);
    }
}
