using System.Globalization;

namespace SharedKernel.RepoConfig.Tests;

public sealed class CiTestProjectSelectionCommandTests
{
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
    public async Task Selection_rejects_manifest_drift_from_the_fixed_workflow_matrix(string drift)
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
        error.ToString().ShouldContain("CI test slice manifests", StringComparison.Ordinal);
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
            "mediator-heavy",
            "tests/Mediator.Tests/Mediator.Tests.csproj",
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
        error.ToString().ShouldContain("fast-validation-1, mediator-heavy", StringComparison.Ordinal);
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
