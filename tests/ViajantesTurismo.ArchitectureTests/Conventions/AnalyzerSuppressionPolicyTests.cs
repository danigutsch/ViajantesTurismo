using System.Xml.Linq;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AnalyzerSuppressionPolicyTests
{
    private static readonly HashSet<string> ApprovedSuppressMessageFiles =
    [
        "samples/Mediator/Mediator.Sample/GlobalSuppressions.cs",
        "src/SharedKernel/SharedKernel.Branding/BrandingSettings.cs",
        "src/SharedKernel/SharedKernel.Branding/BrandingSettingsDto.cs",
        "src/ViajantesTurismo.Admin.Domain/Customers/Customer.cs",
        "src/ViajantesTurismo.Admin.ApiService/Customers/CustomerImportEndpoints.cs",
        "src/SharedKernel/SharedKernel.IntegrationTesting/AspireTestApplication.cs",
        "src/ViajantesTurismo.MigrationService/MigrationProcess.cs",
        "tests/SharedKernel.EntityFrameworkCore.Tests/EfCoreCommandTransactionScopeTests.cs",
        "tests/SharedKernel.EntityFrameworkCore.Tests/PostgreSqlFixture.cs",
        "tests/SharedKernel.EntityFrameworkCore.Tests/PostgreSqlIntegrationEventTransportScenario.cs",
        "tests/SharedKernel.EntityFrameworkCore.Tests/PostgreSqlScenarioCleanup.cs",
        "tests/SharedKernel.EntityFrameworkCore.Tests/PostgreSqlTestDatabase.cs",
        "tests/SharedKernel.EventSourcing.Npgsql.Tests/PostgreSqlTestDatabaseLease.cs",
        "tests/SharedKernel.EventSourcing.Npgsql.Tests/PostgreSqlTestServerFixture.cs",
        "tests/SharedKernel.EventSourcing.Npgsql.Tests/PostgreSqlTestCleanup.cs",
        "tests/SharedKernel.Npgsql.Tests/PostgreSqlTestDatabaseLease.cs",
        "tests/SharedKernel.Npgsql.Tests/PostgreSqlTestServerFixture.cs",
        "tests/SharedKernel.Npgsql.Tests/PostgreSqlTestCleanup.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/Customers/CustomerEmailPostgreSqlScenario.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/Documents/PostgreSqlDocumentStoreScenario.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/Initialization/DevelopmentDataInitializationPostgreSqlScenario.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/Messaging/AdminMessagingMigrationScenario.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/Tours/TourStorePostgreSqlScenario.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/PostgreSqlTestCleanup.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/PostgreSqlTestDatabase.cs",
        "tests/ViajantesTurismo.Admin.Infrastructure.Tests/PostgreSqlTestServerFixture.cs",
        "tests/ViajantesTurismo.Admin.IntegrationTests/Infrastructure/PostgreSqlTestDatabase.cs",
        "tests/ViajantesTurismo.Admin.IntegrationTests/Observability/PostgreSqlIndexHealthCollectorScenario.cs",
        "tests/ViajantesTurismo.Admin.UnitTests/MigrationService/MigrationProcessTestHost.cs",
        "tests/ViajantesTurismo.Management.WebIntegrationTests/PostgreSqlManagementUserTokenStoreScenario.cs",
        "tests/ViajantesTurismo.Management.WebIntegrationTests/PostgreSqlTestCleanup.cs",
        "tests/ViajantesTurismo.Management.WebIntegrationTests/PostgreSqlTestDatabase.cs",
        "tests/ViajantesTurismo.Management.WebIntegrationTests/PostgreSqlTestServerFixture.cs"
    ];

    [Fact]
    public void Project_and_props_nowarn_entries_should_match_the_approved_inventory()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var noWarnEntries = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.csproj")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.props"))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .SelectMany(path => AnalyzerSuppressionPolicyTestsHelpers.FindNoWarnEntries(repositoryRoot, path))
            .ToArray();

        noWarnEntries.ShouldBeEquivalentTo("Directory.Build.props:ASPIRE010");
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

    [Fact]
    public void Repository_global_analyzers_and_configuration_should_not_enforce_API_request_locations()
    {
        // Arrange
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var globalBuildPropsPath = Path.Combine(repositoryRoot, "Directory.Build.props");
        var globalBuildProps = XDocument.Load(globalBuildPropsPath);
        var globalAnalyzerDirectories = globalBuildProps.Descendants("ProjectReference")
            .Where(reference => string.Equals(reference.Attribute("OutputItemType")?.Value, "Analyzer", StringComparison.Ordinal))
            .Select(reference => reference.Attribute("Include")?.Value
                ?? throw new InvalidOperationException("A global analyzer project reference must declare Include."))
            .Select(include => include.Replace(
                "$(MSBuildThisFileDirectory)",
                repositoryRoot + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
            .Select(include => Path.GetDirectoryName(Path.GetFullPath(include, repositoryRoot))
                ?? throw new InvalidOperationException($"Could not resolve the global analyzer path '{include}'."))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] requestBindingTokens =
        [
            "AsParameters",
            "FromBody",
            "FromForm",
            "FromHeader",
            "FromQuery",
            "FromRoute",
            "body-only",
            "body only",
            "BodyOnly"
        ];

        // Act
        var analyzerEvidence = globalAnalyzerDirectories
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .SelectMany(path => requestBindingTokens
                .Where(token => File.ReadAllText(path).Contains(token, StringComparison.OrdinalIgnoreCase))
                .Select(token => $"{Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')}:{token}"))
            .ToArray();
        var configurationEvidence = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, ".editorconfig")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.props"))
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.targets"))
            .SelectMany(path => requestBindingTokens
                .Where(token => File.ReadAllText(path).Contains(token, StringComparison.OrdinalIgnoreCase))
                .Select(token => $"{Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')}:{token}"))
            .ToArray();

        // Assert
        globalAnalyzerDirectories.ShouldNotBeEmpty();
        (analyzerEvidence.Length == 0).ShouldBeTrue(
            $"Expected repository-global analyzers not to enforce API request locations, but found:{Environment.NewLine}{string.Join(Environment.NewLine, analyzerEvidence)}");
        (configurationEvidence.Length == 0).ShouldBeTrue(
            $"Expected analyzer configuration not to enable a body-only request rule, but found:{Environment.NewLine}{string.Join(Environment.NewLine, configurationEvidence)}");
    }

}
