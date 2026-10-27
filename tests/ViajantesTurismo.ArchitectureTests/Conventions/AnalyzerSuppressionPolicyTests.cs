using System.Xml.Linq;
using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AnalyzerSuppressionPolicyTests
{
    private static readonly HashSet<string> ApprovedSuppressMessageFiles =
    [
        // Preserve the epic's existing exact-site telemetry, Blazor and fixture-ownership exceptions.
        "tests/ViajantesTurismo.Admin.ApiServiceTests/Documents/DocumentPrivacyTelemetryCapture.cs",
        "tests/ViajantesTurismo.HostedTests/Catalog/Infrastructure/Tours/CatalogTourSlugLockPostgreSqlTests.cs",
        "src/ViajantesTurismo.Management.Web/Components/Pages/Payments/Finances.razor.cs",
        "src/ViajantesTurismo.Management.Web/Components/Pages/Payments/Instructions.razor.cs",
        "src/ViajantesTurismo.Management.Web/Components/Pages/Payments/PaymentAllocationFormRow.cs",
        "src/ViajantesTurismo.Management.Web/Components/Pages/Payments/PaymentTargetChoice.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Payments/PostgreSqlPaymentScenario.cs",
        "samples/Mediator/Mediator.Sample/GlobalSuppressions.cs",
        "src/SharedKernel/SharedKernel.Branding/BrandingSettings.cs",
        "src/SharedKernel/SharedKernel.Branding/BrandingSettingsDto.cs",
        "src/ViajantesTurismo.Admin.Domain/Customers/Customer.cs",
        "src/ViajantesTurismo.Admin.ApiService/Customers/CustomerImportEndpoints.cs",
        "src/SharedKernel/SharedKernel.IntegrationTesting/AspireTestApplication.cs",
        "src/SharedKernel/SharedKernel.Testing/GatedCountingReadStream.cs",
        "src/ViajantesTurismo.MigrationService/MigrationProcess.cs",
        "src/ViajantesTurismo.Catalog.Contracts.Application/CatalogTourImageDto.cs",
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
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Customers/CustomerEmailPostgreSqlScenario.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Documents/PostgreSqlDocumentStoreScenario.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Initialization/DevelopmentDataInitializationPostgreSqlScenario.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Messaging/AdminMessagingMigrationScenario.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Tours/TourStorePostgreSqlScenario.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Temporal/CivilDatePostgreSqlScenario.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/PostgreSqlTestCleanup.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/PostgreSqlTestDatabase.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/PostgreSqlTestServerFixture.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Api/Infrastructure/PostgreSqlTestDatabase.cs",
        "tests/ViajantesTurismo.HostedTests/Admin/Infrastructure/Observability/PostgreSqlIndexHealthCollectorScenario.cs",
        "tests/ViajantesTurismo.Admin.UnitTests/MigrationService/MigrationProcessTestHost.cs",
        "tests/ViajantesTurismo.HostedTests/Management/Security/PostgreSqlManagementUserTokenStoreScenario.cs",
        "tests/ViajantesTurismo.HostedTests/Management/Security/PostgreSqlTestCleanup.cs",
        "tests/ViajantesTurismo.HostedTests/Management/Security/PostgreSqlTestDatabase.cs",
        "tests/ViajantesTurismo.HostedTests/Management/Security/PostgreSqlTestServerFixture.cs",
        "tests/ViajantesTurismo.HostedTests/Infrastructure/HostedTestFixture.cs",
        "tests/ViajantesTurismo.HostedTests/Infrastructure/HostedTestCleanup.cs"
    ];

    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public void Repository_policy_enumeration_excludes_staged_library_source_and_preserves_application_source()
    {
        // Arrange
        using var workspace = new TemporaryDirectory();
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(workspace.Path, "src"));
        var libraryDirectory = Directory.CreateDirectory(Path.Combine(workspace.Path, ".local", "sharedkernel-source"));
        var sourceFile = Path.Combine(sourceDirectory.FullName, "Application.cs");
        File.WriteAllText(sourceFile, "namespace Application; internal sealed class Example;");
        File.WriteAllText(Path.Combine(libraryDirectory.FullName, "Library.cs"), "namespace Library; internal sealed class Example;");

        // Act
        var files = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(workspace.Path, "*.cs").ToArray();

        // Assert
        files.ShouldBeEquivalentTo(sourceFile);
    }

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
    public void Hand_written_source_should_only_use_approved_exact_site_pragma_suppressions()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var unapprovedPragmas = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsGeneratedSource(repositoryRoot, path))
            .SelectMany(path => AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressions(repositoryRoot, path))
            .ToArray();

        var areEqual = unapprovedPragmas.Length == 0;
        areEqual.ShouldBeTrue(
            $"Expected hand-written source to use only approved exact-site pragma warning suppressions, but found:{Environment.NewLine}{string.Join(Environment.NewLine, unapprovedPragmas)}");
    }

    [Fact]
    public void SuppressMessage_attributes_should_stay_on_the_approved_analyzer_policy_allowlist()
    {
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var filesWithSuppressMessage = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => AnalyzerSuppressionPolicyTestsHelpers.ContainsFrameworkSuppressMessageAttribute(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !ApprovedSuppressMessageFiles.Contains(path))
            .ToArray();

        var areEqual1 = filesWithSuppressMessage.Length == 0;
        areEqual1.ShouldBeTrue(
            $"Expected SuppressMessage attributes to stay on the approved analyzer policy allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, filesWithSuppressMessage)}");
    }

    [Fact]
    public void Global_analyzer_registration_and_configuration_surfaces_should_match_the_allowlist()
    {
        // Arrange
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        string[] expectedSurfaces =
        [
            "build:Directory.Build.props",
            "build:src/Directory.Build.targets",
            "build:tests/Directory.Build.props",
            "custom-config:.editorconfig:SKSTYLE001",
            "custom-config:.editorconfig:SKSTYLE002",
            "custom-config:.editorconfig:SKSTYLE003",
            "custom-config:.editorconfig:SKSTYLE004",
            "custom-config:.editorconfig:SKSTYLE015",
            "custom-config:.editorconfig:SKSTYLE017",
            "custom-config:.editorconfig:SKTEST001",
            "custom-config:.editorconfig:SKTEST002",
            "custom-config:.editorconfig:SKTEST003",
            "custom-config:.editorconfig:SKTEST004",
            "custom-config:.editorconfig:SKTEST005",
            "custom-config:.editorconfig:SKTEST006",
            "custom-config:.editorconfig:SKTEST010:tests/ViajantesTurismo.Admin.ApiServiceTests/Documents/DocumentPrivacyTelemetryCapture.cs:none",
            "custom-config:.editorconfig:SKTEST010:tests/ViajantesTurismo.HostedTests/Catalog/Infrastructure/Tours/CatalogTourSlugLockPostgreSqlTests.cs:none",
            "custom-config:.editorconfig:SKTEST010:tests/ViajantesTurismo.Catalog.UnitTests/PollingBackgroundServiceTestHarness.cs:none",
            "import:tests/Directory.Build.props:$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))",
            "package:Directory.Build.props:PandaBear.SharedKernel.Style.Analyzers",
            "package:Directory.Build.props:PandaBear.SharedKernel.Style.CodeFixes",
            "package:tests/Directory.Build.props:PandaBear.SharedKernel.Testing.Analyzers"
        ];
        var buildFiles = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "Directory.Build.props")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "Directory.Build.targets"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var actualSurfaces = new List<string>();

        // Act
        foreach (var buildFile in buildFiles)
        {
            var relativePath = Path.GetRelativePath(repositoryRoot, buildFile).Replace('\\', '/');
            var document = XDocument.Load(buildFile);
            actualSurfaces.Add($"build:{relativePath}");

            foreach (var import in document.Descendants().Where(element => element.Name.LocalName == "Import"))
            {
                var project = import.Attribute("Project")?.Value
                    ?? throw new InvalidOperationException($"Import in '{relativePath}' must declare Project.");
                actualSurfaces.Add($"import:{relativePath}:{project.Replace('\\', '/')}");
            }

            foreach (var packageReference in document.Descendants().Where(element => element.Name.LocalName == "PackageReference"))
            {
                var include = packageReference.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException($"PackageReference in '{relativePath}' must declare Include.");
                actualSurfaces.Add($"package:{relativePath}:{include}");
            }

            foreach (var projectReference in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
            {
                var outputItemType = projectReference.Attribute("OutputItemType")?.Value
                    ?? projectReference.Elements().FirstOrDefault(element => element.Name.LocalName == "OutputItemType")?.Value
                    ?? "default";
                var include = projectReference.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException($"ProjectReference in '{relativePath}' must declare Include.");
                actualSurfaces.Add($"project:{relativePath}:{include.Replace('\\', '/')}:{outputItemType}");
            }

            foreach (var analyzer in document.Descendants().Where(element => element.Name.LocalName == "Analyzer"))
            {
                var include = analyzer.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException($"Analyzer in '{relativePath}' must declare Include.");
                actualSurfaces.Add($"analyzer:{relativePath}:{include.Replace('\\', '/')}");
            }

            foreach (var globalConfig in document.Descendants().Where(element => element.Name.LocalName == "GlobalAnalyzerConfigFiles"))
            {
                var include = globalConfig.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException($"GlobalAnalyzerConfigFiles in '{relativePath}' must declare Include.");
                actualSurfaces.Add($"global-config-item:{relativePath}:{include.Replace('\\', '/')}");
            }
        }

        actualSurfaces.AddRange(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.globalconfig")
            .Select(path => $"global-config-file:{Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')}"));

        const string diagnosticPrefix = "dotnet_diagnostic.";
        var configuredCustomDiagnostics = new HashSet<string>(StringComparer.Ordinal);
        var section = string.Empty;
        foreach (var rawLine in File.ReadLines(Path.Combine(repositoryRoot, ".editorconfig")))
        {
            var line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1];
            }

            if (!line.StartsWith(diagnosticPrefix + "SK", StringComparison.Ordinal))
            {
                continue;
            }

            var value = line[diagnosticPrefix.Length..];
            var diagnosticId = value[..value.IndexOf('.', StringComparison.Ordinal)];
            var surface = $"custom-config:.editorconfig:{diagnosticId}";
            if (diagnosticId == "SKTEST010")
            {
                surface += $":{section}:{line[(line.IndexOf('=', StringComparison.Ordinal) + 1)..].Trim()}";
            }

            configuredCustomDiagnostics.Add(surface);
        }

        actualSurfaces.AddRange(configuredCustomDiagnostics);

        // Assert
        actualSurfaces.OrderBy(value => value, StringComparer.Ordinal)
            .ShouldBe(expectedSurfaces.OrderBy(value => value, StringComparer.Ordinal));
    }

}
