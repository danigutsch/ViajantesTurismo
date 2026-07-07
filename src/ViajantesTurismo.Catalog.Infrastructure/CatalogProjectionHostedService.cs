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
    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<CatalogProjectionRunner>();
        await runner.Project(stoppingToken).ConfigureAwait(false);

        return 0;
    }
}
