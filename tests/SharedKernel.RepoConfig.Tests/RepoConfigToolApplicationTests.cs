using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class RepoConfigToolApplicationTests
{
    [Fact]
    public async Task Verify_reports_missing_roadmap_structure()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], output, error, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Repository config verification failed:", StringComparison.Ordinal);
        errorText.ShouldContain("roadmap: Missing required directory.", StringComparison.Ordinal);
        errorText.ShouldContain("roadmap/config.json: Missing required file.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Init_creates_structure_that_verify_accepts()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var initExitCode = await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken);
        var verifyExitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var verifyOutputText = verifyOutput.ToString();

        // Assert
        initExitCode.ShouldBe(0);
        verifyExitCode.ShouldBe(0);
        verifyOutputText.ShouldContain("Repository config is valid.", StringComparison.Ordinal);
        workspace.FileExists("roadmap/config.json").ShouldBeTrue();
        workspace.FileExists("roadmap/items/RM-001-roadmap-gitops.json").ShouldBeTrue();
    }

    [Fact]
    public async Task Init_writes_full_schema_templates()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken);
        var configSchemaText = workspace.ReadFile("roadmap/schema/roadmap-config.schema.json");
        var itemSchemaText = workspace.ReadFile("roadmap/schema/roadmap-item.schema.json");

        // Assert
        exitCode.ShouldBe(0);
        configSchemaText.ShouldContain("\"required\": [", StringComparison.Ordinal);
        configSchemaText.ShouldContain("\"$schema\": \"https://json-schema.org/draft/2020-12/schema\"", StringComparison.Ordinal);
        configSchemaText.ShouldNotContain("github.com/danigutsch/ViajantesTurismo", StringComparison.Ordinal);
        configSchemaText.ShouldContain("\"integrations\"", StringComparison.Ordinal);
        itemSchemaText.ShouldContain("\"$schema\": \"https://json-schema.org/draft/2020-12/schema\"", StringComparison.Ordinal);
        itemSchemaText.ShouldNotContain("github.com/danigutsch/ViajantesTurismo", StringComparison.Ordinal);
        itemSchemaText.ShouldContain("\"size\"", StringComparison.Ordinal);
        itemSchemaText.ShouldContain("\"uniqueItems\": true", StringComparison.Ordinal);
        itemSchemaText.ShouldContain("\"minimum\": 0.1", StringComparison.Ordinal);
    }

    [Fact]
    public void Config_schema_matches_shipped_config_contract()
    {
        // Arrange
        using var configDocument = JsonDocument.Parse(RoadmapConfigSchemaTestFiles.ReadCheckedInConfig());
        using var checkedInSchemaDocument = JsonDocument.Parse(RoadmapConfigSchemaTestFiles.ReadCheckedInSchema());
        using var generatedSchemaDocument = JsonDocument.Parse(RoadmapTemplates.ConfigSchemaJson);

        // Act
        var checkedInSchema = checkedInSchemaDocument.RootElement;
        var generatedSchema = generatedSchemaDocument.RootElement;
        var projectSchema = checkedInSchema.GetProperty("properties").GetProperty("project");
        var githubSchema = checkedInSchema.GetProperty("properties").GetProperty("integrations").GetProperty("properties").GetProperty("github");

        // Assert
        JsonElement.DeepEquals(checkedInSchema, generatedSchema).ShouldBeTrue();
        RoadmapConfigSchemaTestFiles.ShouldDeclareConfigProperties(configDocument.RootElement, checkedInSchema);
        projectSchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        githubSchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Init_writes_templates_with_resolvable_local_schema_references()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken);
        using var configDocument = JsonDocument.Parse(workspace.ReadFile("roadmap/config.json"));
        using var itemDocument = JsonDocument.Parse(workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json"));
        var configSchema = configDocument.RootElement.GetProperty("$schema").GetString();
        var itemSchema = itemDocument.RootElement.GetProperty("$schema").GetString();
        var configSchemaPath = Path.GetFullPath(Path.Combine(workspace.RootPath, "roadmap", configSchema ?? string.Empty));
        var itemSchemaPath = Path.GetFullPath(Path.Combine(workspace.RootPath, "roadmap/items", itemSchema ?? string.Empty));

        // Assert
        exitCode.ShouldBe(0);
        File.Exists(configSchemaPath).ShouldBeTrue();
        File.Exists(itemSchemaPath).ShouldBeTrue();
    }

    [Fact]
    public async Task Set_updates_github_repository_config()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var setOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var setError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["set", "github.repository", "example/repository", "--root", workspace.RootPath], setOutput, setError, workspace.RootPath, TestContext.Current.CancellationToken);
        var configText = workspace.ReadFile("roadmap/config.json");

        // Assert
        exitCode.ShouldBe(0);
        configText.ShouldContain("\"repository\": \"example/repository\"", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Set_preserves_existing_github_projection_settings_and_valid_config()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var setOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var setError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);

        // Act
        var setExitCode = await RepoConfigToolApplication.Run(["set", "github.repository", "example/repository", "--root", workspace.RootPath], setOutput, setError, workspace.RootPath, TestContext.Current.CancellationToken);
        var verifyExitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(workspace.ReadFile("roadmap/config.json"));
        var github = document.RootElement.GetProperty("integrations").GetProperty("github");

        // Assert
        setExitCode.ShouldBe(0);
        verifyExitCode.ShouldBe(0);
        github.GetProperty("enabled").GetBoolean().ShouldBeFalse();
        github.GetProperty("sourceOfTruth").GetString().ShouldBe("projection");
        github.GetProperty("repository").GetString().ShouldBe("example/repository");
    }

    [Fact]
    public async Task Verify_rejects_obsolete_github_project_field_mapping()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"sourceOfTruth\": \"projection\"", "\"sourceOfTruth\": \"projection\",\n      \"projectFieldMapping\": {}", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("integrations.github.projectFieldMapping is not supported.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_rejects_invalid_github_projection_source_of_truth()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"sourceOfTruth\": \"projection\"", "\"sourceOfTruth\": \"manual\"", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("integrations.github.sourceOfTruth must be projection.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_allows_missing_github_projection_source_of_truth()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace(",\n      \"sourceOfTruth\": \"projection\"", string.Empty, StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(0);
        errorText.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("owner only")]
    [InlineData("owner/repo?state=closed")]
    [InlineData("owner/repo#fragment")]
    [InlineData("owner/repo\\path")]
    [InlineData("owner--name/repository")]
    public async Task Set_rejects_invalid_github_repository_config(string repository)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var setOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var setError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["set", "github.repository", repository, "--root", workspace.RootPath], setOutput, setError, workspace.RootPath, TestContext.Current.CancellationToken);
        var configText = workspace.ReadFile("roadmap/config.json");
        var errorText = setError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("github.repository must be shaped as owner/repository.", StringComparison.Ordinal);
        configText.ShouldNotContain(JsonSerializer.Serialize(repository), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_unknown_dependencies()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"dependencies\": []", "\"dependencies\": [\"RM-404\"]", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown dependency: RM-404.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_duplicate_roadmap_ids_without_aborting_diagnostics()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        workspace.WriteFile("roadmap/items/RM-001-copy.json", workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json"));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Duplicate roadmap item id: RM-001.", StringComparison.Ordinal);
        errorText.ShouldNotContain("sharedkernel-repo:", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_invalid_parent_type()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"theme\":", "\"parent\": 7,\n  \"theme\":", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("parent must be a string when present.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_confidence_below_documented_range()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"confidence\": 0.8", "\"confidence\": 0.09", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("confidence must be between 0.1 and 1.0.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_missing_item_id_prefix()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("  \"itemIdPrefix\": \"RM\",\n", string.Empty, StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required string property: itemIdPrefix.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_missing_integrations_object()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        var integrationsStart = configText.IndexOf("  \"integrations\": {", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/config.json", configText[..integrationsStart].TrimEnd().TrimEnd(',') + Environment.NewLine + "}" + Environment.NewLine);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required object property: integrations.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_duplicate_config_array_values()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"done\",\n      \"dropped\"", "\"done\",\n      \"done\",\n      \"dropped\"", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("project.closedStatuses contains a duplicate value: done.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_invalid_config_array_values()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"done\",\n      \"dropped\"", "\"done\",\n      7", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("project.closedStatuses must contain only non-empty strings.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_rejects_config_array_values_with_leading_or_trailing_whitespace()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"done\",\n      \"dropped\"", "\"done \",\n      \"dropped\"", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("project.closedStatuses must not contain leading or trailing whitespace.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_rejects_item_array_values_with_leading_or_trailing_whitespace()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"area: tooling\"", "\"area: tooling \"", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("labels must not contain leading or trailing whitespace.", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("owner only")]
    [InlineData("owner/repo?state=closed")]
    [InlineData("owner/repo#fragment")]
    [InlineData("owner/repo\\path")]
    public async Task Verify_reports_invalid_enabled_github_repository(string repository)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("\"enabled\": false", $"\"enabled\": true,\n      \"repository\": {JsonSerializer.Serialize(repository)}", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("integrations.github.repository must be shaped as owner/repository when GitHub sync is enabled.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_duplicate_string_array_values_without_cascading_reference_errors()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"dependencies\": []", "\"dependencies\": [\"RM-404\", \"RM-404\"]", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("dependencies contains a duplicate value: RM-404.", StringComparison.Ordinal);
        var unknownDependencyReports = errorText.Split("Unknown dependency: RM-404.", StringSplitOptions.None).Length - 1;
        unknownDependencyReports.ShouldBe(1);
    }

    [Fact]
    public async Task Verify_reports_non_object_theme_file()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        workspace.WriteFile("roadmap/themes/repo-operations.json", "[]");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("roadmap/themes/repo-operations.json: Theme root must be a JSON object.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_items_when_theme_catalog_is_empty()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        workspace.DeleteFile("roadmap/themes/repo-operations.json");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown theme: repo-operations.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_next_unblocked_skips_open_blockers()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var epicText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", epicText.Replace("\"blocks\": []", "\"blocks\": [\"RM-002\"]", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.BlockedIssueJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "next-unblocked", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("RM-001 | epic", StringComparison.Ordinal);
        outputText.ShouldNotContain("RM-002 | issue", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_blockers_of_lists_direct_blockers()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var epicText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", epicText.Replace("\"blocks\": []", "\"blocks\": [\"RM-002\"]", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.BlockedIssueJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "blockers-of", "RM-002", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("RM-001 | epic", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_blockers_of_prefixes_unknown_item_error()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "blockers-of", "RM-999", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();
        var errorText = getError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        outputText.ShouldBe(string.Empty);
        errorText.ShouldBe("sharedkernel-repo: Unknown roadmap item id: RM-999" + Environment.NewLine);
    }

    [Fact]
    public async Task Sync_returns_clean_error_for_noncaller_cancellation()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new CancellationThrowingTextWriter();
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], output, error, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = error.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("sharedkernel-repo: The operation was canceled.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sync_propagates_caller_cancellation()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var output = new CancellationThrowingTextWriter();
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        using var cancellation = new CancellationTokenSource();
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        await cancellation.CancelAsync();

        // Act
        var action = (Func<Task>)(async () => await RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], output, error, workspace.RootPath, cancellation.Token));
        var exception = await action.ShouldThrow<OperationCanceledException>();

        // Assert
        exception.CancellationToken.ShouldBe(cancellation.Token);
    }

    [Fact]
    public async Task Sync_github_dry_run_reports_mapped_issues_without_network()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var syncOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var syncError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], syncOutput, syncError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = syncOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("dry-run: sync labels for owner/repository#997 from RM-001", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_stale_order_file()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-404\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown ordered item: RM-404.", StringComparison.Ordinal);
        errorText.ShouldContain("Missing ordered item: RM-001.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sync_github_dry_run_respects_disabled_integration()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var syncOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var syncError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], syncOutput, syncError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = syncError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("GitHub sync is disabled in roadmap/config.json.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sync_github_dry_run_defaults_to_disabled_when_enabled_is_missing()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var syncOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var syncError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var configText = workspace.ReadFile("roadmap/config.json");
        workspace.WriteFile("roadmap/config.json", configText.Replace("      \"enabled\": false,\n", string.Empty, StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["sync", "github", "--dry-run", "--root", workspace.RootPath], syncOutput, syncError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = syncError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("GitHub sync is disabled in roadmap/config.json.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_duplicate_github_issue_mappings()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.IssueWithGitHubMappingJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Duplicate GitHub issue mapping: 997.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_unknown_theme()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"theme\": \"repo-operations\"", "\"theme\": \"missing-theme\"", StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Unknown theme: missing-theme.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_missing_theme_without_unknown_theme_cascade()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("  \"theme\": \"repo-operations\",\n", string.Empty, StringComparison.Ordinal));

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("Missing required string property: theme.", StringComparison.Ordinal);
        errorText.ShouldNotContain("Unknown theme:", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_non_object_order_file()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        workspace.WriteFile("roadmap/order.json", "[]");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("order.json root must be a JSON object.", StringComparison.Ordinal);
        errorText.ShouldNotContain("sharedkernel-repo:", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_malformed_json_files_with_paths()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        workspace.WriteFile("roadmap/config.json", "{");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", "{");
        workspace.WriteFile("roadmap/themes/repo-operations.json", "{");
        workspace.WriteFile("roadmap/order.json", "{");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("roadmap/config.json: Invalid JSON:", StringComparison.Ordinal);
        errorText.ShouldContain("roadmap/items/RM-001-roadmap-gitops.json: Invalid JSON:", StringComparison.Ordinal);
        errorText.ShouldContain("roadmap/themes/repo-operations.json: Invalid JSON:", StringComparison.Ordinal);
        errorText.ShouldContain("roadmap/order.json: Invalid JSON:", StringComparison.Ordinal);
        errorText.ShouldNotContain("sharedkernel-repo:", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_reports_order_file_sequence_mismatch()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.HigherPriorityIssueJson);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("order.json items must match priority order: order ascending, score descending, then id.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_order_file_uses_priority_tiebreakers()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var tiedItem = RoadmapTestContent.HigherPriorityIssueJson
            .Replace("\"id\": \"RM-002\"", "\"id\": \"RM-000\"", StringComparison.Ordinal)
            .Replace("\"order\": 5", "\"order\": 10", StringComparison.Ordinal)
            .Replace("\"reach\": 10", "\"reach\": 1", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/RM-000-follow-up.json", tiedItem);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-000\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Verify_reports_blocked_by_cycles()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var verifyError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var epicText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", epicText
            .Replace("\"blockedBy\": []", "\"blockedBy\": [\"RM-002\"]", StringComparison.Ordinal)
            .Replace("\"blocks\": []", "\"blocks\": [\"RM-002\"]", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", RoadmapTestContent.BlockedIssueJson
            .Replace("\"blocks\": []", "\"blocks\": [\"RM-001\"]", StringComparison.Ordinal));
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["verify", "--root", workspace.RootPath], verifyOutput, verifyError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = verifyError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("blockedBy cycle includes", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_next_priority_orders_items_by_order_score_and_id()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var defaultItem = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        var lowerScoreItem = RoadmapTestContent.HigherPriorityIssueJson
            .Replace("\"id\": \"RM-002\"", "\"id\": \"RM-000\"", StringComparison.Ordinal)
            .Replace("\"order\": 5", "\"order\": 10", StringComparison.Ordinal)
            .Replace("\"reach\": 10", "\"reach\": 1", StringComparison.Ordinal);
        var sameScoreItem = defaultItem
            .Replace("\"id\": \"RM-001\"", "\"id\": \"RM-002\"", StringComparison.Ordinal)
            .Replace("Establish GitOps roadmap and repo configuration tooling", "Same score follow-up", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/RM-000-follow-up.json", lowerScoreItem);
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", sameScoreItem);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\", \"RM-000\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "next-priority", "--limit", "3", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();
        var firstIndex = outputText.IndexOf("RM-001 |", StringComparison.Ordinal);
        var secondIndex = outputText.IndexOf("RM-002 |", StringComparison.Ordinal);
        var thirdIndex = outputText.IndexOf("RM-000 |", StringComparison.Ordinal);

        // Assert
        exitCode.ShouldBe(0);
        firstIndex.ShouldBeGreaterThan(-1);
        secondIndex.ShouldBeGreaterThan(-1);
        thirdIndex.ShouldBeGreaterThan(-1);
        firstIndex.ShouldBeLessThan(secondIndex);
        secondIndex.ShouldBeLessThan(thirdIndex);
    }

    [Fact]
    public async Task Get_next_blockers_orders_equal_blockers_by_id()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var defaultItem = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        var laterId = defaultItem
            .Replace("\"id\": \"RM-001\"", "\"id\": \"RM-003\"", StringComparison.Ordinal)
            .Replace("\"type\": \"epic\"", "\"type\": \"blocker\"", StringComparison.Ordinal);
        var earlierId = defaultItem
            .Replace("\"id\": \"RM-001\"", "\"id\": \"RM-002\"", StringComparison.Ordinal)
            .Replace("\"type\": \"epic\"", "\"type\": \"blocker\"", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/a-blocker.json", laterId);
        workspace.WriteFile("roadmap/items/b-blocker.json", earlierId);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\", \"RM-003\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "next-blockers", "--limit", "2", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldStartWith("RM-002 | blocker", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_low_hanging_fruit_orders_equal_items_by_id()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var defaultItem = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        var laterId = defaultItem
            .Replace("\"id\": \"RM-001\"", "\"id\": \"RM-003\"", StringComparison.Ordinal)
            .Replace("\"type\": \"epic\"", "\"type\": \"issue\"", StringComparison.Ordinal);
        var earlierId = defaultItem
            .Replace("\"id\": \"RM-001\"", "\"id\": \"RM-002\"", StringComparison.Ordinal)
            .Replace("\"type\": \"epic\"", "\"type\": \"issue\"", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/a-issue.json", laterId);
        workspace.WriteFile("roadmap/items/b-issue.json", earlierId);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\", \"RM-003\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "low-hanging-fruit", "--type", "issue", "--limit", "2", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldStartWith("RM-002 | issue", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_pareto_orders_equal_score_and_effort_by_order_then_id()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var defaultItem = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        var items = new[]
        {
            (FileName: "a-issue.json", Id: "RM-002", Order: 20),
            (FileName: "b-issue.json", Id: "RM-003", Order: 10),
            (FileName: "c-issue.json", Id: "RM-006", Order: 10),
            (FileName: "d-issue.json", Id: "RM-004", Order: 30),
            (FileName: "e-issue.json", Id: "RM-005", Order: 40),
            (FileName: "f-issue.json", Id: "RM-007", Order: 50)
        };
        foreach (var item in items)
        {
            var itemText = defaultItem
                .Replace("\"id\": \"RM-001\"", $"\"id\": \"{item.Id}\"", StringComparison.Ordinal)
                .Replace("\"type\": \"epic\"", "\"type\": \"issue\"", StringComparison.Ordinal)
                .Replace("\"order\": 10", $"\"order\": {item.Order}", StringComparison.Ordinal);
            workspace.WriteFile($"roadmap/items/{item.FileName}", itemText);
        }

        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-003\", \"RM-006\", \"RM-002\", \"RM-004\", \"RM-005\", \"RM-007\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "pareto", "--type", "issue", "--limit", "2", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldStartWith("RM-003 | issue", StringComparison.Ordinal);
        outputText.ShouldContain("RM-006 | issue", StringComparison.Ordinal);
        outputText.ShouldNotContain("RM-002 | issue", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_pareto_limit_uses_unblocked_open_items()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        var epicText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", epicText.Replace("\"blocks\": []", "\"blocks\": [\"RM-003\", \"RM-004\", \"RM-005\", \"RM-006\"]", StringComparison.Ordinal));
        var unblockedItem = RoadmapTestContent.UnblockedEnablerJson.Replace("\"id\": \"RM-003\"", "\"id\": \"RM-002\"", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/RM-002-enabler.json", unblockedItem);
        for (var index = 3; index <= 6; index++)
        {
            var blockedItem = RoadmapTestContent.BlockedIssueJson
                .Replace("\"id\": \"RM-002\"", $"\"id\": \"RM-00{index}\"", StringComparison.Ordinal)
                .Replace("\"order\": 20", $"\"order\": {index * 10 + 10}", StringComparison.Ordinal);
            workspace.WriteFile($"roadmap/items/RM-00{index}-blocked.json", blockedItem);
        }

        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\", \"RM-003\", \"RM-004\", \"RM-005\", \"RM-006\"] }");

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "pareto", "--limit", "10", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var outputText = getOutput.ToString();

        // Assert
        exitCode.ShouldBe(0);
        outputText.ShouldContain("RM-002 |", StringComparison.Ordinal);
        outputText.ShouldNotContain("RM-001 |", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sync_github_dry_run_orders_mapped_items_by_priority()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var defaultItem = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", defaultItem.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var followUpItem = RoadmapTestContent.HigherPriorityIssueJson
            .Replace("\"id\": \"RM-002\"", "\"id\": \"RM-000\"", StringComparison.Ordinal)
            .Replace("\"order\": 5", "\"order\": 10", StringComparison.Ordinal)
            .Replace("\"reach\": 10", "\"reach\": 1", StringComparison.Ordinal)
            .Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 998 } },\n  \"labels\": [", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/RM-000-follow-up.json", followUpItem);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-000\"] }");
        var syncer = new GitHubRoadmapSyncer(RoadmapProject.Load(workspace.RootPath));

        // Act
        var result = syncer.Preview();

        // Assert
        result.Messages[0].ShouldBe("dry-run: sync labels for owner/repository#997 from RM-001");
        result.Messages[1].ShouldBe("dry-run: sync labels for owner/repository#998 from RM-000");
    }

    [Fact]
    public async Task Sync_github_apply_syncs_labels_after_confirming_the_mapping_is_not_a_pull_request()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.NotFound, "{}");
        handler.EnqueueJson(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        var result = await syncer.Apply(TestContext.Current.CancellationToken);

        // Assert
        result.Messages.ShouldContain("updated labels for owner/repository#997 from RM-001");
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].PathAndQuery.ShouldBe("/repos/owner/repository/pulls/997");
        handler.Requests[1].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[1].PathAndQuery.ShouldBe("/repos/owner/repository/issues/997/labels");
        handler.Requests.ShouldNotContain(request => request.Method == HttpMethod.Patch);
        handler.Requests[1].Body.ShouldNotBeNull();
        handler.Requests[1].Body.ShouldContain("area: tooling", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sync_github_apply_skips_mapped_items_without_labels()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        var mappedItem = itemText
            .Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal)
            .Replace("\"labels\": [\n    \"area: tooling\",\n    \"configuration\",\n    \"documentation\"\n  ]", "\"labels\": []", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", mappedItem);
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = new TestHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        var result = await syncer.Apply(TestContext.Current.CancellationToken);

        // Assert
        result.Messages.ShouldContain("skipped owner/repository#997 from RM-001 because it has no labels");
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sync_github_apply_resets_the_timeout_for_each_item()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var defaultItem = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", defaultItem.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var followUpItem = RoadmapTestContent.IssueWithGitHubMappingJson.Replace("\"issue\": 997", "\"issue\": 998", StringComparison.Ordinal);
        workspace.WriteFile("roadmap/items/RM-002-follow-up.json", followUpItem);
        workspace.WriteFile("roadmap/order.json", "{ \"items\": [\"RM-001\", \"RM-002\"] }");
        var project = RoadmapProject.Load(workspace.RootPath);
        var timeProvider = new FakeTimeProvider();
        using var handler = new TimeAdvancingHttpMessageHandler(timeProvider);
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient, timeProvider);

        // Act
        var result = await syncer.Apply(TestContext.Current.CancellationToken);

        // Assert
        result.Messages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Sync_github_apply_throws_timeout_when_the_per_item_limit_is_reached()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        var timeProvider = new FakeTimeProvider();
        using var handler = new TimingOutHttpMessageHandler(timeProvider);
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient, timeProvider);

        // Act
        Func<Task> action = () => syncer.Apply(TestContext.Current.CancellationToken);
        var exception = await action.ShouldThrow<GitHubSyncTimeoutException>();

        // Assert
        exception.Message.ShouldBe("GitHub sync timed out after 30 seconds.");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "GitHub pull request check failed for #997: HTTP 401 (authentication required).")]
    [InlineData(HttpStatusCode.Forbidden, "GitHub pull request check failed for #997: HTTP 403 (access denied or rate limited).")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "GitHub pull request check failed for #997: HTTP 422 (request validation failed).")]
    [InlineData(HttpStatusCode.TooManyRequests, "GitHub pull request check failed for #997: HTTP 429 (rate limited).")]
    [InlineData(HttpStatusCode.BadGateway, "GitHub pull request check failed for #997: HTTP 502.")]
    public async Task Sync_github_apply_uses_safe_status_hints_for_pull_request_check_failures(HttpStatusCode statusCode, string expectedMessage)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = new TestHttpMessageHandler();
        handler.Enqueue(GitHubSyncTestResponseFactory.UntrustedError(statusCode));
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        Func<Task> action = () => syncer.Apply(TestContext.Current.CancellationToken);
        var exception = await action.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe(expectedMessage);
        exception.Message.ShouldNotContain("ghp_reason_token", StringComparison.Ordinal);
        exception.Message.ShouldNotContain("ghp_body_token", StringComparison.Ordinal);
        exception.Message.ShouldNotContain("ghp_header_token", StringComparison.Ordinal);
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].PathAndQuery.ShouldBe("/repos/owner/repository/pulls/997");
    }

    [Fact]
    public async Task Sync_github_apply_does_not_expose_remote_details_for_label_failure()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.NotFound, "{}");
        handler.Enqueue(GitHubSyncTestResponseFactory.UntrustedError(HttpStatusCode.UnprocessableEntity));
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        Func<Task> action = () => syncer.Apply(TestContext.Current.CancellationToken);
        var exception = await action.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("GitHub label sync failed for #997: HTTP 422 (request validation failed).");
        exception.Message.ShouldNotContain("ghp_reason_token", StringComparison.Ordinal);
        exception.Message.ShouldNotContain("ghp_body_token", StringComparison.Ordinal);
        exception.Message.ShouldNotContain("ghp_header_token", StringComparison.Ordinal);
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[1].Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task Sync_github_apply_rejects_pull_request_mappings()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var initOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var initError = new StringWriter(CultureInfo.InvariantCulture);
        (await RepoConfigToolApplication.Run(["init", "--root", workspace.RootPath], initOutput, initError, workspace.RootPath, TestContext.Current.CancellationToken)).ShouldBe(0);
        RoadmapConfigTestOperations.EnableGitHubSync(workspace);
        var itemText = workspace.ReadFile("roadmap/items/RM-001-roadmap-gitops.json");
        workspace.WriteFile("roadmap/items/RM-001-roadmap-gitops.json", itemText.Replace("\"labels\": [", "\"integrations\": { \"github\": { \"issue\": 997 } },\n  \"labels\": [", StringComparison.Ordinal));
        var project = RoadmapProject.Load(workspace.RootPath);
        using var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);
        var syncer = new GitHubRoadmapSyncer(project, httpClient);

        // Act
        Func<Task> action = () => syncer.Apply(TestContext.Current.CancellationToken);
        var exception = await action.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("points to a pull request", StringComparison.Ordinal);
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].PathAndQuery.ShouldBe("/repos/owner/repository/pulls/997");
    }

    [Fact]
    public async Task Get_by_label_rejects_unquoted_label_with_spaces()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "by-label", "area:", "tooling", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = getError.ToString();

        // Assert
        exitCode.ShouldBe(2);
        errorText.ShouldContain("Unknown or incomplete get query: by-label", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_reports_tool_name_for_config_errors()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        using var getOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var getError = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(["get", "next-priority", "--root", workspace.RootPath], getOutput, getError, workspace.RootPath, TestContext.Current.CancellationToken);
        var errorText = getError.ToString();

        // Assert
        exitCode.ShouldBe(1);
        errorText.ShouldContain("sharedkernel-repo: Roadmap config is invalid.", StringComparison.Ordinal);
    }
}
