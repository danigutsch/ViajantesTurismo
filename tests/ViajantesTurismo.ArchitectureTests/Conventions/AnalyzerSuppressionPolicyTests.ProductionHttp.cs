using System.Xml.Linq;

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
        const string multipleInvocationSuppression = """
            public sealed class Registration
            {
                public void Configure(ServiceCollection services)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    _ = (services.AddHttpClient("catalog-api"), services.AddHttpClient("branding-api"));
            #pragma warning restore VTARCH001
                }
            }
            """;
        const string conditionalExpressionSuppression = """
            public sealed class Registration
            {
                public void Configure(ServiceCollection services, bool useCatalog)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    _ = useCatalog ? services.AddHttpClient("catalog-api") : services.AddHttpClient("branding-api");
            #pragma warning restore VTARCH001
                }
            }
            """;
        const string nestedDestinationAssignmentSuppression = """
            public sealed class Registration
            {
                public void Configure(ServiceCollection services)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    services.AddHttpClient(ResourceNames.CatalogApi, client => client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}"));
            #pragma warning restore VTARCH001
                }
            }
            """;
        const string objectCreationAssignmentSuppression = """
            public sealed class Registration
            {
                public void Configure(HttpClient client, HttpRequestMessage request, Uri brandingUri, Uri catalogUri)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    var pair = new Pair(client.BaseAddress = brandingUri, request.RequestUri = catalogUri);
            #pragma warning restore VTARCH001
                }
            }
            """;
        const string exactAssignmentSuppression = """
            public sealed class Registration
            {
                public void Configure(HttpClient client)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    client.BaseAddress = new Uri("https+http://branding-api");
            #pragma warning restore VTARCH001
                }
            }
            """;
        const string exactAliasAssignmentSuppression = """
            public sealed class Registration
            {
                public void Configure(HttpClient client, Uri brandingUri)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    client.BaseAddress = brandingUri;
            #pragma warning restore VTARCH001
                }
            }
            """;
        const string typedNamedClientSuppression = """
            public sealed class Registration
            {
                public void Configure(ServiceCollection services)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(ResourceNames.CatalogApi);
            #pragma warning restore VTARCH001
                }
            }
            """;
        const string multipleTypedClientSuppression = """
            public sealed class Registration
            {
                public void Configure(ServiceCollection services)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    services.AddHttpClient<ICatalogClient, BrandingAdapter>();
            #pragma warning restore VTARCH001
                }
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
        var multipleInvocationViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(multipleInvocationSuppression, "MultipleInvocationSuppression.cs");
        var conditionalExpressionViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(conditionalExpressionSuppression, "ConditionalExpressionSuppression.cs");
        var nestedDestinationAssignmentViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(nestedDestinationAssignmentSuppression, "NestedDestinationAssignmentSuppression.cs");
        var objectCreationAssignmentViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(objectCreationAssignmentSuppression, "ObjectCreationAssignmentSuppression.cs");
        var exactAssignmentViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactAssignmentSuppression, "ExactAssignmentSuppression.cs");
        var exactAliasAssignmentViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactAliasAssignmentSuppression, "ExactAliasAssignmentSuppression.cs");
        var typedNamedClientViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(typedNamedClientSuppression, "TypedNamedClientSuppression.cs");
        var multipleTypedClientViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(multipleTypedClientSuppression, "MultipleTypedClientSuppression.cs");
        var exactExpressionViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactExpressionSuppression, "ExactExpressionSuppression.cs");
        var exactLocalViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactLocalSuppression, "ExactLocalSuppression.cs");
        var exactReturnViolations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(exactReturnSuppression, "ExactReturnSuppression.cs");

        // Assert
        broadPragmaViolations.ShouldNotBeEmpty();
        missingJustificationViolations.ShouldContain(static violation => violation.Contains("missing '// Justification: ...'", StringComparison.Ordinal));
        blockPragmaViolations.ShouldNotBeEmpty();
        controlFlowViolations.ShouldNotBeEmpty();
        nestedBlockViolations.ShouldNotBeEmpty();
        multipleInvocationViolations.ShouldNotBeEmpty();
        conditionalExpressionViolations.ShouldNotBeEmpty();
        nestedDestinationAssignmentViolations.ShouldNotBeEmpty();
        objectCreationAssignmentViolations.ShouldNotBeEmpty();
        exactExpressionViolations.ShouldBeEmpty();
        exactLocalViolations.ShouldBeEmpty();
        exactReturnViolations.ShouldBeEmpty();
        exactAssignmentViolations.ShouldBeEmpty();
        exactAliasAssignmentViolations.ShouldBeEmpty();
        typedNamedClientViolations.ShouldNotBeEmpty();
        multipleTypedClientViolations.ShouldNotBeEmpty();
    }

    [Fact]
    public void Active_conditional_pragma_suppressions_should_be_inspected()
    {
        // Arrange
        const string source = """
            #if NET10_0
            #pragma warning disable VTARCH001
            services.AddHttpClient(ResourceNames.BrandingApi);
            #pragma warning restore VTARCH001
            #endif
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(
            source,
            "ConditionalRegistration.cs");

        // Assert
        violations.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("NET10_0_OR_GREATER")]
    [InlineData("NETSTANDARD2_0")]
    public void Every_conditional_pragma_branch_should_be_inspected(string symbol)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var source = $$"""
            #if {{symbol}}
            #pragma warning disable VTARCH001
            services.AddHttpClient(ResourceNames.BrandingApi);
            #pragma warning restore VTARCH001
            #endif
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindUnapprovedPragmaWarningSuppressionsInSource(
            source,
            "ConditionalRegistration.cs");

        // Assert
        violations.ShouldNotBeEmpty();
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
    [InlineData("SuppressMessageAttribute")]
    [InlineData("UnconditionalSuppressMessageAttribute")]
    public void Global_suppression_aliases_should_be_rejected_across_source_files(string attributeType)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeType);
        var aliasSource = $"global using Ignore = System.Diagnostics.CodeAnalysis.{attributeType};";
        const string consumerSource = "[assembly: Ignore(\"Architecture\", \"VTARCH001\")]";

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(aliasSource, "GlobalAliases.cs")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(consumerSource, "GlobalSuppressions.cs"))
            .ToArray();

        // Assert
        violations.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("SuppressMessageAttribute")]
    [InlineData("UnconditionalSuppressMessageAttribute")]
    public void Conditional_global_suppression_aliases_should_be_rejected_across_source_files(string attributeType)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeType);
        var aliasSource = $$"""
            #if NET10_0
            global using Ignore = System.Diagnostics
                /* split to exercise disabled trivia */
                .CodeAnalysis.{{attributeType}};
            #endif
            """;
        const string consumerSource = "[assembly: Ignore(\"Architecture\", \"VTARCH001\")]";

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(aliasSource, "ConditionalGlobalAliases.cs")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(consumerSource, "GlobalSuppressions.cs"))
            .ToArray();

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Fact]
    public void Conditional_unrelated_global_aliases_should_not_be_rejected()
    {
        // Arrange
        const string aliasSource = """
            #if NET10_0
            global using Ignore = Contoso.SuppressMessageAttribute;
            #endif
            """;
        const string consumerSource = "[assembly: Ignore(\"Architecture\", \"VTARCH001\")]";

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(aliasSource, "ConditionalGlobalAliases.cs")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(consumerSource, "GlobalSuppressions.cs"))
            .ToArray();

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Active_conditional_suppress_message_attributes_should_be_rejected()
    {
        // Arrange
        const string suppressionAttributeName = "SuppressMessage";
        var source = $$"""
            #if NET10_0
            [assembly: System.Diagnostics.CodeAnalysis.{{suppressionAttributeName}}("Architecture", "VTARCH001")]
            #endif
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(
            source,
            "ConditionalSuppressions.cs");

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("NET10_0_OR_GREATER")]
    [InlineData("NETSTANDARD2_0")]
    public void Every_conditional_suppress_message_branch_should_be_inspected(string symbol)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        const string suppressionAttributeName = "SuppressMessage";
        var source = $$"""
            #if {{symbol}}
            [assembly: System.Diagnostics.CodeAnalysis.{{suppressionAttributeName}}("Architecture", "VTARCH001")]
            #endif
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(
            source,
            "ConditionalSuppressions.cs");

        // Assert
        violations.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(\"Style\", \"CA1822\")]")]
    [InlineData("using Ignore = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;")]
    public void Unrelated_conditional_suppression_content_should_remain_allowed(string conditionalContent)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionalContent);
        var source = $$"""
            #if NETSTANDARD2_0
            {{conditionalContent}}
            #endif
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(
            source,
            "ConditionalSuppressions.cs");

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Custom_same_name_suppress_message_attributes_should_not_be_rejected()
    {
        // Arrange
        const string source = """
            [assembly: Contoso.SuppressMessage("Architecture", "VTARCH001")]

            namespace Contoso
            {
                public sealed class SuppressMessageAttribute : System.Attribute
                {
                    public SuppressMessageAttribute(string category, string checkId)
                    {
                    }
                }
            }
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(
            source,
            "CustomSuppressions.cs");

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Multiline_global_suppression_aliases_should_be_rejected()
    {
        // Arrange
        const string aliasSource = """
            global using Ignore = System.Diagnostics
                /* split active syntax */
                .CodeAnalysis.SuppressMessageAttribute;
            """;
        const string consumerSource = "[assembly: Ignore(\"Architecture\", \"VTARCH001\")]";

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(aliasSource, "GlobalAliases.cs")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(consumerSource, "GlobalSuppressions.cs"))
            .ToArray();

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Fact]
    public void Alias_like_text_inside_disabled_block_comments_should_not_be_rejected()
    {
        // Arrange
        const string source = """
            #if NET10_0
            /*
            global using Ignore = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
            */
            #endif
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(source, "CommentedAliases.cs");

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Unresolved_cross_file_suppress_message_check_ids_should_be_rejected()
    {
        // Arrange
        const string idsSource = "public static class RuleIds { public const string HttpBoundary = \"VTARCH001\"; }";
        const string suppressionSource = """
            using Ignore = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
            [assembly: Ignore("Architecture", RuleIds.HttpBoundary)]
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(idsSource, "RuleIds.cs")
            .Concat(AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(suppressionSource, "Suppressions.cs"))
            .ToArray();

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Fact]
    public void Resolved_non_target_suppress_message_check_ids_should_remain_allowed()
    {
        // Arrange
        const string source = """
            using Ignore = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
            [assembly: Ignore("Architecture", RuleIds.OtherBoundary)]

            public static class RuleIds
            {
                public const string OtherBoundary = "OTHER001";
            }
            """;

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindProductionHttpSuppressMessageSuppressionsInSource(source, "Suppressions.cs");

        // Assert
        violations.ShouldBeEmpty();
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
    [InlineData("<NoWarn>&#x56;TARCH001</NoWarn>")]
    [InlineData("<NoWarn><![CDATA[VTARCH001]]></NoWarn>")]
    [InlineData("<WarningsNotAsErrors>CS0001,VTARCH001</WarningsNotAsErrors>")]
    [InlineData("<warningsnotaserrors>$(WarningsNotAsErrors);vtarch001</warningsnotaserrors>")]
    [InlineData("<WarningsNotAsErrors>CS0618</WarningsNotAsErrors>")]
    [InlineData("<Project><ItemGroup><GlobalAnalyzerConfigFiles Include=\"policy.txt\" /></ItemGroup></Project>")]
    [InlineData("<Project><PropertyGroup><CodeAnalysisRuleSet>disable-vtarch001.ruleset</CodeAnalysisRuleSet></PropertyGroup></Project>")]
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
    [InlineData("RunAnalyzers", "false")]
    [InlineData("RunAnalyzers", "0")]
    [InlineData("RunAnalyzers", "off")]
    [InlineData("RunAnalyzersDuringBuild", "false")]
    [InlineData("RunAnalyzersDuringBuild", "no")]
    public void Analyzer_execution_disabling_values_should_be_rejected(string propertyName, string value)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var project = $"<Project><PropertyGroup><{propertyName}>{value}</{propertyName}></PropertyGroup></Project>";

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindBroadProductionHttpSuppressionsInSource(project, "Project.csproj");

        // Assert
        violations.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("RunAnalyzers")]
    [InlineData("RunAnalyzersDuringBuild")]
    public void Explicitly_enabled_analyzer_execution_should_remain_allowed(string propertyName)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        var project = $"<Project><PropertyGroup><{propertyName}>true</{propertyName}></PropertyGroup></Project>";

        // Act
        var violations = AnalyzerSuppressionPolicyTestsHelpers.FindBroadProductionHttpSuppressionsInSource(project, "Project.csproj");

        // Assert
        violations.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("EditorConfigFiles", "Include", "policy.txt")]
    [InlineData("EditorConfigFiles", "Update", "policy.rules")]
    [InlineData("GlobalAnalyzerConfigFiles", "Include", "policy.custom")]
    [InlineData("GlobalAnalyzerConfigFiles", "Update", "policy.settings")]
    public void Repository_declared_analyzer_config_inputs_should_be_rejected_before_they_can_suppress_the_rule(
        string itemName,
        string operation,
        string path)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var project = $"<Project><ItemGroup><{itemName} {operation}=\"{path}\" /></ItemGroup></Project>";
        const string policy = "dotnet_diagnostic.VTARCH001.severity = none";

        // Act
        var inputViolations = AnalyzerSuppressionPolicyTestsHelpers.FindBroadProductionHttpSuppressionsInSource(project, "Project.csproj");
        var policyViolations = AnalyzerSuppressionPolicyTestsHelpers.FindBroadProductionHttpSuppressionsInSource(policy, "policy.txt");

        // Assert
        inputViolations.ShouldHaveSingleItem();
        policyViolations.ShouldHaveSingleItem();
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
    public void Production_http_analyzer_public_api_baselines_should_be_enforced()
    {
        // Arrange
        var repositoryRoot = AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "ViajantesTurismo.Architecture.Analyzers",
            "ViajantesTurismo.Architecture.Analyzers.csproj");

        // Act
        var project = XDocument.Load(projectPath);
        var publicApiAnalyzerReferences = project.Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal)
                && string.Equals(
                    element.Attribute("Include")?.Value,
                    "Microsoft.CodeAnalysis.PublicApiAnalyzers",
                    StringComparison.Ordinal))
            .ToArray();

        // Assert
        publicApiAnalyzerReferences.ShouldHaveSingleItem(static reference =>
            string.Equals(reference.Attribute("PrivateAssets")?.Value, "all", StringComparison.OrdinalIgnoreCase)
            || reference.Elements().Any(static element =>
                string.Equals(element.Name.LocalName, "PrivateAssets", StringComparison.Ordinal)
                && string.Equals(element.Value, "all", StringComparison.OrdinalIgnoreCase)));
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
