using ViajantesTurismo.AppHost;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.SharedKernelToolCallerTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class PerformanceTestingResourceTests
{
    private const string PinnedImage = "grafana/k6:0.49.0@sha256:8cd78f9d0de5f50bc8821cceecf356d5d9e839e6611c226a3fcf13c591080fbd";

    public static TheoryData<string?, string?, string[]> Environments => new()
    {
        { null, null, ["tool", "run", "sharedkernel-k6", "--", "run", "--scenario", "admin-smoke"] },
        { "0", PinnedImage, ["tool", "run", "sharedkernel-k6", "--", "run", "--scenario", "admin-smoke"] },
        { "1", null, ["tool", "run", "sharedkernel-k6", "--", "run", "--scenario", "admin-smoke", "--container"] },
        {
            "1",
            PinnedImage,
            ["tool", "run", "sharedkernel-k6", "--", "run", "--scenario", "admin-smoke", "--container", "--image", PinnedImage]
        },
    };

    [Theory]
    [MemberData(nameof(Environments))]
    public async Task Admin_smoke_runs_the_configured_k6_scenario(string? useDocker, string? image, string[] expected)
    {
        // Arrange
        Dictionary<string, string?> environment = new(StringComparer.Ordinal)
        {
            ["VT_K6_USE_DOCKER"] = useDocker,
            ["VT_K6_DOCKER_IMAGE"] = image,
        };

        // Act
        var arguments = PerformanceTestingResourceExtensions.GetAdminSmokeArguments(name => environment.GetValueOrDefault(name));
        var failure = await ParseWithDiagramDirective(
            GetRepositoryRoot(),
            new ToolCommandLine("src/ViajantesTurismo.AppHost/PerformanceTestingResourceExtensions.cs", "sharedkernel-k6", arguments[4..]),
            TestContext.Current.CancellationToken);

        // Assert
        arguments.ShouldBe(expected);
        failure.ShouldBeNull();
    }
}
