using System.Text.RegularExpressions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AdminTestArchitectureGuardTests
{
    [Fact]
    public void Admin_Test_Architecture_Guide_Should_Declare_The_Canonical_Documentation_Owner()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "tests", "README.md");
        var guideText = File.ReadAllText(guidePath);
        var architectureReadmePath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.ArchitectureTests", "README.md");
        var architectureReadmeText = File.ReadAllText(architectureReadmePath);

        Assert.Contains("This file is the canonical quick-reference for the Admin test taxonomy.", guideText, StringComparison.Ordinal);
        Assert.Contains("docs/TEST_GUIDELINES.md", guideText, StringComparison.Ordinal);
        Assert.Contains("AdminTestArchitectureGuardTests", architectureReadmeText, StringComparison.Ordinal);
        Assert.Contains("tests/README.md", architectureReadmeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Hosted_Test_Infrastructure_Should_Use_The_Approved_Fixture_And_Base_Class_Model()
    {
        var repositoryRoot = GetRepositoryRoot();
        var integrationInfrastructurePath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.IntegrationTests", "Infrastructure");
        var systemTestBasesPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure", "Bases");
        var systemTestFixturesPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure", "Fixtures");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "AssemblyFixture.cs"),
            "[assembly: Xunit.AssemblyFixture(typeof(ViajantesTurismo.Admin.IntegrationTests.Infrastructure.ApiFixture))]");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "ApiFixture.cs"),
            "public sealed class ApiFixture : ViajantesTurismo.Admin.Testing.Integration.IAdminTestHost, IAsyncLifetime");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "ApiFixture.cs"),
            "_appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<ViajantesTurismo_AppHost>();");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "ApiFixture.cs"),
            "_client = _app.CreateHttpClient(ResourceNames.Api);");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSystemTestBase.cs"),
            "public abstract class AspireSystemTestBase<TFixture>(TFixture fixture) : PageTest");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSystemTestBase.cs"),
            "protected Uri ApiBaseUri => Fixture.ApiBaseUri;");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            "[Collection(E2ETestCollections.Serial)]");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            "public abstract class AspireSerialSystemTestBase(AspireSerialSystemTestFixture fixture) : AspireSystemTestBase<AspireSerialSystemTestFixture>(fixture)");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            "await Fixture.ResetDatabase(cts.Token);");

        AssertFileDoesNotContain(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            "protected Task ClearDatabase");

        AssertFileContains(
            Path.Combine(systemTestFixturesPath, "AspireSystemTestFixture.cs"),
            "public sealed class AspireSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime, IDisposable");

        AssertFileContains(
            Path.Combine(systemTestFixturesPath, "AspireSerialSystemTestFixture.cs"),
            "public sealed class AspireSerialSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime, IDisposable");

        AssertFileDoesNotExist(
            Path.Combine(systemTestBasesPath, "E2ETestBase.cs"));

        AssertFileDoesNotExist(
            Path.Combine(systemTestBasesPath, "E2ESerialTestBase.cs"));

        AssertFileDoesNotExist(
            Path.Combine(systemTestFixturesPath, "E2EFixture.cs"));
    }

    [Fact]
    public void Admin_Hosted_Test_Infrastructure_Should_Not_Expose_Generic_ServiceProvider_Reach_Through()
    {
        var infrastructureRoots = new[]
        {
            Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.Admin.IntegrationTests", "Infrastructure"),
            Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure")
        };

        var offendingMembers = infrastructureRoots
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(path => FindGenericServiceProviderReachThrough(path))
            .ToArray();

        Assert.False(
            offendingMembers.Length != 0,
            $"Expected Admin hosted test infrastructure to avoid public generic service-provider reach-through, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingMembers)}");
    }

    private static void AssertFileContains(string filePath, string expectedText)
    {
        var fileContents = File.ReadAllText(filePath);
        Assert.Contains(expectedText, fileContents, StringComparison.Ordinal);
    }

    private static void AssertFileDoesNotExist(string filePath)
    {
        Assert.False(File.Exists(filePath), $"Did not expect file to exist: {filePath}");
    }

    private static void AssertFileDoesNotContain(string filePath, string unexpectedText)
    {
        var fileContents = File.ReadAllText(filePath);
        Assert.DoesNotContain(unexpectedText, fileContents, StringComparison.Ordinal);
    }

    private static string[] FindGenericServiceProviderReachThrough(string filePath)
    {
        var repositoryRoot = GetRepositoryRoot();
        var lines = File.ReadAllLines(filePath);
        var offenses = new List<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (!PublicServiceProviderReachThroughRegex().IsMatch(lines[lineIndex]))
            {
                continue;
            }

            offenses.Add($"{Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')}:L{lineIndex + 1} {lines[lineIndex].Trim()}");
        }

        return [.. offenses];
    }

    private static string GetRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var solutionPath = Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx");
            if (File.Exists(solutionPath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    private static bool IsGeneratedTestPath(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Contains("/bin/", StringComparison.Ordinal)
            || normalizedPath.Contains("/obj/", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*public\s+.*\b(?:IServiceProvider|IServiceScope|CreateScope|CreateAsyncScope|RunInScope)\b", RegexOptions.Compiled)]
    private static partial Regex PublicServiceProviderReachThroughRegex();
}
