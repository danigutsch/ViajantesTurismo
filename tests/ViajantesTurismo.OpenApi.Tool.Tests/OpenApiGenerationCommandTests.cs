using System.Diagnostics;

namespace ViajantesTurismo.OpenApi.Tool.Tests;

public sealed class OpenApiGenerationCommandTests
{
    [Fact]
    public void Admin_generation_scopes_configuration_to_the_child_process()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "admin"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, Path.Combine(Path.GetTempPath(), "dotnet"));

        // Assert
        Path.IsPathFullyQualified(startInfo.FileName).ShouldBeTrue();
        startInfo.WorkingDirectory.ShouldBe("/repo");
        startInfo.ArgumentList.ShouldContain("build");
        startInfo.ArgumentList.ShouldContain("src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj");
        startInfo.ArgumentList.ShouldContain("-p:GenerateAdminOpenApiArtifacts=true");
        startInfo.ArgumentList.ShouldContain("--no-restore");
        startInfo.Environment.ContainsKey("OpenApi__BuildGeneration").ShouldBeFalse();
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"].ShouldBe("OpenApiGeneration");
        startInfo.Environment["DOTNET_ENVIRONMENT"].ShouldBe("OpenApiGeneration");
        startInfo.Environment.ContainsKey("Authentication__Authority").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__Issuer").ShouldBeFalse();
    }

    [Fact]
    public void Catalog_refresh_skips_restore()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "catalog", "--refresh"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, Path.Combine(Path.GetTempPath(), "dotnet"));

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
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, Path.Combine(Path.GetTempPath(), "dotnet"));

        // Assert
        startInfo.ArgumentList.ShouldContain("src/ViajantesTurismo.Branding.ApiService/ViajantesTurismo.Branding.ApiService.csproj");
        startInfo.ArgumentList.ShouldContain("-p:RefreshBrandingOpenApiArtifacts=true");
    }

    [Fact]
    public void Generation_environment_removes_hostile_parent_settings()
    {
        // Arrange
        var startInfo = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "dotnet"));
        startInfo.Environment["Authentication__Authority"] = "https://authority.example";
        startInfo.Environment["Authentication__Issuer"] = "https://issuer.example";
        startInfo.Environment["Authentication__AllowHttpDevelopmentAuthority"] = "true";
        startInfo.Environment["Authentication__ClientId"] = "client-id";
        startInfo.Environment["Authentication__ClientSecret"] = "client-secret";
        startInfo.Environment["Authentication__DataProtection__CertificatePath"] = "/certificate.pfx";
        startInfo.Environment["Authentication__DataProtection__CertificatePassword"] = "certificate-password";
        startInfo.Environment["ConnectionStrings__CatalogDatabase"] = "Host=database.example;Password=secret";
        startInfo.Environment["OTEL_EXPORTER_OTLP_ENDPOINT"] = "https://otel.example";
        startInfo.Environment["HTTPS_PROXY"] = "https://proxy.example";
        startInfo.Environment["OpenApiToolTests__Unrelated"] = "sentinel";

        // Act
        OpenApiGenerationCommand.ApplyGenerationEnvironment(startInfo);
        var dotnetDirectory = Path.GetDirectoryName(startInfo.FileName).ShouldNotBeNull();

        // Assert
        startInfo.Environment.ContainsKey("Authentication__Authority").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__Issuer").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__AllowHttpDevelopmentAuthority").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__ClientId").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__ClientSecret").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__DataProtection__CertificatePath").ShouldBeFalse();
        startInfo.Environment.ContainsKey("Authentication__DataProtection__CertificatePassword").ShouldBeFalse();
        startInfo.Environment.ContainsKey("ConnectionStrings__CatalogDatabase").ShouldBeFalse();
        startInfo.Environment.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT").ShouldBeFalse();
        startInfo.Environment.ContainsKey("HTTPS_PROXY").ShouldBeFalse();
        startInfo.Environment.ContainsKey("OpenApiToolTests__Unrelated").ShouldBeFalse();
        startInfo.Environment.ContainsKey("OpenApi__BuildGeneration").ShouldBeFalse();
        startInfo.Environment["PATH"].ShouldContain(dotnetDirectory, StringComparison.Ordinal);
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"].ShouldBe("1");
        startInfo.Environment["OTEL_SDK_DISABLED"].ShouldBe("true");
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
            "dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate",
            StringComparison.Ordinal);
        error.ToString().ShouldBeEmpty();
    }
}
