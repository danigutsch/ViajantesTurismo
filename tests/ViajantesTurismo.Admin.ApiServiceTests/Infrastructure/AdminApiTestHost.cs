using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore;
using PandaBear.MalwareScanning;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Mediator;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Creates in-memory admin API hosts with test authentication, no hosted services and fake capacity locking.</summary>
internal static class AdminApiTestHost
{
    private const string Audience = "admin-api";

    /// <summary>Creates a test host for the optional environment, applying the optional service and configuration overrides.</summary>
    public static WebApplicationFactory<AdminApiHostEntryPoint> Create(
        Action<IServiceCollection>? configureTestServices = null,
        string? environment = null,
        Action<IDictionary<string, string?>>? configureConfiguration = null)
    {
        var hostEnvironment = environment ?? Environments.Development;
        var disableMalwareScanner = !string.Equals(
            hostEnvironment,
            Environments.Production,
            StringComparison.Ordinal);
        var configuration = CreateConfiguration(disableMalwareScanner);
        configureConfiguration?.Invoke(configuration);

        return WebApplicationTestHost.Create<AdminApiHostEntryPoint>(
            environment: hostEnvironment,
            configureTestServices: services =>
            {
                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
                services.RemoveAll<IHostedService>();
                services.Replace(
                    ServiceDescriptor.Singleton<ITourCapacityMutationLock, NoOpTourCapacityMutationLock>());
                configureTestServices?.Invoke(services);
            },
            configuration: configuration);
    }

    private static Dictionary<string, string?> CreateConfiguration(bool disableMalwareScanner)
    {
        var configuration = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=viajantes-admin",
            [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
            [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority
        };

        if (disableMalwareScanner)
        {
            configuration[ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString;
            return configuration;
        }

        configuration[ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav";
        configuration[ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310";

        return configuration;
    }

    /// <summary>Gets the hosting environment name of the factory.</summary>
    public static string GetEnvironmentName(WebApplicationFactory<AdminApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.Services.GetRequiredService<IHostEnvironment>().EnvironmentName;
    }

    /// <summary>Authenticates the client for the admin API audience with the given role.</summary>
    public static void ConfigureAuthenticatedClient(HttpClient client, string role)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, role);
    }

    /// <summary>Scans empty content with the configured malware scanner and returns the result.</summary>
    public static async ValueTask<MalwareScanResult> ScanEmptyContent(
        WebApplicationFactory<AdminApiHostEntryPoint> factory,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        var scanner = scope.ServiceProvider.GetRequiredService<IMalwareScanner>();
        await using var content = new MemoryStream();

        return await scanner.Scan(content, length: 0, ct);
    }

    /// <summary>Asserts that the services required by the mapped mutation endpoints resolve.</summary>
    public static void VerifyMappedMutationDependencies(WebApplicationFactory<AdminApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IQueryService>();
        _ = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<UpdateCustomerCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<CustomerImportWorkflowService>();
        _ = scope.ServiceProvider.GetRequiredService<IMalwareScanner>();
        _ = scope.ServiceProvider.GetRequiredService<ISender>();
        scope.ServiceProvider.GetRequiredService<ITourCapacityMutationLock>()
            .ShouldBeOfType<NoOpTourCapacityMutationLock>();
    }

    /// <summary>Asserts that the services required by the mapped document endpoints resolve.</summary>
    public static void VerifyMappedDocumentDependencies(WebApplicationFactory<AdminApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IDocumentQueryService>();
        _ = scope.ServiceProvider.GetRequiredService<DocumentAuditWriter>();
        _ = scope.ServiceProvider.GetRequiredService<GenerateContractDraftCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<BeginDocumentReviewCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<RequestDocumentChangesCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<UpdateDocumentFieldCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<ApproveDocumentCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<FinalizeDocumentCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<GetFinalizedDocumentArtifactHandler>();
        _ = scope.ServiceProvider.GetRequiredService<RegenerateDocumentDraftCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<VoidDocumentCommandHandler>();
    }
}
