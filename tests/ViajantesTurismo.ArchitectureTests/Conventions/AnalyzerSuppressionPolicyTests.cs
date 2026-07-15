namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AnalyzerSuppressionPolicyTests
{
    private static readonly HashSet<string> ApprovedSuppressMessageFiles =
    [
        "samples/Mediator/Mediator.Sample/GlobalSuppressions.cs",
        "src/SharedKernel/SharedKernel.Branding/BrandingSettings.cs",
        "src/SharedKernel/SharedKernel.Branding/BrandingSettingsDto.cs",
        "src/ViajantesTurismo.Admin.Domain/Customers/Customer.cs",
        "src/SharedKernel/SharedKernel.IntegrationTesting/AspireTestApplication.cs",
        "tests/ViajantesTurismo.Admin.IntegrationTests/Infrastructure/PostgreSqlTestDatabase.cs",
        "tests/ViajantesTurismo.Admin.IntegrationTests/Observability/PostgreSqlIndexHealthCollectorScenario.cs"
    ];

    [Fact]
    public void Project_and_props_should_not_use_nowarn_entries()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var noWarnEntries = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.csproj")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.props"))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .SelectMany(path => AnalyzerSuppressionPolicyTestsHelpers.FindNoWarnEntries(repositoryRoot, path))
            .ToArray();

        (noWarnEntries.Length == 0).ShouldBeTrue(
            $"Expected project and props files not to use NoWarn entries, but found:{Environment.NewLine}{string.Join(Environment.NewLine, noWarnEntries)}");
    }

    [Fact]
    public void Hand_written_source_should_not_use_pragma_warning_suppressions()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var filesWithPragmas = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsGeneratedSource(repositoryRoot, path))
            .Where(AnalyzerSuppressionPolicyTestsHelpers.ContainsPragmaWarningDirective)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();

        (filesWithPragmas.Length == 0).ShouldBeTrue(
            $"Expected hand-written source not to use pragma warning suppressions, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithPragmas)}");
    }

    [Fact]
    public void SuppressMessage_attributes_should_stay_on_the_approved_analyzer_policy_allowlist()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var filesWithSuppressMessage = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => AnalyzerSuppressionPolicyTestsHelpers.SuppressMessageAttributeRegex().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !ApprovedSuppressMessageFiles.Contains(path))
            .ToArray();

        (filesWithSuppressMessage.Length == 0).ShouldBeTrue(
            $"Expected SuppressMessage attributes to stay on the approved analyzer policy allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithSuppressMessage)}");
    }

}
