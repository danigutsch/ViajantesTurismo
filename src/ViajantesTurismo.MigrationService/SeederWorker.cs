using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Tools;

namespace ViajantesTurismo.MigrationService;

internal sealed class SeederWorker(IServiceScopeFactory scopeFactory, ILogger<SeederWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.SeedingStarted();

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = new AdminContextSeeder(dbContext);
        await seeder.Seed(stoppingToken);

        logger.SeedingCompleted();
    }
}

internal static partial class SeederWorkerLogger
{
    [LoggerMessage(1, LogLevel.Information, "Starting database seeding...")]
    public static partial void SeedingStarted(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Database seeding completed.")]
    public static partial void SeedingCompleted(this ILogger logger);
}
