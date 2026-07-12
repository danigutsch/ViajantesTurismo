using System.Globalization;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class RepoConfigToolCoverageTests
{
    [Fact]
    public void Program_help_returns_success()
    {
        // Arrange
        string[] args = ["--help"];

        // Act
        var exitCode = Program.Main(args);

        // Assert
        exitCode.ShouldBe(0);
    }

    [Fact]
    public void Unknown_command_returns_usage_error()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["unknown"], output, error, workspace.RootPath);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(2);
        errorText.ShouldContain("Unknown command: unknown", StringComparison.Ordinal);
    }

    [Fact]
    public void Init_reports_when_structure_already_exists()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var firstOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var firstError = new StringWriter(CultureInfo.InvariantCulture);
        using var secondOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var secondError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], firstOutput, firstError, workspace.RootPath).ShouldBe(0);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], secondOutput, secondError, workspace.RootPath);
        var outputText = secondOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("Repository roadmap structure is already initialized.", StringComparison.Ordinal);
    }

    [Fact]
    public void Diff_reports_valid_and_invalid_structure()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var validOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var validError = new StringWriter(CultureInfo.InvariantCulture);
        using var invalidOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var invalidError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);

        // Act
        var validExitCode = RepoConfigToolApplication.Run(["diff", "--root", workspace.RootPath], validOutput, validError, workspace.RootPath);
        workspace.DeleteFile("roadmap/config.json");
        var invalidExitCode = RepoConfigToolApplication.Run(["diff", "--root", workspace.RootPath], invalidOutput, invalidError, workspace.RootPath);

        // Assert
        validExitCode.ShouldBe(0);
        validOutput.ToString().ShouldContain("Repository config has no drift.", StringComparison.Ordinal);
        invalidExitCode.ShouldBe(1);
        invalidError.ToString().ShouldContain("Repository config drift:", StringComparison.Ordinal);
    }

    [Fact]
    public void Set_reports_usage_and_unsupported_key_errors()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var missingOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var missingError = new StringWriter(CultureInfo.InvariantCulture);
        using var extraOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var extraError = new StringWriter(CultureInfo.InvariantCulture);
        using var unsupportedOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var unsupportedError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);

        // Act
        var missingExitCode = RepoConfigToolApplication.Run(["set", "--root", workspace.RootPath], missingOutput, missingError, workspace.RootPath);
        var extraExitCode = RepoConfigToolApplication.Run(["set", "github.repository", "owner/repo", "extra", "--root", workspace.RootPath], extraOutput, extraError, workspace.RootPath);
        var unsupportedExitCode = RepoConfigToolApplication.Run(["set", "unknown", "value", "--root", workspace.RootPath], unsupportedOutput, unsupportedError, workspace.RootPath);

        // Assert
        missingExitCode.ShouldBe(2);
        missingError.ToString().ShouldContain("Missing set key and value.", StringComparison.Ordinal);
        extraExitCode.ShouldBe(2);
        extraError.ToString().ShouldContain("Unknown argument: extra", StringComparison.Ordinal);
        unsupportedExitCode.ShouldBe(1);
        unsupportedError.ToString().ShouldContain("sharedkernel-repo: Unsupported config key: unknown", StringComparison.Ordinal);
    }

    [Fact]
    public void Get_project_queries_cover_priority_taxonomy_and_blocker_views()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var epicText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", epicText.Replace("\"blocks\": []", "\"blocks\": [\"RM-002\"]", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.BlockedIssueJson);
        workspace.WriteFile("roadmap/items/RM-003-enabler.json", RoadmapTestContent.UnblockedEnablerJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\", \"RM-003\"] }");
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        using var nextIssueOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var nextIssueError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var nextPriority = RepoConfigToolApplication.Run(["get", "next-priority", "--limit", "2", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var nextBlockers = RepoConfigToolApplication.Run(["get", "next-blockers", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var nextEnablers = RepoConfigToolApplication.Run(["get", "next-enablers", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var lowHangingFruit = RepoConfigToolApplication.Run(["get", "low-hanging-fruit", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var pareto = RepoConfigToolApplication.Run(["get", "pareto", "--limit", "1", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var blockingOverview = RepoConfigToolApplication.Run(["get", "blocking-overview", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var tags = RepoConfigToolApplication.Run(["get", "tags", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var labels = RepoConfigToolApplication.Run(["get", "labels", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var byTag = RepoConfigToolApplication.Run(["get", "by-tag", "enabler", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var byLabel = RepoConfigToolApplication.Run(["get", "by-label", "area: tooling", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var nextIssue = RepoConfigToolApplication.Run(["get", "next-unblocked", "--type", "issue", "--root", workspace.RootPath], nextIssueOutput, nextIssueError, workspace.RootPath);
        var outputText = output.ToString();
        var nextIssueOutputText = nextIssueOutput.ToString();

        // Assert
        nextPriority.ShouldBe(0);
        nextBlockers.ShouldBe(0);
        nextEnablers.ShouldBe(0);
        lowHangingFruit.ShouldBe(0);
        pareto.ShouldBe(0);
        blockingOverview.ShouldBe(0);
        tags.ShouldBe(0);
        labels.ShouldBe(0);
        byTag.ShouldBe(0);
        byLabel.ShouldBe(0);
        nextIssue.ShouldBe(0);
        outputText.ShouldContain("RM-001 | epic", StringComparison.Ordinal);
        outputText.ShouldContain("RM-003 | enabler", StringComparison.Ordinal);
        outputText.ShouldContain("RM-002 blocked by RM-001", StringComparison.Ordinal);
        outputText.ShouldContain("enabler | 1", StringComparison.Ordinal);
        outputText.ShouldContain("area: tooling | 3", StringComparison.Ordinal);
        nextIssueOutputText.ShouldContain("No matching roadmap items.", StringComparison.Ordinal);
    }

    [Fact]
    public void Get_option_validation_reports_usage_before_loading_config()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var missingType = RepoConfigToolApplication.Run(["get", "next-priority", "--type", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var invalidLimit = RepoConfigToolApplication.Run(["get", "next-priority", "--limit", "none", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var unknownOption = RepoConfigToolApplication.Run(["get", "next-priority", "--unknown", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var errorText = error.ToString();

        // Assert
        missingType.ShouldBe(2);
        invalidLimit.ShouldBe(2);
        unknownOption.ShouldBe(2);
        errorText.ShouldContain("Missing required value for --type.", StringComparison.Ordinal);
        errorText.ShouldContain("Missing or invalid required value for --limit.", StringComparison.Ordinal);
        errorText.ShouldContain("Unknown get option: --unknown", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_reports_usage_errors_and_empty_mappings()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);

        // Act
        var missingTarget = RepoConfigToolApplication.Run(["sync", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var conflictingFlags = RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--apply", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var unknownArgument = RepoConfigToolApplication.Run(["sync", "github", "--unknown", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var emptyMappings = RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var errorText = error.ToString();
        var outputText = output.ToString();

        // Assert
        missingTarget.ShouldBe(2);
        conflictingFlags.ShouldBe(2);
        unknownArgument.ShouldBe(2);
        emptyMappings.ShouldBe(0);
        errorText.ShouldContain("Missing sync target: github.", StringComparison.Ordinal);
        errorText.ShouldContain("Use either --dry-run or --apply, not both.", StringComparison.Ordinal);
        errorText.ShouldContain("Unknown sync argument.", StringComparison.Ordinal);
        outputText.ShouldContain("No roadmap items have GitHub issue mappings.", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_propagates_http_request_errors_for_application_boundary()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = TestHttpMessageHandler.FromException(new HttpRequestException("network unavailable"));
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        var action = (Func<object?>)(() => syncer.Sync(dryRun: false));

        // Assert
        action.ShouldThrow<HttpRequestException>().Message.ShouldContain("network unavailable", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_config_and_item_policy_violations()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        workspace.WriteFile("roadmap/config.json", RoadmapTestContent.InvalidConfigJson);
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", RoadmapTestContent.InvalidItemJson);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("sourceOfTruth must be repository.", StringComparison.Ordinal);
        errorText.ShouldContain("allowed.statuses must contain at least one status.", StringComparison.Ordinal);
        errorText.ShouldContain("project.ordering must be order.", StringComparison.Ordinal);
        errorText.ShouldContain("integrations.github.enabled must be a Boolean.", StringComparison.Ordinal);
        errorText.ShouldContain("scoring.model must be RICE.", StringComparison.Ordinal);
        errorText.ShouldContain("id must start with RM-.", StringComparison.Ordinal);
        errorText.ShouldContain("Unknown roadmap type: mystery.", StringComparison.Ordinal);
        errorText.ShouldContain("order must be 1 or greater.", StringComparison.Ordinal);
        errorText.ShouldContain("blockedBy must be an array.", StringComparison.Ordinal);
    }
}
