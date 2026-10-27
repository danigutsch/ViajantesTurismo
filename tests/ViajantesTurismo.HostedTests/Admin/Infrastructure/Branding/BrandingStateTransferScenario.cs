extern alias AdminWorker;

using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.Branding;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Branding;

/// <summary>
/// Runs the Branding producer host (store, outbox, relay) and the Admin worker host (transport consumer, inbox,
/// projection) against real PostgreSQL databases, as two processes would in production.
/// </summary>
internal sealed class BrandingStateTransferScenario : IAsyncDisposable
{
    private readonly IHost brandingHost;
    private readonly IHost adminWorkerHost;

    private BrandingStateTransferScenario(IHost brandingHost, IHost adminWorkerHost)
    {
        this.brandingHost = brandingHost;
        this.adminWorkerHost = adminWorkerHost;
    }

    /// <summary>Builds, prepares the databases of, and starts the Branding producer host and the Admin worker host.</summary>
    public static async Task<BrandingStateTransferScenario> Start(
        PostgreSqlTestDatabase catalogDatabase,
        PostgreSqlTestDatabase adminDatabase,
        CancellationToken ct)
    {
        var brandingBuilder = CreateBuilder(catalogDatabase, adminDatabase);
        brandingBuilder.AddBrandingInfrastructure(addOutboxRelay: true);
        var brandingHost = brandingBuilder.Build();

        var adminBuilder = CreateBuilder(catalogDatabase, adminDatabase);
        adminBuilder.AddAdminIntegrationEventWorkerInfrastructure();
        AdminWorker::ViajantesTurismo.Admin.IntegrationEventWorker.AdminWorkerIntegrationEvents
            .AddAdminWorkerIntegrationEventDelivery(adminBuilder.Services);
        var adminWorkerHost = adminBuilder.Build();

        // Production migrates through MigrationService, whose Admin composition the projection scenario mirrors.
        await (await BrandingProjectionPostgreSqlScenario.Create(adminDatabase, ct)).DisposeAsync();
        await using (var scope = adminWorkerHost.Services.CreateAsyncScope())
        {
            // The shared transport table belongs to Catalog migrations in production; the worker's transport
            // context maps exactly that table, so creating its model stands in for them here.
            await scope.ServiceProvider.GetRequiredService<AdminBrandingTransportDbContext>().Database.EnsureCreatedAsync(ct);
        }

        await using (var scope = brandingHost.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<BrandingDbContext>().Database.MigrateAsync(ct);
        }

        await brandingHost.StartAsync(ct);
        await adminWorkerHost.StartAsync(ct);
        return new BrandingStateTransferScenario(brandingHost, adminWorkerHost);
    }

    /// <summary>Saves branding settings through the Branding producer host's settings store.</summary>
    public async Task Save(BrandingSettings settings, CancellationToken ct)
    {
        await using var scope = brandingHost.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBrandingSettingsStore>().SaveSettings(settings, ct);
    }

    /// <summary>Polls the Admin projection until its source revision reaches the given revision or the timeout elapses, returning the last state read.</summary>
    public async Task<BrandingProjectionState?> WaitForProjectionRevision(long revision, TimeSpan timeout, CancellationToken ct)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutSource.CancelAfter(timeout);
        BrandingProjectionState? current = null;
        while (!timeoutSource.IsCancellationRequested)
        {
            await using var scope = adminWorkerHost.Services.CreateAsyncScope();
            current = await scope.ServiceProvider.GetRequiredService<IBrandingProjectionStore>().GetCurrent(ct);
            if (current?.SourceRevision >= revision)
            {
                return current;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
        }

        return current;
    }

    /// <summary>Captures the document branding snapshot from the Admin worker's current projection.</summary>
    public async Task<PandaBear.Results.Result<DocumentBrandingSnapshotValues>> CaptureDocumentBranding(CancellationToken ct)
    {
        await using var scope = adminWorkerHost.Services.CreateAsyncScope();
        var source = new DocumentBrandingSource(scope.ServiceProvider.GetRequiredService<IBrandingProjectionStore>());
        return await source.Capture(ct);
    }

    /// <summary>Stops both hosts and releases them.</summary>
    public async ValueTask DisposeAsync()
    {
        await adminWorkerHost.StopAsync(CancellationToken.None);
        await brandingHost.StopAsync(CancellationToken.None);
        adminWorkerHost.Dispose();
        brandingHost.Dispose();
    }

    private static HostApplicationBuilder CreateBuilder(PostgreSqlTestDatabase catalogDatabase, PostgreSqlTestDatabase adminDatabase)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = catalogDatabase.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = adminDatabase.ConnectionString
        });
        return builder;
    }
}
