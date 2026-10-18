using System.Xml.Linq;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace SharedKernel.RepoConfig.Tests;

[Trait(SharedKernelTestTraitNames.CapabilityName, "PackageConsumption")]
public sealed class CandidateCompatibilityTests
{
    [Fact]
    public void Downloaded_candidate_configuration_uses_the_current_feed_and_exclusive_internal_mapping()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        var candidate = Path.Combine(workspace.RootPath, "downloaded", "candidate.json");
        var scratch = Path.Combine(workspace.RootPath, "restore");

        // Act
        var path = CandidateCompatibilityCommand.WriteRestoreConfiguration(candidate, scratch);
        var config = XDocument.Load(path);

        // Assert
        config.Descendants("add").ShouldContain(x => (string?)x.Attribute("value") == Path.GetDirectoryName(candidate));
        var sources = config.Descendants("packageSource").ToArray();
        sources.ShouldHaveSingleItem(x => (string?)x.Attribute("key") == "local")
            .Elements("package").ShouldHaveSingleItem().Attribute("pattern")!.Value.ShouldBe("SharedKernel.*");
        sources.ShouldHaveSingleItem(x => (string?)x.Attribute("key") == "nuget.org")
            .Elements("package").ShouldHaveSingleItem().Attribute("pattern")!.Value.ShouldBe("*");
        config.Descendants("auditSources").ShouldHaveSingleItem();
    }

    [Fact]
    public void Extracted_library_project_dependencies_in_lock_files_are_rejected()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        var root = workspace.RootPath;
        Directory.CreateDirectory(Path.Combine(root, "src/SharedKernel/SharedKernel.Results"));
        File.WriteAllText(Path.Combine(root, "src/SharedKernel/SharedKernel.Results/SharedKernel.Results.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(root, "Directory.Packages.props"), """<Project><PackageVersion Include="SharedKernel.Results" Version="1.0.0" /></Project>""");
        File.WriteAllText(Path.Combine(root, "ViajantesTurismo.slnx"), """<Solution><Project Path="Consumer.csproj" /></Solution>""");
        File.WriteAllText(Path.Combine(root, "Consumer.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(root, "packages.lock.json"), """{"dependencies":{"net10.0":{"SharedKernel.Results":{"type":"Project"}}}}""");
        var package = Path.Combine(root, "SharedKernel.Results.1.0.0.nupkg");
        File.WriteAllText(package, "candidate package");
        var manifest = Path.Combine(root, "candidate.json");
        using var stream = File.OpenRead(package);
        var evidence = new JsonObject
        {
            ["packageVersion"] = "1.0.0",
            ["packageSha256"] = new JsonObject { [Path.GetFileName(package)] = Convert.ToHexStringLower(SHA256.HashData(stream)) }
        };

        // Act
        Action validate = () => CandidateCompatibilityCommand.ValidateConsumer(root, manifest, evidence);

        // Assert
        validate.ShouldThrow<ArgumentException>().Message.ShouldContain("extracted SharedKernel sources", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../outside.nupkg")]
    [InlineData("..\\outside.nupkg")]
    [InlineData("candidate.txt")]
    public void Candidate_package_paths_cannot_escape_the_feed(string name)
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.Evidence["packageSha256"] = new JsonObject { [name] = "invalid" };

        // Act
        Action validate = () => CandidateCompatibilityCommand.ValidateConsumer(scope.Root, scope.Manifest, scope.Evidence);

        // Assert
        validate.ShouldThrow<ArgumentException>().Message.ShouldContain("filename", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_package_hashes_cannot_produce_compatibility_evidence(bool missing)
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.Evidence["packageSha256"] = missing ? null : new JsonObject();

        // Act
        Action validate = () => CandidateCompatibilityCommand.ValidateConsumer(scope.Root, scope.Manifest, scope.Evidence);

        // Assert
        validate.ShouldThrow<ArgumentException>().Message.ShouldContain("hashes are unavailable", StringComparison.Ordinal);
    }

    [Fact]
    public void Tampered_packages_are_rejected_before_restore()
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.WriteFile(Path.GetFileName(scope.Package), "tampered bytes");

        // Act
        Action validate = () => CandidateCompatibilityCommand.ValidateConsumer(scope.Root, scope.Manifest, scope.Evidence);

        // Assert
        validate.ShouldThrow<ArgumentException>().Message.ShouldContain("Candidate package changed", StringComparison.Ordinal);
    }

    [Fact]
    public void Source_references_cannot_bypass_candidate_packages()
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.WriteFile("Consumer.csproj", """<Project><ProjectReference Include="src/SharedKernel/SharedKernel.Results/SharedKernel.Results.csproj" /></Project>""");

        // Act
        Action validate = () => CandidateCompatibilityCommand.ValidateConsumer(scope.Root, scope.Manifest, scope.Evidence);

        // Assert
        validate.ShouldThrow<ArgumentException>().Message.ShouldContain("extracted SharedKernel sources", StringComparison.Ordinal);
    }

    [Fact]
    public void Stale_lock_resolution_is_rejected()
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.WriteFile("packages.lock.json", """{"dependencies":{"net10.0":{"SharedKernel.Results":{"type":"Direct","resolved":"0.9.0"}}}}""");

        // Act
        Action validate = () => CandidateCompatibilityCommand.ValidateConsumer(scope.Root, scope.Manifest, scope.Evidence);

        // Assert
        validate.ShouldThrow<ArgumentException>().Message.ShouldContain("does not resolve to the tested candidate", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<Project />")]
    [InlineData("<Project><PackageVersion Include=\"SharedKernel.Results\" Version=\"0.9.0\" /></Project>")]
    public void Missing_or_stale_central_versions_are_rejected(string central)
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.WriteFile("Directory.Packages.props", central);

        // Act
        Action validate = () => CandidateCompatibilityCommand.ValidateConsumer(scope.Root, scope.Manifest, scope.Evidence);

        // Assert
        validate.ShouldThrow<ArgumentException>().Message.ShouldContain("candidate minimum version", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("[1.0.0]")]
    public void Matching_candidate_versions_allow_external_dependencies_and_documentation_sources(string version)
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.WriteFile("Directory.Packages.props", $"""<Project><PackageVersion Include="SharedKernel.Results" Version="{version}" /></Project>""");
        scope.WriteFile("Consumer.csproj", """<Project><ProjectReference Include="src/SharedKernel/SharedKernel.Documentation/SharedKernel.Documentation.csproj" /></Project>""");

        // Act
        CandidateCompatibilityCommand.ValidateConsumer(scope.Root, scope.Manifest, scope.Evidence);

        // Assert
        File.Exists(scope.Package).ShouldBeTrue();
    }

    [Theory]
    [InlineData("--candidate", "", "Missing value")]
    [InlineData("--unknown", "value", "Unknown option")]
    [InlineData("--root", ".", "--candidate is required")]
    public async Task Invalid_command_arguments_report_actionable_errors(string option, string value, string expected)
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();
        string[] arguments = string.IsNullOrEmpty(value) ? ["verify-sharedkernel-candidate", option] : ["verify-sharedkernel-candidate", option, value];

        // Act
        var result = await RepoConfigToolApplication.Run(arguments, output, error, Environment.CurrentDirectory, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(1);
        error.ToString().ShouldContain(expected, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Uncommitted_library_evidence_remains_pending_without_running_validation()
    {
        // Arrange
        using var scope = new CandidateCompatibilityTestScope();
        scope.Evidence["libraryDirty"] = true;
        await scope.WriteManifest(TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var destination = Path.Combine(scope.Root, "evidence", "compatibility.json");
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();

        // Act
        var result = await CandidateCompatibilityCommand.Run(["--root", root, "--candidate", scope.Manifest, "--output", destination], output, error, root, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(1);
        var evidence = JsonNode.Parse(await File.ReadAllTextAsync(destination, TestContext.Current.CancellationToken));
        evidence.ShouldNotBeNull();
        evidence["consumerCompatibility"].ShouldNotBeNull().GetValue<string>().ShouldBe("pending");
        evidence["consumerRevision"].ShouldNotBeNull().GetValue<string>().Length.ShouldBe(40);
        error.ToString().ShouldContain("committed library and consumer revisions", StringComparison.Ordinal);
    }

}
