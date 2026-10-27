using System.Globalization;

namespace ViajantesTurismo.Architecture.Analyzers.Tests;

public sealed class NamingConventionAnalyzerTests
{
    private const string DomainAssembly = "ViajantesTurismo.Admin.Domain";

    private const string Support = """
        namespace PandaBear.Messaging.IntegrationEvents
        {
            public interface IIntegrationEvent
            {
            }
        }

        namespace PandaBear.Results
        {
            public readonly struct Result
            {
            }

            public readonly struct Result<T>
            {
            }
        }

        """;

    [Fact]
    public void Supported_diagnostics_are_the_integration_event_and_error_class_name_rules()
    {
        // Arrange
        var analyzer = new NamingConventionAnalyzer();

        // Act
        var diagnosticIds = analyzer.SupportedDiagnostics.Select(static descriptor => descriptor.Id);

        // Assert
        diagnosticIds.ShouldBe([ArchitectureDiagnosticIds.IntegrationEventName, ArchitectureDiagnosticIds.ErrorClassName]);
    }

    [Theory]
    [InlineData("public sealed record TourCreated(int Id) : IIntegrationEvent;")]
    [InlineData("public sealed class TourCreatedEvent : IIntegrationEvent { }")]
    [InlineData("public readonly struct TourCreated : IIntegrationEvent { }")]
    public async Task Concrete_integration_events_without_the_suffix_are_reported(string declaration)
    {
        // Arrange
        var source = $$"""
            {{Support}}
            namespace Demo.Events
            {
                using PandaBear.Messaging.IntegrationEvents;

                {{declaration}}
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(new NamingConventionAnalyzer(), source);

        // Assert
        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(ArchitectureDiagnosticIds.IntegrationEventName);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("IntegrationEvent", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Integration_events_with_the_suffix_and_non_event_types_are_not_reported()
    {
        // Arrange
        var source = $$"""
            {{Support}}
            namespace Demo.Events
            {
                using PandaBear.Messaging.IntegrationEvents;

                public sealed record TourCreatedIntegrationEvent(int Id) : IIntegrationEvent;

                public abstract record BaseMessage : IIntegrationEvent;

                public sealed class TourCreatedHandler { }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(new NamingConventionAnalyzer(), source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Domain_static_classes_that_only_create_results_should_use_the_error_class_suffix()
    {
        // Arrange
        var source = $$"""
            {{Support}}
            namespace Demo.Tours
            {
                using PandaBear.Results;

                public static class TourFailures
                {
                    public static Result NotFound() => default;

                    public static Result<int> Invalid(string reason) => default;
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(new NamingConventionAnalyzer(), source, DomainAssembly);

        // Assert
        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(ArchitectureDiagnosticIds.ErrorClassName);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("TourFailures", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_classes_with_the_suffix_and_mixed_static_classes_are_not_reported()
    {
        // Arrange
        var source = $$"""
            {{Support}}
            namespace Demo.Tours
            {
                using PandaBear.Results;

                public static class TourErrors
                {
                    public static Result NotFound() => default;
                }

                public static class TourRules
                {
                    public static Result Check() => default;

                    public static bool IsOpen() => true;
                }

                public static class Empty
                {
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(new NamingConventionAnalyzer(), source, DomainAssembly);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Static_result_classes_outside_domain_assemblies_are_not_reported()
    {
        // Arrange
        var source = $$"""
            {{Support}}
            namespace Demo.Api
            {
                using PandaBear.Results;

                public static class ApiFailures
                {
                    public static Result Failed() => default;
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(new NamingConventionAnalyzer(), source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }
}
