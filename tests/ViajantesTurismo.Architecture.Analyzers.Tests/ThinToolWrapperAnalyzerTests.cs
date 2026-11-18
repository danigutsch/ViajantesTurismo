using System.Collections.Immutable;
using System.Globalization;
using PandaBear.Testing.Roslyn;

namespace ViajantesTurismo.Architecture.Analyzers.Tests;

public sealed class ThinToolWrapperAnalyzerTests
{
    private static readonly TestAnalyzerConfigOptionsProvider ToolOptions = new(
        ImmutableDictionary<string, string>.Empty.Add("build_property.ViajantesTurismoToolWrapper", "true"));

    [Theory]
    [InlineData("http.GetStringAsync(\"/items\")", "GetStringAsync")]
    [InlineData("http.GetFromJsonAsync<string>(\"/items\")", "GetFromJsonAsync")]
    [InlineData("invoker.SendAsync(new HttpRequestMessage(), default)", "SendAsync")]
    [InlineData("http.SendAsync(new HttpRequestMessage())", "SendAsync")]
    public async Task Tool_wrappers_delegate_direct_HTTP_requests_to_source_clients(string request, string methodName)
    {
        // Arrange
        var source = $$"""
            using System.Net.Http.Json;

            namespace Demo.Tools;

            internal static class Tool
            {
                public static void Run(HttpClient http, HttpMessageInvoker invoker)
                {
                    _ = {{request}};
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(
            new ThinToolWrapperAnalyzer(), source, optionsProvider: ToolOptions);

        // Assert
        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(ArchitectureDiagnosticIds.ToolWrapperHttp);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain($"'{methodName}'", StringComparison.Ordinal);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("client in src", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tool_wrappers_can_configure_transport_and_invoke_a_source_client()
    {
        // Arrange
        const string source = """
            using System.IO;

            namespace Demo.Tools;

            internal static class Tool
            {
                public static void Run(ISourceClient client)
                {
                    using var transport = new HttpClient();
                    transport.BaseAddress = new Uri("https://example.invalid/");
                    var settings = File.ReadAllText("tool-settings.json");
                    _ = client.SendAsync(transport, settings);
                }
            }

            internal interface ISourceClient
            {
                Task SendAsync(HttpClient transport, string settings);
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(
            new ThinToolWrapperAnalyzer(), source, optionsProvider: ToolOptions);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Source_projects_can_execute_HTTP_requests()
    {
        // Arrange
        const string source = """
            namespace Demo.Sources;

            internal static class SourceClient
            {
                public static Task<string> Read(HttpClient client)
                    => client.GetStringAsync("/items");
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(
            new ThinToolWrapperAnalyzer(), source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }
}
