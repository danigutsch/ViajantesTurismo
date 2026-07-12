using System.Globalization;
using System.Net;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class RepoConfigToolApplicationTests
{
    [Fact]
    public void Verify_reports_missing_roadmap_structure()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], output, error, workspace.RootPath);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Repository config verification failed:", StringComparison.Ordinal);
        errorText.ShouldContain("roadmap: Missing required directory.", StringComparison.Ordinal);
        errorText.ShouldContain("roadmap/config.json: Missing required file.", StringComparison.Ordinal);
    }

    [Fact]
    public void Init_creates_structure_that_verify_accepts()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var initExitCode = RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath);
        var verifyExitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var verifyOutputText = verifyOutput.ToString();

        // Assert
        initExitCode.ShouldBe(0);
        verifyExitCode.ShouldBe(0);
        verifyOutputText.ShouldContain("Repository config is valid.", StringComparison.Ordinal);
        workspace.FileExists("roadmap/config.json").ShouldBeTrue();
        workspace.FileExists("roadmap/items/RM-001-roadmap-gitops.json").ShouldBeTrue();
    }

    [Fact]
    public void Set_updates_github_repository_config()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var setOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var setError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["set", "github.repository", "example/repository", "--root", workspace.RootPath], setOutput, setError, workspace.RootPath);
        var configText = workspace.ReadFile("roadmap/config.json");

        // Assert
        exitCode.ShouldBe(0);
        configText.ShouldContain("\"repository\": \"example/repository\"", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_unknown_dependencies()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"dependencies\": []", "\"dependencies\": [\"RM-404\"]", StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown dependency: RM-404.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_confidence_below_documented_range()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"confidence\": 0.8", "\"confidence\": 0.09", StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("confidence must be between 0.1 and 1.0.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_missing_item_id_prefix()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("  \"itemIdPrefix\": \"RM\",\n", string.Empty, StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required string property: itemIdPrefix.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_missing_integrations_object()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        var integrationsStart = configText.IndexOf("  \"integrations\": {", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/config.json", configText[..integrationsStart].TrimEnd().TrimEnd(',') + Environment.NewLine + "}" + Environment.NewLine);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required object property: integrations.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_duplicate_string_array_values()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"gitops\",", "\"gitops\",\n    \"gitops\",", StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("tags contains a duplicate value: gitops.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_non_object_theme_file()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        workspace.WriteFile("roadmap/themes/repo-operations.json", "[]");

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("roadmap/themes/repo-operations.json: Theme root must be a JSON object.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_items_when_theme_catalog_is_empty()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        workspace.DeleteFile("roadmap/themes/repo-operations.json");

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown theme: repo-operations.", StringComparison.Ordinal);
    }

    [Fact]
    public void Get_next_unblocked_skips_open_blockers()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var epicText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", epicText.Replace("\"blocks\": []", "\"blocks\": [\"RM-002\"]", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.BlockedIssueJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = RepoConfigToolApplication.Run(["get", "next-unblocked", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("RM-001 | epic", StringComparison.Ordinal);
        outputText.ShouldNotContain("RM-002 | issue", StringComparison.Ordinal);
    }

    [Fact]
    public void Get_blockers_of_lists_direct_blockers()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var epicText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", epicText.Replace("\"blocks\": []", "\"blocks\": [\"RM-002\"]", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.BlockedIssueJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = RepoConfigToolApplication.Run(["get", "blockers-of", "RM-002", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("RM-001 | epic", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_github_dry_run_reports_mapped_issues_without_network()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var syncOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var syncError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], syncOutput, syncError, workspace.RootPath);
        var outputText = syncOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("dry-run: update owner/repository#997 from RM-001", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_stale_order_file()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-404\"] }");

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown ordered item: RM-404.", StringComparison.Ordinal);
        errorText.ShouldContain("Missing ordered item: RM-001.", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_github_dry_run_respects_disabled_integration()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var syncOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var syncError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"enabled\": true", "\"enabled\": false", StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], syncOutput, syncError, workspace.RootPath);
        var errorText = syncError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("GitHub sync is disabled in roadmap/config.json.", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_github_dry_run_defaults_to_disabled_when_enabled_is_missing()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var syncOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var syncError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"enabled\": true,\n      ", string.Empty, StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], syncOutput, syncError, workspace.RootPath);
        var errorText = syncError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("GitHub sync is disabled in roadmap/config.json.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_duplicate_github_issue_mappings()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.IssueWithGitHubMappingJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Duplicate GitHub issue mapping: 997.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_unknown_theme()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"theme\": \"repo-operations\"", "\"theme\": \"missing-theme\"", StringComparison.Ordinal));

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown theme: missing-theme.", StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_reports_order_file_sequence_mismatch()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.HigherPriorityIssueJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("order.json items must match item order values.", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_managed_section_rejects_malformed_markers()
    {
        // Arrange
        const string CurrentBody = "before\n<!-- roadmap:managed:start -->\nmissing end";
        const string ManagedSection = "<!-- roadmap:managed:start -->\nnew\n<!-- roadmap:managed:end -->";

        // Act
        var action = (Func<object?>)(() => GitHubRoadmapSyncer.UpsertManagedSection(CurrentBody, ManagedSection));

        // Assert
        action.ShouldThrow<InvalidOperationException>().Message.ShouldContain("malformed roadmap managed-section markers", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_managed_section_preserves_unmanaged_whitespace()
    {
        // Arrange
        var currentBody = $"  before  {Environment.NewLine}<!-- roadmap:managed:start -->old<!-- roadmap:managed:end -->{Environment.NewLine}  after  ";
        var managedSection = $"<!-- roadmap:managed:start -->{Environment.NewLine}new{Environment.NewLine}<!-- roadmap:managed:end -->";

        // Act
        var result = GitHubRoadmapSyncer.UpsertManagedSection(currentBody, managedSection);

        // Assert
        result.ShouldBe($"  before  {Environment.NewLine}{managedSection}{Environment.NewLine}  after  ");
    }

    [Fact]
    public void Sync_github_apply_updates_managed_section_and_labels()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = new TestGitHubMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, "{ \"body\": \"before\" }");
        handler.EnqueueJson(HttpStatusCode.OK, "{}");
        handler.EnqueueJson(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        var result = syncer.Sync(dryRun: false);

        // Assert
        result.Messages.ShouldContain("updated owner/repository#997 from RM-001");
        handler.Requests.Count.ShouldBe(3);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[1].Method.ShouldBe(HttpMethod.Patch);
        handler.Requests[1].Body.ShouldNotBeNull();
        handler.Requests[1].Body.ShouldContain("roadmap:managed:start", StringComparison.Ordinal);
        handler.Requests[1].Body.ShouldNotContain("\"title\"", StringComparison.Ordinal);
        handler.Requests[2].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[2].Body.ShouldNotBeNull();
        handler.Requests[2].Body.ShouldContain("area: tooling", StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_github_apply_rejects_pull_request_mappings()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = new TestGitHubMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, "{ \"body\": \"before\", \"pull_request\": {} }");
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        var action = (Func<object?>)(() => syncer.Sync(dryRun: false));

        // Assert
        action.ShouldThrow<InvalidOperationException>().Message.ShouldContain("points to a pull request", StringComparison.Ordinal);
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    public void Get_by_label_rejects_unquoted_label_with_spaces()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["get", "by-label", "area:", "tooling", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath);
        var errorText = getError.ToString();

        // Assert
        exitCode.ShouldBe(2);
        errorText.ShouldContain("Unknown or incomplete get query: by-label", StringComparison.Ordinal);
    }

    [Fact]
    public void Get_reports_tool_name_for_config_errors()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = RepoConfigToolApplication.Run(["get", "next-priority", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath);
        var errorText = getError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("sharedkernel-repo: Roadmap config is invalid.", StringComparison.Ordinal);
    }
}
