using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Scheduling;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Periodically reclaims old, never-referenced logos in bounded PostgreSQL batches.</summary>
internal sealed class BrandingLogoReclamationHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<BrandingLogoReclamationHostedService> logger)
    : PollingBackgroundService(logger, "branding-logo-reclamation", TimeSpan.FromHours(1))
{
    private const int BatchSize = 128;
    private static readonly TimeSpan OrphanGracePeriod = TimeSpan.FromDays(1);

    /// <inheritdoc />
    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using var _ = scope.ConfigureAwait(false);
        var context = scope.ServiceProvider.GetRequiredService<BrandingDbContext>();
        var cutoff = timeProvider.GetUtcNow() - OrphanGracePeriod;
        return await PostgreSqlBrandingLogos.Reclaim(context, cutoff, BatchSize, stoppingToken).ConfigureAwait(false);
    }
}
