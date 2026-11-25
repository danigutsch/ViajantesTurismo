using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SharedKernel.AspNetCore.Authentication;
using SharedKernel.DispositionHold;
using SharedKernel.MalwareScanning;
using SharedKernel.MalwareScanning.ClamAv;
using SharedKernel.Mediator;
using SharedKernel.Mediator.EntityFrameworkCore;
using SharedKernel.Results;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.ApiServiceTests.Documents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Creates in-memory admin API hosts with test authentication and no hosted services.</summary>
internal static class AdminApiTestHost
{
    private const string Audience = "admin-api";

    /// <summary>Records measurements from this test host's meter factory.</summary>
    internal static MeterFactoryRecorder RecordMetrics(WebApplicationFactory<AdminApiHostEntryPoint> factory) =>
        new(factory.Services.GetRequiredService<IMeterFactory>());

    /// <summary>Captures traces from this test host's document operation source.</summary>
    internal static DocumentPrivacyTelemetryCapture RecordDocumentTraces(WebApplicationFactory<AdminApiHostEntryPoint> factory) =>
        new(factory.Services.GetRequiredService<DocumentOperationTelemetry>().Source);

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
                var transactionOptions = new DbContextOptionsBuilder<DbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                    .Options;
                services.Replace(ServiceDescriptor.Scoped(_ => new DbContext(transactionOptions)));
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
        _ = scope.ServiceProvider.GetRequiredService<CustomerImportWorkflowService>();
        _ = scope.ServiceProvider.GetRequiredService<IMalwareScanner>();
        _ = scope.ServiceProvider.GetRequiredService<ISender>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<ConfirmBookingCommand, Result>>();
    }

    /// <summary>Asserts that the services required by the mapped document endpoints resolve.</summary>
    public static void VerifyMappedDocumentDependencies(WebApplicationFactory<AdminApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IDocumentQueryService>();
        _ = scope.ServiceProvider.GetRequiredService<DocumentAuditWriter>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<GenerateContractDraftCommand, Result<Guid>>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<GenerateContractDraftCommand, Result<Guid>>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<BeginDocumentReviewCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<BeginDocumentReviewCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<RequestDocumentChangesCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<RequestDocumentChangesCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<AcknowledgeDocumentChangesCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<AcknowledgeDocumentChangesCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<UpdateDocumentFieldCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<UpdateDocumentFieldCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<ApproveDocumentCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<ApproveDocumentCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<FinalizeDocumentCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<FinalizeDocumentCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<GetFinalizedDocumentArtifactHandler>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<RegenerateDocumentDraftCommand, Result<Guid>>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<RegenerateDocumentDraftCommand, Result<Guid>>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<VoidDocumentCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<VoidDocumentCommand, Result>>();
        _ = scope.ServiceProvider.GetRequiredService<AdminTransactionBehavior<PlaceDocumentHoldCommand, Result<Hold>>>();
        _ = scope.ServiceProvider.GetRequiredService<IAfterRollback<PlaceDocumentHoldCommand, Result<Hold>>>();
    }
}
