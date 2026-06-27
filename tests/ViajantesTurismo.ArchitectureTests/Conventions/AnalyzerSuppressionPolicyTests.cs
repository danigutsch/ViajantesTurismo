namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AnalyzerSuppressionPolicyTests
{
    private static readonly HashSet<string> ApprovedSuppressMessageFiles =
    [
        "samples/Mediator/Mediator.Sample/GlobalSuppressions.cs",
        "tests/SharedKernel.IntegrationTesting/AspireTestApplication.cs"
    ];

    [Fact]
    public void Project_And_Props_Should_Not_Use_NoWarn_Entries()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var noWarnEntries = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.csproj")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.props"))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .SelectMany(path => AnalyzerSuppressionPolicyTestsHelpers.FindNoWarnEntries(repositoryRoot, path))
            .ToArray();

        Assert.True(
            noWarnEntries.Length == 0,
            $"Expected project and props files not to use NoWarn entries, but found:{Environment.NewLine}{string.Join(Environment.NewLine, noWarnEntries)}");
    }

    [Fact]
    public void Hand_Written_Source_Should_Not_Use_Pragma_Warning_Suppressions()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var filesWithPragmas = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsGeneratedSource(repositoryRoot, path))
            .Where(AnalyzerSuppressionPolicyTestsHelpers.ContainsPragmaWarningDirective)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();

        Assert.True(
            filesWithPragmas.Length == 0,
            $"Expected hand-written source not to use pragma warning suppressions, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithPragmas)}");
    }

    [Fact]
    public void SuppressMessage_Attributes_Should_Stay_On_The_Approved_Analyzer_Policy_Allowlist()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var filesWithSuppressMessage = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => AnalyzerSuppressionPolicyTestsHelpers.SuppressMessageAttributeRegex().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !ApprovedSuppressMessageFiles.Contains(path))
            .ToArray();

        Assert.True(
            filesWithSuppressMessage.Length == 0,
            $"Expected SuppressMessage attributes to stay on the approved analyzer policy allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithSuppressMessage)}");
    }

}
