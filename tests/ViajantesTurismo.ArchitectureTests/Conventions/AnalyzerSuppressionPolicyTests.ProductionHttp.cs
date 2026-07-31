namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AnalyzerSuppressionPolicyTests
{
    private static readonly HashSet<string> ApprovedProductionHttpPragmaSuppressions = [];

    [Fact]
    public void Production_http_pragma_suppressions_should_require_one_allowlisted_statement_and_justification()
    {
        // Arrange
        const string broadSuppression = """
            public sealed class Registration
            {
                public void Configure()
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    RegisterFirst();
                    RegisterSecond();
            #pragma warning restore VTARCH001
                }

                private void RegisterFirst() { }
                private void RegisterSecond() { }
            }
            """;
        const string missingJustificationSuppression = """
            public sealed class Registration
            {
                public void Configure()
                {
            #pragma warning disable VTARCH001
                    Register();
            #pragma warning restore VTARCH001
                }

                private void Register() { }
            }
            """;
        const string blockSuppression = """
            public sealed class Registration
            {
                public void Configure()
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    {
                        RegisterFirst();
                        RegisterSecond();
                    }
            #pragma warning restore VTARCH001
                }

                private void RegisterFirst() { }
                private void RegisterSecond() { }
            }
            """;
        const string controlFlowSuppression = """
            public sealed class Registration
            {
                public void Configure(bool enabled)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    if (enabled)
                    {
                        Register();
                    }
            #pragma warning restore VTARCH001
                }

                private void Register() { }
            }
            """;
        const string nestedBlockSuppression = """
            public sealed class Registration
            {
                public void Configure()
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    Run(() =>
                    {
                        RegisterFirst();
                        RegisterSecond();
                    });
            #pragma warning restore VTARCH001
                }

                private void Run(System.Action action) { }
                private void RegisterFirst() { }
                private void RegisterSecond() { }
            }
            """;
        const string exactExpressionSuppression = """
            public sealed class Registration
            {
                public void Configure()
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    Register();
            #pragma warning restore VTARCH001
                }

                private void Register() { }
            }
            """;
        const string exactLocalSuppression = """
            public sealed class Registration
            {
                public void Configure()
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    var registration = Register();
            #pragma warning restore VTARCH001
                }

                private object Register() => new();
            }
            """;
        const string exactReturnSuppression = """
            public sealed class Registration
            {
                public object Configure()
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    return Register();
            #pragma warning restore VTARCH001
                }

                private object Register() => new();
            }
            """;

        // Act
        var broadPragmaViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(broadSuppression, "BroadSuppression.cs");
        var missingJustificationViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(missingJustificationSuppression, "MissingJustification.cs");
        var blockPragmaViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(blockSuppression, "BlockSuppression.cs");
        var controlFlowViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(controlFlowSuppression, "ControlFlowSuppression.cs");
        var nestedBlockViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(nestedBlockSuppression, "NestedBlockSuppression.cs");
        var exactExpressionViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactExpressionSuppression, "ExactExpressionSuppression.cs");
        var exactLocalViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactLocalSuppression, "ExactLocalSuppression.cs");
        var exactReturnViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactReturnSuppression, "ExactReturnSuppression.cs");

        // Assert
        broadPragmaViolations.ShouldNotBeEmpty();
        missingJustificationViolations.ShouldContain(static violation => violation.Contains("missing '// Justification: ...'", StringComparison.Ordinal));
        blockPragmaViolations.ShouldNotBeEmpty();
        controlFlowViolations.ShouldNotBeEmpty();
        nestedBlockViolations.ShouldNotBeEmpty();
        exactExpressionViolations.ShouldBeEmpty();
        exactLocalViolations.ShouldBeEmpty();
        exactReturnViolations.ShouldBeEmpty();
    }

    [Fact]
    public void Production_http_source_suppressions_should_be_exact_site_and_allowlisted()
    {
        // Arrange
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var sourceFiles = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.cs")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsGeneratedSource(repositoryRoot, path))
            .ToArray();

        // Act
        var actualSuppressions = sourceFiles
            .SelectMany(path => AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpPragmaSuppressions(repositoryRoot, path))
            .ToHashSet(StringComparer.Ordinal);
        var attributeSuppressions = sourceFiles
            .Where(path => File.ReadAllText(path).Contains("SuppressMessage", StringComparison.Ordinal))
            .SelectMany(path => AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressions(repositoryRoot, path))
            .ToArray();
        var matchesAllowlist = actualSuppressions.SetEquals(ApprovedProductionHttpPragmaSuppressions);

        // Assert
        matchesAllowlist.ShouldBeTrue(
            $"Expected VTARCH001 pragma suppressions to match the code-owner-reviewed exact-site allowlist, but found:{Environment.NewLine}{string.Join(Environment.NewLine, actualSuppressions)}");
        attributeSuppressions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Architecture\", \"VTARCH001: Production HTTP\")] public sealed class Suppressed { }")]
    [InlineData("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Architecture\", \"VT\" + \"ARCH001\")] public sealed class Suppressed { }")]
    [InlineData("[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Architecture\", \"VTARCH001\")] public sealed class Suppressed { }")]
    [InlineData("using Suppress = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute; [Suppress(\"Architecture\", \"VTARCH001\")] public sealed class Suppressed { }")]
    public void Production_http_suppress_message_attributes_should_be_rejected(string source)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(source);

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(source, "Suppression.cs");

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("dotnet_diagnostic.VTARCH001.severity = none")]
    [InlineData("dotnet_diagnostic.VTARCH001.severity = silent")]
    [InlineData("dotnet_diagnostic.VTARCH001.severity = suggestion")]
    [InlineData("dotnet_analyzer_diagnostic.category-Architecture.severity = none")]
    [InlineData("dotnet_analyzer_diagnostic.category-Architecture.severity = silent")]
    [InlineData("dotnet_analyzer_diagnostic.category-Architecture.severity = suggestion")]
    [InlineData("dotnet_analyzer_diagnostic.severity = none")]
    [InlineData("dotnet_analyzer_diagnostic.severity = silent")]
    [InlineData("dotnet_analyzer_diagnostic.severity = suggestion")]
    public void Production_http_severity_suppressions_should_be_rejected(string setting)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(setting);

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindBroadProductionHttpSuppressionsInSource(setting, ".editorconfig");

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("<NoWarn>vtarch001</NoWarn>")]
    [InlineData("<WarningsNotAsErrors>CS0001,VTARCH001</WarningsNotAsErrors>")]
    [InlineData("<warningsnotaserrors>$(WarningsNotAsErrors);vtarch001</warningsnotaserrors>")]
    public void Production_http_msbuild_suppressions_should_be_rejected(string setting)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(setting);

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindBroadProductionHttpSuppressionsInSource(setting, "Project.csproj");

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(
        "<Project><ItemGroup><ProjectReference Include=\"../ViajantesTurismo.Architecture.Analyzers/"
        + "ViajantesTurismo.Architecture.Analyzers.csproj\" OutputItemType=\"Analyzer\" /></ItemGroup></Project>")]
    [InlineData(
        "<Project><ItemGroup><ProjectReference Include=\"../ViajantesTurismo.Architecture.Analyzers/"
        + "ViajantesTurismo.Architecture.Analyzers.csproj\"><OutputItemType>Analyzer</OutputItemType>"
        + "</ProjectReference></ItemGroup></Project>")]
    [InlineData(
        "<Project><ItemGroup><ProjectReference Update=\"../ViajantesTurismo.Architecture.Analyzers/"
        + "ViajantesTurismo.Architecture.Analyzers.csproj\" OutputItemType=\"Analyzer\" /></ItemGroup></Project>")]
    [InlineData("<Project><ItemGroup><Analyzer Include=\"../ViajantesTurismo.Architecture.Analyzers.dll\" /></ItemGroup></Project>")]
    public void Production_http_analyzer_opt_in_shapes_should_be_detected(string projectXml)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(projectXml);

        // Act
        var enablesAnalyzer = AnalyzerSuppressionPolicyTestsHelpers.EnablesProductionHttpAnalyzerInProject(projectXml);

        // Assert
        enablesAnalyzer.ShouldBeTrue();
    }

    [Fact]
    public void Ordinary_project_references_should_not_enable_the_production_http_analyzer()
    {
        // Arrange
        const string projectXml = "<Project><ItemGroup><ProjectReference Include=\"../ViajantesTurismo.Architecture.Analyzers/"
            + "ViajantesTurismo.Architecture.Analyzers.csproj\" /></ItemGroup></Project>";

        // Act
        var enablesAnalyzer = AnalyzerSuppressionPolicyTestsHelpers.EnablesProductionHttpAnalyzerInProject(projectXml);

        // Assert
        enablesAnalyzer.ShouldBeFalse();
    }

    [Fact]
    public void Repository_policy_files_should_not_suppress_production_http_enforcement()
    {
        // Arrange
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var policyFiles = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.csproj")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.props"))
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.targets"))
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, ".editorconfig"))
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.globalconfig"))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Act
        var broadSettings = policyFiles
            .SelectMany(path => AnalyzerSuppressionPolicyTestsHelpers.FindBroadProductionHttpSuppressions(repositoryRoot, path))
            .ToArray();

        // Assert
        broadSettings.ShouldBeEmpty();
    }

    [Fact]
    public void Production_http_analyzer_should_remain_app_owned_and_staged_until_activation_dependencies_complete()
    {
        // Arrange
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var analyzerProjectPath = Path.Combine(
            repositoryRoot,
            "src",
            "ViajantesTurismo.Architecture.Analyzers",
            "ViajantesTurismo.Architecture.Analyzers.csproj");
        var analyzerTestsProjectPath = Path.Combine(
            repositoryRoot,
            "tests",
            "ViajantesTurismo.Architecture.Analyzers.Tests",
            "ViajantesTurismo.Architecture.Analyzers.Tests.csproj");
        var analyzerReadmePath = Path.Combine(repositoryRoot, "src", "ViajantesTurismo.Architecture.Analyzers", "README.md");
        var solutionPath = Path.Combine(repositoryRoot, "ViajantesTurismo.slnx");
        var allProjectFiles = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.csproj")
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .ToArray();
        var productionApiProjects = Directory.GetFiles(
            Path.Combine(repositoryRoot, "src"),
            "ViajantesTurismo.*.ApiService.csproj",
            SearchOption.AllDirectories)
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .ToArray();
        var centralBuildFiles = AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.props")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(repositoryRoot, "*.targets"))
            .Where(path => !AnalyzerSuppressionPolicyTestsHelpers.IsIgnoredPath(path))
            .ToArray();

        // Act
        var analyzerProject = File.ReadAllText(analyzerProjectPath);
        var analyzerTestsProject = File.ReadAllText(analyzerTestsProjectPath);
        var analyzerReadme = File.ReadAllText(analyzerReadmePath);
        var solution = File.ReadAllText(solutionPath);
        var productionApiProjectSet = productionApiProjects.ToHashSet(StringComparer.Ordinal);
        var allowedAnalyzerProjectFiles = productionApiProjects.ToHashSet(StringComparer.Ordinal);
        allowedAnalyzerProjectFiles.Add(analyzerProjectPath);
        allowedAnalyzerProjectFiles.Add(analyzerTestsProjectPath);
        var enabledProductionProjects = productionApiProjects
            .Where(AnalyzerSuppressionPolicyTestsHelpers.EnablesProductionHttpAnalyzer)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();
        var productionAnalyzerReferences = productionApiProjects
            .Where(path => File.ReadAllText(path).Contains("ViajantesTurismo.Architecture.Analyzers", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();
        var enabledNonProductionProjects = allProjectFiles
            .Where(path => !productionApiProjectSet.Contains(path))
            .Where(AnalyzerSuppressionPolicyTestsHelpers.EnablesProductionHttpAnalyzer)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();
        var unauthorizedAnalyzerProjectReferences = allProjectFiles
            .Where(path => !allowedAnalyzerProjectFiles.Contains(path))
            .Where(path => File.ReadAllText(path).Contains("ViajantesTurismo.Architecture.Analyzers", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();
        var centralAnalyzerEnablement = centralBuildFiles
            .Where(AnalyzerSuppressionPolicyTestsHelpers.EnablesProductionHttpAnalyzer)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();
        var centralAnalyzerReferences = centralBuildFiles
            .Where(path => File.ReadAllText(path).Contains("ViajantesTurismo.Architecture.Analyzers", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();
        var codeFixProjectExists = Directory.Exists(Path.Combine(repositoryRoot, "src", "ViajantesTurismo.Architecture.CodeFixes"));

        // Assert
        productionApiProjects.Length.ShouldBe(3);
        enabledProductionProjects.ShouldBeEmpty();
        productionAnalyzerReferences.ShouldBeEmpty();
        enabledNonProductionProjects.ShouldBeEmpty();
        unauthorizedAnalyzerProjectReferences.ShouldBeEmpty();
        centralAnalyzerEnablement.ShouldBeEmpty();
        centralAnalyzerReferences.ShouldBeEmpty();
        analyzerProject.ShouldNotContain("SharedKernel.", StringComparison.Ordinal);
        analyzerTestsProject.ShouldContain("SharedKernel.Testing.Roslyn", StringComparison.Ordinal);
        solution.ShouldContain("src/ViajantesTurismo.Architecture.Analyzers/ViajantesTurismo.Architecture.Analyzers.csproj", StringComparison.Ordinal);
        solution.ShouldContain("tests/ViajantesTurismo.Architecture.Analyzers.Tests/ViajantesTurismo.Architecture.Analyzers.Tests.csproj", StringComparison.Ordinal);
        analyzerReadme.ShouldContain("Staged rollout: not enabled", StringComparison.Ordinal);
        analyzerReadme.ShouldContain("asynchronous Admin", StringComparison.Ordinal);
        analyzerReadme.ShouldContain("retires the standalone Branding", StringComparison.Ordinal);
        analyzerReadme.ShouldContain("OutputItemType=\"Analyzer\"", StringComparison.Ordinal);
        codeFixProjectExists.ShouldBeFalse();
    }
}
