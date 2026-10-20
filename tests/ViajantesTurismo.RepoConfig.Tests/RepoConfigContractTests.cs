using System.Text.Json;

namespace ViajantesTurismo.RepoConfig.Tests;

public sealed class RepoConfigContractTests
{

    [Fact]
    public async Task Config_schema_matches_shipped_config_contract()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        await CandidateCompatibilityCommand.RunProcess("dotnet",
            ["tool", "run", "sharedkernel-repo", "--", "init", "--root", workspace.RootPath],
            CiTestSelectionTestProcess.GetRepositoryRoot(), TextWriter.Null, TestContext.Current.CancellationToken);

        using var configDocument = JsonDocument.Parse(RoadmapConfigSchemaTestFiles.ReadCheckedInConfig());
        using var checkedInSchemaDocument = JsonDocument.Parse(RoadmapConfigSchemaTestFiles.ReadCheckedInSchema());
        using var generatedSchemaDocument = JsonDocument.Parse(workspace.ReadFile("roadmap/schema/roadmap-config.schema.json"));

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
    public async Task Item_schema_matches_shipped_item_schema_contract()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        await CandidateCompatibilityCommand.RunProcess("dotnet",
            ["tool", "run", "sharedkernel-repo", "--", "init", "--root", workspace.RootPath],
            CiTestSelectionTestProcess.GetRepositoryRoot(), TextWriter.Null, TestContext.Current.CancellationToken);

        using var checkedInSchemaDocument = JsonDocument.Parse(RoadmapConfigSchemaTestFiles.ReadCheckedInItemSchema());
        using var generatedSchemaDocument = JsonDocument.Parse(workspace.ReadFile("roadmap/schema/roadmap-item.schema.json"));

        // Act
        var checkedInSchema = checkedInSchemaDocument.RootElement;
        var generatedSchema = generatedSchemaDocument.RootElement;
        var parentSchema = checkedInSchema.GetProperty("properties").GetProperty("parent");

        // Assert
        JsonElement.DeepEquals(checkedInSchema, generatedSchema).ShouldBeTrue();
        parentSchema.GetProperty("pattern").GetString().ShouldBe("\\S");
    }
}
