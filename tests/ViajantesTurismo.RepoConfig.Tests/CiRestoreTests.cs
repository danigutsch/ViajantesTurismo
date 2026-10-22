namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiRestoreTests
{
    [Fact]
    public async Task Selected_projects_restore_without_a_generated_solution_becoming_a_package_input()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.AddProject("src/First/First.csproj");
        workspace.AddProject("src/Second/Second.csproj");
        workspace.WriteFile("src/First/First.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""");
        workspace.WriteFile("src/Second/Second.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""");
        workspace.WriteFile("NuGet.Config", "<configuration><packageSources><clear /></packageSources></configuration>");
        workspace.WriteFile("projects.txt", "src/First/First.csproj\nsrc/Second/Second.csproj\n");
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["restore-ci-projects", "--projects-file", "projects.txt"],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(0);
        File.Exists(Path.Combine(workspace.RootPath, "src/First/obj/project.assets.json")).ShouldBeTrue();
        File.Exists(Path.Combine(workspace.RootPath, "src/Second/obj/project.assets.json")).ShouldBeTrue();
    }

    [Fact]
    public async Task Restore_cannot_escape_the_checkout()
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        workspace.WriteFile("projects.txt", "../outside/Outside.csproj");
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var result = await RepoConfigToolApplication.Run(["restore-ci-projects", "--projects-file", "projects.txt"],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(1);
        error.ToString().ShouldContain("checkout", StringComparison.Ordinal);
    }
}
