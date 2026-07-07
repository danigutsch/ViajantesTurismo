using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Scheduling;
using ViajantesTurismo.Catalog.Application.Projections;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class CatalogProjectionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CatalogProjectionHostedService> logger)
    : PollingBackgroundService(logger, "catalog-projections", TimeSpan.FromSeconds(5))
{
    internal ValueTask<int> RunBatch(CancellationToken stoppingToken) => ExecuteBatch(stoppingToken);

    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<CatalogProjectionRunner>();
        return await runner.Project(stoppingToken).ConfigureAwait(false);
    }
}
