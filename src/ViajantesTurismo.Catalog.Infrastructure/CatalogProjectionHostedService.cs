using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Scheduling;
using ViajantesTurismo.Catalog.Application.Projections;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Polls every five seconds and runs the Catalog projections in a fresh scope.
/// </summary>
/// <param name="scopeFactory">Creates the scope each batch runs in.</param>
/// <param name="logger">Logs the polling service.</param>
internal sealed class CatalogProjectionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CatalogProjectionHostedService> logger)
    : PollingBackgroundService(logger, CatalogProjectionServiceName, TimeSpan.FromSeconds(5))
{
    private const string CatalogProjectionServiceName = "catalog-projections";

    /// <summary>Runs one projection batch and returns the number of items processed.</summary>
    internal ValueTask<int> RunBatch(CancellationToken stoppingToken) => ExecuteBatch(stoppingToken);

    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<CatalogProjectionRunner>();
        return await runner.Project(stoppingToken).ConfigureAwait(false);
    }
}
