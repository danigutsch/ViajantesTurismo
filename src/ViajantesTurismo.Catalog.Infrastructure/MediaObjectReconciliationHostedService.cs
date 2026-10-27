using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Scheduling;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Periodically reconciles stored media objects with catalog records and deletes orphans older than the grace period.</summary>
internal sealed class MediaObjectReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<MediaObjectReconciliationHostedService> logger)
    : PollingBackgroundService(logger, MediaObjectReconciliationServiceName, TimeSpan.FromMinutes(5))
{
    private const string MediaObjectReconciliationServiceName = "catalog-media-object-reconciliation";
    private static readonly TimeSpan OrphanGracePeriod = TimeSpan.FromDays(1);

    /// <summary>Runs one reconciliation batch and returns the number of orphan objects deleted.</summary>
    internal ValueTask<int> RunBatch(CancellationToken stoppingToken) => ExecuteBatch(stoppingToken);

    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<MediaObjectReconciliationService>();
        var report = await service.Reconcile(deleteOrphans: true, OrphanGracePeriod, stoppingToken).ConfigureAwait(false);

        return report.DeletedOrphanObjectKeys.Count;
    }
}
