using System.Globalization;

namespace ViajantesTurismo.Architecture.Analyzers.Tests;

public sealed class ProductionApiHttpAnalyzerTests
{
    [Fact]
    public void Supported_diagnostics_match_the_production_api_http_contract()
    {
        // Arrange
        var analyzer = new ProductionApiHttpAnalyzer();

        // Act
        var diagnosticIds = analyzer.SupportedDiagnostics.Select(static descriptor => descriptor.Id);

        // Assert
        diagnosticIds.ShouldHaveSingleItem().ShouldBe(ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Current_admin_to_branding_typed_registration_reports_the_destination_and_remediation()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(ServiceCollection services)
                {
                    services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(
                        client => client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}"));
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        var diagnostic = diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        message.ShouldContain("Branding API ('branding-api')", StringComparison.Ordinal);
        message.ShouldContain("versioned integration event", StringComparison.Ordinal);
        message.ShouldContain("explicit gRPC", StringComparison.Ordinal);
        message.ShouldContain("in-process module call", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("client.BaseAddress = new Uri(\"https+http://branding-api\");")]
    [InlineData("const string Destination = ResourceNames.BrandingApi; client.BaseAddress = new Uri(Destination);")]
    [InlineData("var destination = ResourceNames.BrandingApi; var uri = new Uri($\"https+http://{destination}\"); client.BaseAddress = uri;")]
    [InlineData("var destination = new Uri($\"https+http://{ResourceNames.BrandingApi}/v1/settings\"); client.BaseAddress = destination;")]
    public async Task Base_address_resolves_literals_constants_aliases_and_interpolated_aspire_uris(string statement)
    {
        // Arrange
        var source = $$"""
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    {{statement}}
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Interpolated_uri_resolves_a_static_authority_alias()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    var host = "branding-api";
                    client.BaseAddress = new Uri($"https+http://{host}");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Repository_resource_tokens_in_a_uri_path_are_not_destinations()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    client.BaseAddress = new Uri($"https://provider.example/{ResourceNames.BrandingApi}");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Repository_resource_aliases_in_concatenated_uri_paths_are_not_destinations()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    var path = ResourceNames.BrandingApi;
                    client.BaseAddress = new Uri("https://provider.example/" + path);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_own_resource_branch_does_not_mask_a_cross_api_destination()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client, bool useAdmin)
                {
                    var destination = useAdmin ? ResourceNames.Api : ResourceNames.BrandingApi;
                    client.BaseAddress = new Uri($"https+http://{destination}");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Named_add_http_client_resolves_a_repository_resource_name_alias()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(ServiceCollection services)
                {
                    const string ClientName = ResourceNames.BrandingApi;
                    services.AddHttpClient(ClientName);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Typed_add_http_client_resolves_the_known_branding_client_symbol()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(ServiceCollection services)
                {
                    services.AddHttpClient<IBrandingApiClient, BrandingApiClient>();
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Typed_clients_with_only_a_similar_name_are_not_guessed()
    {
        // Arrange
        const string source = """
            namespace Contoso
            {
                public interface IBrandingApiClient
                {
                }

                public sealed class BrandingApiClient : IBrandingApiClient
                {
                }
            }

            namespace ViajantesTurismo.Admin.ApiService
            {
                public static class Registration
                {
                    public static void Configure(ServiceCollection services)
                    {
                        services.AddHttpClient<Contoso.IBrandingApiClient, Contoso.BrandingApiClient>();
                    }
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Http_client_factory_create_client_resolves_a_named_internal_client()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class ClientFactory
            {
                public static HttpClient Create(IHttpClientFactory factory)
                {
                    var clientName = ResourceNames.BrandingApi;
                    return factory.CreateClient(clientName);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Unrelated_http_client_factory_types_are_not_guessed()
    {
        // Arrange
        const string source = """
            namespace Contoso
            {
                public interface IHttpClientFactory
                {
                    HttpClient CreateClient(string name);
                }
            }

            namespace ViajantesTurismo.Admin.ApiService
            {
                public static class ClientFactory
                {
                    public static HttpClient Create(Contoso.IHttpClientFactory factory)
                    {
                        return factory.CreateClient(ResourceNames.BrandingApi);
                    }
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Direct_http_client_call_resolves_a_uri_alias()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class BrandingReader
            {
                public static Task<HttpResponseMessage> Read(HttpClient client)
                {
                    var resource = ResourceNames.BrandingApi;
                    var destination = new Uri($"https+http://{resource}/v1/settings");
                    return client.GetAsync(destination);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Theory]
    [InlineData("var request = new HttpRequestMessage(HttpMethod.Get, new Uri($\"https+http://{ResourceNames.BrandingApi}/v1/settings\"));")]
    [InlineData("var request = new HttpRequestMessage { RequestUri = new Uri($\"https+http://{ResourceNames.BrandingApi}/v1/settings\") };")]
    [InlineData("var request = new HttpRequestMessage(); request.RequestUri = new Uri($\"https+http://{ResourceNames.BrandingApi}/v1/settings\");")]
    public async Task Send_resolves_known_http_request_message_destinations(string requestDeclaration)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(requestDeclaration);
        var source = $$"""
            namespace ViajantesTurismo.Admin.ApiService;

            public static class BrandingReader
            {
                public static Task<HttpResponseMessage> Read(HttpClient client)
                {
                    {{requestDeclaration}}
                    return client.SendAsync(request);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Http_request_message_destinations_reassigned_from_runtime_configuration_are_not_guessed()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class BrandingReader
            {
                public static Task<HttpResponseMessage> Read(HttpClient client, string configuredEndpoint)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https+http://{ResourceNames.BrandingApi}/v1/settings"));
                    request.RequestUri = new Uri(configuredEndpoint);
                    return client.SendAsync(request);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("client.BaseAddress ??= new Uri($\"https+http://{ResourceNames.BrandingApi}\");")]
    [InlineData("var request = new HttpRequestMessage(); request.RequestUri ??= new Uri($\"https+http://{ResourceNames.BrandingApi}\");")]
    public async Task Coalesce_assignments_resolve_known_http_destinations(string statement)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(statement);
        var source = $$"""
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    {{statement}}
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Fact]
    public async Task Relative_request_paths_that_match_resource_values_are_not_destinations()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class BrandingReader
            {
                public static Task<HttpResponseMessage> Read(HttpClient client)
                {
                    return client.GetAsync("branding-api");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unrelated_http_client_extensions_are_not_treated_as_requests()
    {
        // Arrange
        const string source = """
            namespace Demo
            {
                public static class HttpClientExtensions
                {
                    public static void RecordDestination(this HttpClient client, string destination)
                    {
                    }
                }
            }

            namespace ViajantesTurismo.Admin.ApiService
            {
                public static class Telemetry
                {
                    public static void Record(HttpClient client)
                    {
                        client.RecordDestination(ResourceNames.BrandingApi);
                    }
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ViajantesTurismo.Management.Web")]
    [InlineData("ViajantesTurismo.Public.Web")]
    [InlineData("ViajantesTurismo.AppHost")]
    [InlineData("ViajantesTurismo.OpenApi.Tool")]
    [InlineData("ViajantesTurismo.Admin.Tests")]
    [InlineData("ViajantesTurismo.Admin.ApiServiceTests")]
    public async Task Non_production_api_projects_are_excluded(string assemblyName)
    {
        // Arrange
        const string source = """
            namespace DemoProject;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source, assemblyName);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("https+http://identity-provider")]
    [InlineData("https://keycloak.example.test")]
    [InlineData("https+http://litellm")]
    [InlineData("https+http://seaweedfs")]
    [InlineData("http://opentelemetry-collector:4318")]
    [InlineData("https://storage.example.test")]
    public async Task External_identity_ai_storage_and_otlp_destinations_are_excluded(string destination)
    {
        // Arrange
        var source = $$"""
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    client.BaseAddress = new Uri("{{destination}}");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Health_telemetry_grpc_events_and_in_process_calls_are_not_ordinary_http_diagnostics()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class ApprovedCommunication
            {
                public static void Configure(HealthBuilder health, TelemetryOptions telemetry, GrpcClient grpc, EventPublisher events, BrandingModule module)
                {
                    health.AddUrlGroup(new Uri($"https+http://{ResourceNames.BrandingApi}/health"));
                    telemetry.Endpoint = new Uri($"https+http://{ResourceNames.OpenTelemetryCollector}:4318");
                    grpc.Connect($"https+http://{ResourceNames.BrandingApi}");
                    events.Publish(new BrandingUpdatedIntegrationEvent());
                    module.ReadSettings();
                }
            }

            public sealed class HealthBuilder
            {
                public void AddUrlGroup(Uri uri) { }
            }

            public sealed class TelemetryOptions
            {
                public Uri? Endpoint { get; set; }
            }

            public sealed class GrpcClient
            {
                public void Connect(string address) { }
            }

            public sealed class EventPublisher
            {
                public void Publish(BrandingUpdatedIntegrationEvent message) { }
            }

            public sealed class BrandingUpdatedIntegrationEvent
            {
            }

            public sealed class BrandingModule
            {
                public void ReadSettings() { }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Runtime_configured_destinations_are_not_guessed()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client, string configuredEndpoint)
                {
                    client.BaseAddress = new Uri(configuredEndpoint);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Local_aliases_reassigned_from_runtime_configuration_are_not_guessed()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client, string configuredEndpoint)
                {
                    var destination = ResourceNames.BrandingApi;
                    destination = configuredEndpoint;
                    client.BaseAddress = new Uri(destination);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Local_alias_reassignment_after_a_forbidden_use_does_not_hide_the_use()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client, string configuredEndpoint)
                {
                    var destination = ResourceNames.BrandingApi;
                    client.BaseAddress = new Uri($"https+http://{destination}");
                    destination = configuredEndpoint;
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldHaveSingleItem(static candidate => candidate.Id == ArchitectureDiagnosticIds.ProductionApiHttp);
    }

    [Theory]
    [InlineData(AnalyzerTestHarness.AdminApiAssemblyName, "Api")]
    [InlineData(AnalyzerTestHarness.CatalogApiAssemblyName, "CatalogApi")]
    [InlineData(AnalyzerTestHarness.BrandingApiAssemblyName, "BrandingApi")]
    public async Task Production_apis_may_reference_their_own_resource(string assemblyName, string resourceName)
    {
        // Arrange
        var source = $$"""
            namespace ProductionApi;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    client.BaseAddress = new Uri($"https+http://{ResourceNames.{{resourceName}}}");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source, assemblyName);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Exact_site_pragma_with_the_diagnostic_ID_and_justification_suppresses_one_call()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(ServiceCollection services)
                {
            #pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
                    services.AddHttpClient(ResourceNames.BrandingApi);
            #pragma warning restore VTARCH001
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source);

        // Assert
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Generated_sources_are_excluded()
    {
        // Arrange
        const string source = """
            namespace ViajantesTurismo.Admin.ApiService;

            public static class Registration
            {
                public static void Configure(HttpClient client)
                {
                    client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}");
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHarness.GetAnalyzerDiagnostics(source, path: "Generated.g.cs");

        // Assert
        diagnostics.ShouldBeEmpty();
    }
}
