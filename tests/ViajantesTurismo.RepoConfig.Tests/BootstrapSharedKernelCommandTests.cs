using System.Text.Json;
using System.Text.Json.Nodes;

namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class BootstrapSharedKernelCommandTests
{
    [Theory]
    [InlineData("required", "--library-root is required.")]
    [InlineData("missing-value", "Missing value for --library-root")]
    [InlineData("unknown", "Unknown option: --unknown")]
    public async Task Rejects_incomplete_or_unknown_bootstrap_options(string scenario, string expectedError)
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        string[] options = scenario switch
        {
            "required" => [],
            "missing-value" => ["--library-root"],
            _ => ["--unknown", "value"]
        };
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["bootstrap-sharedkernel", .. options], output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(expectedError, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("dirty", "clean checkout of the pinned SharedKernel revision")]
    [InlineData("wrong-revision", "clean checkout of the pinned SharedKernel revision")]
    [InlineData("null-manifest", "Candidate manifest must be a JSON object")]
    [InlineData("malformed-manifest", "BytePositionInLine")]
    [InlineData("missing-version", "Candidate package version is required")]
    public async Task Rejects_invalid_manifest_or_unverified_library_before_staging(string scenario, string expectedError)
    {
        // Arrange
        using var library = new TemporaryCiTestRepository();
        using var consumer = new TemporaryCiTestRepository();
        var ct = TestContext.Current.CancellationToken;
        library.WriteFile(".gitignore", "bin/\nobj/\n");
        var revision = await library.CommitFiles(ct);
        var pin = new JsonObject
        {
            ["libraryRevision"] = scenario == "wrong-revision" ? new string('a', 40) : revision,
            ["packageVersion"] = "1.2.3"
        };
        if (scenario == "missing-version")
        {
            pin.Remove("packageVersion");
        }
        var manifest = scenario switch
        {
            "null-manifest" => "null",
            "malformed-manifest" => "{",
            _ => pin.ToJsonString()
        };
        consumer.WriteFile(".config/sharedkernel-candidate.json", manifest);
        if (scenario == "dirty")
        {
            library.WriteFile("unreviewed.txt", "uncommitted library change");
        }
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["bootstrap-sharedkernel", "--library-root", library.RootPath], output, error, consumer.RootPath, ct);

        // Assert
        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(expectedError, StringComparison.Ordinal);
        output.ToString().ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delegates_verified_candidate_staging_with_explicit_root_and_expected_feed(bool explicitFeed)
    {
        // Arrange
        using var library = new TemporaryCiTestRepository();
        using var consumer = new TemporaryCiTestRepository();
        var ct = TestContext.Current.CancellationToken;
        var repositoryRoot = CiTestSelectionTestProcess.GetRepositoryRoot();
        library.WriteFile("global.json", await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "global.json"), ct));
        library.WriteFile(".gitignore", "bin/\nobj/\n");
        library.WriteFile("tools/SharedKernel.Versioning.Tool/SharedKernel.Versioning.Tool.csproj", """
            <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
              <TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType>
            </PropertyGroup></Project>
            """);
        library.WriteFile("tools/SharedKernel.Versioning.Tool/Program.cs", """
            System.Console.WriteLine("forwarded:" + System.Text.Json.JsonSerializer.Serialize(args));
            System.Console.WriteLine("candidate staged");
            """);
        var revision = await library.CommitFiles(ct);
        consumer.WriteFile(".config/sharedkernel-candidate.json", new JsonObject
        {
            ["libraryRevision"] = revision,
            ["packageVersion"] = "1.2.3"
        }.ToJsonString());
        var feed = Path.Combine(consumer.RootPath, "candidate packages");
        string[] feedOptions = explicitFeed ? ["--feed", feed] : [];
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["bootstrap-sharedkernel", "--root", consumer.RootPath, "--library-root", library.RootPath, .. feedOptions],
            output, error, library.RootPath, ct);

        // Assert
        exitCode.ShouldBe(0);
        error.ToString().ShouldBe(string.Empty);
        output.ToString().ShouldContain("candidate staged", StringComparison.Ordinal);
        var forwardedJson = output.ToString().Split('\n')
            .ShouldHaveSingleItem(static line => line.StartsWith("forwarded:", StringComparison.Ordinal))["forwarded:".Length..];
        var forwarded = JsonSerializer.Deserialize<string[]>(forwardedJson).ShouldNotBeNull();
        forwarded.ShouldBe([
            "stage-candidate", "--repo-root", library.RootPath,
            "--candidate", Path.Combine(consumer.RootPath, ".config", "sharedkernel-candidate.json"),
            "--feed", explicitFeed ? feed : Path.Combine(library.RootPath, "artifacts", "api-compat", "packages", "1.2.3"),
            "--output", Path.Combine(consumer.RootPath, ".local", "sharedkernel-feed")
        ]);
    }
}
