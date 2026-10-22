using System.Text.Json;

namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiTestSliceExecutionTests
{
    [Theory]
    [InlineData(false, false, 0, "success", "collect_test_coverage")]
    [InlineData(false, true, 0, "success", "install_playwright")]
    [InlineData(false, false, 7, "failed", "collect_test_coverage")]
    [InlineData(true, false, 0, "failed", "build_projects")]
    public async Task Slice_execution_preserves_phase_logs_and_manifest_on_success_and_failure(
        bool brokenBuild, bool playwright, int collectorExit, string status, string expectedPhase)
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        const string project = "tests/Slice.Tests/Slice.Tests.csproj";
        workspace.AddProject(project);
        workspace.WriteFile(project, """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""");
        workspace.WriteFile("NuGet.Config", "<configuration><packageSources><clear /></packageSources></configuration>");
        workspace.WriteFile("scripts/collect-test-coverage.sh", $"echo collector-output\nexit {collectorExit}\n");
        workspace.WriteFile("scripts/install-playwright.sh", "test -f \"$1\" && test \"$2\" = chromium\n");
        if (playwright) { workspace.WriteFile("tests/Slice.Tests/bin/Debug/net10.0/playwright.ps1", "# browser adapter fixture"); }
        var token = TestContext.Current.CancellationToken;
        var revision = await workspace.CommitFiles(token);
        await CandidateCompatibilityCommand.RunProcess("dotnet", ["restore", project], workspace.RootPath, TextWriter.Null, token);
        if (brokenBuild) { workspace.WriteFile("tests/Slice.Tests/Broken.cs", "this is invalid C#;"); }
        string[] arguments = playwright
            ? ["run-ci-test-slice", "--slice-name", "Execution Test", "--install-playwright", "--", project]
            : ["run-ci-test-slice", "--slice-name", "Execution Test", project];
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(arguments, output, error, workspace.RootPath, token);
        var results = Path.Combine(workspace.RootPath, "TestResults");
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(results, "execution-test-manifest.json"), token));
        var timings = await File.ReadAllTextAsync(Path.Combine(results, "execution-test-phase-timings.tsv"), token);

        // Assert
        result.ShouldBe(status == "success" ? 0 : 1);
        manifest.RootElement.GetProperty("status").GetString().ShouldBe(status);
        manifest.RootElement.GetProperty("revision").GetString().ShouldBe(revision);
        manifest.RootElement.GetProperty("projects")[0].GetString().ShouldBe(project);
        var phases = manifest.RootElement.GetProperty("phases").EnumerateArray().ToArray();
        phases.Select(phase => phase.GetProperty("name").GetString()).ShouldContain(expectedPhase);
        phases[^1].GetProperty("status").GetString().ShouldBe(status);
        foreach (var phase in phases)
        {
            var log = phase.GetProperty("log_file").GetString();
            log.ShouldNotBeNull();
            File.Exists(Path.Combine(workspace.RootPath, log)).ShouldBeTrue();
        }
        timings.ShouldContain("total_slice", StringComparison.Ordinal);
        timings.ShouldContain(status, StringComparison.Ordinal);
        if (!brokenBuild) { output.ToString().ShouldContain("collector-output", StringComparison.Ordinal); }
    }

    [Theory]
    [InlineData("--unknown", "Unknown slice option")]
    [InlineData("--slice-name", "Missing slice option value")]
    [InlineData("", "--slice-name is required")]
    public async Task Invalid_slice_options_fail_before_starting_processes(string option, string expected)
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var arguments = string.IsNullOrEmpty(option) ? new[] { "run-ci-test-slice" } : new[] { "run-ci-test-slice", option };

        // Act
        var result = await RepoConfigToolApplication.Run(arguments, output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(1);
        error.ToString().ShouldContain(expected, StringComparison.Ordinal);
        Directory.Exists(Path.Combine(workspace.RootPath, "TestResults")).ShouldBeFalse();
    }
}
