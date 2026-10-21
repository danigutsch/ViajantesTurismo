using System.Globalization;
using System.Xml.Linq;

namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiTestProjectSelectionCommandTests
{
    [Theory]
    [InlineData("admin-api-integration")]
    [InlineData("admin-integration")]
    [InlineData("admin-system")]
    [InlineData("fast-validation-1")]
    [InlineData("fast-validation-2")]
    public async Task Extracted_library_test_slice_is_skipped_without_skipping_remaining_application_tests(string slice)
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        ArgumentNullException.ThrowIfNull(slice);
        repository.AddCanonicalTestSlices();
        var manifest = Path.Combine(repository.RootPath, "scripts", "ci-test-slices", slice + ".txt");
        var extractedProjects = await File.ReadAllLinesAsync(manifest, TestContext.Current.CancellationToken);
        var extractedProject = extractedProjects.Single();
        var solutionPath = Path.Combine(repository.RootPath, "ViajantesTurismo.slnx");
        var solution = XDocument.Load(solutionPath);
        solution.Descendants("Project")
            .Single(project => project.Attribute("Path")?.Value == extractedProject)
            .Remove();
        solution.Save(solutionPath);
        repository.WriteSlice(slice);
        var outputDirectory = Path.Combine(repository.RootPath, "TestResults", "selected-ci-test-slices");
        var githubOutput = Path.Combine(repository.RootPath, "TestResults", "github-output.txt");
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["select-ci-test-projects", "--mode", "full", "--root", repository.RootPath,
                "--output-directory", outputDirectory, "--github-output", githubOutput],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldBe(string.Empty);
        var selectedSlice = await File.ReadAllLinesAsync(Path.Combine(outputDirectory, slice + ".txt"), TestContext.Current.CancellationToken);
        selectedSlice.ShouldBeEmpty();
        var githubOutputs = await File.ReadAllLinesAsync(githubOutput, TestContext.Current.CancellationToken);
        githubOutputs.ShouldContain(slice.Replace('-', '_') + "_required=false");
        githubOutputs.ShouldContain("selected_test_project_count=4");
        githubOutputs.ShouldContain("build_required=true");
        if (slice.StartsWith("fast-validation-", StringComparison.Ordinal))
        {
            var root = CiTestSelectionTestProcess.GetRepositoryRoot();
            var workflow = await File.ReadAllTextAsync(Path.Combine(root, ".github", "workflows", "ci.yml"), TestContext.Current.CancellationToken);
            var row = workflow.Split("          - id: " + slice + "\n", StringSplitOptions.None)[1]
                .Split("          - id: ", StringSplitOptions.None)[0];
            var changeOutput = row.Split('\n')
                .Single(line => line.TrimStart().StartsWith("change_output:", StringComparison.Ordinal))
                .Split(':', 2)[1].Trim();
            githubOutputs.ShouldContain(changeOutput + "=false");
            workflow.ShouldContain(changeOutput + ": ${{ steps.change-scope.outputs." + changeOutput + " }}", StringComparison.Ordinal);
        }
        var remainingManifests = await Task.WhenAll(Directory.EnumerateFiles(outputDirectory, "*.txt")
            .Where(path => Path.GetFileName(path) != "openapi-tool-windows.txt")
            .Select(path => File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken)));
        var remainingProjects = remainingManifests.SelectMany(projects => projects).Order(StringComparer.Ordinal).ToArray();
        var expectedProjects = solution.Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
        remainingProjects.ShouldBe(expectedProjects);
    }

    [Fact]
    public async Task Full_selection_writes_manifests_and_github_outputs()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        var outputDirectory = Path.Combine(repository.RootPath, "TestResults", "selected-ci-test-slices");
        var githubOutput = Path.Combine(repository.RootPath, "TestResults", "github-output.txt");
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            [
                "select-ci-test-projects",
                "--mode",
                "full",
                "--output-directory",
                outputDirectory,
                "--github-output",
                githubOutput,
                "--root",
                repository.RootPath
            ],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldBe(string.Empty);
        (await File.ReadAllLinesAsync(
            Path.Combine(outputDirectory, "fast-validation-1.txt"),
            TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem()
            .ShouldBe("tests/Fast1.Tests/Fast1.Tests.csproj");
        (await File.ReadAllLinesAsync(
            Path.Combine(outputDirectory, "fast-validation-2.txt"),
            TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem()
            .ShouldBe("tests/Fast2.Tests/Fast2.Tests.csproj");
        (await File.ReadAllLinesAsync(
            Path.Combine(outputDirectory, "admin-system.txt"),
            TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem()
            .ShouldBe("tests/AdminSystem.Tests/AdminSystem.Tests.csproj");
        var githubOutputs = await File.ReadAllLinesAsync(githubOutput, TestContext.Current.CancellationToken);
        githubOutputs.ShouldContain("build_required=true");
        githubOutputs.ShouldContain("fast_validation_1_required=true");
        githubOutputs.ShouldContain("fast_validation_2_required=true");
        githubOutputs.ShouldContain("fast_validation_required=true");
        githubOutputs.ShouldContain("admin_system_required=true");
        githubOutputs.ShouldContain("selection_fallback=false");
    }

    [Fact]
    public async Task Selective_mode_requires_a_complete_git_range()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("tests/Fast.Tests/Fast.Tests.csproj");
        repository.WriteSlice("fast-validation", "tests/Fast.Tests/Fast.Tests.csproj");
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["select-ci-test-projects", "--mode", "merge-base", "--base", "base", "--root", repository.RootPath],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(2);
        error.ToString().ShouldContain("Selective CI test selection requires --base and --head.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Selective_mode_requires_a_base_sha()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["select-ci-test-projects", "--mode", "merge-base", "--head", "head", "--root", repository.RootPath],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(2);
        error.ToString().ShouldContain("Selective CI test selection requires --base and --head.", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("empty")]
    public async Task Selection_rejects_manifest_drift_or_unassigned_solution_tests(string drift)
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        if (drift == "missing")
        {
            repository.DeleteSlice("fast-validation-1");
        }
        else if (drift == "extra")
        {
            repository.WriteSlice("unexpected", "tests/Fast1.Tests/Fast1.Tests.csproj");
        }
        else
        {
            repository.WriteSlice("fast-validation-1");
        }

        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["select-ci-test-projects", "--mode", "full", "--root", repository.RootPath],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        var expectedError = drift == "empty" ? "CI test slice membership" : "CI test slice manifests";
        error.ToString().ShouldContain(expectedError, StringComparison.Ordinal);
        if (drift == "empty")
        {
            error.ToString().ShouldContain("tests/Fast1.Tests/Fast1.Tests.csproj", StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Full_selection_rejects_an_unassigned_xunit_solution_project()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        const string omittedProject = "tests/Omitted.Tests/Omitted.Tests.csproj";
        repository.AddXunitProject(omittedProject);
        var outputDirectory = Path.Combine(repository.RootPath, "TestResults", "selected-ci-test-slices");
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            [
                "select-ci-test-projects",
                "--mode",
                "full",
                "--output-directory",
                outputDirectory,
                "--root",
                repository.RootPath
            ],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(omittedProject, StringComparison.Ordinal);
        Directory.Exists(outputDirectory).ShouldBeFalse();
    }

    [Fact]
    public async Task Full_selection_rejects_a_test_project_assigned_to_multiple_slices()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        const string duplicateProject = "tests/Fast1.Tests/Fast1.Tests.csproj";
        repository.WriteSlice(
            "fast-validation-2",
            "tests/Fast2.Tests/Fast2.Tests.csproj",
            duplicateProject);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["select-ci-test-projects", "--mode", "full", "--root", repository.RootPath],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(duplicateProject, StringComparison.Ordinal);
        error.ToString().ShouldContain("fast-validation-1, fast-validation-2", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Full_selection_rejects_a_manifest_project_that_is_not_an_xunit_test()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        const string helperProject = "tests/Testing.Helper/Testing.Helper.csproj";
        repository.AddProject(helperProject);
        repository.WriteSlice(
            "fast-validation-1",
            "tests/Fast1.Tests/Fast1.Tests.csproj",
            helperProject);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["select-ci-test-projects", "--mode", "full", "--root", repository.RootPath],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(helperProject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Option_like_git_revisions_fail_open_without_invoking_git()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        var attemptedOutput = Path.Combine(repository.RootPath, "git-option-output");
        var githubOutput = Path.Combine(repository.RootPath, "github-output.txt");
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            [
                "select-ci-test-projects",
                "--mode",
                "direct",
                "--base",
                $"--output={attemptedOutput}",
                "--head",
                new string('a', 40),
                "--github-output",
                githubOutput,
                "--root",
                repository.RootPath
            ],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldContain("full hexadecimal Git object IDs", StringComparison.Ordinal);
        var githubOutputs = await File.ReadAllLinesAsync(githubOutput, TestContext.Current.CancellationToken);
        githubOutputs.ShouldContain("selection_fallback=true");
        githubOutputs.ShouldContain("fast_validation_1_required=true");
        githubOutputs.ShouldContain("fast_validation_2_required=true");
        githubOutputs.ShouldContain("fast_validation_required=true");
        File.Exists(attemptedOutput).ShouldBeFalse();
    }

    [Theory]
    [InlineData("deleted")]
    [InlineData("malformed")]
    public async Task Selective_mode_fails_open_when_a_solution_test_project_cannot_be_loaded(string projectState)
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddCanonicalTestSlices();
        const string projectPath = "tests/Fast1.Tests/Fast1.Tests.csproj";
        if (projectState == "deleted")
        {
            File.Delete(Path.Combine(repository.RootPath, projectPath));
        }
        else
        {
            repository.WriteFile(projectPath, "<Project>");
        }

        var outputDirectory = Path.Combine(repository.RootPath, "selected");
        var githubOutput = Path.Combine(repository.RootPath, "github-output.txt");
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            [
                "select-ci-test-projects",
                "--mode",
                "direct",
                "--base",
                new string('a', 40),
                "--head",
                new string('b', 40),
                "--output-directory",
                outputDirectory,
                "--github-output",
                githubOutput,
                "--root",
                repository.RootPath
            ],
            output,
            error,
            repository.RootPath,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldContain("fell back to full validation", StringComparison.Ordinal);
        var githubOutputs = await File.ReadAllLinesAsync(githubOutput, TestContext.Current.CancellationToken);
        githubOutputs.ShouldContain("selection_fallback=true");
        githubOutputs.ShouldContain("fast_validation_required=true");
        var selectedProjects = await File.ReadAllLinesAsync(
            Path.Combine(outputDirectory, "fast-validation-1.txt"),
            TestContext.Current.CancellationToken);
        selectedProjects.ShouldContain(projectPath);
    }
}
