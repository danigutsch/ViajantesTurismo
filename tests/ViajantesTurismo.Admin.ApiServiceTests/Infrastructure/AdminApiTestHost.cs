using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SharedKernel.AspNetCore;
using SharedKernel.MalwareScanning;
using SharedKernel.MalwareScanning.ClamAv;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.RecordPayment;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingNotes;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Application.Tours.UpdateTour;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

internal static class AdminApiTestHost
{
    private const string Audience = "admin-api";

    public static WebApplicationFactory<AdminApiHostEntryPoint> Create(
        Action<IServiceCollection>? configureTestServices = null,
        string? environment = null)
    {
        var hostEnvironment = environment ?? Environments.Development;
        var disableMalwareScanner = !string.Equals(
            hostEnvironment,
            Environments.Production,
            StringComparison.Ordinal);
        var configuration = CreateConfiguration(disableMalwareScanner);

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

    public static string GetEnvironmentName(WebApplicationFactory<AdminApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.Services.GetRequiredService<IHostEnvironment>().EnvironmentName;
    }

    public static void ConfigureAuthenticatedClient(HttpClient client, string role)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, role);
    }

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

    public static void VerifyMappedMutationDependencies(WebApplicationFactory<AdminApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IQueryService>();
        _ = scope.ServiceProvider.GetRequiredService<CreateTourCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<UpdateTourCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<UpdateCustomerCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<CustomerImportWorkflowService>();
        _ = scope.ServiceProvider.GetRequiredService<IMalwareScanner>();
        _ = scope.ServiceProvider.GetRequiredService<CreateBookingCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<ConfirmBookingCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<CancelBookingCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<UpdateBookingDetailsCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<UpdateBookingNotesCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<UpdateBookingDiscountCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<RecordPaymentCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<CompleteBookingCommandHandler>();
        _ = scope.ServiceProvider.GetRequiredService<DeleteBookingCommandHandler>();
        scope.ServiceProvider.GetRequiredService<ITourCapacityMutationLock>()
            .ShouldBeOfType<NoOpTourCapacityMutationLock>();
    }

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
