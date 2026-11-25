using System.Collections.Immutable;
using System.Globalization;
using SharedKernel.Testing.Roslyn;

namespace ViajantesTurismo.Architecture.Analyzers.Tests;

/// <summary>Verifies advisory placement against resolved helper dependencies.</summary>
public sealed class SharedKernelHelperPlacementAnalyzerTests
{
    private const string CandidateConfiguration = "build_property.ViajantesTurismoSharedKernelHelperCandidates";
    private const string OwnerConfiguration = "build_property.ViajantesTurismoSharedKernelCapabilityOwners";
    private const string HelperSource = """
        using Duration = System.TimeSpan;

        namespace Demo.Helpers;

        internal static class ElapsedHelper
        {
            public static Duration Interval(int ticks) => Duration.FromTicks(ticks);
        }
        """;

    [Theory]
    [InlineData("ViajantesTurismo.Admin.Application", "clock=PandaBear.SharedKernel.Scheduling", "source", "PandaBear.SharedKernel.Scheduling")]
    [InlineData("ViajantesTurismo.Admin.UnitTests", "", "test", "a focused new SharedKernel library")]
    public async Task Explicit_helper_candidate_reports_resolved_types_and_capability_ownership(
        string assembly, string owners, string scope, string destination)
    {
        // Arrange
        var options = new TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string>.Empty
            .Add(CandidateConfiguration, "Demo.Helpers.ElapsedHelper=clock")
            .Add(OwnerConfiguration, owners));
        var analyzer = new SharedKernelHelperPlacementAnalyzer();

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(
            analyzer, HelperSource, assembly, optionsProvider: options);

        // Assert
        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(ArchitectureDiagnosticIds.SharedKernelHelperPlacement);
        diagnostic.Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Info);
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        message.ShouldContain($"{scope} helper 'Demo.Helpers.ElapsedHelper'", StringComparison.Ordinal);
        message.ShouldContain("System.TimeSpan", StringComparison.Ordinal);
        message.ShouldContain("clock", StringComparison.Ordinal);
        message.ShouldContain(destination, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Application_policy_is_not_analyzed_without_an_explicit_role()
    {
        // Arrange
        var analyzer = new SharedKernelHelperPlacementAnalyzer();

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(analyzer, HelperSource);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Candidate_with_a_repository_local_dependency_in_another_partial_declaration_is_not_recommended()
    {
        // Arrange
        const string source = """
            using Tour = Demo.Helpers.CatalogTour;

            namespace Demo.Helpers;

            internal sealed class CatalogTour { }

            internal static partial class ElapsedHelper
            {
                public static TimeSpan Interval(int ticks) => TimeSpan.FromTicks(ticks);
            }

            internal static partial class ElapsedHelper
            {
                public static System.Collections.Generic.List<Tour> Tours() => [];
            }
            """;
        var options = new TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string>.Empty
            .Add(CandidateConfiguration, "Demo.Helpers.ElapsedHelper=clock")
            .Add(OwnerConfiguration, "clock=PandaBear.SharedKernel.Scheduling"));
        var analyzer = new SharedKernelHelperPlacementAnalyzer();

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(analyzer, source, optionsProvider: options);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Candidate_reports_array_element_evidence_and_uses_only_valid_owner_mappings()
    {
        // Arrange
        const string source = """
            namespace Demo.Helpers;

            internal static class ElapsedHelper
            {
                private sealed class Nested { }

                public static T[] Copy<T>(T[] values)
                {
                    _ = new Nested();
                    return [..values];
                }

                public static TimeSpan[] Durations() => [];
            }
            """;
        var options = new TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string>.Empty
            .Add(CandidateConfiguration, "invalid|=ignored|also-invalid=| Demo.Helpers.ElapsedHelper = clock ")
            .Add(OwnerConfiguration, "clock=Other.Library"));
        var analyzer = new SharedKernelHelperPlacementAnalyzer();

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(analyzer, source, optionsProvider: options);

        // Assert
        var message = diagnostics.ShouldHaveSingleItem().GetMessage(CultureInfo.InvariantCulture);
        message.ShouldContain("System.TimeSpan", StringComparison.Ordinal);
        message.ShouldContain("a focused new SharedKernel library", StringComparison.Ordinal);
        message.ShouldNotContain("Nested", StringComparison.Ordinal);
    }

    [Fact]
    public void Initialize_requires_a_context()
    {
        // Arrange
        var analyzer = new SharedKernelHelperPlacementAnalyzer();

        // Act
        var exception = ShouldAssertionExtensions.ShouldThrow<ArgumentNullException>(() => analyzer.Initialize(null!));

        // Assert
        exception.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData("ViajantesTurismo.Catalog.Testing", true)]
    [InlineData("ViajantesTurismo.Catalog.Application", false)]
    public async Task Only_test_helpers_can_depend_on_xUnit(string assembly, bool permitted)
    {
        // Arrange
        const string source = """
            namespace Demo.Helpers;

            internal static class ElapsedHelper
            {
                public static void Verify(string value)
                {
                    using var writer = new SharedKernel.Testing.InvariantStringWriter();
                    writer.Write(value);
                    Xunit.Assert.Equal(value, writer.ToString());
                }
            }
            """;
        var options = new TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string>.Empty
            .Add(CandidateConfiguration, "Demo.Helpers.ElapsedHelper=clock"));
        var analyzer = new SharedKernelHelperPlacementAnalyzer();

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(
            analyzer, source, assembly, optionsProvider: options);

        // Assert
        diagnostics.Length.ShouldBe(permitted ? 1 : 0);
        if (permitted)
        {
            var message = diagnostics[0].GetMessage(CultureInfo.InvariantCulture);
            message.ShouldContain("Xunit.Assert", StringComparison.Ordinal);
            message.ShouldContain("SharedKernel.Testing.InvariantStringWriter", StringComparison.Ordinal);
        }
    }
}
