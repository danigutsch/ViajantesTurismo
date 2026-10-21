using System.Text;

namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class JsonLintCommandTests
{
    [Theory]
    [InlineData("jsonc", 0)]
    [InlineData("utf8-bom", 0)]
    [InlineData("malformed", 1)]
    [InlineData("invalid-utf8", 1)]
    [InlineData("missing-tracked-file", 1)]
    public async Task Validates_tracked_json_without_traversing_untracked_caches(string scenario, int expectedExitCode)
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        var cancellationToken = TestContext.Current.CancellationToken;
        const string trackedPath = " settings café.json";
        var content = scenario == "malformed"
            ? "{\"value\":"
            : "// Repository JSONC\n{\"url\":\"https://example.test/a//b\",\"values\":[1,],/* comment */}";
        workspace.WriteFile(trackedPath, content);
        if (scenario == "utf8-bom")
        {
            await File.WriteAllBytesAsync(Path.Combine(workspace.RootPath, trackedPath),
                [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(content)], cancellationToken);
        }
        else if (scenario == "invalid-utf8")
        {
            await File.WriteAllBytesAsync(Path.Combine(workspace.RootPath, trackedPath),
                [0x7B, 0x22, 0xFF, 0x22, 0x3A, 0x31, 0x7D], cancellationToken);
        }
        if (scenario == "malformed")
        {
            workspace.WriteFile("another.json", "{");
        }
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["init"], cancellationToken);
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["add", "--all"], cancellationToken);
        if (scenario == "missing-tracked-file")
        {
            File.Delete(Path.Combine(workspace.RootPath, trackedPath));
        }
        Directory.CreateDirectory(Path.Combine(workspace.RootPath, ".nuget", "packages", "microsoft.extensions.configuration.json"));
        workspace.WriteFile(".nuget/packages/cache.json", "not valid JSON");
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["lint-json"], output, error, workspace.RootPath, cancellationToken);

        // Assert
        exitCode.ShouldBe(expectedExitCode);
        if (expectedExitCode == 0)
        {
            error.ToString().ShouldBe(string.Empty);
        }
        else
        {
            error.ToString().ShouldContain(trackedPath, StringComparison.Ordinal);
            if (scenario == "malformed")
            {
                error.ToString().ShouldContain("another.json", StringComparison.Ordinal);
            }
        }
        error.ToString().ShouldNotContain(".nuget", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_directory_arguments_instead_of_silently_skipping_validation()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["lint-json", "roadmap"], output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("validates all tracked JSON files and does not accept arguments", StringComparison.Ordinal);
    }
}
