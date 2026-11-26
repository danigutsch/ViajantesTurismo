using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SharedKernel.MalwareScanning.ClamAv;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Owns one real Catalog host with the local outbox polling service enabled.</summary>
internal sealed class CatalogGalleryRelayHost(IHost host) : IAsyncDisposable
{
    /// <summary>Gets the registered inbox lease duration for the fixture clock.</summary>
    internal TimeSpan InboxLeaseDuration => host.Services.GetRequiredService<IOptions<IntegrationEventOptions>>().Value.IdempotencyLockDuration;

    /// <summary>Starts the ordinary Catalog infrastructure and hosted relay over the existing test database.</summary>
    internal static async Task<CatalogGalleryRelayHost> Start(string connectionString, DateTimeOffset now, CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = connectionString,
            [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
        });
        builder.AddCatalogInfrastructure(addOutboxRelay: true);
        builder.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        var host = builder.Build();
        try
        {
            await host.StartAsync(ct);
            return new CatalogGalleryRelayHost(host);
        }
        catch
        {
            await DisposeHost(host);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await host.StopAsync();
        }
        finally
        {
            await DisposeHost(host);
        }
    }

    private static async ValueTask DisposeHost(IHost host)
    {
        if (host is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
            return;
        }

        host.Dispose();
    }
}
