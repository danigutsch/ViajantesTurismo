namespace ViajantesTurismo.OpenApi.Tool.Tests;

public sealed class OpenApiGenerationCommandTests
{
    [Fact]
    public void Admin_generation_scopes_configuration_to_the_child_process()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "admin"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, isCi: false);

        // Assert
        startInfo.FileName.ShouldBe("dotnet");
        startInfo.WorkingDirectory.ShouldBe("/repo");
        startInfo.ArgumentList.ShouldContain("build");
        startInfo.ArgumentList.ShouldContain("src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj");
        startInfo.ArgumentList.ShouldContain("-p:GenerateAdminOpenApiArtifacts=true");
        startInfo.ArgumentList.ShouldNotContain("--no-restore");
        startInfo.Environment["OpenApi__BuildGeneration"].ShouldBe("true");
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"].ShouldBe("OpenApiGeneration");
        startInfo.Environment["DOTNET_ENVIRONMENT"].ShouldBe("OpenApiGeneration");
        startInfo.Environment.ContainsKey("Authentication__Authority").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__Issuer").ShouldBeFalse();
    }

    [Fact]
    public void Catalog_refresh_skips_restore_in_ci()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "catalog", "--refresh"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, isCi: true);

        // Assert
        startInfo.ArgumentList.ShouldContain("--no-restore");
        startInfo.ArgumentList.ShouldContain("src/ViajantesTurismo.Catalog.ApiService/ViajantesTurismo.Catalog.ApiService.csproj");
        startInfo.ArgumentList.ShouldContain("-p:RefreshCatalogOpenApiArtifacts=true");
    }

    [Fact]
    public void Branding_refresh_generates_branding_contract_artifacts()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "branding", "--refresh"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, isCi: false);

        // Assert
        startInfo.ArgumentList.ShouldContain("src/ViajantesTurismo.Branding.ApiService/ViajantesTurismo.Branding.ApiService.csproj");
        startInfo.ArgumentList.ShouldContain("-p:RefreshBrandingOpenApiArtifacts=true");
    }

    [Fact]
    public void Invalid_generation_arguments_fail_with_a_clear_message()
    {
        // Arrange
        Action parse = () => OpenApiGenerationOptions.Parse(["generate", "orders"], "/repo");

        // Act
        var exception = parse.ShouldThrow<ArgumentException>();

        // Assert
        exception.Message.ShouldContain("generate <admin|catalog|branding> [--refresh]", StringComparison.Ordinal);
    }

    [Fact]
    public void Resolves_the_repository_root_from_a_nested_directory()
    {
        // Arrange
        var nestedDirectory = Path.Combine(AppContext.BaseDirectory, "nested");

        // Act
        var repositoryRoot = OpenApiToolApplication.FindRepositoryRoot(nestedDirectory);

        // Assert
        File.Exists(Path.Combine(repositoryRoot, "ViajantesTurismo.slnx")).ShouldBeTrue();
    }

    [Fact]
    public async Task Help_displays_the_documented_dotnet_run_invocation()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(["--help"], output, error);

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain(
            "dotnet run --project tools/ViajantesTurismo.OpenApi.Tool -- generate",
            StringComparison.Ordinal);
        error.ToString().ShouldBeEmpty();
    }
}
