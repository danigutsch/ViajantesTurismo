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

}
