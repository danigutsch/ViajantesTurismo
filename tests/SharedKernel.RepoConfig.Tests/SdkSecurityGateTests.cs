using System.Xml.Linq;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class SdkSecurityGateTests
{
    [Theory]
    [InlineData("CA5351", "internal static class Probe { internal static byte[] Hash(byte[] input) => System.Security.Cryptography.MD5.HashData(input); }")]
    [InlineData("CA5359", "internal static class Probe { internal static void Configure() { System.Net.ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true; } }")]
    [InlineData("CA5369", "internal static class Probe { internal static object? Parse(System.IO.Stream input) => new System.Xml.Serialization.XmlSerializer(typeof(string)).Deserialize(input); }")]
    public async Task Repository_sdk_analysis_rejects_security_regressions(string diagnostic, string source)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var project = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup", new XElement("IsRoslynComponent", "true")),
            new XElement("Import", new XAttribute("Project", Path.Combine(root, "Directory.Build.props"))));
        workspace.WriteFile("SecurityProbe.csproj", project.ToString());
        workspace.WriteFile("Probe.cs", source);

        // Act
        var result = await CiTestSelectionTestProcess.Run("dotnet", root,
            ["build", Path.Combine(workspace.RootPath, "SecurityProbe.csproj"), "-c", "Release", "--nologo"],
            TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldNotBe(0);
        var diagnostics = result.StandardOutput + result.StandardError;
        diagnostics.ShouldContain("error " + diagnostic, StringComparison.Ordinal);
    }
}
