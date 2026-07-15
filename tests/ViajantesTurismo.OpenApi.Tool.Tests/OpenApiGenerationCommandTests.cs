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
    public void Admin_refresh_requests_refresh_contract_artifacts()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "admin", "--refresh"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, Path.Combine(Path.GetTempPath(), "dotnet"));

        // Assert
        startInfo.ArgumentList.ShouldContain("-p:RefreshAdminOpenApiArtifacts=true");
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
    public void Catalog_generation_requests_generated_contract_artifacts()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "catalog"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, Path.Combine(Path.GetTempPath(), "dotnet"));

        // Assert
        startInfo.ArgumentList.ShouldContain("-p:GenerateCatalogOpenApiArtifacts=true");
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
    public void Branding_generation_requests_generated_contract_artifacts()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "branding"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options, Path.Combine(Path.GetTempPath(), "dotnet"));

        // Assert
        startInfo.ArgumentList.ShouldContain("-p:GenerateBrandingOpenApiArtifacts=true");
    }

    [Fact]
    public void Unknown_generation_target_reports_the_invalid_target_value()
    {
        // Arrange
        var options = new OpenApiGenerationOptions((OpenApiTarget)999, false, "/repo");

        // Act
        Action createStartInfo = () => OpenApiGenerationCommand.CreateStartInfo(
            options,
            Path.Combine(Path.GetTempPath(), "dotnet"));
        var exception = createStartInfo.ShouldThrow<ArgumentOutOfRangeException>();

        // Assert
        exception.ParamName.ShouldBe("options");
        exception.ActualValue.ShouldBeOfType<OpenApiTarget>().ShouldBe((OpenApiTarget)999);
    }

    [Fact]
    public void Uses_the_current_absolute_dotnet_host_by_default()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "admin"], "/repo");

        // Act
        var startInfo = OpenApiGenerationCommand.CreateStartInfo(options);
        var dotnetHostPath = startInfo.FileName;

        // Assert
        Path.IsPathFullyQualified(dotnetHostPath).ShouldBeTrue();
        File.Exists(dotnetHostPath).ShouldBeTrue();
        Path.GetFileNameWithoutExtension(dotnetHostPath).ShouldBe("dotnet");
    }

    [Fact]
    public void Rejects_a_relative_dotnet_host_path()
    {
        // Arrange
        var options = OpenApiGenerationOptions.Parse(["generate", "admin"], "/repo");

        // Act
        Action createStartInfo = () => OpenApiGenerationCommand.CreateStartInfo(options, "dotnet");
        var exception = createStartInfo.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe("dotnetHostPath");
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
    public async Task Invalid_generation_arguments_return_usage_error()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["generate", "orders"],
            output,
            error,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(2);
        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Error: Expected: generate <admin|catalog|branding> [--refresh].", StringComparison.Ordinal);
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
    public void Rejects_a_directory_outside_the_repository()
    {
        // Arrange
        using var processScope = new OpenApiToolTestProcessScope();

        // Act
        Action findRepositoryRoot = () => OpenApiToolApplication.FindRepositoryRoot(processScope.GeneratedDirectory);
        var exception = findRepositoryRoot.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("Could not locate ViajantesTurismo.slnx", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_an_operational_error_when_process_start_returns_no_child()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["generate", "admin"],
            output,
            error,
            OpenApiToolTestProcessScope.ReturnNoProcess,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Error: OpenAPI generation failed: Could not start dotnet build.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_an_operational_error_when_process_start_fails()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["generate", "admin"],
            output,
            error,
            OpenApiToolTestProcessScope.ThrowProcessStartFailure,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Error: OpenAPI generation failed: The test process could not start.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_incomplete_cleanup_for_an_unstarted_child_process()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["generate", "admin"],
            output,
            error,
            OpenApiToolTestProcessScope.ReturnUnstartedProcess,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Child process cleanup did not complete.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Help_displays_the_documented_dotnet_run_invocation()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["--help"],
            output,
            error,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain(
            "dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate",
            StringComparison.Ordinal);
        error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Empty_arguments_display_the_documented_dotnet_run_invocation()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            [],
            output,
            error,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain(
            "dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate",
            StringComparison.Ordinal);
        error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Short_help_displays_the_documented_dotnet_run_invocation()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["-h"],
            output,
            error,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain(
            "dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate",
            StringComparison.Ordinal);
        error.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("admin", "ViajantesTurismo.Admin.Contracts.Http", "ViajantesTurismo.Admin.ApiService", "Admin OpenAPI")]
    [InlineData("catalog", "ViajantesTurismo.Catalog.Contracts.Http", "ViajantesTurismo.Catalog.ApiService", "Catalog OpenAPI")]
    [InlineData("branding", "ViajantesTurismo.Branding.Contracts.Http", "ViajantesTurismo.Branding.ApiService", "Branding OpenAPI")]
    public async Task Generates_fresh_expected_OpenApi_artifacts_in_a_test_owned_directory(
        string target,
        string contractsProjectName,
        string apiAssemblyName,
        string artifactDisplayName)
    {
        // Arrange
        var repositoryRoot = OpenApiToolApplication.FindRepositoryRoot(Directory.GetCurrentDirectory());
        var canonicalDirectory = Path.Combine(repositoryRoot, "src", contractsProjectName, "OpenApi");
        using var processScope = new OpenApiToolTestProcessScope(forceDocumentGeneration: true);
        var snapshots = new JsonSnapshotArtifactSet(
            canonicalDirectory,
            processScope.GeneratedDirectory,
            ".openapi.json",
            $"{apiAssemblyName}_",
            artifactDisplayName,
            "Generated by the OpenAPI tool.",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["v1"] = $"{apiAssemblyName}.json"
            });
        using var output = new StringWriter();
        using var error = new StringWriter();
        Directory.GetFiles(processScope.GeneratedDirectory).ShouldBeEmpty();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["generate", target],
            output,
            error,
            processScope.Start,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();
        snapshots.AssertCanonicalArtifactsMatchGeneratedArtifacts();
    }

    [Fact]
    public async Task Pre_cancelled_generation_returns_a_cancellation_error()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        using var processScope = new OpenApiToolTestProcessScope();

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["generate", "admin"],
            output,
            error,
            processScope.Start,
            cancellation.Token);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Error: OpenAPI generation was cancelled.", StringComparison.Ordinal);
        processScope.ChildStarted.ShouldBeFalse();
    }

    [Fact]
    public async Task Cancellation_after_child_start_stops_the_child_process()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var processScope = new OpenApiToolTestProcessScope();
        processScope.CancelAfterChildStart(cancellation);

        // Act
        var exitCode = await OpenApiToolApplication.Run(
            ["generate", "admin"],
            output,
            error,
            processScope.Start,
            cancellation.Token);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Error: OpenAPI generation was cancelled.", StringComparison.Ordinal);
        error.ToString().Contains("Child process cleanup did not complete.", StringComparison.Ordinal).ShouldBeFalse();
        processScope.ChildStarted.ShouldBeTrue();
        processScope.ChildProcessId.ShouldBeGreaterThan(0);
        Action getChildProcess = () => Process.GetProcessById(processScope.ChildProcessId);
        getChildProcess.ShouldThrow<ArgumentException>();
    }
}
